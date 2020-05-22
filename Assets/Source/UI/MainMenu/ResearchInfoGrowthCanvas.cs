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
	public Text gaugeText;

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
		if (priceButtonObject.activeSelf && _needSumLevel == false)
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
	bool _needSumLevel;
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
		if (_selectedLevel > (PlayerData.instance.researchLevel + 1)) hideRightButton = true;
		rightButton.gameObject.SetActive(!hideRightButton);

		if (_tweenReferenceForGauge != null)
			_tweenReferenceForGauge.Kill();

		int current = 0;
		int max = 0;
		if (selectCurrentTargetLevel)
		{
			int requiredGold = researchTableData.requiredGold;
			int prevRequiredAccumulatedPowerLevel = 0;
			if (_selectedLevel != 1)
			{
				ResearchTableData prevResearchTableData = TableDataManager.instance.FindResearchTableData(_selectedLevel - 1);
				prevRequiredAccumulatedPowerLevel = prevResearchTableData.requiredAccumulatedPowerLevel;
			}
			max = researchTableData.requiredAccumulatedPowerLevel - prevRequiredAccumulatedPowerLevel;
			current = GetCurrentAccumulatedPowerLevel() - prevRequiredAccumulatedPowerLevel;
			gaugeText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
			_needSumLevel = (current < max);

			float ratio = (float)current / (float)max;
			ratio = Mathf.Min(1.0f, ratio);
			gaugeImage.fillAmount = 0.0f;
			_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, ratio, 0.5f).SetEase(Ease.OutQuad).SetDelay(0.3f);
			if (_started)
				gaugeImageTweenAnimation.DORestart();
			else
				_reserveGaugeMoveTweenAnimation = true;

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
				int prevRequiredAccumulatedPowerLevel = 0;
				if (_selectedLevel != 1)
				{
					ResearchTableData prevResearchTableData = TableDataManager.instance.FindResearchTableData(_selectedLevel - 1);
					prevRequiredAccumulatedPowerLevel = prevResearchTableData.requiredAccumulatedPowerLevel;
				}
				current = max = researchTableData.requiredAccumulatedPowerLevel - prevRequiredAccumulatedPowerLevel;
				gaugeText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
				gaugeImage.fillAmount = 1.0f;
				disableButtonText.SetLocalizedText(UIString.instance.GetString("ResearchUI_DoneButton"));
			}

			if (_selectedLevel > (PlayerData.instance.researchLevel + 1))
			{
				gaugeText.text = "???";
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
		if (_needSumLevel)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ResearchUI_NotEnoughLevel"), 2.0f);
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
			ResearchCanvas.instance.currencySmallInfo.RefreshInfo();
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