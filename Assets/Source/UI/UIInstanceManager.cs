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
}
