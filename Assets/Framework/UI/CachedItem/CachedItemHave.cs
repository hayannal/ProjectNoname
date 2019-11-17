using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class CachedItemHave<T> where T : MonoBehaviour
{
	List<T> _listCachedItem = new List<T>();

	public T GetCachedItem(GameObject cachedObjectPrefab, Transform parentTransform)
	{
		for (int i = 0; i < _listCachedItem.Count; ++i)
		{
			if (!_listCachedItem[i].gameObject.activeSelf)
			{
				_listCachedItem[i].gameObject.SetActive(true);
				return _listCachedItem[i];
			}
		}

		GameObject newObject = Object.Instantiate(cachedObjectPrefab, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		T newItem = newObject.GetComponent<T>();
		_listCachedItem.Add(newItem);
		return newItem;
	}
}
