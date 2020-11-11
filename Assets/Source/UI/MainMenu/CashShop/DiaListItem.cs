using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class DiaListItem : MonoBehaviour
{
	public Text amountText;
	public Text priceText;
	public RectTransform priceTextTransform;
	public Text wonText;
	public GameObject addObject;
	public Text addText;
	public Button iapBridgeButton;

	public string serverItemId
	{
		get
		{
			if (_shopDiamondTableData != null)
				return _shopDiamondTableData.serverItemId;
			return "";
		}
	}
	ShopDiamondTableData _shopDiamondTableData;
	public void SetInfo(ShopDiamondTableData shopDiamondTableData)
	{
		_shopDiamondTableData = shopDiamondTableData;

		amountText.text = shopDiamondTableData.buyingGems.ToString("N0");
		if (Application.systemLanguage == SystemLanguage.Korean)
		{
			priceTextTransform.anchoredPosition = new Vector2(10.0f, 0.0f);
			priceText.text = shopDiamondTableData.kor.ToString("N0");
			wonText.gameObject.SetActive(true);
			wonText.SetLocalizedText(BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"));
		}
		else
		{
			priceTextTransform.anchoredPosition = Vector2.zero;
			priceText.text = string.Format("$ {0:0.##}", shopDiamondTableData.eng);
			wonText.gameObject.SetActive(false);
		}

		bool useAdd = (string.IsNullOrEmpty(shopDiamondTableData.addText) == false);
		addObject.SetActive(useAdd);
		if (useAdd)
			addText.SetLocalizedText(UIString.instance.GetString(shopDiamondTableData.addText));
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
		RequestServerPacket(product, false);
	}

	void RequestServerPacket(Product product, bool confirmPending)
	{
#if UNITY_ANDROID
		Debug.LogFormat("PurchaseComplete. isoCurrencyCode = {0} / localizedPrice = {1}", product.metadata.isoCurrencyCode, product.metadata.localizedPrice);

		GooglePurchaseData data = new GooglePurchaseData(product.receipt);

		// 플레이팹은 센트 단위로 시작하기 때문에 100을 곱해서 넘기는게 맞는데 한국돈 결제일때는 얼마로 보내야하는거지? 이렇게 * 100 해도 되는건가?
		PlayFabApiManager.instance.RequestValidateDiaBox(product.metadata.isoCurrencyCode, (uint)(product.metadata.localizedPrice * 100), data.inAppPurchaseData, data.inAppDataSignature,
			_shopDiamondTableData.buyingGems, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);

		// 플레이팹은 센트 단위로 시작하기 때문에 100을 곱해서 넘기는게 맞는데 한국돈 결제일때는 얼마로 보내야하는거지? 이렇게 * 100 해도 되는건가?
		PlayFabApiManager.instance.RequestValidateDiaBox(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, _shopDiamondTableData.buyingGems, () =>
#endif
		{
			DropDiaBox();
			if (confirmPending)
			{
				CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
				IAPListenerWrapper.instance.ConfirmPending(product);
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
			RandomBoxScreenCanvas.instance.SetInfo(IsBigDiaBox() ? RandomBoxScreenCanvas.eBoxType.Dia4_6 : RandomBoxScreenCanvas.eBoxType.Dia1_3, dropProcessor, 0, 0, () =>
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

	bool IsBigDiaBox()
	{
		if (_shopDiamondTableData == null)
			return false;
		char lastCharacter = _shopDiamondTableData.diamondPack[_shopDiamondTableData.diamondPack.Length - 1];
		switch (lastCharacter)
		{
			case '4':
			case '5':
			case '6':
				return true;
		}
		return false;
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
	{
		WaitingNetworkCanvas.Show(false);

		if (reason == PurchaseFailureReason.UserCancelled)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_UserCancel"), 2.0f);
		else if (reason == PurchaseFailureReason.DuplicateTransaction)
		{
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
			{
				WaitingNetworkCanvas.Show(true);
				RequestServerPacket(product, true);
			}, () =>
			{
			}, true);
		}
		else
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_PurchaseFailure"), 2.0f);
			Debug.LogFormat("PurchaseFailed reason {0}", reason.ToString());
		}
	}
}