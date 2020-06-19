using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarBoostIndicatorCanvas : ObjectIndicatorCanvas
{
	public static NodeWarBoostIndicatorCanvas instance;

	public GameObject buttonRootObject;
	public RectTransform alarmRootTransform;

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
		RefreshAlarmObject();
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);
		alarmRootTransform.gameObject.SetActive(false);
	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(alarmRootTransform);
		if (PlayerData.instance.nodeWarBoostRemainCount > 0 && PlayerData.instance.nodeWarBoostRemainCount <= 3)
			AlarmObject.Show(alarmRootTransform);
	}

	public void OnClickBoostButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("NodeWarBoostInfoCanvas", null);
	}
}