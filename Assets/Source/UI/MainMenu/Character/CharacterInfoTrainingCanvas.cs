using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class CharacterInfoTrainingCanvas : MonoBehaviour
{
	public static CharacterInfoTrainingCanvas instance;

	public const int TrainingMax = 10000;

	public GameObject needGroupObject;
	public GameObject contentGroupObject;

	public Transform trainingTextTransform;
	public OrbFill percentOrbFill;
	public Text trainingPercentValueText;
	public DOTweenAnimation percentValueTweenAnimation;
	public Text addPercentValueText;
	public Text hpValueText;
	public DOTweenAnimation hpValueTweenAnimation;
	public Text attackValueText;
	public DOTweenAnimation attackValueTweenAnimation;

	public GameObject priceButtonObject;
	public GameObject[] priceTypeObjectList;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect[] priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	public Text remainTimeText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		RefreshInfo(false);
	}

	void Update()
	{
		UpdateRemainTime();
		UpdateRefresh();
		UpdateValueText();
	}

	static Color _percentColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	static Color _fullPercentColor = new Color(0.937f, 0.937f, 0.298f, 1.0f);

	#region Info
	string _actorId;
	float _trainingRatio;
	CharacterData _characterData;
	public void RefreshInfo(bool ignoreResetFillGauge)
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		_actorId = actorId;
		_characterData = characterData;
		if (characterData.limitBreakLevel <= 1)
		{
			needGroupObject.SetActive(true);
			contentGroupObject.SetActive(false);
			return;
		}
		needGroupObject.SetActive(false);
		contentGroupObject.SetActive(true);

		_trainingRatio = (float)characterData.trainingValue / TrainingMax;
		trainingPercentValueText.text = string.Format("{0:0.##}%", _trainingRatio * 100.0f);
		trainingPercentValueText.color = (_trainingRatio < 1.0f) ? _percentColor : _fullPercentColor;
		RefreshStatus(_trainingRatio);

		if (ignoreResetFillGauge == false)
			percentOrbFill.ResetFill();
		percentOrbFill.Fill = _trainingRatio;

		if (_trainingRatio >= 1.0f)
		{
			priceButtonObject.SetActive(false);
			maxButtonObject.SetActive(true);
			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			remainTimeText.gameObject.SetActive(false);
			_needUpdate = false;
			return;
		}

		int price = 0;
		bool disablePrice = false;
		CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
		if (PlayerData.instance.dailyTrainingGoldCompleted == false && PlayerData.instance.dailyTrainingDiaCompleted == false)
		{
			// 매일 1회는 골드 구매다.
			currencyType = CurrencyData.eCurrencyType.Gold;
			price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TrainingGold");
			disablePrice = (CurrencyData.instance.gold < price);
			remainTimeText.gameObject.SetActive(false);
			_needUpdate = false;
		}
		else if (PlayerData.instance.dailyTrainingGoldCompleted && PlayerData.instance.dailyTrainingDiaCompleted == false)
		{
			// 그 다음 1회는 다이아 구매다.
			price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TrainingDiamond");
			disablePrice = (CurrencyData.instance.dia < price);

			remainTimeText.gameObject.SetActive(true);
			remainTimeText.color = Color.white;
			_nextResetDateTime = PlayerData.instance.dailyTrainingGoldResetTime;
			_needUpdate = true;
		}
		else if (PlayerData.instance.dailyTrainingGoldCompleted && PlayerData.instance.dailyTrainingDiaCompleted)
		{
			price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TrainingDiamond");
			disablePrice = true;

			remainTimeText.gameObject.SetActive(true);
			remainTimeText.color = Color.gray;
			_nextResetDateTime = PlayerData.instance.dailyTrainingDiaResetTime;
			_needUpdate = true;
		}
		priceText.text = price.ToString("N0");
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		for (int i = 0; i < priceTypeObjectList.Length; ++i)
		{
			priceTypeObjectList[i].SetActive((int)currencyType == i);
			if ((int)currencyType == i)
				priceGrayscaleEffect[i].enabled = disablePrice;
		}
		priceButtonObject.SetActive(true);
		maxButtonObject.SetActive(false);
	}

	void RefreshStatus(float trainingRatio)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		float currentHpRate = actorTableData.trainingHp * trainingRatio;
		float currentAtkRate = actorTableData.trainingAtk * trainingRatio;
		hpValueText.text = string.Format("+{0:0.##}%", currentHpRate * 100.0f);
		attackValueText.text = string.Format("+{0:0.##}%", currentAtkRate * 100.0f);
	}
	#endregion

	DateTime _nextResetDateTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		// DailyPackageInfo 처리와 비슷하게 간다.
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

		if (PlayerData.instance.dailyTrainingGoldCompleted == false && PlayerData.instance.dailyTrainingDiaCompleted == false)
		{
			RefreshInfo(true);
			_needRefresh = false;
		}
	}

	public void OnClickTrainingTextButton()
	{
		string text = UIString.instance.GetString("GameUI_TrainingMore");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, trainingTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickTrainingButton()
	{
		if (_trainingRatio >= 1.0f)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxTrainingToast"), 2.0f);
			return;
		}

		int price = 0;
		int priceGold = 0;
		int priceDia = 0;
		string stringId = "";
		CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
		if (PlayerData.instance.dailyTrainingGoldCompleted == false && PlayerData.instance.dailyTrainingDiaCompleted == false)
		{
			stringId = "GameUI_ConfirmTraining";
			price = priceGold = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TrainingGold");
			currencyType = CurrencyData.eCurrencyType.Gold;
			if (CurrencyData.instance.gold < priceGold)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
				return;
			}
		}
		else if (PlayerData.instance.dailyTrainingGoldCompleted && PlayerData.instance.dailyTrainingDiaCompleted == false)
		{
			stringId = "GameUI_ConfirmTrainingSecond";
			price = priceDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TrainingDiamond");
			if (CurrencyData.instance.dia < priceDia)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
				return;
			}
		}
		else if (PlayerData.instance.dailyTrainingGoldCompleted && PlayerData.instance.dailyTrainingDiaCompleted)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NoMoreTraining"), 2.0f);
			return;
		}

		int addTrainingPoint = 0;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;
		addTrainingPoint = UnityEngine.Random.Range(actorTableData.trainingMin, actorTableData.trainingMax + 1);
		if (_characterData.trainingValue + addTrainingPoint > TrainingMax)
			addTrainingPoint = TrainingMax - _characterData.trainingValue;
		if (priceDia > 0)
			addTrainingPoint *= BattleInstanceManager.instance.GetCachedGlobalConstantInt("TrainingMulti");

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(stringId), currencyType, price, false, () =>
			{
				PlayFabApiManager.instance.RequestCharacterTraining(_characterData, addTrainingPoint, priceGold, priceDia, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					OnRecvTraining(addTrainingPoint);
				});
			});
		});
	}

	void OnRecvTraining(int addTrainingPoint)
	{
		CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
		Timing.RunCoroutine(TrainingProcess(addTrainingPoint));
	}

	float _addValue = 0.0f;
	IEnumerator<float> TrainingProcess(int addTrainingPoint)
	{
		// 인풋 차단
		CharacterInfoCanvas.instance.inputLockObject.SetActive(true);

		// priceButton도 하이드
		priceButtonObject.SetActive(false);
		remainTimeText.gameObject.SetActive(false);

		float tweenDelay = 0.5f;

		// 올라가는 퍼센트 텍스트
		float targetTrainingRatio = (float)_characterData.trainingValue / TrainingMax;
		_addValue = targetTrainingRatio - _trainingRatio;
		addPercentValueText.text = string.Format("+{0:0.##}%", _addValue * 100.0f);
		addPercentValueText.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.7f);

		// 게이지 애니메이션
		_valueChangeSpeed = -_addValue / valueChangeTime;
		_floatCurrentValue = _addValue;
		_updateValueText = true;
		yield return Timing.WaitForSeconds(valueChangeTime);
		percentValueTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// status tween
		RefreshStatus(targetTrainingRatio);
		hpValueTweenAnimation.DORestart();
		attackValueTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(0.8f);

		// Refresh
		RefreshInfo(true);
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TrainingDone"), 2.0f);
		yield return Timing.WaitForSeconds(1.0f);

		// 인풋 복구
		CharacterInfoCanvas.instance.inputLockObject.SetActive(false);
	}


	const float valueChangeTime = 0.6f;
	float _valueChangeSpeed;
	float _floatCurrentValue;
	bool _updateValueText;
	void UpdateValueText()
	{
		if (_updateValueText == false)
			return;

		_floatCurrentValue += _valueChangeSpeed * Time.deltaTime;

		if (_floatCurrentValue <= 0.0f)
		{
			_floatCurrentValue = 0.0f;
			_updateValueText = false;
		}
		float newValue = _trainingRatio + (_addValue - _floatCurrentValue);
		percentOrbFill.Fill = newValue;
		trainingPercentValueText.text = string.Format("{0:0.##}%", newValue * 100.0f);
		if (_updateValueText)
			addPercentValueText.text = string.Format("+{0:0.##}%", _floatCurrentValue * 100.0f);
		else
			addPercentValueText.gameObject.SetActive(false);
	}
}