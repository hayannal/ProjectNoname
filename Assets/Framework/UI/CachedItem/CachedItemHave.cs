using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CachedItemHave<T> where T : MonoBehaviour
{
	List<T> _listCachedItem = new List<T>();

	public T GetCachedItem(GameObject cachedObjectPrefab, Transform parentTransform)
	{
		for (int i = 0; i < _listCachedItem.Count; ++i)
		{
			if (!_listCachedItem[i].gameObject.activeSelf)
				return _listCachedItem[i];
		}

		GameObject newObject = Object.Instantiate(cachedObjectPrefab, parentTransform);
		T newItem = newObject.GetComponent<T>();
		_listCachedItem.Add(newItem);
		return newItem;
	}
}
