using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarBoostIndicatorCanvas : ObjectIndicatorCanvas
{
	public GameObject buttonRootObject;
	public RectTransform alarmRootTransform;

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

	public void OnClickBoostButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("NodeWarBoostInfoCanvas", null);
	}
}