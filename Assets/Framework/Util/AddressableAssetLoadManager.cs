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
public class AsyncOperationResult
{
	public AsyncOperationHandle<GameObject> handle;
	public string category;

	public bool IsDone
	{
		get
		{
			if (handle.IsValid() == false)
				return false;
			return handle.IsDone;
		}
	}

	public GameObject Result
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
			Addressables.Release<GameObject>(handle);
	}
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

	Dictionary<string, AsyncOperationResult> _dicAddressableAsset = new Dictionary<string, AsyncOperationResult>();
	Dictionary<string, int> _dicCategoryCount = new Dictionary<string, int>();

	public static AsyncOperationResult GetAddressableAsset(string address, string category = "")
	{
		return instance.InternalGetAddressableAsset(address, category);
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
	AsyncOperationResult InternalGetAddressableAsset(string address, string category = "")
	{
		if (_dicAddressableAsset.ContainsKey(address))
			return _dicAddressableAsset[address];

		AsyncOperationResult asyncOperationResult = new AsyncOperationResult();
		asyncOperationResult.handle = Addressables.LoadAssetAsync<GameObject>(address);
		asyncOperationResult.category = category;
		_dicAddressableAsset.Add(address, asyncOperationResult);
		return asyncOperationResult;
	}

	void InternalReleaseAll()
	{
		Dictionary<string, AsyncOperationResult>.Enumerator e = _dicAddressableAsset.GetEnumerator();
		while (e.MoveNext())
			e.Current.Value.SafeRelease();
		_dicAddressableAsset.Clear();
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
}
