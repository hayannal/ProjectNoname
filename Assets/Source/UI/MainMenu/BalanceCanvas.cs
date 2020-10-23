using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class BalanceCanvas : MonoBehaviour
{
	public static BalanceCanvas instance;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public GameObject inputLockObject;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject balanceGroundObjectPrefab;
	public Transform highestCharacterPpTextTransform;
	public SwapCanvasListItem highestCharacterListItem;
	public Text highestCharacterPpValueText;

	public Text selectText;
	public GameObject emptySlotObject;
	public SwapCanvasListItem targetCharacterListItem;
	public GameObject targetCharacterPpGroupObject;
	public Text targetCharacterPpValueText;
	public GameObject targetCharacterLevelGroupObject;
	public Text targetCharacterLevelValueText;

	public Transform myBalancePpTextTransform;
	public Text myBalancePpValueText;
	public CanvasGroup remainTimeCanvasGroup;
	public DOTweenAnimation fadeTweenAnimation;
	public Text remainTimeText;

	public Slider useCountSlider;
	public Text useCountText;
	public Text useCountValueText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public RectTransform alarmRootTransform;

	public GameObject resultGroupObject;
	public Text currentPpText;
	public Text addPpText;
	public DOTweenAnimation ppValueTweenAnimation;
	public GameObject messageTextObject;
	public GameObject exitObject;

	Vector2 _defaultAnchoredPosition;
	void Awake()
	{
		instance = this;
		_defaultAnchoredPosition = targetCharacterListItem.cachedRectTransform.anchoredPosition;
	}

	GameObject _balanceGroundObject;
	void Start()
	{
		_balanceGroundObject = Instantiate<GameObject>(balanceGroundObjectPrefab, _rootOffsetPosition, Quaternion.Euler(0.0f, -90.0f, 0.0f));

		if (EventManager.instance.reservedOpenBalanceEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BalanceName"), UIString.instance.GetString("GameUI_BalanceDesc"), UIString.instance.GetString("GameUI_BalanceMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenBalanceEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.balance);
		}

		remainTimeCanvasGroup.alpha = 0.0f;
		RefreshRemainTime();
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (restore)
			return;

		if (_balanceGroundObject != null)
			_balanceGroundObject.SetActive(true);
		SetInfoCameraMode(true);
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		DisableNewBalancePp();
		_balanceGroundObject.SetActive(false);
		SetInfoCameraMode(false);
		_lastTargetActorId = "";
	}

	float _canvasRemainTime = 0.0f;
	void Update()
	{
		if (_canvasRemainTime > 0.0f)
		{
			_canvasRemainTime -= Time.deltaTime;
			if (_canvasRemainTime <= 0.0f)
			{
				_canvasRemainTime = 0.0f;
				fadeTweenAnimation.DORestart();
			}
		}

		UpdateRemainTime();
		UpdateRefresh();
		UpdatePpText();
	}

	CharacterData _highestPpCharacter;
	CharacterData _targetPpCharacter;
	int _priceOnce;
	public void RefreshInfo(string targetActorId)
	{
		_priceOnce = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BalanceGoldOnce");

		// 제일 먼저 pp 가장 많은 캐릭을 찾아야한다.
		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		CharacterData highestPpCharacter = listCharacterData[0];
		ActorTableData highestActorTableData = TableDataManager.instance.FindActorTableData(listCharacterData[0].actorId);
		for (int i = 1; i < listCharacterData.Count; ++i)
		{
			CharacterData characterData = listCharacterData[i];
			if (characterData.pp < highestPpCharacter.pp)
				continue;

			if (characterData.pp > highestPpCharacter.pp)
			{
				highestPpCharacter = characterData;
				highestActorTableData = TableDataManager.instance.FindActorTableData(characterData.actorId);
				continue;
			}

			if (characterData.powerLevel > highestPpCharacter.powerLevel)
			{
				highestPpCharacter = characterData;
				highestActorTableData = TableDataManager.instance.FindActorTableData(characterData.actorId);
				continue;
			}

			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(characterData.actorId);
			if (actorTableData.grade > highestActorTableData.grade || actorTableData.orderIndex > highestActorTableData.orderIndex)
			{
				highestPpCharacter = characterData;
				highestActorTableData = TableDataManager.instance.FindActorTableData(characterData.actorId);
				continue;
			}
		}

		_highestPpCharacter = highestPpCharacter;
		highestCharacterListItem.Initialize(highestPpCharacter.actorId, highestPpCharacter.powerLevel, highestPpCharacter.transcendLevel, 0, null, null);
		highestCharacterPpValueText.text = highestPpCharacter.pp.ToString("N0");

		selectText.text = UIString.instance.GetString("BalanceUI_Select");
		RefreshTargetActor(targetActorId);

		AlarmObject.Hide(alarmRootTransform);
		if (PlayerData.instance.balancePpAlarmState)
			AlarmObject.Show(alarmRootTransform, true, true);

		myBalancePpValueText.text = PlayerData.instance.balancePp.ToString("N0");
		RefreshSliderPrice();
	}

	string _lastTargetActorId;
	int _targetCharacterBaseLevel;
	public void RefreshTargetActor(string targetActorId)
	{
		bool changed = false;
		if (string.IsNullOrEmpty(targetActorId))
		{
			_targetPpCharacter = null;
			emptySlotObject.SetActive(true);
			targetCharacterListItem.gameObject.SetActive(false);
			targetCharacterPpGroupObject.SetActive(false);
			targetCharacterLevelGroupObject.SetActive(false);
			changed = true;
		}
		else
		{
			if (_lastTargetActorId == targetActorId)
				return;

			emptySlotObject.SetActive(false);
			targetCharacterListItem.gameObject.SetActive(true);
			targetCharacterPpGroupObject.SetActive(true);
			targetCharacterLevelGroupObject.SetActive(false);

			_targetPpCharacter = PlayerData.instance.GetCharacterData(targetActorId);
			if (_targetPpCharacter == null)
				return;
			targetCharacterListItem.Initialize(targetActorId, _targetPpCharacter.powerLevel, _targetPpCharacter.transcendLevel, 0, null, null);
			targetCharacterListItem.contentRectTransform.anchoredPosition = new Vector2(-19.5f, 12.0f);
			targetCharacterPpValueText.text = _targetPpCharacter.pp.ToString("N0");

			// 등록된 캐릭터의 현재 찍어둔 레벨 말고 pp로 가능한 레벨을 구해놓는다.
			_targetCharacterBaseLevel = GetReachablePowerLevel(_targetPpCharacter.pp, 0);
			_lastTargetActorId = targetActorId;
			changed = true;
		}

		// 캐릭터를 바꿨으면 
		if (changed)
			RefreshSliderPrice();
	}

	int GetReachablePowerLevel(int pp, int defaultLevel)
	{
		int powerLevel = defaultLevel;
		for (int i = TableDataManager.instance.powerLevelTable.dataArray.Length - 1; i >= 0; --i)
		{
			if (TableDataManager.instance.powerLevelTable.dataArray[i].powerLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
				continue;

			// limitBreak는 우선 검사안해본다.
			//if (TableDataManager.instance.powerLevelTable.dataArray[i].requiredLimitBreak > 0)

			if (pp >= TableDataManager.instance.powerLevelTable.dataArray[i].requiredAccumulatedPowerPoint)
			{
				powerLevel = TableDataManager.instance.powerLevelTable.dataArray[i].powerLevel;
				break;
			}
		}
		return powerLevel;
	}

	void RefreshSliderPrice()
	{
		int maxCount = PlayerData.instance.balancePp;
		if (_targetPpCharacter == null)
			maxCount = 0;
		else
		{
			int diff = _highestPpCharacter.pp - _targetPpCharacter.pp;
			if (diff < maxCount)
				maxCount = diff;
			int priceCount = CurrencyData.instance.gold / _priceOnce;
			if (priceCount < maxCount)
				maxCount = priceCount;
		}

		useCountSlider.minValue = 0.0f;
		useCountSlider.maxValue = maxCount;
		useCountSlider.value = 0.0f;
		OnValueChangedRepeatCount(0.0f);
	}

	void RefreshRemainTime()
	{
		if (PlayerData.instance.balancePpPurchased)
		{
			_nextResetDateTime = PlayerData.instance.balancePpResetTime;
			_needUpdate = true;
			remainTimeText.gameObject.SetActive(true);
		}
		else
		{
			remainTimeText.gameObject.SetActive(false);
		}
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		LobbyCanvas.Home();
	}

	public void OnClickHighestPpTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("BalanceUI_BestPPCharMore"), 300, highestCharacterPpTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickMyBalancePpTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("BalanceUI_PPYouGotMore"), 300, myBalancePpTextTransform, new Vector2(0.0f, -35.0f));
		DisableNewBalancePp();
	}

	void DisableNewBalancePp()
	{
		PlayerData.instance.balancePpAlarmState = false;
		AlarmObject.Hide(alarmRootTransform);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.RefreshBalanceAlarmObject();
	}

	public void OnClickSelectButton()
	{
		if (PlayerData.instance.balancePp == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_NotEnoughBalancePP"), 2.0f);
			return;
		}

		// 창 전환
		UIInstanceManager.instance.ShowCanvasAsync("BalanceSelectCanvas", () =>
		{
			BalanceSelectCanvas.instance.RefreshGrid(_targetPpCharacter == null ? "" : (string)_targetPpCharacter.actorId);
		});
	}

	public void OnClickMinusButton()
	{
		_sliderCount -= 1;
		_sliderCount = Mathf.Max(_sliderCount, 0);
		useCountSlider.value = _sliderCount;
		RefreshSliderCount();
	}

	public void OnClickPlusButton()
	{
		_sliderCount += 1;
		_sliderCount = Mathf.Min(_sliderCount, Mathf.RoundToInt(useCountSlider.maxValue));
		useCountSlider.value = _sliderCount;
		RefreshSliderCount();
	}

	int _sliderCount = 0;
	public void OnValueChangedRepeatCount(float value)
	{
		_sliderCount = Mathf.RoundToInt(value);
		RefreshSliderCount();
	}

	void RefreshSliderCount()
	{
		useCountText.text = _sliderCount.ToString();
		int totalPrice = _priceOnce * _sliderCount;
		priceText.text = totalPrice.ToString("N0");
		useCountValueText.text = string.Format("{0} / {1}", _sliderCount, Mathf.RoundToInt(useCountSlider.maxValue));

		if (_targetPpCharacter != null)
		{
			targetCharacterPpValueText.text = (_targetPpCharacter.pp + _sliderCount).ToString("N0");
			targetCharacterPpValueText.color = (_sliderCount == 0) ? Color.white : Color.green;

			// 해당 pp에 맞는 레벨을 가져온다.
			int targetPowerLevel = GetReachablePowerLevel(_targetPpCharacter.pp + _sliderCount, _targetCharacterBaseLevel);
			targetCharacterLevelValueText.text = targetPowerLevel.ToString();
			targetCharacterLevelValueText.color = (_targetCharacterBaseLevel != targetPowerLevel) ? Color.green : Color.white;
			targetCharacterLevelGroupObject.SetActive(_targetPpCharacter.powerLevel != targetPowerLevel);
		}

		priceButtonObject.SetActive(true);
		bool disablePrice = (_targetPpCharacter == null || CurrencyData.instance.gold < _priceOnce || _sliderCount == 0);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;
	}

	public void OnClickBalancePpPurchaseButton()
	{
		// 이미 구매했다면 
		if (PlayerData.instance.balancePpPurchased)
		{
			remainTimeCanvasGroup.alpha = 1.0f;
			_canvasRemainTime = 5.0f;
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_PurchaseTodayDone"), 2.0f);
			return;
		}

		int addPp = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BalancePowerPointsDay");
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BalancePowerPointsDiamond");
		if (CurrencyData.instance.dia < price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("BalanceUI_Purchase", addPp), CurrencyData.eCurrencyType.Diamond, price, false, () =>
			{
				PlayFabApiManager.instance.RequestPurchaseBalancePp(addPp, price, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					currencySmallInfo.RefreshInfo();
					myBalancePpValueText.text = PlayerData.instance.balancePp.ToString("N0");
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_PurchaseDone"), 2.0f);
					RefreshRemainTime();
				});
			});
		});
	}

	public void OnClickPriceButton()
	{
		if (PlayerData.instance.balancePp == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_NotEnoughBalancePP"), 2.0f);
			return;
		}

		if (_targetPpCharacter == null)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_SelectCharToast"), 2.0f);
			return;
		}

		if (_sliderCount == 0)
		{
			// 원래 같으면 등록이 되지 않지만 낮았던 캐릭이라 등록했고 그 캐릭이
			// 최대 캐릭터와 동일한 pp만큼 성장해서 더이상 슬라이드를 못움직이는 상태일거다. 이땐 메세지를 다르게 처리한다.
			if (_targetPpCharacter.pp == _highestPpCharacter.pp)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_SameAsBest"), 2.0f);
				return;
			}

			// 그게 아니라면 평소대로 보여주면 된다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_SetSliderValue"), 2.0f);
			return;
		}

		// 패킷 보내기 전에 미리 현재값으로 셋팅해둔다.
		_currentPp = _targetPpCharacter.pp;
		_addValue = _sliderCount;
		currentPpText.text = _targetPpCharacter.pp.ToString("N0");
		addPpText.text = string.Format("+{0:N0}", _sliderCount);

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("BalanceUI_UseConfirm", _sliderCount), CurrencyData.eCurrencyType.Gold, _priceOnce * _sliderCount, false, () =>
			{
				PlayFabApiManager.instance.RequestUseBalancePp(_targetPpCharacter, _sliderCount, _priceOnce * _sliderCount, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					OnRecvUseBalance();
				});
			});
		});
	}

	void OnRecvUseBalance()
	{
		currencySmallInfo.RefreshInfo();
		DisableNewBalancePp();
		Timing.RunCoroutine(BalanceProcess());
	}

	float _currentPp;
	float _addValue;
	IEnumerator<float> BalanceProcess()
	{
		// 인풋 차단
		inputLockObject.SetActive(true);
		backKeyButton.interactable = false;

		// 배경 페이드
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		yield return Timing.WaitForSeconds(0.15f);

		// 대상 캐릭터 아이콘 가운데로 이동
		targetCharacterListItem.cachedRectTransform.DOAnchorPos(new Vector2(0.0f, _defaultAnchoredPosition.y), 0.6f);
		yield return Timing.WaitForSeconds(0.6f);

		// 새로운 결과 팝업창이 나오고
		messageTextObject.SetActive(false);
		exitObject.SetActive(false);
		resultGroupObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		float tweenDelay = 0.5f;

		// pp 늘어나는 연출
		_ppChangeSpeed = -_addValue / ppChangeTime;
		_floatCurrentPp = _addValue;
		_updatePpText = true;
		yield return Timing.WaitForSeconds(ppChangeTime);
		ppValueTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// 터치하여 나가기 보여주고
		messageTextObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);

		// Refresh
		_targetCharacterBaseLevel = GetReachablePowerLevel(_targetPpCharacter.pp, 0);
		myBalancePpValueText.text = PlayerData.instance.balancePp.ToString("N0");
		RefreshSliderPrice();
		DotMainMenuCanvas.instance.RefreshCharacterAlarmObject();
	}

	const float ppChangeTime = 0.6f;
	float _ppChangeSpeed;
	float _floatCurrentPp;
	int _lastPp;
	bool _updatePpText;
	void UpdatePpText()
	{
		if (_updatePpText == false)
			return;

		_floatCurrentPp += _ppChangeSpeed * Time.deltaTime;
		int currentPpInt = (int)(_floatCurrentPp + 0.99f);
		if (currentPpInt <= 0)
		{
			currentPpInt = 0;
			_updatePpText = false;
		}
		if (currentPpInt != _lastPp)
		{
			_lastPp = currentPpInt;
			currentPpText.text = (_currentPp + (_addValue - _lastPp)).ToString("N0");
			if (_lastPp > 0)
				addPpText.text = string.Format("+{0:N0}", _lastPp);
			else
				addPpText.text = "";
		}
	}

	public void OnClickExitResultButton()
	{
		// result Object 통째로 끄고
		resultGroupObject.SetActive(false);

		// 이미 창은 리프레쉬 되었을테니 알파 애니메이션으로 복구
		Timing.RunCoroutine(ExitResultProcess());
	}

	IEnumerator<float> ExitResultProcess()
	{
		// 대상 캐릭터 아이콘 위치 복구
		targetCharacterListItem.cachedRectTransform.DOAnchorPos(new Vector2(_defaultAnchoredPosition.x, _defaultAnchoredPosition.y), 0.6f);
		yield return Timing.WaitForSeconds(0.4f);

		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1.0f, 0.3f).SetEase(Ease.Linear);
		yield return Timing.WaitForSeconds(0.2f);

		// 인풋 복구
		inputLockObject.SetActive(false);
		backKeyButton.interactable = true;

		// 레벨업이 가능한 상태라면 바로가기로 보내준다.
		int reachablePowerLevel = GetReachablePowerLevel(_targetPpCharacter.pp, 0);
		if (_targetPpCharacter.powerLevel < reachablePowerLevel)
		{
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("BalanceUI_LevelUpPossible"), () =>
			{
				Timing.RunCoroutine(ChangeCanvasProcess(_targetPpCharacter.actorId));
			});
		}
	}

	IEnumerator<float> ChangeCanvasProcess(string actorId)
	{
		DelayedLoadingCanvas.Show(true);

		FadeCanvas.instance.FadeOut(0.3f, 1, true);
		yield return Timing.WaitForSeconds(0.3f);

		OnClickBackButton();

		while (BalanceCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		UIInstanceManager.instance.ShowCanvasAsync("CharacterListCanvas", () =>
		{
			CharacterListCanvas.instance.OnClickListItem(actorId);
			CharacterListCanvas.instance.OnClickYesButton();
		});

		while ((CharacterInfoCanvas.instance != null && CharacterInfoCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;

		CharacterInfoCanvas.instance.OnClickMenuButton1();

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.2f);
	}


	DateTime _nextResetDateTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;
		if (remainTimeCanvasGroup.alpha == 0.0f)
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
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (PlayerData.instance.balancePpPurchased == false)
		{
			RefreshRemainTime();
			_needRefresh = false;
		}
	}




	// ResearchCanvas 와 비슷한 구조로 만든다.
	public Transform infoCameraTransform;
	public float infoCameraFov = 43.0f;

	#region Info Camera
	protected Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, 75.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }
	bool _infoCameraMode = false;
	float _lastRendererResolutionFactor;
	float _lastBloomResolutionFactor;
	float _lastFov;
	Color _lastBackgroundColor;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;

	Transform _groundTransform;
	GameObject _prevEnvironmentSettingObject;
	protected void SetInfoCameraMode(bool enable)
	{
		if (_infoCameraMode == enable)
			return;

		if (enable)
		{
			if (MainSceneBuilder.instance.lobby)
				LobbyCanvas.instance.OnEnterMainMenu(true);

			// disable prev component
			CameraFovController.instance.enabled = false;
			CustomFollowCamera.instance.enabled = false;

			// save prev info
			_lastRendererResolutionFactor = CustomRenderer.instance.RenderTextureResolutionFactor;
			_lastBloomResolutionFactor = CustomRenderer.instance.bloom.RenderTextureResolutoinFactor;
			_lastFov = UIInstanceManager.instance.GetCachedCameraMain().fieldOfView;
			_lastBackgroundColor = UIInstanceManager.instance.GetCachedCameraMain().backgroundColor;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

			// ground setting
			_prevEnvironmentSettingObject = StageManager.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
				_groundTransform = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.characterInfoGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
			else
				_groundTransform.gameObject.SetActive(true);
			CharacterInfoGround.instance.stoneObject.SetActive(false);

			if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
				TimeSpaceGround.instance.EnableObjectDeformer(false);

			// setting
			CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = 0.8f;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = new Color(0.183f, 0.19f, 0.208f);
			CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition + _rootOffsetPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;
		}
		else
		{
			if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
				return;
			if (CustomFollowCamera.instance.gameObject == null)
				return;
			if (StageManager.instance == null)
				return;
			if (BattleInstanceManager.instance.playerActor.gameObject == null)
				return;

			CharacterInfoGround.instance.stoneObject.SetActive(true);
			_groundTransform.gameObject.SetActive(false);
			_prevEnvironmentSettingObject.SetActive(true);

			CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = _lastBackgroundColor;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

			if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
				TimeSpaceGround.instance.EnableObjectDeformer(true);

			CameraFovController.instance.enabled = true;
			CustomFollowCamera.instance.enabled = true;
			LobbyCanvas.instance.OnEnterMainMenu(false);
		}
		_infoCameraMode = enable;
	}
	#endregion
}