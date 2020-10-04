using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// 번들은 한판 끝나고 바로 바로 내리면 로딩이 길어지고
// 그렇다고 하나도 안내리다간 메모리 해제 안되서 꽉차게 되서
// 이걸 유동적으로 조절할 수 있는 매니저 같은게 필요했다.
// 챕터를 옮길때마다 곧바로 다 해제하는 것도 썩 좋은 방법은 아니니
// 일정량 로딩량이 쌓일때 리셋하는 형태로 가본다.
public class AsyncOperationResult<T> where T : Object
{
	public AsyncOperationHandle<T> handle;
	// 콜백이 오면 이 값을 변경해둔다.
	public bool onLoadComplete;
	public string category;
	// handle의 콜백을 받아서 처리하는 형태로 바꾸면서 퍼포먼스도 살리고 데드락도 막기로 하면서
	// 여러개의 콜백을 관리해야할 필요성이 생겼다.
	// 단독으로 가지고 있던걸 리스트로 바꿔서 관리한다. 평소에는 null이고 하나라도 필요할때 new해서 쓰기로 한다.
	//public System.Action<T> callback;
	public List<System.Action<T>> listCallback;

	public bool IsDone
	{
		get
		{
			// yield return handle 없이 매프레임 그냥 검사하는건
			// 로드 프로세스의 일부가 Unity의 메인 쓰레드에서 실행되기 때문에 데드락이 발생할 수 있다고 한다.
			// 그래서 이걸 검사하는건 yield return handle 후에 하거나 혹은 Completed 콜백 후에 해야한다고 한다.
			// 절대 그냥 업데이트문에서 돌리면 안된다!
			// 그렇다고 IsDone자체를 없애자니 모든걸 다 콜백으로만 처리해야해서 구조를 짜기 불편한 점도 있다.
			// 그래서 추가한게 onLoadComplete다! 이건 handle 변수가 아니기때문에 호출되도 문제가 없다.
			if (onLoadComplete == false)
				return false;

			if (handle.IsValid() == false)
				return false;
			if (handle.Status != AsyncOperationStatus.Succeeded)
				return false;
			return handle.IsDone;
		}
	}

	public T Result
	{
		get
		{
			if (IsDone)
				return handle.Result;
			return null;
		}
	}

	public void SafeRelease()
	{
		if (handle.IsValid())
			Addressables.Release<T>(handle);
	}
}

public class AsyncOperationGameObjectResult : AsyncOperationResult<GameObject>
{
}

public class AsyncOperationSpriteResult : AsyncOperationResult<Sprite>
{
}

public class AddressableAssetLoadManager : MonoBehaviour
{
	static AddressableAssetLoadManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("AddressableAssetLoadManager")).AddComponent<AddressableAssetLoadManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static AddressableAssetLoadManager _instance = null;

	Dictionary<string, AsyncOperationGameObjectResult> _dicAddressableGameObject = new Dictionary<string, AsyncOperationGameObjectResult>();
	Dictionary<string, AsyncOperationSpriteResult> _dicAddressableSprite = new Dictionary<string, AsyncOperationSpriteResult>();
	Dictionary<string, int> _dicCategoryCount = new Dictionary<string, int>();

	public static AsyncOperationGameObjectResult GetAddressableGameObject(string address, string category = "", System.Action<GameObject> callback = null)
	{
		return instance.InternalGetAddressableGameObject(address, category, callback);
	}

	public static AsyncOperationSpriteResult GetAddressableSprite(string address, string category = "", System.Action<Sprite> callback = null)
	{
		return instance.InternalGetAddressableSprite(address, category, callback);
	}

	public static void ReleaseAll()
	{
		if (_instance == null)
			return;

		_instance.InternalReleaseAll();
	}

	public static void CheckRelease()
	{
		if (_instance == null)
			return;

		_instance.InternalCheckRelease();
	}

	// 여기서 카테고리는 번들 단위도 아니고, 그냥 게임 내에서 부르는 로직상의 의미다.
	// "Map"이라고 오면 "Map"으로 불리워진 에셋의 전체수량을 기억해놨다가 일정량 이상 높아지면 릴리즈 하는 형태로 간다.
	// 레퍼런스 카운트와 달리 같은걸 호출했다고 +1 하진 않는다.
	AsyncOperationGameObjectResult InternalGetAddressableGameObject(string address, string category = "", System.Action<GameObject> callback = null)
	{
		if (_dicAddressableGameObject.ContainsKey(address))
		{
			if (callback != null)
			{
				if (_dicAddressableGameObject[address].IsDone)
					callback(_dicAddressableGameObject[address].Result);
				else
				{
					if (_dicAddressableGameObject[address].listCallback == null)
						_dicAddressableGameObject[address].listCallback = new List<System.Action<GameObject>>();
					_dicAddressableGameObject[address].listCallback.Add(callback);
				}
			}
			return _dicAddressableGameObject[address];
		}

		// 이렇게 Dictionary에 넣어놔야 같은 프레임에 같은 어드레스를 호출해도 두번 Addressables.LoadAssetAsync호출되면서 레퍼런스 카운트가 증가되는걸 막을 수 있다.
		AsyncOperationGameObjectResult asyncOperationResult = new AsyncOperationGameObjectResult();
		asyncOperationResult.handle = Addressables.LoadAssetAsync<GameObject>(address);
		asyncOperationResult.handle.Completed += OnLoadDone;
		asyncOperationResult.category = category;
		_dicAddressableGameObject.Add(address, asyncOperationResult);
		if (callback != null)
		{
			asyncOperationResult.listCallback = new List<System.Action<GameObject>>();
			asyncOperationResult.listCallback.Add(callback);
		}
		return asyncOperationResult;
	}

	AsyncOperationSpriteResult InternalGetAddressableSprite(string address, string category = "", System.Action<Sprite> callback = null)
	{
		if (_dicAddressableSprite.ContainsKey(address))
		{
			if (callback != null)
			{
				if (_dicAddressableSprite[address].IsDone)
					callback(_dicAddressableSprite[address].Result);
				else
				{
					// 동시호출로 인해 들어올때가 대부분일거다.
					// 가장 중요한 handle은 최초로 호출된 handle을 복사해서 오고 나머지 정보는 동일하게 채운다.
					// dictionary에는 넣지 않고 callback 대기 리스트에만 넣어놨다가 콜백을 처리한다.
					if (_dicAddressableSprite[address].listCallback == null)
						_dicAddressableSprite[address].listCallback = new List<System.Action<Sprite>>();
					_dicAddressableSprite[address].listCallback.Add(callback);
				}
			}
			return _dicAddressableSprite[address];
		}

		AsyncOperationSpriteResult asyncOperationResult = new AsyncOperationSpriteResult();
		asyncOperationResult.handle = Addressables.LoadAssetAsync<Sprite>(address);
		asyncOperationResult.handle.Completed += OnLoadDone;
		asyncOperationResult.category = category;
		_dicAddressableSprite.Add(address, asyncOperationResult);
		if (callback != null)
		{
			asyncOperationResult.listCallback = new List<System.Action<Sprite>>();
			asyncOperationResult.listCallback.Add(callback);
		}
		return asyncOperationResult;
	}

	void OnLoadDone(AsyncOperationHandle<GameObject> handle)
	{
		string address = "";
		AsyncOperationGameObjectResult asyncOperationResult = null;
		Dictionary<string, AsyncOperationGameObjectResult>.Enumerator e = _dicAddressableGameObject.GetEnumerator();
		while (e.MoveNext())
		{
			// hashcode로 검사해도 찾아지긴 한다. handle이 struct라서 직접 비교할 수 없어서 HashCode로 비교하는거다.
			if (e.Current.Value.handle.GetHashCode() == handle.GetHashCode())
			{
				address = e.Current.Key;
				asyncOperationResult = e.Current.Value;
				break;
			}
		}

		if (handle.Status != AsyncOperationStatus.Succeeded)
		{
			// 로딩의 결과가 Succeeded가 아닐때가 난감한데 로딩하려는데 데이터를 못찾은거다. 이게 말이 되나.
			// 이 경우에는 우선 디버그 로그부터 찍어두고
			Debug.LogErrorFormat("AsyncOperationStatus Failed!! Address Name = {0}", address);

			// 프리징은 아닐테지만 로딩을 기다리고 있을테니 다시한번 로드를 호출해봐야하나
			return;
		}

		// 제대로 로드되었다면 플래그를 걸어서 기록해둔다.
		asyncOperationResult.onLoadComplete = true;

		// 등록된 콜백 리스트가 있다면 루프 돌면서 전부 실행해준다. 이후 콜백은 비워둔다.
		if (asyncOperationResult.listCallback != null)
		{
			for (int i = 0; i < asyncOperationResult.listCallback.Count; ++i)
				asyncOperationResult.listCallback[i](handle.Result);
			asyncOperationResult.listCallback.Clear();
		}
	}

	void OnLoadDone(AsyncOperationHandle<Sprite> handle)
	{
		string address = "";
		AsyncOperationSpriteResult asyncOperationResult = null;
		Dictionary<string, AsyncOperationSpriteResult>.Enumerator e = _dicAddressableSprite.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.Value.handle.GetHashCode() == handle.GetHashCode())
			{
				address = e.Current.Key;
				asyncOperationResult = e.Current.Value;
				break;
			}
		}

		if (handle.Status != AsyncOperationStatus.Succeeded)
		{
			Debug.LogErrorFormat("AsyncOperationStatus Failed!! Address Name = {0}", address);
			return;
		}

		asyncOperationResult.onLoadComplete = true;
		if (asyncOperationResult.listCallback != null)
		{
			for (int i = 0; i < asyncOperationResult.listCallback.Count; ++i)
				asyncOperationResult.listCallback[i](handle.Result);
			asyncOperationResult.listCallback.Clear();
		}
	}

	void InternalReleaseAll()
	{
		//_listAddressableGameObjectForCallback.Clear();
		//_listAddressableSpriteForCallback.Clear();
		Dictionary<string, AsyncOperationGameObjectResult>.Enumerator e1 = _dicAddressableGameObject.GetEnumerator();
		while (e1.MoveNext())
			e1.Current.Value.SafeRelease();
		_dicAddressableGameObject.Clear();
		Dictionary<string, AsyncOperationSpriteResult>.Enumerator e2 = _dicAddressableSprite.GetEnumerator();
		while (e2.MoveNext())
			e2.Current.Value.SafeRelease();
		_dicAddressableSprite.Clear();
		_dicCategoryCount.Clear();
	}

	#region Check Release
	void InternalCheckRelease()
	{
		// 지금은 우선 항상 릴리즈로 해둔다.
		// 차후 맵 로딩 10회 후 릴리즈라던가
		// 카테고리 카운트를 세서 일정량이 넘었을때 그룹별로 해제라던가를 하면 될거다.
		InternalReleaseAll();
	}
	#endregion

	// 더이상 사용하지 않는 Callback 구조. 매프레임 IsDone 돌리는건 너무 비효율적이기도 하고 Deadlock 발생할 가능성이 있어서 쓰면 안된다.
	/*
	#region Load Callback
	List<AsyncOperationGameObjectResult> _listAddressableGameObjectForCallback = new List<AsyncOperationGameObjectResult>();
	List<AsyncOperationSpriteResult> _listAddressableSpriteForCallback = new List<AsyncOperationSpriteResult>();
	void Update()
	{
		for (int i = _listAddressableGameObjectForCallback.Count - 1; i >= 0; --i)
		{
			if (_listAddressableGameObjectForCallback[i].IsDone == false)
				continue;
			
			if (_listAddressableGameObjectForCallback[i].callback != null)
				_listAddressableGameObjectForCallback[i].callback(_listAddressableGameObjectForCallback[i].Result);
			_listAddressableGameObjectForCallback.Remove(_listAddressableGameObjectForCallback[i]);
		}
		for (int i = _listAddressableSpriteForCallback.Count - 1; i >= 0; --i)
		{
			if (_listAddressableSpriteForCallback[i].IsDone == false)
				continue;

			if (_listAddressableSpriteForCallback[i].callback != null)
				_listAddressableSpriteForCallback[i].callback(_listAddressableSpriteForCallback[i].Result);
			_listAddressableSpriteForCallback.Remove(_listAddressableSpriteForCallback[i]);
		}
	}
	#endregion
	*/
}
