using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	Dictionary<GameObject, List<ObjectIndicatorCanvas>> _dicInstancePool = new Dictionary<GameObject, List<ObjectIndicatorCanvas>>();
	public ObjectIndicatorCanvas GetCachedObjectIndicatorCanvas(GameObject prefab, Transform parentTransform = null)
	{
		List<ObjectIndicatorCanvas> listCachedObjectIndicatorCanvas = null;
		if (_dicInstancePool.ContainsKey(prefab))
			listCachedObjectIndicatorCanvas = _dicInstancePool[prefab];
		else
		{
			listCachedObjectIndicatorCanvas = new List<ObjectIndicatorCanvas>();
			_dicInstancePool.Add(prefab, listCachedObjectIndicatorCanvas);
		}

		for (int i = 0; i < listCachedObjectIndicatorCanvas.Count; ++i)
		{
			if (!listCachedObjectIndicatorCanvas[i].gameObject.activeSelf)
			{
				listCachedObjectIndicatorCanvas[i].transform.parent = parentTransform;
				listCachedObjectIndicatorCanvas[i].gameObject.SetActive(true);
				return listCachedObjectIndicatorCanvas[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, parentTransform);
		ObjectIndicatorCanvas objectIndicatorCanvas = newObject.GetComponent<ObjectIndicatorCanvas>();
		listCachedObjectIndicatorCanvas.Add(objectIndicatorCanvas);
		return objectIndicatorCanvas;
	}
}
