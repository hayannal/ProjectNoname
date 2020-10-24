using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

// 스크린 스페이스 캔버스라서 캐릭터에 썼던 이름과 비슷하게 지어둔다.
public class ResearchInfoGrowthCanvas : MonoBehaviour
{
	public static ResearchInfoGrowthCanvas instance;

	public Transform researchTextTransform;
	public Text researchText;
	public Button leftButton;
	public Button rightButton;

	public RectTransform positionRectTransform;
	public Text levelText;
	public Button levelResetButton;
	public Image gaugeImage;
	public DOTweenAnimation gaugeImageTweenAnimation;
	public GameObject hpObject;
	public GameObject attackObject;
	public GameObject diaObject;
	public Text hpText;
	public Text attackText;
	public Text diaText;
	public Transform conditionTransform;
	public GameObject conditionSumLevelGroupObject;
	public GameObject conditionCharacterCountGroupObject;
	public GameObject questionObject;
	public Text sumPowerLevelText;
	public Text characterPowerLevelText;
	public Text sumLevelGaugeText;
	public Text characterCountGaugeText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;
	public GameObject disableButtonObject;
	public Image disableButtonImage;
	public Text disableButtonText;
	public RectTransform alarmRootTransform;

	public GameObject effectPrefab;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	bool _started = false;
	void Start()
    {
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_started = true;
	}

	Vector2 _leftTweenPosition = new Vector2(-150.0f, 0.0f);
	Vector2 _rightTweenPosition = new Vector2(150.0f, 0.0f);
	TweenerCore<Vector2, Vector2, VectorOptions> _tweenReferenceForMove;
	void MoveTween(bool left)
	{
		if (_tweenReferenceForMove != null)
			_tweenReferenceForMove.Kill();

		positionRectTransform.gameObject.SetActive(false);
		positionRectTransform.gameObject.SetActive(true);
		positionRectTransform.anchoredPosition = left ? _leftTweenPosition : _rightTweenPosition;
		_tweenReferenceForMove = positionRectTransform.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutQuad);

		// RefreshInfo 호출하고 항상 트윈으로 나오기때문에 여기서 셋팅한다.
		if (priceButtonObject.activeSelf && _notEnough == false)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}

	void OnEnable()
	{
		RefreshInfo();
		MoveTween(true);
	}

	bool _reserveGaugeMoveTweenAnimation;
	void Update()
	{
		if (_reserveGaugeMoveTweenAnimation)
		{
			gaugeImageTweenAnimation.DORestart();
			_reserveGaugeMoveTweenAnimation = false;
		}
	}

	int _selectedLevel;
	public void RefreshInfo()
	{
		researchText.text = UIString.instance.GetString("ResearchUI_Research");

		SelectDefaultLevel();
		RefreshLevelInfo();
	}

	void SelectDefaultLevel()
	{
		// 처음 켜질땐 항상 현재 도달하려는 연구레벨로 켜지면 된다.
		if (PlayerData.instance.researchLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxResearchLevel"))
			_selectedLevel = PlayerData.instance.researchLevel;
		else
			_selectedLevel = PlayerData.instance.researchLevel + 1;
	}

	ObscuredInt _price;
	ObscuredInt _rewardDia;
	bool _notEnough;
	TweenerCore<float, float, FloatOptions> _tweenReferenceForGauge;
	void RefreshLevelInfo()
	{
		levelText.text = UIString.instance.GetString("GameUI_Lv", _selectedLevel);

		ResearchTableData researchTableData = TableDataManager.instance.FindResearchTableData(_selectedLevel);
		if (researchTableData == null)
			return;

		attackText.text = researchTableData.displayAtk.ToString("N0");
		hpText.text = researchTableData.displayHp.ToString("N0");
		diaText.text = researchTableData.rewardDiamond.ToString("N0");
		attackObject.SetActive(researchTableData.displayAtk > 0);
		hpObject.SetActive(researchTableData.displayHp > 0);
		diaObject.SetActive(researchTableData.rewardDiamond > 0);

		bool maxReached = (_selectedLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxResearchLevel"));
		bool selectCurrentTargetLevel = (_selectedLevel == (PlayerData.instance.researchLevel + 1));

		bool hideResetButton = false;
		if (selectCurrentTargetLevel) hideResetButton = true;
		if (maxReached) hideResetButton = true;
		levelResetButton.gameObject.SetActive(!hideResetButton);

		leftButton.gameObject.SetActive(_selectedLevel != 1);
		bool hideRightButton = false;
		if (maxReached) hideRightButton = true;
		if (_selectedLevel > (PlayerData.instance.researchLevel + 2)) hideRightButton = true;
		rightButton.gameObject.SetActive(!hideRightButton);

		if (_tweenReferenceForGauge != null)
			_tweenReferenceForGauge.Kill();

		int current = 0;
		int max = 0;
		if (researchTableData.requiredType == 0)
		{
			conditionSumLevelGroupObject.SetActive(true);
			conditionCharacterCountGroupObject.SetActive(false);
			questionObject.SetActive(false);
			current = GetCurrentAccumulatedPowerLevel();
			_conditionParameter0 = max = researchTableData.requiredAccumulatedPowerLevel;
			sumPowerLevelText.text = UIString.instance.GetString("GameUI_PowerOnly");
			sumLevelGaugeText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
		}
		else
		{
			conditionSumLevelGroupObject.SetActive(false);
			conditionCharacterCountGroupObject.SetActive(true);
			questionObject.SetActive(false);
			current = GetCurrentConditionCharacterCount(researchTableData.requiredCharacterLevel);
			_conditionParameter1 = max = researchTableData.requiredCharacterCount;
			_conditionParameter0 = researchTableData.requiredCharacterLevel;
			characterPowerLevelText.text = UIString.instance.GetString("GameUI_PowerSizeDiff", researchTableData.requiredCharacterLevel);
			characterCountGaugeText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
		}
		_notEnough = (current < max);
		

		if (selectCurrentTargetLevel)
		{
			float ratio = (float)current / (float)max;
			ratio = Mathf.Min(1.0f, ratio);
			gaugeImage.fillAmount = 0.0f;
			_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, ratio, 0.5f).SetEase(Ease.OutQuad).SetDelay(0.3f);
			if (_started)
				gaugeImageTweenAnimation.DORestart();
			else
				_reserveGaugeMoveTweenAnimation = true;

			int requiredGold = researchTableData.requiredGold;
			priceText.text = requiredGold.ToString("N0");
			bool disablePrice = (CurrencyData.instance.gold < requiredGold || current < max);
			priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			priceText.color = !disablePrice ? Color.white : Color.gray;
			goldGrayscaleEffect.enabled = disablePrice;
			priceButtonObject.SetActive(true);
			disableButtonObject.SetActive(false);
			_price = requiredGold;
			_rewardDia = researchTableData.rewardDiamond;
		}
		else
		{
			if (_selectedLevel < (PlayerData.instance.researchLevel + 1))
			{
				gaugeImage.fillAmount = 1.0f;
				disableButtonText.SetLocalizedText(UIString.instance.GetString("ResearchUI_DoneButton"));
			}

			if (_selectedLevel > (PlayerData.instance.researchLevel + 2))
			{
				conditionSumLevelGroupObject.SetActive(false);
				conditionCharacterCountGroupObject.SetActive(false);
				questionObject.SetActive(true);
				gaugeImage.fillAmount = 0.0f;
				disableButtonText.SetLocalizedText(UIString.instance.GetString("ResearchUI_FormerFirstButton"));
			}
			else if (_selectedLevel > (PlayerData.instance.researchLevel + 1))
			{
				gaugeImage.fillAmount = 0.0f;
				disableButtonText.SetLocalizedText(UIString.instance.GetString("ResearchUI_FormerFirstButton"));
			}
			
			priceButtonObject.SetActive(false);
			disableButtonImage.color = ColorUtil.halfGray;
			disableButtonText.color = ColorUtil.halfGray;
			disableButtonObject.SetActive(true);
			_price = _rewardDia = 0;
		}
	}

	public static int GetCurrentAccumulatedPowerLevel()
	{
		int result = 0;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
			result += PlayerData.instance.listCharacterData[i].powerLevel;
		return result;
	}

	public static int GetCurrentConditionCharacterCount(int requiredCharacterPowerLevel)
	{
		int result = 0;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			if (PlayerData.instance.listCharacterData[i].powerLevel >= requiredCharacterPowerLevel)
				++result;
		}
		return result;
	}

	public static bool CheckResearch(int researchLevel, bool checkPrice = false)
	{
		ResearchTableData researchTableData = TableDataManager.instance.FindResearchTableData(researchLevel);
		if (researchTableData == null)
			return false;

		int current = 0;
		int max = 0;
		if (researchTableData.requiredType == 0)
		{
			current = GetCurrentAccumulatedPowerLevel();
			max = researchTableData.requiredAccumulatedPowerLevel;
		}
		else
		{
			current = GetCurrentConditionCharacterCount(researchTableData.requiredCharacterLevel);
			max = researchTableData.requiredCharacterCount;
		}
		if (current < max)
			return false;

		if (checkPrice)
		{
			if (CurrencyData.instance.gold < researchTableData.requiredGold)
				return false;
		}

		return true;
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("ResearchUI_ResearchMore"), 250, researchTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLevelResetButton()
	{
		bool left = (_selectedLevel > (PlayerData.instance.researchLevel + 1));
		SelectDefaultLevel();
		RefreshLevelInfo();
		MoveTween(left);
	}

	public void OnClickLeftButton()
	{
		_selectedLevel -= 1;
		RefreshLevelInfo();
		MoveTween(true);
	}

	public void OnClickRightButton()
	{
		_selectedLevel += 1;
		RefreshLevelInfo();
		MoveTween(false);
	}

	int _conditionParameter0;
	int _conditionParameter1;
	public void OnClickConditionSumLevelTextButton()
	{
		string text = UIString.instance.GetString("GameUI_ResearchConditionMoreZero", _conditionParameter0);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 250, conditionTransform, new Vector2(0.0f, -55.0f));
	}

	public void OnClickConditionCharacterCountTextButton()
	{
		string text = UIString.instance.GetString("GameUI_ResearchConditionMoreOne", _conditionParameter0, _conditionParameter1);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 250, conditionTransform, new Vector2(0.0f, -55.0f));
	}

	// 드래그 빼기로 하는데 코드는 남겨놔도 되서 캔버스에서 이벤트만 빼두기로 한다.
	public void OnEndDrag(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;

		bool left = (pointerEventData.delta.x > 0.0f);
		if (left)
		{
			if (leftButton.gameObject.activeSelf)
				OnClickLeftButton();
		}
		else
		{
			if (rightButton.gameObject.activeSelf)
				OnClickRightButton();
		}
	}

	public void OnClickDisableButton()
	{
		if (_selectedLevel < (PlayerData.instance.researchLevel + 1))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ResearchUI_Done"), 2.0f);
			return;
		}

		if (_selectedLevel > (PlayerData.instance.researchLevel + 1))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ResearchUI_FormerFirst"), 2.0f);
			return;
		}
	}

	public void OnClickButton()
	{
		if (_notEnough)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString(conditionSumLevelGroupObject.activeSelf ? "ResearchUI_NotEnoughLevel" : "ResearchUI_NotEnoughCharacter"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		priceButtonObject.SetActive(false);
		PlayFabApiManager.instance.RequestResearchLevelUp(_selectedLevel, _price, _rewardDia, () =>
		{
			// 다이아 보상 받는건 연출 뒤에 반영되게 하려고 예외처리 해둔다.
			//ResearchCanvas.instance.currencySmallInfo.RefreshInfo();
			ResearchCanvas.instance.currencySmallInfo.goldText.text = CurrencyData.instance.gold.ToString("N0");

			DotMainMenuCanvas.instance.RefreshResearchAlarmObject();
			Timing.RunCoroutine(ResearchLevelUpProcess());
		});
	}

	IEnumerator<float> ResearchLevelUpProcess()
	{
		// 인풋 차단
		ResearchCanvas.instance.inputLockObject.SetActive(true);

		// 오브젝트 정지
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();
		yield return Timing.WaitForSeconds(0.3f);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
		yield return Timing.WaitForSeconds(2.0f);

		// 여기서 다이아 갱신까지 다시 되게 한다.
		ResearchCanvas.instance.currencySmallInfo.RefreshInfo();

		// Toast 알림
		string stringId = diaObject.activeSelf ? "ResearchUI_RewardedCurrency" : "ResearchUI_RewardedStat";
		ToastCanvas.instance.ShowToast(UIString.instance.GetString(stringId), 3.0f);
		yield return Timing.WaitForSeconds(1.0f);

		// nextInfo
		priceButtonObject.SetActive(true);
		if (rightButton.gameObject.activeSelf)
		{
			OnClickRightButton();
			yield return Timing.WaitForSeconds(0.4f);
		}
		else
		{
			RefreshLevelInfo();
		}

		// 토글 복구
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();

		// 인풋 복구
		ResearchCanvas.instance.inputLockObject.SetActive(false);
	}
}