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
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflrflr", "", true, true);
		_cachedDropProcessor.AdjustDropRange(3.7f);
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
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Character, _cachedDropProcessor, _repeatRemainCount, () =>
			{
				OnCompleteRandomBoxScreen(DropManager.instance.GetGrantCharacterInfo(), DropManager.instance.GetLimitBreakPointInfo(), OnResult);
			});
		});
	}




	public static void OnCompleteRandomBoxScreen(List<string> listGrantInfo, List<DropManager.CharacterLbpRequest> listLbpInfo, System.Action resultAction)
	{
		if (listGrantInfo.Count + listLbpInfo.Count > 0)
		{
			_listGrantInfo = listGrantInfo;
			_listLbpInfo = listLbpInfo;
			_resultAction = resultAction;

			UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxShowCanvas", () =>
			{
				// 여러개 있을거 대비해서 순차적으로 넣어야한다.
				_grant = listGrantInfo.Count > 0;
				_index = 0;
				CharacterBoxShowCanvas.instance.ShowCanvas(_grant ? listGrantInfo[0] : listLbpInfo[0].actorId, OnConfirmCharacterShow);
			});
		}
		else
		{
			if (resultAction != null)
				resultAction();
		}
	}

	// 임시로 들고있다가 연출 후 바로 null로 버린다. 복사없이 레퍼런스만 들고있다가 버리는거다.
	static List<string> _listGrantInfo;
	static List<DropManager.CharacterLbpRequest> _listLbpInfo;
	static bool _grant;
	static int _index;
	static System.Action _resultAction;
	static void OnConfirmCharacterShow()
	{
		++_index;
		if (_grant)
		{
			if (_index < _listGrantInfo.Count)
				CharacterBoxShowCanvas.instance.ShowCanvas(_listGrantInfo[_index], OnConfirmCharacterShow);
			else
			{
				_grant = false;
				_index = 0;
			}
		}
		if (_grant == false)
		{
			if (_index < _listLbpInfo.Count)
				CharacterBoxShowCanvas.instance.ShowCanvas(_listLbpInfo[_index].actorId, OnConfirmCharacterShow);
			else
			{
				_listGrantInfo = null;
				_listLbpInfo = null;

				if (_resultAction != null)
					_resultAction();
				_resultAction = null;
			}
		}
	}

	public static void OnResult()
	{
		// 결과창은 각 패킷이 자신의 Response에 맞춰서 보여줘야한다.
		// 결과창을 닫을때 RandomBoxScreenCanvas도 같이 닫아주면 알아서 시작점인 CashShopCanvas로 돌아오게 될거다.
		UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxResultCanvas", () =>
		{
			// 여기서 꺼야 제일 자연스럽다. 결과창이 로딩되서 보여지는 동시에 Show모드에서 돌아온다.
			if (CharacterBoxShowCanvas.instance != null && CharacterBoxShowCanvas.instance.gameObject.activeSelf)
				CharacterBoxShowCanvas.instance.gameObject.SetActive(false);

			CharacterBoxResultCanvas.instance.RefreshInfo();
		});
	}
}