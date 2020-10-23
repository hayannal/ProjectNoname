using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BalanceCanvas : MonoBehaviour
{
	public static BalanceCanvas instance;

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

	public Slider useCountSlider;
	public Text useCountText;
	public Text useCountValueText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
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

	CharacterData _highestPpCharacter;
	CharacterData _targetPpCharacter;
	int _priceOnce;
	public void RefreshInfo(string targetActorId)
	{
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
		_priceOnce = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BalanceGoldOnce");

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
			targetCharacterPpValueText.text = _targetPpCharacter.pp.ToString("N0");

			// 등록된 캐릭터의 현재 찍어둔 레벨 말고 pp로 가능한 레벨을 구해놓는다.
			for (int i = TableDataManager.instance.powerLevelTable.dataArray.Length - 1; i >= 0; --i)
			{
				if (_targetPpCharacter.pp >= TableDataManager.instance.powerLevelTable.dataArray[i].requiredAccumulatedPowerPoint)
				{
					_targetCharacterBaseLevel = TableDataManager.instance.powerLevelTable.dataArray[i].powerLevel;
					break;
				}
			}

			_lastTargetActorId = targetActorId;
			changed = true;
		}

		// 캐릭터를 바꿨으면 
		if (changed)
			RefreshSliderPrice();
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
			int targetPowerLevel = _targetCharacterBaseLevel;
			for (int i = TableDataManager.instance.powerLevelTable.dataArray.Length - 1; i >= 0; --i)
			{
				if ((_targetPpCharacter.pp + _sliderCount) >= TableDataManager.instance.powerLevelTable.dataArray[i].requiredAccumulatedPowerPoint)
				{
					targetPowerLevel = TableDataManager.instance.powerLevelTable.dataArray[i].powerLevel;
					break;
				}
			}
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
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_SetSliderValue"), 2.0f);
			return;
		}

		DisableNewBalancePp();
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