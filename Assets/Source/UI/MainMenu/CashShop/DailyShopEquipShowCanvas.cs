using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class DailyShopEquipShowCanvas : EquipShowCanvasBase
{
	public static DailyShopEquipShowCanvas instance;

	public EquipListStatusInfo equipStatusInfo;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		//bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		//if (restore)
		//	return;
	}

	void OnDisable()
	{
		//if (StackCanvas.Pop(gameObject))
		//	return;
		SetInfoCameraMode(false);
	}

	// CharacterBoxShowCanvas와 마찬가지로 StackCanvas와는 독립적으로 가기로 한다.
	System.Action _okAction;
	public void ShowCanvas(EquipData equipData, System.Action okAction)
	{
		_okAction = okAction;

		SetInfoCameraMode(true);

		if (equipData != null)
		{
			EquipInfoGround.instance.CreateEquipObject(equipData, false);
			equipStatusInfo.RefreshInfo(equipData, false);
		}
	}

	public void OnClickConfirmButton()
	{
		// CharacterBoxShowCanvas때와 바로 닫는형태로 처리해둔다.
		gameObject.SetActive(false);

		if (_okAction != null)
			_okAction();
	}
}