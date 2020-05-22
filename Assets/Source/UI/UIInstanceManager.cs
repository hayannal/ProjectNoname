using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class UIInstanceManager : MonoBehaviour
{
	public static UIInstanceManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("UIInstanceManager")).AddComponent<UIInstanceManager>();
			return _instance;
		}
	}
	static UIInstanceManager _instance = null;

	void Update()
	{
		UpdateAsyncOperation();
	}

	#region Camera
	Camera _cachedCameraMain = null;
	public Camera GetCachedCameraMain()
	{
		if (_cachedCameraMain == null)
			_cachedCameraMain = Camera.main;
		return _cachedCameraMain;
	}
	#endregion

	Dictionary<GameObject, List<ObjectIndicatorCanvas>> _dicObjectIndicatorInstancePool = new Dictionary<GameObject, List<ObjectIndicatorCanvas>>();
	public ObjectIndicatorCanvas GetCachedObjectIndicatorCanvas(GameObject prefab)
	{
		List<ObjectIndicatorCanvas> listCachedObjectIndicatorCanvas = null;
		if (_dicObjectIndicatorInstancePool.ContainsKey(prefab))
			listCachedObjectIndicatorCanvas = _dicObjectIndicatorInstancePool[prefab];
		else
		{
			listCachedObjectIndicatorCanvas = new List<ObjectIndicatorCanvas>();
			_dicObjectIndicatorInstancePool.Add(prefab, listCachedObjectIndicatorCanvas);
		}

		for (int i = 0; i < listCachedObjectIndicatorCanvas.Count; ++i)
		{
			if (!listCachedObjectIndicatorCanvas[i].gameObject.activeSelf)
			{
				listCachedObjectIndicatorCanvas[i].gameObject.SetActive(true);
				return listCachedObjectIndicatorCanvas[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		ObjectIndicatorCanvas objectIndicatorCanvas = newObject.GetComponent<ObjectIndicatorCanvas>();
		listCachedObjectIndicatorCanvas.Add(objectIndicatorCanvas);
		return objectIndicatorCanvas;
	}

	#region Monster Gauge
	MonsterHPGaugeRootCanvas _cachedMonsterHPGaugeRootCanvas = null;
	MonsterHPGaugeRootCanvas GetCachedMonsterHPGaugeRootCanvas()
	{
		if (_cachedMonsterHPGaugeRootCanvas == null)
			_cachedMonsterHPGaugeRootCanvas = Instantiate<GameObject>(BattleManager.instance.monsterHPGaugeRootCanvasPrefab).GetComponent<MonsterHPGaugeRootCanvas>();
		return _cachedMonsterHPGaugeRootCanvas;
	}

	Dictionary<GameObject, List<MonsterHPGauge>> _dicMonsterHPGaugeInstancePool = new Dictionary<GameObject, List<MonsterHPGauge>>();
	public MonsterHPGauge GetCachedMonsterHPgauge(GameObject prefab)
	{
		List<MonsterHPGauge> listCachedMonsterHPGauge = null;
		if (_dicMonsterHPGaugeInstancePool.ContainsKey(prefab))
			listCachedMonsterHPGauge = _dicMonsterHPGaugeInstancePool[prefab];
		else
		{
			listCachedMonsterHPGauge = new List<MonsterHPGauge>();
			_dicMonsterHPGaugeInstancePool.Add(prefab, listCachedMonsterHPGauge);
		}

		for (int i = 0; i < listCachedMonsterHPGauge.Count; ++i)
		{
			if (!listCachedMonsterHPGauge[i].gameObject.activeSelf)
			{
				listCachedMonsterHPGauge[i].gameObject.SetActive(true);
				return listCachedMonsterHPGauge[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, GetCachedMonsterHPGaugeRootCanvas().cachedTransform);
		MonsterHPGauge monsterHPGauge = newObject.GetComponent<MonsterHPGauge>();
		listCachedMonsterHPGauge.Add(monsterHPGauge);
		return monsterHPGauge;
	}
	#endregion

	#region Portal Gauge
	Dictionary<GameObject, List<PortalGauge>> _dicPortalGaugeInstancePool = new Dictionary<GameObject, List<PortalGauge>>();
	public PortalGauge GetCachedPortalGauge(GameObject prefab)
	{
		List<PortalGauge> listCachedPortalGauge = null;
		if (_dicPortalGaugeInstancePool.ContainsKey(prefab))
			listCachedPortalGauge = _dicPortalGaugeInstancePool[prefab];
		else
		{
			listCachedPortalGauge = new List<PortalGauge>();
			_dicPortalGaugeInstancePool.Add(prefab, listCachedPortalGauge);
		}

		for (int i = 0; i < listCachedPortalGauge.Count; ++i)
		{
			if (!listCachedPortalGauge[i].gameObject.activeSelf)
			{
				listCachedPortalGauge[i].gameObject.SetActive(true);
				return listCachedPortalGauge[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, GetCachedMonsterHPGaugeRootCanvas().cachedTransform);
		PortalGauge portalGauge = newObject.GetComponent<PortalGauge>();
		listCachedPortalGauge.Add(portalGauge);
		return portalGauge;
	}
	#endregion

	#region Object Pool
	Dictionary<GameObject, List<GameObject>> _dicInstancePool = new Dictionary<GameObject, List<GameObject>>();
	public GameObject GetCachedObject(GameObject prefab, Transform parentTransform)
	{
		List<GameObject> listCachedGameObject = null;
		if (_dicInstancePool.ContainsKey(prefab))
			listCachedGameObject = _dicInstancePool[prefab];
		else
		{
			listCachedGameObject = new List<GameObject>();
			_dicInstancePool.Add(prefab, listCachedGameObject);
		}

		for (int i = 0; i < listCachedGameObject.Count; ++i)
		{
			if (!listCachedGameObject[i].activeSelf)
			{
				listCachedGameObject[i].transform.SetParent(parentTransform);
				listCachedGameObject[i].SetActive(true);
				return listCachedGameObject[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		listCachedGameObject.Add(newObject);
		return newObject;
	}
	#endregion

	#region AlarmObject
	List<AlarmObject> _listCachedAlarmObject = new List<AlarmObject>();
	public AlarmObject GetCachedAlarmObject(Transform parentTransform)
	{
		for (int i = 0; i < _listCachedAlarmObject.Count; ++i)
		{
			if (!_listCachedAlarmObject[i].gameObject.activeSelf)
			{
				_listCachedAlarmObject[i].cachedRectTransform.SetParent(parentTransform);
				_listCachedAlarmObject[i].cachedRectTransform.anchoredPosition3D = Vector3.zero;
				_listCachedAlarmObject[i].cachedRectTransform.anchoredPosition = Vector2.zero;
				_listCachedAlarmObject[i].cachedRectTransform.localRotation = Quaternion.identity;
				_listCachedAlarmObject[i].cachedRectTransform.localScale = Vector3.one;
				_listCachedAlarmObject[i].gameObject.SetActive(true);
				return _listCachedAlarmObject[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(CommonCanvasGroup.instance.alarmObjectPrefab, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		AlarmObject alarmObject = newObject.GetComponent<AlarmObject>();
		_listCachedAlarmObject.Add(alarmObject);
		return alarmObject;
	}
	#endregion



	#region Async Load
	class LoadCanvasAsync
	{
		public string canvasAddress;
		public AsyncOperationGameObjectResult handleCanvasPrefab;
		public System.Action calllback;
		public bool showDelayedLoadingCanvas;
	}
	List<LoadCanvasAsync> _listAsyncOperationResult = new List<LoadCanvasAsync>();
	Dictionary<string, GameObject> _dicCanvasPool = new Dictionary<string, GameObject>();
	// 사실 여러개가 동시에 호출되면 showWaitingNetworkCanvas를 보장하지 않으나 한번에 하나의 창만 로드할땐 보장해준다.
	public void ShowCanvasAsync(string canvasAddress, System.Action callback, bool showDelayedLoadingCanvas = true)
	{
		if (_dicCanvasPool.ContainsKey(canvasAddress))
		{
			_dicCanvasPool[canvasAddress].SetActive(true);
			if (callback != null)
				callback();
			return;
		}

		LoadCanvasAsync loadCanvasAsync = new LoadCanvasAsync();
		loadCanvasAsync.canvasAddress = canvasAddress;
		loadCanvasAsync.handleCanvasPrefab = AddressableAssetLoadManager.GetAddressableGameObject(canvasAddress, "Canvas");
		loadCanvasAsync.calllback = callback;
		loadCanvasAsync.showDelayedLoadingCanvas = showDelayedLoadingCanvas;
		_listAsyncOperationResult.Add(loadCanvasAsync);

		if (showDelayedLoadingCanvas)
			DelayedLoadingCanvas.Show(true);
	}

	void UpdateAsyncOperation()
	{
		bool loadFinish = false;
		bool showDelayedLoadingCanvas = false;
		for (int i = _listAsyncOperationResult.Count - 1; i >= 0; --i)
		{
			if (_listAsyncOperationResult[i].handleCanvasPrefab.IsDone == false)
				continue;

			// 중복호출 했다면 이미 딕셔너리에 들어있을 수 있다.
			if (_dicCanvasPool.ContainsKey(_listAsyncOperationResult[i].canvasAddress))
			{
				_dicCanvasPool[_listAsyncOperationResult[i].canvasAddress].SetActive(true);
			}
			else
			{
				GameObject newObject = Instantiate<GameObject>(_listAsyncOperationResult[i].handleCanvasPrefab.Result);
				_dicCanvasPool.Add(_listAsyncOperationResult[i].canvasAddress, newObject);
			}
			if (_listAsyncOperationResult[i].calllback != null)
				_listAsyncOperationResult[i].calllback();
			showDelayedLoadingCanvas = _listAsyncOperationResult[i].showDelayedLoadingCanvas;
			_listAsyncOperationResult.Remove(_listAsyncOperationResult[i]);
			loadFinish = true;
		}

		if (loadFinish && _listAsyncOperationResult.Count == 0 && showDelayedLoadingCanvas)
			DelayedLoadingCanvas.Show(false);
	}
	#endregion

}
