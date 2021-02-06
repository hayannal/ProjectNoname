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
			//if (_shopDiamondTableData != null)
			//	return _shopDiamondTableData.serverItemId;
			return "";
		}
	}
	ShopDiamondTableData _shopDiamondTableData;
	public void SetInfo(ShopDiamondTableData shopDiamondTableData)
	{
		int goldAmount = 5500;
		int scrollAmount = (goldAmount > 0) ? 5 : 1;
		if (buyingGoldText != null)
			buyingGoldText.text = goldAmount.ToString("N0");
		if (buyingReturnScrollText != null)
			buyingReturnScrollText.text = scrollAmount.ToString("N0");

		//Product product = CodelessIAPStoreListener.Instance.GetProduct(_shopDailyDiamondTableData.serverItemId);
		Product product = CodelessIAPStoreListener.Instance.GetProduct("levelbox0");
		if (product != null && product.metadata != null && product.metadata.localizedPrice > 0)
			priceText.text = product.metadata.localizedPriceString;
		else
		{
			if (Application.systemLanguage == SystemLanguage.Korean)
				priceText.text = string.Format("{0}{1:N0}", BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"), 5000);
			else
				priceText.text = string.Format("$ {0:0.##}", 0.99f);
		}
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
			_shopDiamondTableData.buyingGems, 1, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidateReturnScroll(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, _shopDiamondTableData.buyingGems, 1, () =>
#endif
		{
			CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
			IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);

			DropDiaBox();
		}, (error) =>
		{
			if (error.Error == PlayFab.PlayFabErrorCode.ReceiptAlreadyUsed)
			{
				CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
				IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);
			}
		});
	}

	void DropDiaBox()
	{
		// 연출
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출을 처리해도 될 순간까지 오면 WaitingNetworkCanvas 켜놨던거 하이드.
			WaitingNetworkCanvas.Show(false);

			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "ShopDiamond", "", true, true);
			dropProcessor.AdjustDropRange(3.7f);
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Dia4_6, dropProcessor, 0, 0, () =>
			{
				DropManager.instance.ClearLobbyDropInfo();
				CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

				UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
				{
					CurrencyBoxResultCanvas.instance.RefreshInfo(0, _shopDiamondTableData.buyingGems);
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