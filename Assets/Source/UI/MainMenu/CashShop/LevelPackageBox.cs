using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using PlayFab;
using PlayFab.ClientModels;

public class LevelPackageBox : MonoBehaviour
{
	public Transform iconImageRootTransform;
	public Text valueXText;
	public Text priceText;
	public RectTransform priceTextTransform;
	public Text prevPriceText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;
	public Text nameText;

	public Button iapBridgeButton;
	public IAPButton iapButton;

	ShopLevelPackageTableData _shopLevelPackageTableData;
	GameObject _subPrefabObject;
	public void RefreshInfo(ShopLevelPackageTableData shopLevelPackageTableData)
	{
		if (_subPrefabObject != null)
			_subPrefabObject.SetActive(false);
		int imagePrefabIndex = FindImagePrefabIndex(shopLevelPackageTableData.imagePrefab);
		_subPrefabObject = UIInstanceManager.instance.GetCachedObject(CashShopCanvas.instance.levelPackageInfo.levelPackagePrefabList[imagePrefabIndex], iconImageRootTransform);

		valueXText.text = string.Format("{0}x", shopLevelPackageTableData.times);

		Product product = CodelessIAPStoreListener.Instance.GetProduct(shopLevelPackageTableData.serverItemId);
		if (product != null && product.metadata != null && product.metadata.localizedPrice > 0)
		{
			prevPriceText.gameObject.SetActive(false);
			priceText.text = product.metadata.localizedPriceString;
		}
		else
		{
			if (Application.systemLanguage == SystemLanguage.Korean)
			{
				prevPriceText.text = shopLevelPackageTableData.beforeKor.ToString("N0");
				prevPriceText.gameObject.SetActive(shopLevelPackageTableData.beforeKor > 0);
				priceText.text = string.Format("{0}{1:N0}", BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"), shopLevelPackageTableData.kor);
			}
			else
			{
				prevPriceText.text = string.Format("$ {0:0.##}", shopLevelPackageTableData.beforEng);
				prevPriceText.gameObject.SetActive(shopLevelPackageTableData.beforEng > 0.0f);
				priceText.text = string.Format("$ {0:0.##}", shopLevelPackageTableData.eng);
			}
		}
		nameText.SetLocalizedText(UIString.instance.GetString(shopLevelPackageTableData.boxName));

		RefreshLineImage();
		_updateRefreshLineImage = true;
		_shopLevelPackageTableData = shopLevelPackageTableData;

		// 다른 캐시상품들과 달리 프리팹 하나에서 정보를 바꿔가며 내용을 구성하기 때문에 productId에는 최초꺼로 설정되어있다.
		// 이걸 현재에 맞는 상품으로 바꿔주는 절차가 필요하다.
		iapButton.productId = shopLevelPackageTableData.serverItemId;
		gameObject.SetActive(true);
	}

	int FindImagePrefabIndex(string name)
	{
		for (int i = 0; i < CashShopCanvas.instance.levelPackageInfo.levelPackagePrefabList.Length; ++i)
		{
			if (CashShopCanvas.instance.levelPackageInfo.levelPackagePrefabList[i].name == name)
				return i;
		}
		return 0;
	}

	void RefreshLineImage()
	{
		Vector3 diff = rightTopRectTransform.position - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopCanvas.instance.lineLengthRatio);
	}

	bool _updateRefreshLineImage;
	void Update()
	{
		if (_updateRefreshLineImage)
		{
			RefreshLineImage();
			_updateRefreshLineImage = false;
		}
	}

	public void OnClickButton()
	{
		if (CashShopCanvas.instance.levelPackageInfo.standbyUnprocessed)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingInventory"));
			return;
		}

		// 구매할 수 있는 인덱스인지 확인해야한다.
		int firstIndex = -1;
		for (int i = 0; i < TableDataManager.instance.shopLevelPackageTable.dataArray.Length; ++i)
		{
			int researchLevel = TableDataManager.instance.shopLevelPackageTable.dataArray[i].level;
			if (PlayerData.instance.researchLevel < researchLevel)
				continue;
			if (PlayerData.instance.IsPurchasedLevelPackage(researchLevel))
				continue;

			firstIndex = i;
			break;
		}
		if (TableDataManager.instance.shopLevelPackageTable.dataArray[firstIndex].level != _shopLevelPackageTableData.level)
		{
			CashShopCanvas.instance.levelPackageInfo.scrollSnap.GoToPanel(0);
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_CannotBuyFirstProduct"), 2.0f);
			return;
		}

		if ((_shopLevelPackageTableData.buyingEquipKey > 0 || _shopLevelPackageTableData.buyingLegendEquipKey > 0) && TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}

	public void OnResponse()
	{
		// 통신이 되는 상황이면 나머지 스텝을 진행하면 되는데 인풋차단부터 걸어놔야 안전하다.
		WaitingNetworkCanvas.Show(true);

		// 숨겨둔 IAP 버튼을 호출해서 결제 진행
		iapBridgeButton.onClick.Invoke();
	}

	public void OnPurchaseComplete(Product product)
	{
		RequestServerPacket(product);
	}

	void RequestServerPacket(Product product)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidateLevelPackage(product.metadata.isoCurrencyCode, (uint)(product.metadata.localizedPrice * 100), data.inAppPurchaseData, data.inAppDataSignature,
			_shopLevelPackageTableData, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidateLevelPackage(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload,
			_shopLevelPackageTableData, () =>
#endif
			{
				CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
				IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);

				// 여기서부턴 연출을 시작해야한다.
				DropLevelPackage();

			}, (error) =>
			{
				if (error.Error == PlayFab.PlayFabErrorCode.ReceiptAlreadyUsed)
				{
					CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
					IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);
				}
			});
	}

	DropProcessor _cachedDropProcessor;
	public void DropLevelPackage(ShopLevelPackageTableData overrideShopLevelPackageTableData = null)
	{
		if (overrideShopLevelPackageTableData != null)
			_shopLevelPackageTableData = overrideShopLevelPackageTableData;

		// 레벨 패키지에 뭐가 들어있냐에 따라 굴리는 로직이 달라진다.
		// 크게 나누자면 장비가 있고 없고의 차이다.
		// 하지만 둘다한테 필요한게 있는데 바로 레벨패키지 목록 갱신이다.
		List<int> listResult = PlayerData.instance.AddLevelPackage(_shopLevelPackageTableData.level);
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonLevelPackageList = serializer.SerializeObject(listResult);

		if (_shopLevelPackageTableData.buyingEquipKey > 0 || _shopLevelPackageTableData.buyingLegendEquipKey > 0)
		{
			// 이땐 드랍을 굴리는데
			// 특이한 점은 showingDrop말고 실제 드랍이 껴있을테니 그것도 함께 굴려야한다는거다.
			// showingDrop에는 골드 다이아가 들어있을테고 그건 패킷으로 보내지 않고 무시하지만
			// equipKeyDrop에 있는 장비는 실제로 뽑아야할 장비가 들어있으므로 패킷으로 보내서 itemInstanceId를 받아야 클라에서도 템을 생성할 수 있게된다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _shopLevelPackageTableData.showingDrop, _shopLevelPackageTableData.equipKeyDrop, true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);
			if (CheatingListener.detectedCheatTable)
				return;

			// 장비가 있을땐 EquipBox 패킷을 보내며 결과로 오면 드랍연출을 보여준다.
			// 만약 이거하기전에 클라가 강종되면 키가 남아있는채로 끝날거고
			// 다음 재접에서 캐시샵을 열때 미처리된게 있다면서 이 로직을 다시 수행하면 된다.
			// 그리고 레벨패키지 리스트 갱신을 위해 별도 패킷을 또 한번 날릴바엔 장비뽑기에 실어서 보내는게 나아서 추가인자로 넘기기로 한다.
			PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), 0, _shopLevelPackageTableData.buyingEquipKey, _shopLevelPackageTableData.buyingLegendEquipKey, jsonLevelPackageList, OnRecvEquipBox);
		}
		else
		{
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _shopLevelPackageTableData.showingDrop, "", true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);

			// 장비가 없을땐 연출 상자 보여주면 끝이긴 한데 그전에 레벨 패키지를 샀음을 서버에 기록해놔야한다.
			// 사실 이거 하기전에 클라를 종료시키더라도 이미 패키지로 산 재화는 다 들어갔기 때문에 복구같은건 할 필요 없다.
			// 단지 패키지를 한번 더 살 수 있게 되는건데 이건 허용하기로 한다.
			PlayFabApiManager.instance.RequestUpdateLevelPackageList(jsonLevelPackageList, OnRecvComplete);
		}
	}

	void OnRecvComplete()
	{
		OnRandomBoxScreen(null);
	}

	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		// 아이콘의 프리로드를 진행한다.
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

		OnRandomBoxScreen(listGrantItem);
	}

	void OnRandomBoxScreen(List<ItemInstance> listGrantItem)
	{
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			WaitingNetworkCanvas.Show(false);

			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Dia4_6, _cachedDropProcessor, 0, 0, () =>
			{
				// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
				DropManager.instance.ClearLobbyDropInfo();
				CashShopCanvas.instance.currencySmallInfo.RefreshInfo();
				CurrencyData.instance.OnRecvRefillEnergy(_shopLevelPackageTableData.buyingEnergy);

				if (listGrantItem == null)
				{
					// 장비가 들어있지 않은 레벨패키지는 공용 재화창을 사용하고
					UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
					{
						CurrencyBoxResultCanvas.instance.RefreshInfo(_shopLevelPackageTableData.buyingGold, _shopLevelPackageTableData.buyingGems, 0);
					});
				}
				else
				{
					// 장비가 들어있는 레벨패키지는 장비창꺼를 같이 쓴다.
					UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
					{
						EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem, _shopLevelPackageTableData.buyingGold, _shopLevelPackageTableData.buyingGems);
					});
				}
			});
		});
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
	{
		WaitingNetworkCanvas.Show(false);

		if (reason == PurchaseFailureReason.UserCancelled)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_UserCancel"), 2.0f);
		else if (reason == PurchaseFailureReason.DuplicateTransaction)
		{
		}
		else
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_PurchaseFailure"), 2.0f);
			Debug.LogFormat("PurchaseFailed reason {0}", reason.ToString());
		}
	}

	public void RetryPurchase(Product product, ShopLevelPackageTableData shopLevelPackageTableData)
	{
		// 이미 구매했던 상품인지 확인해야한다.
		// IsPurchasedLevelPackage로 검사하는거라 구매하지 않은 먼 레벨의 패키지를 사려고 할수도 있는데(조작같은거로) 이건 허용하기로 한다.
		// 어쨌든 사지 않은걸 사는거니 허용.
		if (PlayerData.instance.IsPurchasedLevelPackage(shopLevelPackageTableData.level))
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingAccount", product.metadata.localizedTitle), null, -1, true);
			return;
		}

		// 레벨패키지는 다른 구매와 달리 인벤토리 체크를 해야한다.
		if ((shopLevelPackageTableData.buyingEquipKey > 0 || shopLevelPackageTableData.buyingLegendEquipKey > 0) && TimeSpaceData.instance.IsInventoryVisualMax())
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingInventory", product.metadata.localizedTitle), null, -1, true);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);

			// 구매 복구시에는 강제로 셋팅해서 처리해야한다.
			_shopLevelPackageTableData = shopLevelPackageTableData;
			RequestServerPacket(product);
		}, () =>
		{
		}, true);
	}
}