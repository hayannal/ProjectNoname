using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class FloatingDamageTextRootCanvas : MonoBehaviour
{
	public static FloatingDamageTextRootCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.floatingDamageTextRootCanvasPrefab).GetComponent<FloatingDamageTextRootCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static FloatingDamageTextRootCanvas _instance = null;

	public GameObject[] floatingDamageTextPrefabList;
	public Vector3[] positionAnimationTargetList;


	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	public void ShowText(FloatingDamageText.eFloatingDamageType floatingDamageType, Actor actor)
	{
		// position ani
		int index = FloatingDamageTextRootCanvas.instance.GetPositionAnimationIndex(actor);
		index = index % floatingDamageTextPrefabList.Length;

		FloatingDamageText floatingDamageText = GetCachedFloatingDamageText(floatingDamageTextPrefabList[index]);
		floatingDamageText.InitializeText(floatingDamageType, actor, index);
	}

	Dictionary<GameObject, List<FloatingDamageText>> _dicFloatingDamageTextInstancePool = new Dictionary<GameObject, List<FloatingDamageText>>();
	FloatingDamageText GetCachedFloatingDamageText(GameObject prefab)
	{
		List<FloatingDamageText> listCachedFloatingDamageText = null;
		if (_dicFloatingDamageTextInstancePool.ContainsKey(prefab))
			listCachedFloatingDamageText = _dicFloatingDamageTextInstancePool[prefab];
		else
		{
			listCachedFloatingDamageText = new List<FloatingDamageText>();
			_dicFloatingDamageTextInstancePool.Add(prefab, listCachedFloatingDamageText);
		}

		for (int i = 0; i < listCachedFloatingDamageText.Count; ++i)
		{
			if (!listCachedFloatingDamageText[i].gameObject.activeSelf)
			{
				listCachedFloatingDamageText[i].gameObject.SetActive(true);
				return listCachedFloatingDamageText[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, cachedTransform);
		FloatingDamageText floatingDamageText = newObject.GetComponent<FloatingDamageText>();
		listCachedFloatingDamageText.Add(floatingDamageText);
		return floatingDamageText;
	}

	const float ContinuousDelay = 1.5f;
	Dictionary<Actor, float> _dicLastTime = new Dictionary<Actor, float>();
	Dictionary<Actor, int> _dicLastIndex = new Dictionary<Actor, int>();
	int GetPositionAnimationIndex(Actor actor)
	{
		if (_dicLastTime.ContainsKey(actor) == false)
		{
			_dicLastTime.Add(actor, Time.time);
			_dicLastIndex.Add(actor, 0);
			return 0;
		}

		float lastTime = _dicLastTime[actor];
		int lastIndex = _dicLastIndex[actor];
		if (Time.time > lastTime + ContinuousDelay)
			lastIndex = 0;
		else
			++lastIndex;
		_dicLastTime[actor] = Time.time;
		_dicLastIndex[actor] = lastIndex;
		return lastIndex;
	}



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}