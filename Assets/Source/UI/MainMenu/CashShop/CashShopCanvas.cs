﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public GameObject iapInitializeFailedRectObject;

	public LevelPackageInfo levelPackageInfo;
	public DailyPackageInfo dailyPackageInfo;
	public DailyShopMainInfo dailyShopMainInfo;
	public DailyShopMajorInfo dailyShopMajorInfo;
	public DailyShopMinorInfo dailyShopMinorInfo;

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

	public GameObject equipBox45GroupObject;
	public Text equipBox45NameText;
	public Text equipBox45PriceText;
	public Image equipBox45IconImage;
	public RectTransform equipBox45IconRectTransform;
	public Text equipBox45AddText;

	public GameObject dailyShopChaosInfo;
	public GameObject dailyShopChaosCountRootObject;
	public Text dailyShopChaosCountText;

	public GameObject diaRectObject;
	public DiaListItem[] diaListItemList;
	public GoldListItem[] goldListItemList;

	public GameObject returnScrollRectObject;
	public ReturnScrollItem[] returnScrollItemList;

	public GameObject termsGroupObject;
	public GameObject emptyTermsGroupObject;

	void Awake()
	{
		instance = this;
	}

	float _canvasMatchWidthOrHeightSize;
	float _lineLengthRatio;
	public float lineLengthRatio { get { return _lineLengthRatio; } }
	void Start()
	{
		// 캐시샵이 열리고나서부터는 직접 IAP Button에서 결과 처리를 하면 된다. 그러니 Listener 꺼둔다.
		IAPListenerWrapper.instance.EnableListener(false);

		CanvasScaler parentCanvasScaler = GetComponentInParent<CanvasScaler>();
		if (parentCanvasScaler == null)
			return;

		if (parentCanvasScaler.matchWidthOrHeight == 0.0f)
		{
			_canvasMatchWidthOrHeightSize = parentCanvasScaler.referenceResolution.x;
			_lineLengthRatio = _canvasMatchWidthOrHeightSize / Screen.width;
		}
		else
		{
			_canvasMatchWidthOrHeightSize = parentCanvasScaler.referenceResolution.y;
			_lineLengthRatio = _canvasMatchWidthOrHeightSize / Screen.height;
		}
	}

	void OnEnable()
	{
		RefreshInfo();

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		termsGroupObject.SetActive(OptionManager.instance.language == "KOR");
		emptyTermsGroupObject.SetActive(OptionManager.instance.language != "KOR");

		// 자동 복구 코드 호출
		CheckIAPListener();
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

	void CheckIAPListener()
	{
		// 초기화가 안되어도 캐시샵이 열리는 구조로 바꾸면서 체크
		if (CodelessIAPStoreListener.initializationComplete == false)
			return;

		if (IAPListenerWrapper.instance.pendingProduct == null)
			return;

		Product pendingProduct = IAPListenerWrapper.instance.pendingProduct;
		Debug.LogFormat("Check IAPListener pending product id : {0}", pendingProduct.definition.id);
		//Debug.LogFormat("IAPListener failed product storeSpecificId : {0}", failedProduct.definition.storeSpecificId);

		// 완료되지 않은 구매상품의 아이디에 따라 뭘 진행시킬지 판단해야한다.
		// 레벨패키지인지 확인
		for (int i = 0; i < TableDataManager.instance.shopLevelPackageTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.shopLevelPackageTable.dataArray[i].serverItemId == pendingProduct.definition.id)
			{
				levelPackageInfo.RetryPurchase(pendingProduct, TableDataManager.instance.shopLevelPackageTable.dataArray[i]);
				break;
			}
		}

		// 데일리패키지인지 확인
		for (int i = 0; i < TableDataManager.instance.shopDailyDiamondTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.shopDailyDiamondTable.dataArray[i].serverItemId == pendingProduct.definition.id)
			{
				dailyPackageInfo.RetryPurchase(pendingProduct);
				break;
			}
		}

		// 다이아박스인지 확인
		for (int i = 0; i < diaListItemList.Length; ++i)
		{
			if (diaListItemList[i].serverItemId == pendingProduct.definition.id)
			{
				diaListItemList[i].RetryPurchase(pendingProduct);
				break;
			}
		}

		// 귀환 주문서인지 확인
		for (int i = 0; i < returnScrollItemList.Length; ++i)
		{
			if (returnScrollItemList[i].serverItemId == pendingProduct.definition.id)
			{
				returnScrollItemList[i].RetryPurchase(pendingProduct);
				break;
			}
		}
	}

	public void RefreshDailyShopInfo()
	{
		dailyShopMainInfo.RefreshInfo();
		dailyShopMajorInfo.RefreshInfo();
		dailyShopMinorInfo.RefreshInfo();
	}

	void RefreshInfo()
	{
		iapInitializeFailedRectObject.SetActive(!CodelessIAPStoreListener.initializationComplete);

		if (CodelessIAPStoreListener.initializationComplete)
			levelPackageInfo.RefreshInfo();
		else
			levelPackageInfo.gameObject.SetActive(false);

		if (CodelessIAPStoreListener.initializationComplete)
			dailyPackageInfo.RefreshInfo();
		dailyPackageInfo.gameObject.SetActive(CodelessIAPStoreListener.initializationComplete);
		RefreshDailyShopInfo();

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

		equipBox45GroupObject.SetActive(false);
		ShopBoxTableData equipBox45TableData = TableDataManager.instance.FindShopBoxTableData("EquipmentBox45");
		if (equipBox45TableData != null)
		{
			equipBox45GroupObject.SetActive(CurrencyData.instance.dia >= equipBox45TableData.requiredDiamond && ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace));
			if (equipBox45GroupObject.activeSelf)
			{
				equipBox45NameText.SetLocalizedText(UIString.instance.GetString(equipBox45TableData.boxName));
				equipBox45PriceText.text = equipBox45TableData.requiredDiamond.ToString("N0");
				equipBox45AddText.text = UIString.instance.GetString(equipBox45TableData.addText);
				_equipBox45Price = equipBox45TableData.requiredDiamond;
			}
		}

		dailyShopChaosInfo.SetActive(PlayerData.instance.chaosModeOpened);

		if (CodelessIAPStoreListener.initializationComplete)
		{
			for (int i = 0; i < diaListItemList.Length; ++i)
			{
				if (i >= TableDataManager.instance.shopDiamondTable.dataArray.Length)
					break;
				diaListItemList[i].SetInfo(TableDataManager.instance.shopDiamondTable.dataArray[i]);
			}
		}
		diaRectObject.SetActive(CodelessIAPStoreListener.initializationComplete);

		for (int i = 0; i < goldListItemList.Length; ++i)
		{
			if (i >= TableDataManager.instance.shopGoldTable.dataArray.Length)
				break;
			goldListItemList[i].SetInfo(TableDataManager.instance.shopGoldTable.dataArray[i]);
		}

		bool showReturnScroll = (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.ReturnScroll) && CodelessIAPStoreListener.initializationComplete);
		if (showReturnScroll)
		{
			for (int i = 0; i < returnScrollItemList.Length; ++i)
			{
				if (i >= TableDataManager.instance.shopReturnScrollTable.dataArray.Length)
					break;
				returnScrollItemList[i].SetInfo(TableDataManager.instance.shopReturnScrollTable.dataArray[i]);
			}
		}
		returnScrollRectObject.SetActive(showReturnScroll);
	}

	int _characterBoxPrice;
	public void OnClickCharacterBox()
	{
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

		UIInstanceManager.instance.ShowCanvasAsync("EquipBoxConfirmCanvas", () => {
			EquipBoxConfirmCanvas.instance.ShowCanvas(true, 1, _equipBox1Price, equipBox1NameText.text, "", equipBox1IconImage.sprite, equipBox1IconRectTransform.anchoredPosition, equipBox1IconRectTransform.sizeDelta);
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

		UIInstanceManager.instance.ShowCanvasAsync("EquipBoxConfirmCanvas", () => {
			EquipBoxConfirmCanvas.instance.ShowCanvas(true, 8, _equipBox8Price, equipBox8NameText.text, equipBox8AddText.text, equipBox8IconImage.sprite, equipBox8IconRectTransform.anchoredPosition, equipBox8IconRectTransform.sizeDelta);
		});
	}

	int _equipBox45Price;
	public void OnClickEquipBox45()
	{
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("EquipBoxConfirmCanvas", () => {
			EquipBoxConfirmCanvas.instance.ShowCanvas(true, 45, _equipBox45Price, equipBox45NameText.text, equipBox45AddText.text, equipBox45IconImage.sprite, equipBox45IconRectTransform.anchoredPosition, equipBox45IconRectTransform.sizeDelta);
		});
	}
}