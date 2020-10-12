using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class CharacterInfoTranscendCanvas : MonoBehaviour
{
	public static CharacterInfoTranscendCanvas instance;

	public Transform transcendTextTransform;

	public GameObject[] fillImageObjectList;
	public GameObject[] tweenAnimationObjectList;
	public GameObject[] subTweenAnimationObjectList;

	public GameObject maxInfoObject;
	public GameObject materialInfoObject;

	public GameObject emptySlotObject;
	public GameObject characterSlotObject;
	public Image characterImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;
	public Text nameText;

	public Transform needOriginTextTransform;
	public Text needOriginCountText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	public RectTransform alarmRootTransform;

	public Text rewardText;

	void Awake()
	{
		instance = this;
	}

	bool _started = false;
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_started = true;
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	string _actorId;
	CharacterData _characterData;
	public void RefreshInfo()
	{
		AlarmObject.Hide(alarmRootTransform);

		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		_actorId = actorId;
		_characterData = characterData;

		bool appliedTranscendPoint = false;
		for (int i = 0; i < fillImageObjectList.Length; ++i)
		{
			if (characterData.transcendLevel > i)
			{
				fillImageObjectList[i].gameObject.SetActive(true);
				tweenAnimationObjectList[i].gameObject.SetActive(false);
				subTweenAnimationObjectList[i].gameObject.SetActive(false);
			}
			else
			{
				if (characterData.transcendPoint > i && appliedTranscendPoint == false)
				{
					fillImageObjectList[i].gameObject.SetActive(false);
					tweenAnimationObjectList[i].gameObject.SetActive(true);
					subTweenAnimationObjectList[i].gameObject.SetActive(true);
					appliedTranscendPoint = true;
				}
				else
				{
					fillImageObjectList[i].gameObject.SetActive(false);
					tweenAnimationObjectList[i].gameObject.SetActive(false);
					subTweenAnimationObjectList[i].gameObject.SetActive(false);
				}
			}
		}

		if (characterData.transcendLevel >= CharacterData.TranscendMax)
		{
			maxInfoObject.SetActive(true);
			materialInfoObject.SetActive(false);

			priceButtonObject.SetActive(false);
			maxButtonObject.SetActive(true);
			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			rewardText.text = "";
			return;
		}

		maxInfoObject.SetActive(false);
		materialInfoObject.SetActive(true);
		emptySlotObject.SetActive(!appliedTranscendPoint);
		characterSlotObject.SetActive(appliedTranscendPoint);

		// SwapCanvasListItem에서 필요한 것만 가져와서 사용한다.
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		switch (actorTableData.grade)
		{
			case 0:
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				gradient.color1 = new Color(0.0f, 0.7f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 2:
				gradient.color1 = new Color(1.0f, 0.5f, 0.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}

		needOriginCountText.text = UIString.instance.GetString("GameUI_SpacedFraction", characterData.transcendPoint - characterData.transcendLevel, 1);

		int price = 0;
		switch (characterData.transcendLevel)
		{
			case 0:
				price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TranscendGoldOne");
				rewardText.SetLocalizedText(UIString.instance.GetString("GameUI_TranscendenceOneMenuOpen"));
				break;
			case 1:
				price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TranscendGoldTwo");
				rewardText.SetLocalizedText(UIString.instance.GetString("GameUI_TranscendenceTwoMenuOpen"));
				break;
			case 2:
				price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TranscendGoldThree");
				rewardText.SetLocalizedText(UIString.instance.GetString("GameUI_TranscendenceThreeMenuOpen"));
				break;
		}
		bool disablePrice = (CurrencyData.instance.gold < price || appliedTranscendPoint == false);

		priceText.text = price.ToString("N0");
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;
		priceButtonObject.SetActive(true);
		maxButtonObject.SetActive(false);
		_price = price;

		if (appliedTranscendPoint)
			AlarmObject.Show(alarmRootTransform);
	}
	#endregion
	
	public void OnClickTranscendTextButton()
	{
		string text = UIString.instance.GetString("GameUI_TranscendenceMore");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, transcendTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickNeedOriginTextButton()
	{
		string text = UIString.instance.GetString("GameUI_TranscendenceMaterialMore");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, needOriginTextTransform, new Vector2(10.0f, -30.0f));
	}

	int _price = 0;
	public void OnClickTranscendButton()
	{
		if (_characterData == null)
			return;

		if (_characterData.transcendLevel >= CharacterData.TranscendMax)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxTranscendenceToast"), 2.0f);
			return;
		}

		if (_characterData.transcendPoint <= _characterData.transcendLevel)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TranscendenceRequireOrigin"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_TranscendenceConfirm"), CurrencyData.eCurrencyType.Gold, _price, false, () =>
			{
				PlayFabApiManager.instance.RequestCharacterTranscend(_characterData, _price, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					OnRecvTranscend();
				});
			});
		});
	}

	void OnRecvTranscend()
	{
		CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
		//Timing.RunCoroutine(TrainingProcess(addTrainingPoint));
		RefreshInfo();
		CharacterInfoCanvas.instance.RefreshOpenMenuSlot(_characterData.transcendLevel);


		// Grid 갱신
		CharacterListCanvas.instance.RefreshGrid(false);

		// 알람 Refresh
		CharacterInfoCanvas.instance.RefreshAlarmObjectList();
		CharacterListCanvas.instance.RefreshAlarmList();
		DotMainMenuCanvas.instance.RefreshCharacterAlarmObject();
	}
}