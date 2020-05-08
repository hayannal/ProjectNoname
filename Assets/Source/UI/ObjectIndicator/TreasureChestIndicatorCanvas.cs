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

	public GameObject buttonRootObject;
	public Text[] buttonTextList;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
		RefreshButtonText();
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);
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
				//if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.SecondDailyBox) && IsClearSecondDaily)
				//	stringId = "GameUI_ThreeCharBox";
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
		if (GatePillar.instance.processing)
			return;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return;

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
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		// 가장 핵심은 드랍부터 굴려서 보상정보를 얻어오는거다.
		DropProcessor dropProcessor = DropProcessor.Drop(targetTransform, "Zoflr", "", true, true);
		dropProcessor.AdjustDropRange(3.7f);
		if (CheatingListener.detectedCheatTable)
			return;
		PlayFabApiManager.instance.RequestOpenDailyBox((serverFailure) =>
		{
			if (serverFailure)
			{
				// 뭔가 잘못된건데 응답을 할 필요가 있을까.
			}
			else
			{
				// 연출 및 보상 처리.

				// TreasureChest는 숨겨도 하단 일퀘 갱신은 즉시 보여준다.
				DailyBoxGaugeCanvas.instance.RefreshGauge();

				UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
				{
					RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Origin, dropProcessor, 0, () =>
					{
						CharacterBoxConfirmCanvas.OnCompleteRandomBoxScreen(DropManager.instance.GetGrantCharacterInfo(), DropManager.instance.GetLimitBreakPointInfo(), CharacterBoxConfirmCanvas.OnResult);
					});
				});
			}
		});
	}

	public void OnClickOpenAttendanceBox()
	{

	}
}
