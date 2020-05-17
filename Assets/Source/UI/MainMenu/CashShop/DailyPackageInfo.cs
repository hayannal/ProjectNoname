using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DailyPackageInfo : MonoBehaviour
{
	public Text nameText;
	public Text dayCountText;
	public Text buyingDiaText;
	public Text dailyDiaText;
	public DOTweenAnimation dailyDiaTweenAnimation;
	public Text priceText;
	public RectTransform priceTextTransform;
	public Text wonText;

	public Text addText;
	public GameObject remainDayTextObject;
	public Text remainDayText;
	public GameObject plusButtonObject;

	public Text receiveText;
	public DOTweenAnimation receiveTextTweenAnimation;
	public Text completeText;
	public Text remainTimeText;

	bool _started = false;
	void Start()
	{
		_started = true;
	}

	bool _reserveAnimation;
	void Update()
	{
		if (_reserveAnimation)
		{
			dailyDiaTweenAnimation.DORestart();
			receiveTextTweenAnimation.DORestart();
			_reserveAnimation = false;
		}

		UpdateRemainTime();
		UpdateRefresh();
	}

	ShopDailyDiamondTableData _shopDailyDiamondTableData;
	public void RefreshInfo()
	{
		_shopDailyDiamondTableData = TableDataManager.instance.shopDailyDiamondTable.dataArray[0];
		nameText.SetLocalizedText(UIString.instance.GetString(_shopDailyDiamondTableData.boxName));
		dayCountText.text = _shopDailyDiamondTableData.dailyCount.ToString();
		buyingDiaText.text = _shopDailyDiamondTableData.buyingGems.ToString("N0");
		dailyDiaText.text = _shopDailyDiamondTableData.GemsDailyGems.ToString("N0");
		
		// 서버에서 상태값을 받아와서 비교해야한다.
		if (CurrencyData.instance.dailyDiaRemainCount > 0)
		{
			priceTextTransform.gameObject.SetActive(false);
			addText.gameObject.SetActive(false);
			remainDayTextObject.SetActive(true);
			remainDayText.text = CurrencyData.instance.dailyDiaRemainCount.ToString();
			plusButtonObject.SetActive(CurrencyData.instance.dailyDiaRemainCount <= 3);

			// 이미 오늘자 보상을 받았는지 판단해야한다.
			if (PlayerData.instance.sharedDailyPackageOpened)
			{
				dailyDiaTweenAnimation.DOPause();
				receiveTextTweenAnimation.DOPause();
				receiveText.gameObject.SetActive(false);
				completeText.gameObject.SetActive(true);
				remainTimeText.gameObject.SetActive(true);
				_nextResetDateTime = PlayerData.instance.dailyPackageResetTime;
				_needUpdate = true;
			}
			else
			{
				if (_started)
				{
					dailyDiaTweenAnimation.DORestart();
					receiveTextTweenAnimation.DORestart();
				}
				else
					_reserveAnimation = true;
				receiveText.gameObject.SetActive(true);
				completeText.gameObject.SetActive(false);
				remainTimeText.gameObject.SetActive(false);
				_needUpdate = false;
			}
		}
		else
		{
			dailyDiaTweenAnimation.DOPause();
			receiveTextTweenAnimation.DOPause();
			priceTextTransform.gameObject.SetActive(true);
			bool kor = (OptionManager.instance.language == "KOR");
			priceTextTransform.anchoredPosition = new Vector2(kor ? 10.0f : 0.0f, 0.0f);
			wonText.gameObject.SetActive(kor);
			if (kor)
			{
				priceText.text = _shopDailyDiamondTableData.kor.ToString("N0");
				wonText.SetLocalizedText(BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"));
			}
			else
			{
				priceText.text = string.Format("$ {0:0.##}", _shopDailyDiamondTableData.eng);
				wonText.gameObject.SetActive(false);
			}

			addText.gameObject.SetActive(true);
			addText.SetLocalizedText(UIString.instance.GetString(_shopDailyDiamondTableData.addText));
			remainDayTextObject.SetActive(false);

			receiveText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(false);
			remainTimeText.gameObject.SetActive(false);
			_needUpdate = false;
		}
	}

	DateTime _nextResetDateTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (PlayerData.instance.sharedDailyPackageOpened == false)
			return;
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _nextResetDateTime)
		{
			TimeSpan remainTime = _nextResetDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			// 일퀘과 달리 패킷 없이 클라가 선처리 하기로 했으니 PlayerData쪽에서 sharedDailyPackageOpened 값을 false로 바꿔두길 기다렸다가 갱신한다.
			// 시간을 변조했다면 받을 수 있는거처럼 보이게 될거다.
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (PlayerData.instance.sharedDailyPackageOpened == false)
		{
			RefreshInfo();
			_needRefresh = false;
		}
	}

	public void OnClickButton()
	{
		// 버튼을 누를땐 세가지 경우가 있다.
		if (completeText.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyDailyDiamond"), 2.0f);
			return;
		}

		// 받을 수 있는 상태
		if (receiveText.gameObject.activeSelf)
		{
			PlayFabApiManager.instance.RequestReceiveDailyPackage(_shopDailyDiamondTableData.GemsDailyGems, (serverFailure) =>
			{
				if (serverFailure)
				{
					// 뭔가 잘못된건데 응답을 할 필요가 있을까.
				}
				else
				{
					// 연출 및 보상 처리. 일일 다이아니 양이 그렇게 크지는 않을거다. 드랍 아이디도 다르고 그래서 별도로 구현.
					UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
					{
						DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "DailyDiamond", "", true, true);
						//dropProcessor.AdjustDropRange(3.7f);
						RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Dia1_3, dropProcessor, 0, () =>
						{
							// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
							DropManager.instance.ClearLobbyDropInfo();
							CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

							// 결과로는 공용 재화 획득창을 띄워준다.
							UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
							{
								CurrencyBoxResultCanvas.instance.RefreshInfo(0, _shopDailyDiamondTableData.GemsDailyGems, false, true);
							});
						});
					});
				}
			});
			return;
		}

		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetwork(OnResponse);
	}

	public void OnResponse()
	{
		// 인풋 차단
		WaitingNetworkCanvas.Show(true);

		// 원래라면 IAP 결제를 진행해야하는데 차후에 붙이기로 했으니 성공했다고 가정하고 Validate패킷 대신 일반 구매 패킷으로 처리해본다.
		PlayFabApiManager.instance.RequestValidateDailyPackage(_shopDailyDiamondTableData.serverItemId, _shopDailyDiamondTableData.dailyCount, _shopDailyDiamondTableData.buyingGems, () =>
		{
			// 
			//ConfirmPurchase

			// 연출
			DropDailyPackage();
		});
	}

	DropProcessor _cachedDropProcessor;
	void DropDailyPackage()
	{
		// 일일 다이아 패키지에는 특이한게 하나 있는데
		// 구매 즉시 지급되는 다이아 큰 덩이와 매일 받을 수 있는 다이아가 동시에 들어있다는 점 때문에 첫 구매날에는 두번 상자를 열게된다.
		// 유저한테 느끼는 체감이 좋지 않아서
		// 첫 구매시 첫날 얻을 수 있는 다이아를 한번에 주려다보니 이런 특이한 로직이 필요하게 되었다.
		// 
		// 대신 이건 구매 후 처리하지 못하고 튕겼을땐 복구로직 같은건 따로 없으며, 바로 지급되는 보석만 인벤에 이미 들어있는 상태일거고
		// 첫째날 보상은 유저가 받기 버튼을 직접 눌러서 받게 될거다.

		// 그런데 하나 예외상황이 있는게 이미 구매해서 마지막 날짜꺼까지 받은 상태에서
		// 재구매하면 이미 디비에는 그날 받은거로 되어있어서
		// 첫째날 보상을 지급할 수 없게 되버린다.
		// 그러니 항상 보내는게 아니라 오늘 데일리 보상을 받을 수 있는지를 확인하고 보내야한다.

		_receivableFirstDay = false;
		if (CurrencyData.instance.dailyDiaRemainCount > 0 && PlayerData.instance.sharedDailyPackageOpened == false)
			_receivableFirstDay = true;

		if (_receivableFirstDay)
		{
			PlayFabApiManager.instance.RequestReceiveDailyPackage(_shopDailyDiamondTableData.GemsDailyGems, OnRecvDailyPackage);
			return;
		}

		// 첫째날 처리가 필요없다면 그냥 패키지내에 큰 덩이 하나만 받는 연출 보여주면 된다.
		OnRecvDailyPackage(false);
	}

	bool _receivableFirstDay = false;
	void OnRecvDailyPackage(bool serverFailure)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			WaitingNetworkCanvas.Show(false);

			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "ShopDiamond", "", true, true);
			dropProcessor.AdjustDropRange(3.7f);
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Dia4_6, dropProcessor, 0, () =>
			{
				// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
				DropManager.instance.ClearLobbyDropInfo();
				CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

				// 결과로는 공용 재화 획득창을 띄워준다.
				UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
				{
					int addDia = _shopDailyDiamondTableData.buyingGems;
					if (_receivableFirstDay) addDia += _shopDailyDiamondTableData.GemsDailyGems;
					CurrencyBoxResultCanvas.instance.RefreshInfo(0, addDia, _receivableFirstDay);
				});
			});
		});
	}

	public void OnClickPlusButton()
	{
		// 여기선 강제로 추가만 하면 된다.
		PlayFabApiManager.instance.RequestNetwork(OnResponse);
	}
}