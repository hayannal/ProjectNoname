using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBoxConfirmCanvas : MonoBehaviour
{
	public Slider repeatCountSlider;
	public Text repeatCountText;
	public Text priceText;

	int _priceOnce;
	void OnEnable()
	{
		int characterBoxPrice = 50;
		_priceOnce = characterBoxPrice;

		int maxCount = CurrencyData.instance.dia / characterBoxPrice;
		repeatCountSlider.minValue = 1.0f;
		repeatCountSlider.maxValue = Mathf.Min(maxCount, 5);
		repeatCountSlider.value = 1.0f;
		OnValueChangedRepeatCount(1.0f);
	}

	public void OnValueChangedRepeatCount(float value)
	{
		int count = Mathf.RoundToInt(value);
		repeatCountText.text = count.ToString();
		int totalPrice = _priceOnce * count;
		priceText.text = totalPrice.ToString("N0");

		_repeatRemainCount = count;
	}

	DropProcessor _cachedDropProcessor;
	int _repeatRemainCount;
	public void OnClickButton()
	{
		// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflr", "", true, true);
		PlayFabApiManager.instance.RequestCharacterBox(_priceOnce, OnRecvCharacterBox);

		gameObject.SetActive(false);
	}

	void OnRecvCharacterBox(bool serverFailure)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

		// 최초 1회는 굴린거니까 1을 차감해둔다.
		_repeatRemainCount -= 1;

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// repeatRemainCount를 0으로 보내면 오리진 박스처럼 한번 굴려진 결과가 바로 결과창에 보이게 된다.
			// 하지만 이 값을 1 이상으로 보내면 내부적으로 n회 돌린 후 누적해서 보여주게 된다.
			RandomBoxScreenCanvas.instance.SetInfo(_cachedDropProcessor, false, _repeatRemainCount, () =>
			{
				// 결과창은 각 패킷이 자신의 Response에 맞춰서 보여줘야한다.
				// 결과창을 닫을때 RandomBoxScreenCanvas도 같이 닫아주면 알아서 시작점인 CashShopCanvas로 돌아오게 될거다.
				UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxResultCanvas", () =>
				{
					CharacterBoxResultCanvas.instance.RefreshInfo(false);
				});
			});
		});
	}
}