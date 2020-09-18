using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaosPurifierIndicatorCanvas : ObjectIndicatorCanvas
{
	public GameObject buttonRootObject;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);
	}

	public void OnClickPurifyButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ChaosPurifierConfirmCanvas", null);
	}
}