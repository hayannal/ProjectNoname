using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnScrollPointIndicatorCanvas : ObjectIndicatorCanvas
{
	public static ReturnScrollPointIndicatorCanvas instance;

	public GameObject buttonRootObject;

	void Awake()
	{
		instance = this;
	}

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

	public void OnClickSaveButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ReturnScrollConfirmCanvas", null);
	}
}