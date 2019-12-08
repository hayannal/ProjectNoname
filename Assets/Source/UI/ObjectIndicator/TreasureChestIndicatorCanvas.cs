using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TreasureChestIndicatorCanvas : ObjectIndicatorCanvas
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

	public void OnClickButton()
	{
		Debug.Log("Open shop");
	}
}
