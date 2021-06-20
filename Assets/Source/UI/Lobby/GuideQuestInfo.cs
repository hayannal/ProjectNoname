using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class GuideQuestInfo : MonoBehaviour
{
	public static GuideQuestInfo instance;

	public GameObject smallButtonRootObject;
	public DOTweenAnimation infoRootTweenAnimation;
	public GameObject smallBackButtonRootObject;

	public GameObject contentsRootObject;
	public Sprite[] iconSpriteList;

	public Image iconImage;
	public Text nameText;
	public Image proceedingCountImage;
	public GameObject proceedingCountTextRootObject;
	public Text proceedingCountText;
	public Text additionalRewardText;

	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public GameObject returnScrollIconObject;
	public GameObject equipBoxObject;
	public GameObject equipBigBoxObject;
	public GameObject characterBoxObject;
	public Text rewardCountText;
	public GameObject specialRewardRootObject;
	public Text specialRewardText;
	public GameObject smallBlinkObject;
	public GameObject blinkObject;
	public GameObject disableTextObject;
	public GameObject needUpdateTextObject;

	public RectTransform alarmRootTransform;
	public RectTransform infoAlarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		// GuideQuestInfo오브젝트는 LobbyCanvas에 붙어있기때문에 씬 구축할때 호출되고 이후 쭉 살아있게 된다.
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState)
			return;

		// 박스 드랍 연출 후 이어지는 OnEnable일거다. 예외처리를 해준다.
		if (_claimReopenFlag)
		{
			_claimReopenRemainTime = 0.1f;
			_claimReopenFlag = false;
			return;
		}

		RefreshSmallButton();
		RefreshInfo();
	}

	// Update is called once per frame
	void Update()
    {
		if (_openRemainTime > 0.0f)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime = 0.0f;
				OnClickSmallBackButton();
			}
		}

		if (_closeRemainTime > 0.0f)
		{
			_closeRemainTime -= Time.deltaTime;
			if (_closeRemainTime <= 0.0f)
			{
				_closeRemainTime = 0.0f;
				infoRootTweenAnimation.gameObject.SetActive(false);
				smallButtonRootObject.SetActive(true);
			}
		}

		if (_claimReopenRemainTime > 0.0f)
		{
			// 이상하게 CharacterBoxShowCanvas가 나오고 나서 CharacterBoxResultCanvas가 나올때는 LobbyCanvas가 미리 보이게 된다.
			// 그래서 결과창 뒤에서 퀘스트 알람이 나오게 되길래 이렇게 예외처리 해둔다.
			bool ignore = false;
			if (CharacterBoxResultCanvas.instance != null && CharacterBoxResultCanvas.instance.gameObject.activeSelf)
				ignore = true;

			if (ignore == false)
				_claimReopenRemainTime -= Time.deltaTime;

			if (_claimReopenRemainTime <= 0.0f)
			{
				_claimReopenRemainTime = 0.0f;
				OnClickSmallButton();
			}
		}
	}

	void RefreshSmallButton()
	{
		bool show = false;
		if (GuideQuestData.instance.currentGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
		{
			// 맥스를 넘으면 업데이트를 기다려야한다는 메세지를 띄워야하므로 보여야한다.
			show = true;
		}

		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData != null)
			show = true;

		smallButtonRootObject.SetActive(show);

		if (show)
		{
			infoRootTweenAnimation.gameObject.SetActive(false);
			smallBackButtonRootObject.SetActive(false);
			_openRemainTime = _closeRemainTime = 0.0f;
		}
	}

	bool _complete = false;
	void RefreshInfo()
	{
		if (GuideQuestData.instance.currentGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
		{
			blinkObject.SetActive(false);
			smallBlinkObject.SetActive(false);
			contentsRootObject.SetActive(false);
			needUpdateTextObject.SetActive(true);
			return;
		}

		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		if (guideQuestTableData.iconId < iconSpriteList.Length)
			iconImage.sprite = iconSpriteList[guideQuestTableData.iconId];

		if (GuideQuestData.Type2ClearType(guideQuestTableData.typeId) == GuideQuestData.eQuestClearType.ChapterStage)
		{
			if (int.TryParse(guideQuestTableData.param, out int paramStage))
			{
				int chapter = paramStage / 100;
				int stage = paramStage % 100;
				nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, chapter, stage));
			}
		}
		else if (GuideQuestData.Type2ClearType(guideQuestTableData.typeId) == GuideQuestData.eQuestClearType.PowerLevel)
		{
			string actorName = CharacterData.GetNameByActorId(guideQuestTableData.param);
			nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, guideQuestTableData.needCount, actorName));
		}
		else if (GuideQuestData.Type2ClearType(guideQuestTableData.typeId) == GuideQuestData.eQuestClearType.ExperienceLevel1 ||
			GuideQuestData.Type2ClearType(guideQuestTableData.typeId) == GuideQuestData.eQuestClearType.ExperienceLevel2)
		{
			string actorName = CharacterData.GetNameByActorId(guideQuestTableData.param);
			nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, actorName));
		}
		else if (GuideQuestData.Type2ClearType(guideQuestTableData.typeId) == GuideQuestData.eQuestClearType.IngameLevel)
		{
			nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, guideQuestTableData.param));
		}
		else
			nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, guideQuestTableData.needCount));
		RefreshCountInfo(0);

		if (string.IsNullOrEmpty(guideQuestTableData.rewardAdditionalText))
			additionalRewardText.text = "";
		else
			additionalRewardText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.rewardAdditionalText));

		goldIconObject.SetActive(false);
		diaIconObject.SetActive(false);
		energyIconObject.SetActive(false);
		returnScrollIconObject.SetActive(false);
		equipBoxObject.SetActive(false);
		equipBigBoxObject.SetActive(false);
		characterBoxObject.SetActive(false);

		if (guideQuestTableData.rewardType == "cu")
		{
			if (guideQuestTableData.rewardValue == CurrencyData.GoldCode())
				goldIconObject.SetActive(true);
			else if (guideQuestTableData.rewardValue == CurrencyData.DiamondCode())
				diaIconObject.SetActive(true);
			else if (guideQuestTableData.rewardValue == CurrencyData.EnergyCode())
				energyIconObject.SetActive(true);
			else
				returnScrollIconObject.SetActive(true);
			rewardCountText.text = guideQuestTableData.rewardCount.ToString("N0");
		}
		else if (guideQuestTableData.rewardType == "be")
		{
			equipBoxObject.SetActive(true);
			rewardCountText.text = "";
		}
		else if (guideQuestTableData.rewardType == "bm")
		{
			equipBigBoxObject.SetActive(true);
			rewardCountText.text = "";
		}
		else if (guideQuestTableData.rewardType == "bc")
		{
			characterBoxObject.SetActive(true);
			rewardCountText.text = "";
		}

		specialRewardRootObject.SetActive(false);
		specialRewardText.text = "";
		if (guideQuestTableData.nextNoti > 0)
		{
			string rewardText = string.Format("<color=#FFFF00>{0}</color>", UIString.instance.GetString(guideQuestTableData.nextParameter));
			string countText = string.Format("<size=20><color=#47C9E7>{0}</color></size>", guideQuestTableData.nextNoti);
			specialRewardText.SetLocalizedText(UIString.instance.GetString("QuestUI_NextSpecialReward", rewardText, countText));
			specialRewardRootObject.SetActive(true);
		}

		// 조건이 안맞으면 수행 불가능하다고 알려야하는데 만약 완료한 상태라면 그냥 두면 된다.
		if (guideQuestTableData.viewInBattle && GuideQuestData.instance.IsCompleteQuest() == false)
		{
			if (PlayerData.instance.selectedChapter == PlayerData.instance.highestPlayChapter)
			{ }
			else
			{
				contentsRootObject.SetActive(false);
				disableTextObject.SetActive(true);
			}
		}
	}

	void RefreshCountInfo(int temporaryAddCount)
	{
		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		//proceedingCountText.text = string.Format("{0} / {1}", GuideQuestData.instance.currentGuideQuestProceedingCount, guideQuestTableData.needCount);
		//proceedingCountImage.fillAmount = (float)GuideQuestData.instance.currentGuideQuestProceedingCount / guideQuestTableData.needCount;

		// 진행도 표시
		int currentCount = GuideQuestData.instance.currentGuideQuestProceedingCount + temporaryAddCount;
		int maxCount = guideQuestTableData.needCount;

		// 완료 체크
		_complete = (currentCount >= maxCount);

		if (_complete)
		{
			//descText.SetLocalizedText(UIString.instance.GetString("QuestUI_OneDone"));
			proceedingCountImage.color = DailyFreeItem.GetGoldTextColor();
			//completeText.text = UIString.instance.GetString("QuestUI_QuestCompleteNoti");
		}
		else
		{
			//if (PlayerData.instance.currentChaosMode)
			//	descText.SetLocalizedText(UIString.instance.GetString("QuestUI_NowQuest"));
			//else
			//	descText.SetLocalizedText(string.Format("{0}\n{1}", UIString.instance.GetString("QuestUI_NowQuest"), UIString.instance.GetString("QuestUI_NotChaos")));
			proceedingCountImage.color = Color.white;
		}
		proceedingCountImage.fillAmount = (float)(currentCount) / maxCount;
		proceedingCountText.text = string.Format("{0} / {1}", currentCount, maxCount);
		//completeText.gameObject.SetActive(_complete);

		blinkObject.SetActive(_complete);
		smallBlinkObject.SetActive(_complete);
	}

	public void RefreshAlarmObject()
	{
		bool isCompleteQuest = GuideQuestData.instance.IsCompleteQuest();
		bool showAlarm = false;
		bool onlySmallButtonAlarm = false;
		if (isCompleteQuest) showAlarm = true;
		if (showAlarm == false && GuideQuestData.instance.currentGuideQuestIndex == 0) onlySmallButtonAlarm = true;

		AlarmObject.Hide(alarmRootTransform);
		AlarmObject.Hide(infoAlarmRootTransform);
		if (showAlarm)
		{
			AlarmObject.Show(alarmRootTransform, true, true);
			AlarmObject.Show(infoAlarmRootTransform, true, true);
		}
		if (onlySmallButtonAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void CloseInfo()
	{
		smallButtonRootObject.SetActive(true);
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
		_openRemainTime = _closeRemainTime = _claimReopenRemainTime = 0.0f;
	}

	public void RefreshCondition(bool stage)
	{
		// 업뎃 필요하다는 문구 예외처리.
		if (stage && needUpdateTextObject.activeSelf)
			gameObject.SetActive(false);

		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		if (stage)
		{
			AlarmObject.Hide(alarmRootTransform);
			AlarmObject.Hide(infoAlarmRootTransform);

			if (guideQuestTableData.viewInBattle)
			{
			}
			else
			{
				// 스테이지 들어가는데 viewInBattle이 꺼있으면 루트를 통째로 끈다.
				gameObject.SetActive(false);
			}
		}
	}

	public void OnAddCount(int temporaryAddCount)
	{
		RefreshCountInfo(temporaryAddCount);
	}

	public void OnClickBlinkImage()
	{
		bool lobby = false;
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby) lobby = true;
		if (lobby)
		{
			// 보상처리
			ClaimReward();
		}
		else
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_LobbyToast"), 2.0f);
	}

	float _claimReopenRemainTime;
	void ClaimReward()
	{
		// CumulativeEventListItem에서 가져와서 적절히 변형시켜 쓴다.
		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		if (guideQuestTableData.rewardType == "be" || guideQuestTableData.rewardType == "bm")
		{
			// 상자일 경우 드랍프로세서
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			// 장비박스 했을때처럼 드랍프로세서로부터 하나 뽑아와야한다.
			bool result = PrepareDropProcessor(guideQuestTableData.rewardType, guideQuestTableData.rewardValue, guideQuestTableData.rewardCount);
			if (CheatingListener.detectedCheatTable)
				return;
			if (result == false)
				return;

			if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
				DotMainMenuCanvas.instance.OnClickBackButton();

			PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, 0, 0, 0, 0, DropManager.instance.GetLobbyDropItemInfo(), false, OnRecvEquipBox);
		}
		else if (guideQuestTableData.rewardType == "bc")
		{
			// 가장 핵심은 드랍부터 굴려서 보상정보를 얻어오는거다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflrfh", "", true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);
			if (CheatingListener.detectedCheatTable)
				return;

			if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
				DotMainMenuCanvas.instance.OnClickBackButton();

			PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, 0, 0, 0, 0, null, true, OnRecvCharacterBox);
		}
		else if (guideQuestTableData.rewardType == "cu")
		{
			bool showCurrencySmallInfo = false;

			int addGold = 0;
			int addDia = 0;
			int addEnergy = 0;
			int addReturnScroll = 0;
			if (guideQuestTableData.rewardValue == CurrencyData.GoldCode())
			{
				addGold += guideQuestTableData.rewardCount;
				showCurrencySmallInfo = true;
			}
			else if (guideQuestTableData.rewardValue == CurrencyData.DiamondCode())
			{
				addDia += guideQuestTableData.rewardCount;
				showCurrencySmallInfo = true;
			}
			else if (guideQuestTableData.rewardValue == CurrencyData.EnergyCode())
				addEnergy += guideQuestTableData.rewardCount;
			else
				addReturnScroll += guideQuestTableData.rewardCount;

			if (showCurrencySmallInfo)
				CurrencySmallInfoCanvas.Show(true);

			PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, addDia, addGold, addEnergy, addReturnScroll, null, false, (serverFailure, itemGrantString) =>
			{
				if (showCurrencySmallInfo)
				{
					CurrencySmallInfoCanvas.RefreshInfo();
					Timing.RunCoroutine(DelayedHideCurrencySmallInfo(2.0f));
				}

				infoRootTweenAnimation.gameObject.SetActive(false);
				smallBackButtonRootObject.SetActive(false);
				_openRemainTime = _closeRemainTime = 0.0f;

				RefreshInfo();
				RefreshAlarmObject();
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);

				// 1.5초 뒤에 바로 받은거처럼 
				_claimReopenRemainTime = 1.5f;
			});
		}
	}

	IEnumerator<float> DelayedHideCurrencySmallInfo(float delay)
	{
		yield return Timing.WaitForSeconds(delay);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		CurrencySmallInfoCanvas.Show(false);
	}

	// CumulativeEventListItem에 있는거 가져와서 쓴다.
	DropProcessor _cachedDropProcessor;
	int _cachedDropCount;
	bool PrepareDropProcessor(string type, string value, int count)
	{
		string dropId = "";
		if (type == "be")
		{
			switch (value)
			{
				case "1": dropId = "Dnvuswkdqlu"; break;
				case "2": dropId = "Dnvuswkdqlv"; break;
				case "3": dropId = "Dnvuswkdqlw"; break;
				default:
					return false;
			}
			switch (count)
			{
				case 1: break;
				case 2: dropId = string.Format("{0}{1}", dropId, "w"); break;
				case 3: dropId = string.Format("{0}{1}", dropId, "e"); break;
				case 4: dropId = string.Format("{0}{1}", dropId, "r"); break;
				case 5: dropId = string.Format("{0}{1}", dropId, "t"); break;
				default:
					return false;
			}
		}
		else if (type == "bm")
		{
			dropId = "Wkdql";
			switch (count)
			{
				case 1: dropId = string.Format("{0}{1}", dropId, "q"); break;
				case 2: dropId = string.Format("{0}{1}", dropId, "w"); break;
				case 3: dropId = string.Format("{0}{1}", dropId, "e"); break;
				case 4: dropId = string.Format("{0}{1}", dropId, "r"); break;
				case 5: dropId = string.Format("{0}{1}", dropId, "t"); break;
				default:
					return false;
			}
		}

		_cachedDropCount = count;
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, dropId, "", true, true);
		if (count > 3)
			_cachedDropProcessor.AdjustDropRange(3.7f);
		if (CheatingListener.detectedCheatTable)
			return false;
		List<ObscuredString> listDropItemId = DropManager.instance.GetLobbyDropItemInfo();
		if (listDropItemId.Count != count)
			return false;
		return true;
	}

	// MailCanvasListItem의 OnRecvEquipBox에서 가져와서 수정해서 쓴다.
	bool _claimReopenFlag = false;
	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;
		if (itemGrantString == "")
			return;

		RefreshInfo();
		RefreshAlarmObject();

		// 박스 연출이 끝나고나서 퀘를 받은거처럼 하기 위해 별도의 플래그를 걸어둔다.
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
		_openRemainTime = 0.0f;
		_claimReopenFlag = true;

		// 캐릭터와 달리 장비는 드랍프로세서에서 정보를 뽑아쓰는게 아니라서 미리 클리어해도 상관없다.
		DropManager.instance.ClearLobbyDropInfo();

		TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, _cachedDropCount);

		// 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ItemInstance> listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
		for (int i = 0; i < listGrantItem.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
			if (equipTableData == null)
				continue;

			AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
		}

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Equip1, _cachedDropProcessor, 0, 0, () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
				{
					EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem);
				});
			});
		});
	}

	void OnRecvCharacterBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		RefreshInfo();
		RefreshAlarmObject();

		// 박스 연출이 끝나고나서 퀘를 받은거처럼 하기 위해 별도의 플래그를 걸어둔다.
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
		_openRemainTime = 0.0f;
		_claimReopenFlag = true;

		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Character, _cachedDropProcessor, 0, 0, () =>
			{
				CharacterBoxConfirmCanvas.OnCompleteRandomBoxScreen(DropManager.instance.GetGrantCharacterInfo(), DropManager.instance.GetTranscendPointInfo(), CharacterBoxConfirmCanvas.OnResult);
			});
		});
	}



	#region Show Hide
	public void OnClickSmallButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		smallButtonRootObject.SetActive(false);
		infoRootTweenAnimation.gameObject.SetActive(true);
		smallBackButtonRootObject.SetActive(true);
	}

	float _closeRemainTime;
	public void OnClickSmallBackButton()
	{
		if (_closeRemainTime > 0.0f)
			return;
		if (smallBackButtonRootObject.activeSelf == false)
			return;

		smallBackButtonRootObject.SetActive(false);
		infoRootTweenAnimation.DOPlayBackwards();
		_closeRemainTime = 0.6f;
	}

	float _openRemainTime;
	public void OnCompleteInfoRootTweenAnimation()
	{
		if (smallButtonRootObject.activeSelf)
			return;

		smallBackButtonRootObject.SetActive(true);
		_openRemainTime = 4.0f;
	}
	#endregion
}