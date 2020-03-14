using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TreasureChestIndicatorCanvas : ObjectIndicatorCanvas
{
	enum eButtonType
	{
		Shop,
		DailyBox,
		AttendanceBox,
	}
	eButtonType _buttonType;

	public Text[] buttonTextList;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		InitializeTarget(targetTransform);
		RefreshButtonText();
	}

	void RefreshButtonText()
	{
		_buttonType = eButtonType.Shop;

		if (IsDailyBoxType())
			_buttonType = eButtonType.DailyBox;
		else if (IsAttendanceBoxType())
			_buttonType = eButtonType.AttendanceBox;

		string stringId = "";
		switch (_buttonType)
		{
			case eButtonType.Shop:
				stringId = "GameUI_Shop";
				break;
			case eButtonType.DailyBox:
				//if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.SecondDailyBox) && IsClear)
				//	stringId = "GameUI_Open";
				//else
					stringId = "GameUI_OneCharBox";
				break;
			case eButtonType.AttendanceBox:
				stringId = "GameUI_AttendanceBox";
				break;
		}
		for (int i = 0; i < buttonTextList.Length; ++i)
			buttonTextList[i].SetLocalizedText(UIString.instance.GetString(stringId));
	}

	public static bool IsDailyBoxType()
	{
		if (PlayerData.instance.sharedDailyBoxOpened == false && PlayerData.instance.sealCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("SealMaxCount"))
			return true;
		return false;
	}

	public static bool IsAttendanceBoxType()
	{
		return false;
	}

	public static bool IsSpecialTreasureChest()
	{
		if (IsDailyBoxType())
			return true;
		if (IsAttendanceBoxType())
			return true;
		return false;
	}

	public void OnClickButton()
	{
		if (ContentsManager.IsTutorialChapter())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterTraining"), 2.0f);
			return;
		}

		switch (_buttonType)
		{
			case eButtonType.Shop:
				break;
			case eButtonType.DailyBox:
				OnClickOpenDailyBox();
				return;
			case eButtonType.AttendanceBox:
				OnClickOpenAttendanceBox();
				return;
		}

		Debug.Log("Open shop");
	}

	public void OnClickOpenDailyBox()
	{
		PlayFabApiManager.instance.RequestOpenDailyBox((serverFailure) =>
		{
			if (serverFailure)
			{
				// 뭔가 잘못된건데 응답을 할 필요가 있을까.
			}
			else
			{
				// 뭔가 연출 및 보상 처리.
				// 이건 나중에 템이랑 만들어지면 한다.

				// 연출 다하고 나서는 UI도 갱신
				RefreshButtonText();
				DailyBoxGaugeCanvas.instance.RefreshGauge();
			}
		});
	}

	public void OnClickOpenAttendanceBox()
	{

	}
}
