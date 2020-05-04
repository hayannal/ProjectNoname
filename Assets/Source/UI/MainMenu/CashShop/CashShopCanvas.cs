using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public Text characterBoxNameText;
	public Text characterBoxPriceText;
	public Text equipBox1NameText;
	public Text equipBox1PriceText;
	public Text equipBox8NameText;
	public Text equipBox8PriceText;

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
		}

		ShopBoxTableData equipBox1TableData = TableDataManager.instance.FindShopBoxTableData("EquipmentBox1");
		if (equipBox1TableData != null)
		{
			equipBox1NameText.SetLocalizedText(UIString.instance.GetString(equipBox1TableData.boxName));
			equipBox1PriceText.text = equipBox1TableData.requiredDiamond.ToString("N0");
		}

		ShopBoxTableData equipBox8TableData = TableDataManager.instance.FindShopBoxTableData("EquipmentBox8");
		if (equipBox8TableData != null)
		{
			equipBox8NameText.SetLocalizedText(UIString.instance.GetString(equipBox8TableData.boxName));
			equipBox8PriceText.text = equipBox8TableData.requiredDiamond.ToString("N0");
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

	public void OnClickCharacterBox()
	{
		// 오리진이나 장비 뽑기와 달리 연속뽑기가 있다.
		// 첫번째 연출에서는 뽑기상자를 터치해서 열지만 두번째부터는 자동으로 패킷 보내면서 굴려져야한다.

		int characterBoxPrice = 50;
		if (CurrencyData.instance.dia < characterBoxPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxConfirmCanvas", null);
	}

	DropProcessor _cachedDropProcessor;
	public void OnClickEquipBox1()
	{
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		int equipBoxPrice = 30;
		if (CurrencyData.instance.dia < equipBoxPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, "confirm", "equip box 1", () =>
		{
			// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wkdql", "", true, true);
			if (CheatingListener.detectedCheatTable)
				return;
			PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), equipBoxPrice, OnRecvEquipBox);
		});
	}

	public void OnClickEquipBox8()
	{
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		int equipBoxPrice = 200;
		if (CurrencyData.instance.dia < equipBoxPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, "confirm", "equip box 8", () =>
		{
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wkdwkdql", "", true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);
			if (CheatingListener.detectedCheatTable)
				return;
			PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), equipBoxPrice, OnRecvEquipBox);
		});
	}

	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		currencySmallInfo.RefreshInfo();

		// 연출은 연출대로 두고
		// 연출 끝나고 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ItemInstance> listGrantItem = null;
		int count = 0;
		if (itemGrantString != "")
		{
			listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
			count = listGrantItem.Count;
			for (int i = 0; i < count; ++i)
			{
				EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
				if (equipTableData == null)
					continue;

				AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
			}
		}

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(count == 1 ? RandomBoxScreenCanvas.eBoxType.Equip1 : RandomBoxScreenCanvas.eBoxType.Equip8, _cachedDropProcessor, 0, () =>
			{
				// 결과창은 각 패킷이 자신의 Response에 맞춰서 보여줘야한다.
				// 여기서는 장비 그리드를 띄운다.
				// 결과창을 닫을때 RandomBoxScreenCanvas도 같이 닫아주면 알아서 시작점인 CashShopCanvas로 돌아오게 될거다.
				UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
				{
					EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem);
				});
			});
		});
	}
}