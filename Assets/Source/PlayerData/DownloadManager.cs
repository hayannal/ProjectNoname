using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class DownloadManager : MonoBehaviour
{
	public static DownloadManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("DownloadManager")).AddComponent<DownloadManager>();
				//DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static DownloadManager _instance = null;

	#region Helper
	public static bool AddressableResourceExists(object key, System.Type type)
	{
		foreach (var l in Addressables.ResourceLocators)
		{
			IList<IResourceLocation> locs;
			if (l.Locate(key, type, out locs))
				return true;
		}
		return false;
	}
	#endregion


	// 앱 시작시에 물어보는 번들리소스 체크 프로세스
	public void CheckDownloadProcess()
	{
		StartCoroutine(GetDownloadSizeCoroutine());
	}

	IEnumerator GetDownloadSizeCoroutine()
	{
		// 앱이 켜질때 Addressables.InitializeAsync는 이미 수행했으니 중복호출 하지 않게 하기 위해서 ResourceLocators로부터 정보를 얻어오기로 한다.
		// 어드레서블 항목에 Label 설정없이 전체를 얻어오려면 이 방법으로 체크해야한다.
		IEnumerable<object> keys = null;
		foreach (var locator in Addressables.ResourceLocators)
		{
			if (locator is ResourceLocationMap)
			{
				ResourceLocationMap map = locator as ResourceLocationMap;
				keys = map.Keys;
				break;
			}
		}

		// fastest 모드일때는 null이 나온다. 이러면 그냥 로그인을 진행하면 된다.
		if (keys == null)
		{
			PlayFabApiManager.instance.OnLogin();
			yield break;
		}

		//int keyCount = 0;
		//foreach (var key in keys)
		//{
		//	++keyCount;
		//}
		AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(keys);
		yield return handle;
		long totalDownloadSize = handle.Result;
		Addressables.Release<long>(handle);
		Debug.LogFormat("Total Download Size = {0}", totalDownloadSize / 1024);

		// 하나라도 받을게 있다면 유저에게 확인창을 띄우고 다운로드를 표시해야한다.
		if (totalDownloadSize == 0)
		{
			// 받을게 없다면 하던대로 로그인 진행하면 된다.
			PlayFabApiManager.instance.OnLogin();
			yield break;
		}

		string dataSizeString = "";
		if (totalDownloadSize > 1024 * 1024 / 10)
			dataSizeString = string.Format("{0:0.#}MB", (totalDownloadSize * 1.0f) / (1024 * 1024));
		else
			dataSizeString = string.Format("{0:0.#}KB", (totalDownloadSize * 1.0f) / 1024);
		_totalDownloadString = dataSizeString;

		// 받을게 있다면 캔버스를 띄우고 다운로드 처리를 한다.
		// AuthManager의 RestartProcess에서 가져와 뜯어서 처리한다.
		while (UIString.instance.IsDoneLoadAsyncStringData() == false)
			yield return null;
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;

		// 이땐 로딩 속도를 위해 commonCanvasGroup도 로딩하지 않은 상태라서 직접 로드해서 보여줘야한다.
		_handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");
		//yield return handleCommonCanvasGroup;
		while (_handleCommonCanvasGroup.IsValid() && !_handleCommonCanvasGroup.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleCommonCanvasGroup.Result);
		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_ForceDownloadBeginning", dataSizeString), () =>
		{
			_totalDownloadSize = (totalDownloadSize * 1.0f);
			StartCoroutine(ClearAndDownloadProcess(keys));
		}, 100);
	}

	IEnumerator ClearAndDownloadProcess(IEnumerable<object> keys)
	{
		// 너무 느리니 우선은 꺼둔다.
		//yield return ClearProcess(keys);

		_totalDownloadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);
		_totalDownloadHandle.Completed += DownloadComplete;

		yield return DownloadProgress();
	}

	IEnumerator ClearProcess(IEnumerable<object> keys)
	{
		// 내부적으로는 에셋번들이지만 이걸 한다고 지워지진 않는다.
		//Caching.ClearCache();

		// keys의 문제는 전체를 다 들고있다보니까 그냥 지울수가 없다. 필요없는거만 골라내는 절차가 필요하다.
		// 근데 로직은 맞는데 어드레스 하나씩 확인하다보니 너무 느리다. 다른 방법이 없나.
		List<string> listClearKey = new List<string>();
		foreach (var key in keys)
		{
			AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(key);
			yield return handle;
			if (handle.Result > 0 && AddressableResourceExists(key, typeof(GameObject)))
			{
				listClearKey.Add(key.ToString());
				//Debug.LogFormat("total clear key count = {0} / add key name = {1}", listClearKey.Count, key.ToString());
			}
			Addressables.Release<long>(handle);
		}

		Debug.LogFormat("Clear Key Count : {0}", listClearKey.Count);
		if (listClearKey.Count == 0)
			yield break;

		AsyncOperationHandle<bool> clearHandle = Addressables.ClearDependencyCacheAsync(listClearKey, false);
		yield return clearHandle;
		Addressables.Release<bool>(clearHandle);
	}

	AsyncOperationHandle _totalDownloadHandle;
	float _totalDownloadSize = 0.0f;
	string _totalDownloadString = "";
	IEnumerator DownloadProgress()
	{
		LoadingCanvas.instance.skipProgressAnimation = true;

		// Calculate progress

		while (!_totalDownloadHandle.IsDone)
		{
			var status = _totalDownloadHandle.GetDownloadStatus();
			float progress = status.TotalBytes != 0 ? (float)status.DownloadedBytes / (float)status.TotalBytes : 0f;
			LoadingCanvas.instance.progressImage.fillAmount = progress;

			string progressString = "";
			if (_totalDownloadSize > 1024 * 1024 / 10)
				progressString = string.Format("{0:0.#} / {1}", (_totalDownloadSize * progress) / (1024 * 1024), _totalDownloadString);
			else
				progressString = string.Format("{0:0.#} / {1}", (_totalDownloadSize * progress) / 1024, _totalDownloadString);
			LoadingCanvas.instance.progressText.text = progressString;

			yield return new WaitForSeconds(.1f);
		}
	}

	AsyncOperationHandle<GameObject> _handleCommonCanvasGroup;
	void DownloadComplete(AsyncOperationHandle handle)
	{
		StopCoroutine(DownloadProgress());

		var status = handle.Status;
		if (status == AsyncOperationStatus.Succeeded)
		{
			Addressables.Release(_totalDownloadHandle);
			Addressables.Release<GameObject>(_handleCommonCanvasGroup);
			SceneManager.LoadScene(0);
		}
		else
		{
			Debug.LogFormat("Download failed with reason: {0}", handle.OperationException.Message);
		}
	}




	#region LobbyDownload
	public void CheckLobbyDownloadState()
	{
		// 튜토가 끝나고 다운로드 받을 데이터가 있는지 확인 후 0보다 크다면 로비에서 다운받는 절차를 진행하는 모드를 시작하기 위해 플래그를 걸어둔다.
		StartCoroutine(CheckLobbyDownloadCoroutine());
	}

	IEnumerable<object> _keys = null;
	IEnumerator CheckLobbyDownloadCoroutine()
	{
		IEnumerable<object> keys = null;
		foreach (var locator in Addressables.ResourceLocators)
		{
			if (locator is ResourceLocationMap)
			{
				ResourceLocationMap map = locator as ResourceLocationMap;
				keys = map.Keys;
				break;
			}
		}

		if (keys == null)
			yield break;

		AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(keys);
		yield return handle;
		long totalDownloadSize = handle.Result;
		Addressables.Release<long>(handle);

		// 하나라도 받을게 있다면 저 모드를 켜두고 씬 이동할때 나머지들을 처리하면 된다.
		// 맵을 튜토리얼껄 띄운다거나 게이트필라 동작하지 않게 막는다거나 등등
		if (totalDownloadSize == 0)
			yield break;

		// DownloadManager는 항상 필요한게 아니라서 씬이동시 삭제되기 때문에 PlayerData쪽에 저장해둔다.
		PlayerData.instance.lobbyDownloadState = true;
		PlayerData.instance.lobbyDownloadSize = totalDownloadSize;
		yield break;
	}

	public void ShowLobbyDownloadInfo()
	{
		IEnumerable<object> keys = null;
		foreach (var locator in Addressables.ResourceLocators)
		{
			if (locator is ResourceLocationMap)
			{
				ResourceLocationMap map = locator as ResourceLocationMap;
				keys = map.Keys;
				break;
			}
		}

		long totalDownloadSize = PlayerData.instance.lobbyDownloadSize;
		string dataSizeString = "";
		if (totalDownloadSize > 1024 * 1024 / 10)
			dataSizeString = string.Format("{0:0.#}MB", (totalDownloadSize * 1.0f) / (1024 * 1024));
		else
			dataSizeString = string.Format("{0:0.#}KB", (totalDownloadSize * 1.0f) / 1024);
		_totalDownloadString = dataSizeString;
		_totalDownloadSize = (totalDownloadSize * 1.0f);
		_keys = keys;

		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_ForceDownload", _totalDownloadString), () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("LobbyDownloadCanvas", () =>
			{
				StartCoroutine(ClearAndLobbyDownloadProcess(_keys));
			});
		});
	}

	IEnumerator ClearAndLobbyDownloadProcess(IEnumerable<object> keys)
	{
		// 너무 느리니 우선은 꺼둔다.
		//yield return ClearProcess(keys);

		_totalDownloadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);
		_totalDownloadHandle.Completed += LobbyDownloadComplete;

		yield return LobbyDownloadProgress();
	}

	IEnumerator LobbyDownloadProgress()
	{
		// Calculate progress

		while (!_totalDownloadHandle.IsDone)
		{
			var status = _totalDownloadHandle.GetDownloadStatus();
			float progress = status.TotalBytes != 0 ? (float)status.DownloadedBytes / (float)status.TotalBytes : 0f;
			LobbyDownloadCanvas.instance.progressImage.fillAmount = progress;

			string progressString = "";
			if (_totalDownloadSize > 1024 * 1024 / 10)
				progressString = string.Format("{0:0.#} / {1}", (_totalDownloadSize * progress) / (1024 * 1024), _totalDownloadString);
			else
				progressString = string.Format("{0:0.#} / {1}", (_totalDownloadSize * progress) / 1024, _totalDownloadString);
			LobbyDownloadCanvas.instance.progressText.text = progressString;

			yield return new WaitForSeconds(.1f);
		}
	}

	void LobbyDownloadComplete(AsyncOperationHandle handle)
	{
		StopCoroutine(LobbyDownloadProgress());

		var status = handle.Status;
		if (status == AsyncOperationStatus.Succeeded)
		{
			// 로비에 있으니 메세지박스 하나는 띄워놓고 확인 누르면 씬을 이동시켜줘야한다.
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_LobbyDownloadComplete"), () =>
			{
				PlayerData.instance.lobbyDownloadState = false;
				Addressables.Release(_totalDownloadHandle);
				SceneManager.LoadScene(0);
			});
		}
		else
		{
			Debug.LogFormat("Download failed with reason: {0}", handle.OperationException.Message);
		}
	}
	#endregion
}