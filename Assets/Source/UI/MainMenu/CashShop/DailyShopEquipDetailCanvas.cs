using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DailyShopEquipDetailCanvas : EquipShowCanvasBase
{
	public static DailyShopEquipDetailCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject);

		// 아래 ShowCanvas함수 안에서 SetInfoCameraMode를 호출해주고 있으니 여기선 할필요 없다.
		//SetInfoCameraMode(true);
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		if (EquipInfoGround.instance.diffMode)
			EquipInfoGround.instance.RestoreDiffMode();

		SetInfoCameraMode(false);
	}

	System.Action _okAction;
	public void ShowCanvas(bool show, EquipTableData equipTableData, System.Action okAction)
	{
		_okAction = okAction;

		SetInfoCameraMode(true);

		EquipInfoGround.instance.ChangeDiffMode(equipTableData);
	}

	public void OnClickDetailButton()
	{
		// 현재 보여지고 있는 장착된 템이라서 카메라만 옮겨주면 될거다.
		UIInstanceManager.instance.ShowCanvasAsync("EquipInfoDetailCanvas", null);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();

		if (_okAction != null)
			_okAction();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		LobbyCanvas.Home();
	}
}