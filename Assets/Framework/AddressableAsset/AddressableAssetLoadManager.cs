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
	public string category;
	public System.Action<T> callback;

	public bool IsDone
	{
		get
		{
			if (handle.IsValid() == false)
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
			// 사실 여기서 IsDone이 안되어있으면 문제긴 한데 이 콜백은 동시호출도 아니고 천천히 로딩되면 받는거 위주로 쓸거기 때문에 중복호출에 대한 처리까지는 하지 않는다.
			if (callback != null && _dicAddressableGameObject[address].IsDone)
				callback(_dicAddressableGameObject[address].Result);
			return _dicAddressableGameObject[address];
		}

		AsyncOperationGameObjectResult asyncOperationResult = new AsyncOperationGameObjectResult();
		asyncOperationResult.handle = Addressables.LoadAssetAsync<GameObject>(address);
		asyncOperationResult.category = category;
		asyncOperationResult.callback = callback;
		_dicAddressableGameObject.Add(address, asyncOperationResult);
		if (callback != null)
			_listAddressableGameObjectForCallback.Add(asyncOperationResult);
		return asyncOperationResult;
	}

	AsyncOperationSpriteResult InternalGetAddressableSprite(string address, string category = "", System.Action<Sprite> callback = null)
	{
		if (_dicAddressableSprite.ContainsKey(address))
		{
			if (callback != null && _dicAddressableSprite[address].IsDone)
				callback(_dicAddressableSprite[address].Result);
			return _dicAddressableSprite[address];
		}

		AsyncOperationSpriteResult asyncOperationResult = new AsyncOperationSpriteResult();
		asyncOperationResult.handle = Addressables.LoadAssetAsync<Sprite>(address);
		asyncOperationResult.category = category;
		asyncOperationResult.callback = callback;
		_dicAddressableSprite.Add(address, asyncOperationResult);
		if (callback != null)
			_listAddressableSpriteForCallback.Add(asyncOperationResult);
		return asyncOperationResult;
	}

	void InternalReleaseAll()
	{
		_listAddressableGameObjectForCallback.Clear();
		_listAddressableSpriteForCallback.Clear();
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
}
