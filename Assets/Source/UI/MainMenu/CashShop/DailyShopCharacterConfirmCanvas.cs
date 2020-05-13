using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopCharacterConfirmCanvas : MonoBehaviour
{
	public static DailyShopCharacterConfirmCanvas instance;

	public GameObject dailyListItemGroupObject;
	public GameObject bigDailyListItemGroupObject;
	public DailyShopListItem dailyListItem;
	public DailyShopListItem bigDailyListItem;
	public RectTransform detailButtonRectTransform;
	public Transform characterBoxAddImageTransform;
	public Transform bigCharacterBoxAddImageTransform;

	public Text priceText;
	public GameObject[] priceTypeObjectList;
	public GameObject buttonObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		buttonObject.SetActive(true);
	}

	DailyShopData.DailyShopSlotInfo _slotInfo;
	Transform _currentAddImageTransform;
	public void ShowCanvas(bool show, DailyShopData.DailyShopSlotInfo dailyShopSlotInfo, bool big)
	{
		_slotInfo = dailyShopSlotInfo;

		// 골드나 다른 박스들과 달리 지우기엔 컴포넌트가 많아서 차라리 마스킹으로 가리도록 해본다.
		string selectedCharacterId = "";
		if (big)
		{
			bigDailyListItem.RefreshInfo(dailyShopSlotInfo);
			detailButtonRectTransform.anchoredPosition = new Vector3(128.0f, detailButtonRectTransform.anchoredPosition.y);
			_currentAddImageTransform = bigCharacterBoxAddImageTransform;
			selectedCharacterId = bigDailyListItem.selectedCharacterId;
		}
		else
		{
			dailyListItem.RefreshInfo(dailyShopSlotInfo);
			detailButtonRectTransform.anchoredPosition = new Vector3(90.0f, detailButtonRectTransform.anchoredPosition.y);
			_currentAddImageTransform = characterBoxAddImageTransform;
			selectedCharacterId = dailyListItem.selectedCharacterId;
		}
		dailyListItemGroupObject.SetActive(!big);
		bigDailyListItemGroupObject.SetActive(big);

		priceText.text = dailyShopSlotInfo.price.ToString("N0");
		CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
		if (dailyShopSlotInfo.priceType == CurrencyData.GoldCode())
			currencyType = CurrencyData.eCurrencyType.Gold;
		for (int i = 0; i < priceTypeObjectList.Length; ++i)
			priceTypeObjectList[i].SetActive((int)currencyType == i);

		// 신규캐릭터 획득 창이 뜰 항목들을 열때는 미리 로드를 걸어둔다.
		switch (_slotInfo.type)
		{
			case "fc":
			case "uch":
			case "fl1":
			case "fl2":
			case "fl3":
				AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(selectedCharacterId), "Character");
				break;
		}
	}

	public void OnClickDetailButton()
	{
		// 타입에 따라 달라져야 한다.
		switch (_slotInfo.type)
		{
			// 상자는 툴팁만 표시하면 끝
			case "bn":
				TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("ShopUIMore_OnlyNormal"), 300, _currentAddImageTransform, new Vector2(0.0f, -20.0f));
				break;
			case "bh":
				TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("ShopUIMore_OnlyHeroic"), 300, _currentAddImageTransform, new Vector2(0.0f, -20.0f));
				break;
		}
	}

	string _selectedCharacterId;
	public void OnClickOkButton()
	{
		int priceDia = (_slotInfo.priceType == CurrencyData.DiamondCode()) ? _slotInfo.price : 0;
		int priceGold = (_slotInfo.priceType == CurrencyData.GoldCode()) ? _slotInfo.price : 0;

		switch (_slotInfo.type)
		{
			// 캐릭터 박스의 경우엔 상자를 직접 굴려야해서 드랍 프로세서가 필요하다.
			case "bn":
			case "bh":
				_selectedCharacterId = PrepareDropProcessor(_slotInfo.type);
				break;
			// 나머지는 슬롯에 저장되어있으니 받아다가 쓰면 된다.
			default:
				if (dailyListItemGroupObject.activeSelf)
					_selectedCharacterId = dailyListItem.selectedCharacterId;
				else
					_selectedCharacterId = bigDailyListItem.selectedCharacterId;
				break;
		}
		if (CheatingListener.detectedCheatTable)
			return;
		PlayFabApiManager.instance.RequestPurchaseDailyShopItem(_slotInfo.slotId, _slotInfo.type, "", _selectedCharacterId, priceDia, priceGold, OnRecvPurchaseDailyShopItem);

		buttonObject.SetActive(false);
	}

	DropProcessor _cachedDropProcessor;
	string PrepareDropProcessor(string type)
	{
		// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
		string dropId = "";
		switch (type)
		{
			case "bn": dropId = "Zoflredlfqks"; break;
			case "bh": dropId = "Zoflrduddnd"; break;
		}
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, dropId, "", true, true);
		if (CheatingListener.detectedCheatTable)
			return "";
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		if (listGrantInfo.Count == 1)
			return listGrantInfo[0];
		return "";
	}

	void OnRecvPurchaseDailyShopItem(bool serverFailure, string newCharacterId, string itemGrantString)
	{
		if (serverFailure)
			return;

		CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

		// 타입에 따라 드랍 연출이 있는거. 캐릭터 영입창이 뜨는거 등등 나뉘어진다.
		// "fe"를 제외한 10개의 타입에 대해 처리하면 된다. (총 11개의 타입이 존재. fe는 DailyShopEquipConfirmCanvas에서 처리한다.
		switch (_slotInfo.type)
		{
			case "fp":
			case "upn":
			case "uph":
				// pp는 pp 누적시킨 후 토스트만 보여주고 끝이다.
				CharacterData characterData = PlayerData.instance.GetCharacterData(_selectedCharacterId);
				if (characterData != null)
					characterData.pp += _slotInfo.cn;
				CashShopCanvas.instance.RefreshDailyShopInfo();
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_PurchasedPowerPoint"), 2.0f);
				gameObject.SetActive(false);
				return;
			case "bn":
			case "bh":
				if (string.IsNullOrEmpty(newCharacterId) == false)
					PlayerData.instance.AddNewCharacter(_selectedCharacterId, newCharacterId, 1);

				// 오리진 박스처럼 뽑기 연출이 필요한 케이스. CharacterBoxConfirmCanvas에 있는거 가져와서 처리한다.
				// DropManager.instance.ClearLobbyDropInfo() 함수 호출은 아래 OnResult 전달하는 부분에서 알아서 호출할테니 신경쓸필요 없다.
				UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
				{
					gameObject.SetActive(false);
					RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Character, _cachedDropProcessor, 0, () =>
					{
						CharacterBoxConfirmCanvas.OnCompleteRandomBoxScreen(DropManager.instance.GetGrantCharacterInfo(), DropManager.instance.GetLimitBreakPointInfo(), () =>
						{
							// 새캐릭 획득 결과창에서 확인 누르면 바로 캐시샵으로 돌아오면 된다.
							if (CharacterBoxShowCanvas.instance != null && CharacterBoxShowCanvas.instance.gameObject.activeSelf)
								CharacterBoxShowCanvas.instance.gameObject.SetActive(false);
							RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
						});
					});
				});
				return;
		}

		// 나머지는 영입창 바로 뜨는 케이스.
		// 각각의 타입별로 처리는 달라서 결과창만 공통으로 처리하고 패킷 처리는 각각 따로 하기로 한다.
		switch (_slotInfo.type)
		{
			case "fc":
			case "uch":
				if (string.IsNullOrEmpty(newCharacterId))
					break;
				PlayerData.instance.AddNewCharacter(_selectedCharacterId, newCharacterId, 1);
				break;
			case "fl1":
			case "fl2":
			case "fl3":
				CharacterData characterData = PlayerData.instance.GetCharacterData(_selectedCharacterId);
				if (characterData != null)
					characterData.limitBreakPoint += 1;
				break;
		}

		UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxShowCanvas", () =>
		{
			// CharacterBoxShowCanvas는 뽑기 결과창 후 호출되던거라 Stack 처리루틴이 하나도 포함되지 않아서
			// 이렇게 별도로 호출해서 현재 창을 닫도록 한다.
			StackCanvas.Push(gameObject);

			gameObject.SetActive(false);
			CharacterBoxShowCanvas.instance.ShowCanvas(_selectedCharacterId, () =>
			{
				// 새캐릭 획득 결과창에서 확인 누르면 바로 캐시샵으로 돌아오면 된다.
				if (CharacterBoxShowCanvas.instance != null && CharacterBoxShowCanvas.instance.gameObject.activeSelf)
					CharacterBoxShowCanvas.instance.gameObject.SetActive(false);
				StackCanvas.Pop(gameObject);
			});
		});
	}
}