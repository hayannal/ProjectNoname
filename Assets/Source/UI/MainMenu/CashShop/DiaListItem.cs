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
		RequestServerPacket(product);
	}

	void RequestServerPacket(Product product)
	{
#if UNITY_ANDROID
		//Debug.LogFormat("PurchaseComplete. isoCurrencyCode = {0} / localizedPrice = {1}", product.metadata.isoCurrencyCode, product.metadata.localizedPrice);
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);

		// 플레이팹은 센트 단위로 시작하기 때문에 100을 곱해서 넘기는게 맞는데 한국돈 결제일때도 * 100 해서 보내야하는지 궁금해서 테스트 해봤다.
		// 0을 보냈더니 플레이어 현금 구매 Stream이 뜨지 않는다.(player_realmoney_purchase 이벤트)
		// 함수 설명에는 필수 인자가 아니었는데 0보내면 인식을 안하게 내부적으로 되어있는건가 싶다.
		// 그래서 곱하기 100을 안하고 그냥 보내봤더니 1200원 KRW를 샀는데 12원 KRW를 산거처럼 처리된다. 즉 USD로 사든 KRW로 사든 * 100은 무조건 해서 보내야한다.
		PlayFabApiManager.instance.RequestValidateDiaBox(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature,
			_shopDiamondTableData.buyingGems, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidateDiaBox(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, _shopDiamondTableData.buyingGems, () =>
#endif
		{
			CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
			IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);
			DropDiaBox();
		}, (error) =>
		{
			// 거의 그럴일은 없겠지만 서버에서 영수증 처리 후 오는 패킷을 받지 못해서 ConfirmPendingPurchase하지 못했다면
			// 다음번 재시작 후 클라이언트는 아직 미완료된줄 알고 재구매 처리를 할텐데
			// 서버에 보내보니 이미 영수증을 사용했다고 뜬다면 이쪽으로 오게 된다.
			// 이럴땐 이미 디비에는 구매했던 템들이 다 들어있는 상황일테니 ConfirmPendingPurchase 처리를 해주면 된다.
			//
			// 오히려 여기 더 자주 들어올만한 상황은 영수증 패킷 조작해서 보내는 악성 유저들일거다.
			// 그러니 안내메세지 처리같은거 없이 그냥 Confirm처리 하고 시작화면으로 보내도록 한다.
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
			// 미처리된 상품이 있는걸 감지하고 캐시샵에 들어오면 복구할거냐는 창을 띄우는데
			// 이때 No를 누르고 직접 구매했던 상품을 눌러서 구글결제 코드를 작동시키면 이미 구입한 상품이라는 오류 메세지를 보여주고 이걸 닫으면
			// OnPurchaseFailed 를 PurchaseFailureReason.DuplicateTransaction 인자와 호출함과 동시에
			// 곧바로 OnPurchaseComplete 함수도 호출해서 어떤 상품을 구매했었는지 보내온다.
			// 즉 Failed함수와 Complete함수가 동시에 실행되는 것.
			// 예전 IAP 버전초기때는 이 Failed함수만 호출되었던거 같은데 이렇게 Complete도 오다보니 굳이 여기서 예외처리를 할 필요가 없게 되었다.
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