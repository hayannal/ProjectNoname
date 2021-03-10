using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSpaceObjectIndicatorCanvas : ObjectIndicatorCanvas
{
	public GameObject buttonRootObject;
	public Text[] buttonTextList;
	public string[] buttonStringIdList;

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

		alarmRootTransform.gameObject.SetActive(false);
	}

	TimeSpaceObject.eTimeSpaceObjectType _timeSpaceObjectType;
	public void SetType(TimeSpaceObject.eTimeSpaceObjectType timeSpaceObjectType)
	{
		string stringId = "GameUI_AutoEquip";
		int index = (int)timeSpaceObjectType;
		if (index < buttonStringIdList.Length)
			stringId = buttonStringIdList[index];
		for (int i = 0; i < buttonTextList.Length; ++i)
			buttonTextList[i].SetLocalizedText(UIString.instance.GetString(stringId));

		_timeSpaceObjectType = timeSpaceObjectType;

		AlarmObject.Hide(alarmRootTransform);
		if (timeSpaceObjectType == TimeSpaceObject.eTimeSpaceObjectType.Reconstruct && EventManager.instance.reservedOpenReconstructEvent)
			AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void OnClickButton()
	{
		switch (_timeSpaceObjectType)
		{
			case TimeSpaceObject.eTimeSpaceObjectType.AutoEquip:
				TimeSpaceData.instance.AutoEquip();
				break;
			case TimeSpaceObject.eTimeSpaceObjectType.Sell:
				UIInstanceManager.instance.ShowCanvasAsync("EquipSellCanvas", null);
				break;
			case TimeSpaceObject.eTimeSpaceObjectType.Reconstruct:
				UIInstanceManager.instance.ShowCanvasAsync("EquipReconstructCanvas", null);

				if (EventManager.instance.reservedOpenReconstructEvent)
					AlarmObject.Hide(alarmRootTransform);
				break;
		}
	}

	public bool IsShowAlarmObject()
	{
		if (alarmRootTransform.childCount > 0 && alarmRootTransform.GetChild(0).gameObject.activeSelf)
			return true;
		return false;
	}
}