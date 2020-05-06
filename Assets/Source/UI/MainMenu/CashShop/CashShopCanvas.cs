using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public Text characterBoxNameText;
	public Text characterBoxPriceText;
	public Text characterBoxAddText;

	public GameObject equipBoxRectObject;
	public Text equipBox1NameText;
	public Text equipBox1PriceText;
	public Image equipBox1IconImage;
	public RectTransform equipBox1IconRectTransform;
	public Text equipBox8NameText;
	public Text equipBox8PriceText;
	public Image equipBox8IconImage;
	public RectTransform equipBox8IconRectTransform;
	public Text equipBox8AddText;

	public DiaListItem[] diaListItemList;
	public GoldListItem[] goldListItemList;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshInfo();

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		LobbyCanvas.Home();
	}

	void RefreshInfo()
	{
		ShopBoxTableData characterBoxTableData = TableDataManager.instance.FindShopBoxTableData("CharacterBox");
		if (characterBoxTableData != null)
		{
			characterBoxNameText.SetLocalizedText(UIString.instance.GetString(characterBoxTableData.boxName));
			characterBoxPriceText.text = characterBoxTableData.requiredDiamond.ToString("N0");
			characterBoxAddText.SetLocalizedText(UIString.instance.GetString(characterBoxTableData.addText));
			_characterBoxPrice = characterBoxTableData.requiredDiamond;

		}

		equipBoxRectObject.SetActive(ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace));
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace))
		{
			ShopBoxTableData equipBox1TableData = TableDataManager.instance.FindShopBoxTableData("EquipmentBox1");
			if (equipBox1TableData != null)
			{
				equipBox1NameText.SetLocalizedText(UIString.instance.GetString(equipBox1TableData.boxName));
				equipBox1PriceText.text = equipBox1TableData.requiredDiamond.ToString("N0");
				_equipBox1Price = equipBox1TableData.requiredDiamond;
			}

			ShopBoxTableData equipBox8TableData = TableDataManager.instance.FindShopBoxTableData("EquipmentBox8");
			if (equipBox8TableData != null)
			{
				equipBox8NameText.SetLocalizedText(UIString.instance.GetString(equipBox8TableData.boxName));
				equipBox8PriceText.text = equipBox8TableData.requiredDiamond.ToString("N0");
				equipBox8AddText.text = UIString.instance.GetString(equipBox8TableData.addText);
				_equipBox8Price = equipBox8TableData.requiredDiamond;
			}
		}

		for (int i = 0; i < diaListItemList.Length; ++i)
		{
			if (i >= TableDataManager.instance.shopDiamondTable.dataArray.Length)
				break;
			diaListItemList[i].SetInfo(TableDataManager.instance.shopDiamondTable.dataArray[i]);
		}

		for (int i = 0; i < goldListItemList.Length; ++i)
		{
			if (i >= TableDataManager.instance.shopGoldTable.dataArray.Length)
				break;
			goldListItemList[i].SetInfo(TableDataManager.instance.shopGoldTable.dataArray[i]);
		}
	}

	int _characterBoxPrice;
	public void OnClickCharacterBox()
	{
		// 오리진이나 장비 뽑기와 달리 연속뽑기가 있다.
		// 첫번째 연출에서는 뽑기상자를 터치해서 열지만 두번째부터는 자동으로 패킷 보내면서 굴려져야한다.
		if (CurrencyData.instance.dia < _characterBoxPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxConfirmCanvas", () => {
			CharacterBoxConfirmCanvas.instance.RefreshInfo(_characterBoxPrice, characterBoxNameText.text, characterBoxAddText.text);
		});
	}

	
	int _equipBox1Price;
	public void OnClickEquipBox1()
	{
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _equipBox1Price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("EquipBoxConfirmCanvas", () => {
			EquipBoxConfirmCanvas.instance.ShowCanvas(true, true, _equipBox1Price, equipBox1NameText.text, "", equipBox1IconImage.sprite, equipBox1IconRectTransform.anchoredPosition, equipBox1IconRectTransform.sizeDelta);
		});
	}

	int _equipBox8Price;
	public void OnClickEquipBox8()
	{
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _equipBox8Price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("EquipBoxConfirmCanvas", () => {
			EquipBoxConfirmCanvas.instance.ShowCanvas(true, false, _equipBox8Price, equipBox8NameText.text, equipBox8AddText.text, equipBox8IconImage.sprite, equipBox8IconRectTransform.anchoredPosition, equipBox8IconRectTransform.sizeDelta);
		});
	}
}