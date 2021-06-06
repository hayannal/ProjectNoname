using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventBoardIndicatorCanvas : ObjectIndicatorCanvas
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

	public void OnClickEventButton()
	{
		if (CumulativeEventData.instance.GetActiveEventCount() == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_NoActiveEvents"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("CumulativeEventCanvas", null);
	}
}