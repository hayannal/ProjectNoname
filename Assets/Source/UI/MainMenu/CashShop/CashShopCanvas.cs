using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

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

	public DiaListItem[] diaListItemList;
	public GoldListItem[] goldListItemList;

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

	void CheckIAPListener()
	{
		if (IAPListenerWrapper.instance.failedReason != PurchaseFailureReason.DuplicateTransaction || IAPListenerWrapper.instance.failedProduct == null)
		{
			// 결제하다 미처리 된게 아니라면 딱히 처리할 필요가 없어보인다.
			// 나머지 오류는 무시
			return;
		}

		Product failedProduct = IAPListenerWrapper.instance.failedProduct;
		Debug.LogFormat("IAPListener failed product id : {0}", failedProduct.definition.id);
		Debug.LogFormat("IAPListener failed product storeSpecificId : {0}", failedProduct.definition.storeSpecificId);

		// 완료되지 않은 구매상품의 아이디에 따라 뭘 진행시킬지 판단해야한다.
		// 레벨패키지인지 확인
		for (int i = 0; i < TableDataManager.instance.shopLevelPackageTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.shopLevelPackageTable.dataArray[i].serverItemId == failedProduct.definition.id)
			{
				// 사실 여기서 레벨패키지의 현재 상태가 failedProduct와 같은지 다른지 판단할수도 있는데
				// 이걸 다르다고 복구 안해주고 진행중이던 계정을 가져오라 하면 더 문제가 커질 수 있다.
				// 유저가 이 계정을 못찾겠다고 하면서 찾아달라고 문의를 하면 우린 더 많은 일을 해야하기 때문.
				// 그래서 해당 상품으로 셋팅하고 복구를 진행하기로 한다.
				//if (levelPackageInfo.gameObject.activeSelf == false || levelPackageInfo.)
				levelPackageInfo.ForceSetShopLevelPackageTableData(TableDataManager.instance.shopLevelPackageTable.dataArray[i]);
				levelPackageInfo.OnPurchaseFailed(failedProduct, IAPListenerWrapper.instance.failedReason);
				break;
			}
		}

		// 데일리패키지인지 확인
		for (int i = 0; i < TableDataManager.instance.shopDailyDiamondTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.shopDailyDiamondTable.dataArray[i].serverItemId == failedProduct.definition.id)
			{
				// 데일리패키지 역시 레벨패키지와 마찬가지로 이 상품을 구매할 수 있는지 없는지 체크하지 않고 그냥 진행한다.
				dailyPackageInfo.OnPurchaseFailed(failedProduct, IAPListenerWrapper.instance.failedReason);
				break;
			}
		}

		// 다이아박스인지 확인
		DiaListItem failedDiaListItem = null;
		for (int i = 0; i < diaListItemList.Length; ++i)
		{
			if (diaListItemList[i].serverItemId == failedProduct.definition.id)
			{
				failedDiaListItem.OnPurchaseFailed(failedProduct, IAPListenerWrapper.instance.failedReason);
				break;
			}
		}
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

	public void RefreshDailyShopInfo()
	{
		dailyShopMainInfo.RefreshInfo();
		dailyShopMajorInfo.RefreshInfo();
		dailyShopMinorInfo.RefreshInfo();
	}

	void RefreshInfo()
	{
		levelPackageInfo.RefreshInfo();
		dailyPackageInfo.RefreshInfo();
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

		UIInstanceManager.instance.ShowCanvasAsync("EquipBoxConfirmCanvas", () => {
			EquipBoxConfirmCanvas.instance.ShowCanvas(true, false, _equipBox8Price, equipBox8NameText.text, equipBox8AddText.text, equipBox8IconImage.sprite, equipBox8IconRectTransform.anchoredPosition, equipBox8IconRectTransform.sizeDelta);
		});
	}
}