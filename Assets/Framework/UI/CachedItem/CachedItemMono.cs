using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CachedItemMono<T> : MonoBehaviour where T : MonoBehaviour
{
	List<T> _listCachedItem = new List<T>();

	public GameObject cachedObjectPrefab;

	public T GetCachedItem(Transform parentTransform)
	{
		for (int i = 0; i < _listCachedItem.Count; ++i)
		{
			if (!_listCachedItem[i].gameObject.activeSelf)
				return _listCachedItem[i];
		}

		GameObject newObject = Instantiate(cachedObjectPrefab, parentTransform);
		T newItem = newObject.GetComponent<T>();
		_listCachedItem.Add(newItem);
		return newItem;
	}
}
