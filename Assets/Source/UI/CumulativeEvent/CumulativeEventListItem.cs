﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class CumulativeEventListItem : MonoBehaviour
{
	public CumulativeEventData.eEventType eventType;
	public int day;

	public Image blurImage;
	public Image backgroundImge;
	public Sprite[] backgroundSpriteList;

	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public GameObject returnScrollIconObject;
	public GameObject equipBoxObject;
	public Image equipIconImage;

	public Text countText;
	public Text nameText;
	public Text claimText;
	public Text dayText;
	public Text completeText;

	public GameObject addObject;
	public Text addText;
	public Image infoButtonImage;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	void OnEnable()
	{
		CumulativeEventData.EventRewardInfo eventRewardInfo = CumulativeEventData.instance.FindRewardInfo(eventType, day);
		if (eventRewardInfo == null)
			return;
		RefreshInfo(eventRewardInfo);
	}

	CumulativeEventData.EventRewardInfo _slotInfo;
	void RefreshInfo(CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		_slotInfo = eventRewardInfo;

		RefreshClaimState();
		RefreshInfoButton(eventRewardInfo);

		if (_slotInfo.alreadyDesigned)
		{
			RefreshBackground(true);
			return;
		}

		if (eventRewardInfo.type == "cu")
		{
			if (eventRewardInfo.value == CurrencyData.GoldCode())
			{
				//_addGold = eventRewardInfo.count;
				goldIconObject.SetActive(true);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(false);
				returnScrollIconObject.SetActive(false);
				countText.color = DailyFreeItem.GetGoldTextColor();
			}
			else if (eventRewardInfo.value == CurrencyData.DiamondCode())
			{
				//_addDia = eventRewardInfo.cn;
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(true);
				energyIconObject.SetActive(false);
				returnScrollIconObject.SetActive(false);
				countText.color = DailyFreeItem.GetDiaTextColor();
			}
			else if (eventRewardInfo.value == CurrencyData.EnergyCode())
			{
				//_addEnergy = createInfo.cn;
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(true);
				returnScrollIconObject.SetActive(false);
				countText.color = Color.white;
			}
			else
			{
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(false);
				returnScrollIconObject.SetActive(true);
				countText.color = Color.white;
			}
			countText.text = eventRewardInfo.count.ToString("N0");
			countText.gameObject.SetActive(true);
			equipBoxObject.SetActive(false);
			equipIconImage.gameObject.SetActive(false);
			nameText.gameObject.SetActive(false);
			addObject.SetActive(false);
			RefreshBackground(false);
		}
		else if (eventRewardInfo.type == "be")
		{
			goldIconObject.SetActive(false);
			diaIconObject.SetActive(false);
			energyIconObject.SetActive(false);
			returnScrollIconObject.SetActive(false);
			equipBoxObject.SetActive(true);
			equipIconImage.gameObject.SetActive(false);

			countText.gameObject.SetActive(false);
			nameText.gameObject.SetActive(true);
			nameText.SetLocalizedText(UIString.instance.GetString("MailUI_Equipment"));
			addObject.SetActive(true);
			addText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", eventRewardInfo.value)));
			int.TryParse(eventRewardInfo.value, out int grade);
			addText.color = EquipListStatusInfo.GetGradeDropObjectNameColor(grade);
			RefreshBackground(false);
		}
		else if (eventRewardInfo.type == "fe")
		{
			goldIconObject.SetActive(false);
			diaIconObject.SetActive(false);
			energyIconObject.SetActive(false);
			returnScrollIconObject.SetActive(false);
			equipBoxObject.SetActive(false);
			equipIconImage.gameObject.SetActive(true);

			RefreshEquipIconImage(eventRewardInfo.value);
		}
	}

	void RefreshEquipIconImage(string value)
	{
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(value);
		if (equipTableData == null)
			return;

		RefreshBackground(equipTableData.grade == 4);

		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));
		nameText.gameObject.SetActive(true);
		addObject.SetActive(false);
	}

	// CumulativeEventEquipInfoCanvas 같이 고쳐야한다.
	void RefreshBackground(bool isLightenBackground)
	{
		blurImage.color = isLightenBackground ? new Color(0.945f, 0.945f, 0.094f, 0.42f) : new Color(1.0f, 1.0f, 1.0f, 0.35f);
		backgroundImge.color = isLightenBackground ? new Color(1.0f, 1.0f, 1.0f, 0.42f) : new Color(0.829f, 0.915f, 0.514f, 0.604f);
		backgroundImge.sprite = backgroundSpriteList[isLightenBackground ? 0 : 1];
	}

	void RefreshInfoButton(CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		if (eventRewardInfo.type == "cu")
		{
			infoButtonImage.gameObject.SetActive(false);
			if (eventRewardInfo.value == CurrencyData.GoldCode())
			{
			}
			else if (eventRewardInfo.value == CurrencyData.DiamondCode())
			{
			}
			else if (eventRewardInfo.value == CurrencyData.EnergyCode())
			{
			}
			else
			{
				infoButtonImage.gameObject.SetActive(true);
			}
		}
		else if (eventRewardInfo.type == "be")
		{
			infoButtonImage.gameObject.SetActive(false);
		}
		else if (eventRewardInfo.type == "fe")
		{
			infoButtonImage.gameObject.SetActive(true);
		}
	}

	void RefreshClaimState()
	{
		// 프리팹을 하나로 쓰기때문에 모든 타입이 공용으로 사용하게 된다. 타입별로 나눠서 처리.

		int count = 0;
		switch (eventType)
		{
			case CumulativeEventData.eEventType.NewAccount:
				count = CumulativeEventData.instance.newAccountLoginEventCount;
				break;
			case CumulativeEventData.eEventType.DailyBox:
				count = CumulativeEventData.instance.newAccountDailyBoxEventCount;
				break;
			case CumulativeEventData.eEventType.OpenChaos:
				count = CumulativeEventData.instance.openChaosEventCount;
				break;
			case CumulativeEventData.eEventType.LoginRepeat:
				count = CumulativeEventData.instance.repeatLoginEventCount;
				break;
			case CumulativeEventData.eEventType.DailyBoxRepeat:
				count = CumulativeEventData.instance.repeatDailyBoxEventCount;
				break;
			case CumulativeEventData.eEventType.NodeWarRepeat:
				count = CumulativeEventData.instance.repeatNodeWarEventCount;
				break;
		}

		bool recorded = false;
		switch (eventType)
		{
			case CumulativeEventData.eEventType.NewAccount:
				recorded = CumulativeEventData.instance.newAccountLoginRecorded;
				break;
			case CumulativeEventData.eEventType.DailyBox:
				recorded = CumulativeEventData.instance.newAccountDailyBoxRecorded;
				break;
			case CumulativeEventData.eEventType.OpenChaos:
				recorded = CumulativeEventData.instance.openChaosEventRecorded;
				break;
			case CumulativeEventData.eEventType.LoginRepeat:
				recorded = CumulativeEventData.instance.repeatLoginEventRecorded;
				break;
			case CumulativeEventData.eEventType.DailyBoxRepeat:
				recorded = CumulativeEventData.instance.repeatDailyBoxEventRecorded;
				break;
			case CumulativeEventData.eEventType.NodeWarRepeat:
				recorded = CumulativeEventData.instance.repeatNodeWarEventRecorded;
				break;
		}

		if (day <= count)
		{
			claimText.gameObject.SetActive(false);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(true);
			blackObject.gameObject.SetActive(true);
			infoButtonImage.color = Color.gray;
			AlarmObject.Hide(alarmRootTransform);
		}
		else if (recorded == false && day == (count + 1))
		{
			claimText.gameObject.SetActive(true);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			infoButtonImage.color = Color.white;

			if (CumulativeEventData.instance.IsReceivableEvent(eventType))
			{
				claimText.color = new Color(0.0f, 1.0f, 0.0f);
				AlarmObject.Show(alarmRootTransform);
			}
			else
			{
				claimText.color = new Color(0.0f, 0.333f, 0.0f);
				AlarmObject.Hide(alarmRootTransform);
			}
		}
		else
		{
			claimText.gameObject.SetActive(false);
			dayText.SetLocalizedText(UIString.instance.GetString("LoginUI_DayNumber", day));
			dayText.gameObject.SetActive(true);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			infoButtonImage.color = Color.white;
			AlarmObject.Hide(alarmRootTransform);
		}
	}

	public void OnClickInfoButton()
	{
		if (_slotInfo == null)
			return;

		if (_slotInfo.type == "cu")
		{
			if (_slotInfo.value == CurrencyData.GoldCode())
			{
			}
			else if (_slotInfo.value == CurrencyData.DiamondCode())
			{
			}
			else if (_slotInfo.value == CurrencyData.EnergyCode())
			{
			}
			else
			{
				UIInstanceManager.instance.ShowCanvasAsync("ReturnScrollInfoCanvas", null);
			}
		}
		else if (_slotInfo.type == "fe")
		{
			UIInstanceManager.instance.ShowCanvasAsync("CumulativeEventEquipInfoCanvas", () =>
			{
				CumulativeEventEquipInfoCanvas.instance.ShowCanvas(true, _slotInfo);
			});
		}
	}

	List<ObscuredString> _listEquipIdRequest = null;
	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyFreeItem"), 2.0f);	
			return;
		}

		if (dayText.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimYet"), 2.0f);
			return;
		}

		// DailyBox 계열에서는 오늘의 일퀘를 완료했는지 체크해야한다.
		if (CumulativeEventData.IsDailyBoxEvent(eventType))
		{
			if (PlayerData.instance.sharedDailyBoxOpened == false)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimOrigin"), 2.0f);
				return;
			}
		}

		// NodeWar 역시 체크
		if (CumulativeEventData.IsNodeWarEvent(eventType))
		{
			if (PlayerData.instance.nodeWarCleared == false)
			{
				if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.NodeWar))
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimNodeWar"), 2.0f);
				else
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimNodeWarNotOpen"), 2.0f);
				return;
			}
		}

		if (CumulativeEventData.IsRepeatEvent(eventType) && CumulativeEventData.instance.removeRepeatServerFailure)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_InsufficientInfo"), 2.0f);
			return;
		}

		// 보상 연출때문에 몇가지 경우의 수로 나눠서 처리하기로 한다.
		// be나 fe가 있을땐 단일이고 재화의 경우에만 여러개를 모아서 처리하면 된다.
		if (_slotInfo.type == "be")
		{
			// 상자일 경우 드랍프로세서
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			// 장비박스 했을때처럼 드랍프로세서로부터 하나 뽑아와야한다.
			bool result = PrepareDropProcessor(_slotInfo.value, _slotInfo.count);
			if (CheatingListener.detectedCheatTable)
				return;
			if (result == false)
				return;

			PlayFabApiManager.instance.RequestReceiveEventReward(eventType, _slotInfo.type, 0, 0, 0, 0, DropManager.instance.GetLobbyDropItemInfo(), OnRecvEquipBox);
		}
		else if (_slotInfo.type == "fe")
		{
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			if (_listEquipIdRequest == null)
				_listEquipIdRequest = new List<ObscuredString>();
			_listEquipIdRequest.Clear();

			_listEquipIdRequest.Add(_slotInfo.value);

			// 고정 장비일 경우 단일 획득 연출
			PlayFabApiManager.instance.RequestReceiveEventReward(eventType, _slotInfo.type, 0, 0, 0, 0, _listEquipIdRequest, OnRecvEventReward);
		}
		else if (_slotInfo.type == "cu")
		{
			int addGold = 0;
			int addDia = 0;
			int addEnergy = 0;
			int addReturnScroll = 0;
			if (_slotInfo.value == CurrencyData.GoldCode())
				addGold += _slotInfo.count;
			else if (_slotInfo.value == CurrencyData.DiamondCode())
				addDia += _slotInfo.count;
			else if (_slotInfo.value == CurrencyData.EnergyCode())
				addEnergy += _slotInfo.count;
			else
				addReturnScroll += _slotInfo.count;

			if (_slotInfo.type2 == "cu")
			{
				if (_slotInfo.value2 == CurrencyData.GoldCode())
					addGold += _slotInfo.count2;
				else if (_slotInfo.value2 == CurrencyData.DiamondCode())
					addDia += _slotInfo.count2;
				else if (_slotInfo.value2 == CurrencyData.EnergyCode())
					addEnergy += _slotInfo.count2;
				else
					addReturnScroll += _slotInfo.count2;
			}

			// 나머지는 다 재화 단일이거나 재화 복수다. 이럴땐 그냥 토스트 처리만 하면 된다.
			PlayFabApiManager.instance.RequestReceiveEventReward(eventType, _slotInfo.type, addDia, addGold, addEnergy, addReturnScroll, null, (serverFailure, itemGrantString) =>
			{
				// 누적 로그인 이벤트는 첫날꺼를 가지고 판단하는거라 이 호출에서만 처리해도 괜찮다.
				if (eventType == CumulativeEventData.eEventType.NewAccount)
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.CumulativeLogin);

				RefreshClaimState();
				CumulativeEventCanvas.instance.currencySmallInfo.RefreshInfo();
				CumulativeEventCanvas.instance.RefreshAlarmObject(eventType);
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
			});
		}
	}

	void OnRecvEventReward(bool serverFailure, string itemGrantString)
	{
		if (serverFailure)
			return;

		CumulativeEventCanvas.instance.currencySmallInfo.RefreshInfo();
		CumulativeEventCanvas.instance.RefreshAlarmObject(eventType);

		// 직접 사는거라 뽑기 연출을 보여줄 순 없고 전용 획득창을 보여준다.
		if (itemGrantString == "")
			return;
		EquipData grantEquipData = TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, 1);
		if (grantEquipData == null)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("DailyShopEquipShowCanvas", () =>
		{
			// CharacterBoxShowCanvas와 비슷한 구조로 가기 위해 여기서 StackCanvas 처리를 한다.
			StackCanvas.Push(gameObject);

			// DailyShopEquipConfirmCanvas와 달리 이 창은 StackCanvas에 속해있으므로 호출하지 않아도 된다.
			//gameObject.SetActive(false);
			DailyShopEquipShowCanvas.instance.ShowCanvas(grantEquipData, () =>
			{
				// 확인 누르면 바로 으로 돌아오면 된다.
				StackCanvas.Pop(gameObject);
			});
		});
	}

	DropProcessor _cachedDropProcessor;
	bool PrepareDropProcessor(string value, int count)
	{
		// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
		string dropId = "";
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

		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, dropId, "", true, true);
		if (CheatingListener.detectedCheatTable)
			return false;
		List<ObscuredString> listDropItemId = DropManager.instance.GetLobbyDropItemInfo();
		if (listDropItemId.Count != count)
			return false;
		return true;
	}

	// MailCanvasListItem의 OnRecvEquipBox에서 가져와서 수정해서 쓴다.
	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;
		if (itemGrantString == "")
			return;

		CumulativeEventCanvas.instance.currencySmallInfo.RefreshInfo();
		CumulativeEventCanvas.instance.RefreshAlarmObject(eventType);

		// 캐릭터와 달리 장비는 드랍프로세서에서 정보를 뽑아쓰는게 아니라서 미리 클리어해도 상관없다.
		DropManager.instance.ClearLobbyDropInfo();

		TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, _slotInfo.count);

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
}