using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class DailyShopEquipConfirmCanvas : MonoBehaviour
{
	public static DailyShopEquipConfirmCanvas instance;

	public Text gradeText;
	public DailyShopListItem dailyListItem;
	// 장비는 big이 없다.
	//public DailyShopListItem bigDailyListItem;
	// 대신 민맥스 공격력이 필요하다.
	public Text attackText;
	public RectTransform detailButtonRectTransform;
	public Text priceText;
	public GameObject[] priceTypeObjectList;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect[] priceGrayscaleEffect;

	public EquipListStatusInfo materialSmallStatusInfo;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		buttonObject.SetActive(true);
	}

	void OnDisable()
	{
		materialSmallStatusInfo.gameObject.SetActive(false);
	}

	DailyShopData.DailyShopSlotInfo _slotInfo;
	public void ShowCanvas(bool show, DailyShopData.DailyShopSlotInfo dailyShopSlotInfo)
	{
		_slotInfo = dailyShopSlotInfo;

		// 골드나 다른 박스들과 달리 지우기엔 컴포넌트가 많아서 차라리 마스킹으로 가리도록 해본다.
		dailyListItem.RefreshInfo(dailyShopSlotInfo);

		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(dailyShopSlotInfo.value);
		if (equipTableData == null)
			return;

		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipTableData.grade)));
		gradeText.color = EquipListStatusInfo.GetGradeDropObjectNameColor(equipTableData.grade);
		attackText.text = UIString.instance.GetString("GameUI_NumberRange", ActorStatus.GetDisplayAttack(equipTableData.min).ToString("N0"), ActorStatus.GetDisplayAttack(equipTableData.max).ToString("N0"));

		// 장착중인 정보
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)equipTableData.equipType);
		if (equipData != null)
		{
			materialSmallStatusInfo.RefreshInfo(equipData, false);
			materialSmallStatusInfo.gameObject.SetActive(false);
			materialSmallStatusInfo.gameObject.SetActive(true);
		}
		else
			materialSmallStatusInfo.gameObject.SetActive(false);

		priceText.text = dailyShopSlotInfo.price.ToString("N0");
		bool disablePrice = false;
		CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
		if (dailyShopSlotInfo.priceType == CurrencyData.GoldCode())
		{
			currencyType = CurrencyData.eCurrencyType.Gold;
			disablePrice = (CurrencyData.instance.gold < dailyShopSlotInfo.price);
		}
		else
		{
			disablePrice = (CurrencyData.instance.dia < dailyShopSlotInfo.price);
		}
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		for (int i = 0; i < priceTypeObjectList.Length; ++i)
		{
			priceTypeObjectList[i].SetActive((int)currencyType == i);
			if ((int)currencyType == i)
				priceGrayscaleEffect[i].enabled = disablePrice;
		}

		// 구매하면 3d 오브젝트가 떠야하니 미리 로딩을 걸어둔다.
		AddressableAssetLoadManager.GetAddressableGameObject(equipTableData.prefabAddress, "Equip", null);
	}

	public void OnClickDetailButton()
	{
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(_slotInfo.value);
		if (equipTableData == null)
			return;

		// 장비일때만 오는거라 장비가 보이면 된다.
		// 여긴 장비를 만들어내기 전이기 때문에 equipId만 가지고 3d오브젝트를 보여줄 방법이 필요하다.
		// 이 창은 연출과도 관련없으니 스스로 StackCanvas를 처리한다.
		UIInstanceManager.instance.ShowCanvasAsync("DailyShopEquipDetailCanvas", () =>
		{
			gameObject.SetActive(false);
			DailyShopEquipDetailCanvas.instance.ShowCanvas(true, equipTableData, () =>
			{
				// 확인 누르면 바로 캐시샵으로 돌아와서 이 Confirm창을 다시 띄워야한다.
				gameObject.SetActive(true);
				ShowCanvas(true, _slotInfo);
			});
		});
	}

	public void OnClickOkButton()
	{
		if (_slotInfo.priceType == CurrencyData.GoldCode())
		{
			if (CurrencyData.instance.gold < _slotInfo.price)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
				return;
			}
		}
		else
		{
			if (CurrencyData.instance.dia < _slotInfo.price)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
				return;
			}
		}

		int priceDia = (_slotInfo.priceType == CurrencyData.DiamondCode()) ? _slotInfo.price : 0;
		int priceGold = (_slotInfo.priceType == CurrencyData.GoldCode()) ? _slotInfo.price : 0;
		PlayFabApiManager.instance.RequestPurchaseDailyShopItem(_slotInfo.slotId, _slotInfo.type, _slotInfo.value, "", priceDia, priceGold, OnRecvPurchaseDailyShopItem);

		buttonObject.SetActive(false);
	}

	void OnRecvPurchaseDailyShopItem(bool serverFailure, string newCharacterId, string itemGrantString)
	{
		if (serverFailure)
			return;

		CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

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

			gameObject.SetActive(false);
			DailyShopEquipShowCanvas.instance.ShowCanvas(grantEquipData, () =>
			{
				// 확인 누르면 바로 캐시샵으로 돌아오면 된다.
				StackCanvas.Pop(gameObject);
			});
		});
	}
}