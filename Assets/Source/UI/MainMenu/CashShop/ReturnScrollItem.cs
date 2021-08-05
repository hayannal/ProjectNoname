using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class ReturnScrollItem : MonoBehaviour
{
	public Text buyingGoldText;
	public Text buyingReturnScrollText;
	public Text priceText;

	public Button iapBridgeButton;

	public string serverItemId
	{
		get
		{
			if (_shopReturnScrollTableData != null)
				return _shopReturnScrollTableData.serverItemId;
			return "";
		}
	}
	ShopReturnScrollTableData _shopReturnScrollTableData;
	public void SetInfo(ShopReturnScrollTableData shopReturnScrollTableData)
	{
		_shopReturnScrollTableData = shopReturnScrollTableData;

		int goldAmount = _shopReturnScrollTableData.buyingGold;
		int scrollAmount = _shopReturnScrollTableData.buyingReturnScrolls;
		if (buyingGoldText != null)
			buyingGoldText.text = goldAmount.ToString("N0");
		if (buyingReturnScrollText != null)
			buyingReturnScrollText.text = scrollAmount.ToString("N0");

		Product product = CodelessIAPStoreListener.Instance.GetProduct(_shopReturnScrollTableData.serverItemId);
		if (product != null && product.metadata != null && product.metadata.localizedPrice > 0)
			priceText.text = product.metadata.localizedPriceString;
		else
		{
			if (Application.systemLanguage == SystemLanguage.Korean)
				priceText.text = string.Format("{0}{1:N0}", BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"), _shopReturnScrollTableData.kor);
			else
				priceText.text = string.Format("$ {0:0.##}", _shopReturnScrollTableData.eng);
		}
	}

	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ReturnScrollInfoCanvas", null);
	}

	public void OnClickButton()
	{
		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}

	public void OnResponse()
	{
		// 인풋 차단
		WaitingNetworkCanvas.Show(true);

		// 멀리 숨겨둔 IAP 버튼을 호출해서 결제 진행
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
		PlayFabApiManager.instance.RequestValidateReturnScroll(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature,
			_shopReturnScrollTableData.buyingReturnScrolls, _shopReturnScrollTableData.buyingGold, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidateReturnScroll(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, _shopReturnScrollTableData.buyingReturnScrolls, _shopReturnScrollTableData.buyingGold, () =>
#endif
		{
			CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
			IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);

			DropReturnScrollBox();
		}, (error) =>
		{
			if (error.Error == PlayFab.PlayFabErrorCode.ReceiptAlreadyUsed)
			{
				CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
				IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);
			}
		});
	}

	void DropReturnScrollBox()
	{
		// 연출
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출을 처리해도 될 순간까지 오면 WaitingNetworkCanvas 켜놨던거 하이드.
			WaitingNetworkCanvas.Show(false);

			bool bigBox = (_shopReturnScrollTableData.buyingGold > 0);
			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, bigBox ? "ReturnScrolls" : "ReturnScroll", "", true, true);
			if (bigBox) dropProcessor.AdjustDropRange(3.7f);
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Gold, dropProcessor, 0, 0, () =>
			{
				DropManager.instance.ClearLobbyDropInfo();
				CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

				UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
				{
					CurrencyBoxResultCanvas.instance.RefreshInfo(_shopReturnScrollTableData.buyingGold, 0, _shopReturnScrollTableData.buyingReturnScrolls);
				});
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
			WaitingNetworkCanvas.Show(true);
			RequestServerPacket(product);
		}
		else
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_PurchaseFailure"), 2.0f);
			Debug.LogFormat("PurchaseFailed reason {0}", reason.ToString());
		}
	}

	public void RetryPurchase(Product product)
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);
			RequestServerPacket(product);
		}, () =>
		{
		}, true);
	}
}