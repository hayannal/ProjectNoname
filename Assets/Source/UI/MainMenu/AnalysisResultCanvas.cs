using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class AnalysisResultCanvas : MonoBehaviour
{
	public static AnalysisResultCanvas instance;

	public RectTransform emptyRootRectTransform;
	public RectTransform levelUpRootRectTransform;
	public RectTransform goldDiaRootRectTransform;
	public RectTransform characterRootRectTransform;
	public GameObject exitObject;

	public Text levelValueText;
	public DOTweenAnimation levelValueTweenAnimation;
	public RectTransform timeGroupRectTransform;
	public Text timeValueText;
	public DOTweenAnimation timeValueTweenAnimation;

	public RectTransform goldGroupRectTransform;
	public Text goldValueText;
	public GameObject goldBigSuccessObject;
	public RectTransform diaGroupRectTransform;
	public Text diaValueText;
	public RectTransform energyGroupRectTransform;
	public Text energyValueText;

	public Slider itemLineSlider;
	public Text gainCharacterText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;
	
	public class CustomItemContainer : CachedItemHave<CharacterBoxResultListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void Update()
	{
		UpdateLevelText();
		UpdateGoldText();
		UpdateDiaText();
		UpdateEnergyText();
	}

	bool _showLevelUp = false;
	bool _showAnalysisResult = false;
	int _prevAnalysisLevel;
	List<CharacterBoxResultListItem> _listResultListItem = new List<CharacterBoxResultListItem>();
	public void RefreshInfo(bool showLevelUp, int prevLevel, bool showAnalysisResult)
	{
		_showLevelUp = showLevelUp;
		_showAnalysisResult = showAnalysisResult;
		_prevAnalysisLevel = prevLevel;

		// 둘다 없는데 왜 호출한거지
		if (showLevelUp == false && showAnalysisResult == false)
		{
			gameObject.SetActive(false);
			return;
		}

		if (showLevelUp && showAnalysisResult == false)
		{
			// 레벨업 단독일때만 emptyRect를 활성화시켜둔다.
			emptyRootRectTransform.gameObject.SetActive(true);
		}
		else
		{
			// 다른 상황에서는 empty 보여주는 곳 없다.
			emptyRootRectTransform.gameObject.SetActive(false);
		}

		// 나머지 처리는 항상 동일
		levelUpRootRectTransform.gameObject.SetActive(false);
		goldDiaRootRectTransform.gameObject.SetActive(false);
		characterRootRectTransform.gameObject.SetActive(false);
		exitObject.SetActive(false);

		for (int i = 0; i < _listResultListItem.Count; ++i)
			_listResultListItem[i].gameObject.SetActive(false);
		_listResultListItem.Clear();

		Timing.RunCoroutine(RewardProcess());
	}

	int _addLevel;
	bool _goldBigSuccess;
	int _addGold;
	int _addDia;
	int _addEnergy;
	IEnumerator<float> RewardProcess()
	{
		_processed = true;

		// 0.1초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.1f);

		if (_showLevelUp)
		{
			// 레벨업 하기 전 값으로 셋팅 후 Show
			int prevMaxTime = 0;
			AnalysisTableData prevAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(_prevAnalysisLevel);
			if (prevAnalysisTableData != null)
			{
				levelValueText.text = _prevAnalysisLevel.ToString();
				timeValueText.text = AnalysisLevelUpCanvas.GetMaxTimeText(prevAnalysisTableData.maxTime);
				prevMaxTime = prevAnalysisTableData.maxTime;
			}
			levelUpRootRectTransform.gameObject.SetActive(true);
			AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
			timeGroupRectTransform.gameObject.SetActive(analysisTableData != null && prevMaxTime != analysisTableData.maxTime);
			yield return Timing.WaitForSeconds(0.4f);

			// 숫자 변하는 
			_addLevel = AnalysisData.instance.analysisLevel - _prevAnalysisLevel;
			_levelChangeRemainTime = levelChangeTime;
			_levelChangeSpeed = _addLevel / _levelChangeRemainTime;
			_currentLevel = 0.0f;
			_updateLevelText = true;
			yield return Timing.WaitForSeconds(levelChangeTime);

			// 레벨 마지막으로 변하는 타이밍 맞춰서 시간도 변경
			if (timeGroupRectTransform.gameObject.activeSelf)
				timeValueText.text = AnalysisLevelUpCanvas.GetMaxTimeText(analysisTableData.maxTime);

			// 여긴 변경 후 잠시 대기
			yield return Timing.WaitForSeconds((_addLevel > 1) ? 0.3f : 0.05f);

			// 숫자 스케일 점프 애니
			levelValueTweenAnimation.DORestart();
			timeValueTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(0.6f);
		}
		
		if (_showAnalysisResult)
		{
			yield return Timing.WaitForSeconds(0.2f);

			// 결과값들은 다 여기에 있다. 이 값 보고 판단해서 보여줄거 보여주면 된다.
			int addEnergy = AnalysisData.instance.cachedDropEnergy;
			int randomGold = AnalysisData.instance.cachedRandomGold;
			int addGold = DropManager.instance.GetLobbyGoldAmount();
			int addDia = DropManager.instance.GetLobbyDiaAmount();
			List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
			List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();

			// SimpleResult에서 했던거처럼 값이 0보다 큰 것들만 보여주고 숫자가 증가하게 한다.
			_goldBigSuccess = (addGold > 0);
			_addGold = addGold + randomGold;
			_addDia = addDia;
			_addEnergy = addEnergy;
			goldGroupRectTransform.gameObject.SetActive(_addGold > 0);
			diaGroupRectTransform.gameObject.SetActive(_addDia > 0);
			energyGroupRectTransform.gameObject.SetActive(_addEnergy > 0);

			if (_addGold > 0)
			{
				goldValueText.text = "0";
				_goldChangeRemainTime = goldChangeTime;
				_goldChangeSpeed = _addGold / _goldChangeRemainTime;
				_currentGold = 0.0f;

				goldBigSuccessObject.SetActive(false);
			}

			if (_addDia > 0)
			{
				diaValueText.text = "0";
				_diaChangeRemainTime = diaChangeTime;
				_diaChangeSpeed = _addDia / _diaChangeRemainTime;
				_currentDia = 0.0f;
			}

			if (_addEnergy > 0)
			{
				energyValueText.text = "0";
				_energyChangeRemainTime = energyChangeTime;
				_energyChangeSpeed = _addEnergy / _energyChangeRemainTime;
				_currentEnergy = 0.0f;
			}

			goldDiaRootRectTransform.gameObject.SetActive(true);
			yield return Timing.WaitForSeconds(0.4f);

			if (_addGold > 0) _updateGoldText = true;
			if (_addDia > 0) _updateDiaText = true;
			if (_addEnergy > 0) _updateEnergyText = true;
			yield return Timing.WaitForSeconds(0.6f);

			// 마지막 오리진 차례
			gainCharacterText.gameObject.SetActive(false);
			itemLineSlider.value = 0.0f;
			characterRootRectTransform.gameObject.SetActive(true);
			DOTween.To(() => itemLineSlider.value, x => itemLineSlider.value = x, 1.0f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);
			yield return Timing.WaitForSeconds(0.3f);

			gainCharacterText.gameObject.SetActive(true);
			yield return Timing.WaitForSeconds(0.1f);

			// 리스트
			for (int i = 0; i < listGrantInfo.Count; ++i)
			{
				CharacterBoxResultListItem resultListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				int powerLevel = 0;
				int transcendLevel = 0;
				CharacterData characterData = PlayerData.instance.GetCharacterData(listGrantInfo[i]);
				if (characterData != null)
				{
					powerLevel = characterData.powerLevel;
					transcendLevel = characterData.transcendLevel;
				}
				resultListItem.characterListItem.Initialize(listGrantInfo[i], powerLevel, SwapCanvasListItem.GetPowerLevelColorState(characterData), transcendLevel, 0, null, null, null);
				resultListItem.Initialize("ShopUI_NewCharacter", 0);
				_listResultListItem.Add(resultListItem);
				yield return Timing.WaitForSeconds(0.2f);
			}

			for (int i = 0; i < listTrpInfo.Count; ++i)
			{
				CharacterBoxResultListItem resultListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				int powerLevel = 0;
				int transcendLevel = 0;
				bool transcendCondition = false;
				CharacterData characterData = PlayerData.instance.GetCharacterData(listTrpInfo[i].actorId);
				if (characterData != null)
				{
					powerLevel = characterData.powerLevel;
					transcendLevel = characterData.transcendLevel;
					transcendCondition = (characterData.transcendPoint >= CharacterData.GetTranscendPoint(characterData.transcendLevel + 1));
				}
				resultListItem.characterListItem.Initialize(listTrpInfo[i].actorId, powerLevel, SwapCanvasListItem.GetPowerLevelColorState(characterData), transcendLevel, 0, null, null, null);
				resultListItem.Initialize(transcendCondition ? "ShopUI_TranscendReward" : "", 0);
				_listResultListItem.Add(resultListItem);
				yield return Timing.WaitForSeconds(0.2f);
			}

			// pp 뿐만 아니라 transcendPoint도 획득 가능한 곳이다. 알람 갱신
			if (listTrpInfo.Count > 0)
			{
				if (DotMainMenuCanvas.instance != null)
					DotMainMenuCanvas.instance.RefreshCharacterAlarmObject(false);
				LobbyCanvas.instance.RefreshAlarmObject(DotMainMenuCanvas.eButtonType.Character, true);

				// 초월메뉴 최초로 보이는 것도 확인해야한다.
				if (CharacterInfoCanvas.instance != null)
					CharacterInfoCanvas.instance.RefreshOpenMenuSlotByTranscendPoint();
			}
		}

		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		// DropManager.instance.ClearLobbyDropInfo(); 대신 
		AnalysisData.instance.ClearCachedInfo();

		_processed = false;
	}

	bool _processed = false;
	public void OnClickExitButton()
	{
		if (_processed)
			return;

		emptyRootRectTransform.gameObject.SetActive(false);
		levelUpRootRectTransform.gameObject.SetActive(false);
		goldDiaRootRectTransform.gameObject.SetActive(false);
		characterRootRectTransform.gameObject.SetActive(false);

		for (int i = 0; i < _listResultListItem.Count; ++i)
			_listResultListItem[i].gameObject.SetActive(false);
		_listResultListItem.Clear();

		exitObject.SetActive(false);
		gameObject.SetActive(false);
	}


	#region Gold Dia Energy Increase
	const float levelChangeTime = 0.6f;
	float _levelChangeRemainTime;
	float _levelChangeSpeed;
	float _currentLevel;
	int _lastLevel;
	bool _updateLevelText;
	void UpdateLevelText()
	{
		if (_updateLevelText == false)
			return;

		_currentLevel += _levelChangeSpeed * Time.unscaledDeltaTime;
		int currentLevelInt = (int)_currentLevel;
		if (currentLevelInt >= _addLevel)
		{
			currentLevelInt = _addLevel;
			_updateLevelText = false;
		}
		if (currentLevelInt != _lastLevel)
		{
			_lastLevel = currentLevelInt;
			levelValueText.text = (_lastLevel + _prevAnalysisLevel).ToString("N0");
		}
	}


	const float diaChangeTime = 0.6f;
	float _diaChangeRemainTime;
	float _diaChangeSpeed;
	float _currentDia;
	int _lastDia;
	bool _updateDiaText;
	void UpdateDiaText()
	{
		if (_updateDiaText == false)
			return;

		_currentDia += _diaChangeSpeed * Time.deltaTime;
		int currentDiaInt = (int)_currentDia;
		if (currentDiaInt >= _addDia)
		{
			currentDiaInt = _addDia;
			_updateDiaText = false;
		}
		if (currentDiaInt != _lastDia)
		{
			_lastDia = currentDiaInt;
			diaValueText.text = _lastDia.ToString("N0");
		}
	}

	const float goldChangeTime = 0.6f;
	float _goldChangeRemainTime;
	float _goldChangeSpeed;
	float _currentGold;
	int _lastGold;
	bool _updateGoldText;
	void UpdateGoldText()
	{
		if (_updateGoldText == false)
			return;

		_currentGold += _goldChangeSpeed * Time.unscaledDeltaTime;
		int currentGoldInt = (int)_currentGold;
		if (currentGoldInt >= _addGold)
		{
			currentGoldInt = _addGold;
			_updateGoldText = false;

			if (_goldBigSuccess) goldBigSuccessObject.SetActive(true);
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}

	const float energyChangeTime = 0.6f;
	float _energyChangeRemainTime;
	float _energyChangeSpeed;
	float _currentEnergy;
	int _lastEnergy;
	bool _updateEnergyText;
	void UpdateEnergyText()
	{
		if (_updateEnergyText == false)
			return;

		_currentEnergy += _energyChangeSpeed * Time.unscaledDeltaTime;
		int currentEnergyInt = (int)_currentEnergy;
		if (currentEnergyInt >= _addEnergy)
		{
			currentEnergyInt = _addEnergy;
			_updateEnergyText = false;
		}
		if (currentEnergyInt != _lastEnergy)
		{
			_lastEnergy = currentEnergyInt;
			energyValueText.text = _lastEnergy.ToString("N0");
		}
	}
	#endregion












	/*

	public void RefreshInfo()
	{
		// 인자가 없이 오면 DropManager에 들어있는걸 보여준다.
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();
		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		int addBalancePp = DropManager.instance.GetLobbyBalancePpAmount();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();
		RefreshInfo(addGold, addDia, listPpInfo, addBalancePp, listGrantInfo, listTrpInfo);
	}

	List<CharacterBoxResultListItem> _listOriginResultItem = new List<CharacterBoxResultListItem>();
	public void RefreshInfo(int addGold, int addDia, List<DropManager.CharacterPpRequest> listPpInfo, int addBalancePp, List<string> listGrantInfo, List<DropManager.CharacterTrpRequest> listTrpInfo)
	{
		// 골드나 다이아가 
		bool goldDia = (addGold > 0) || (addDia > 0);
		goldDiaRootRectTransform.gameObject.SetActive(goldDia);
		if (goldDia)
		{
			characterRootRectTransform.offsetMax = new Vector2(characterRootRectTransform.offsetMax.x, -goldDiaRootRectTransform.sizeDelta.y);
			goldValueText.text = addGold.ToString("N0");
			diaGroupRectTransform.gameObject.SetActive(addDia > 0);
			diaValueText.text = addDia.ToString("N0");
		}
		else
		{
			characterRootRectTransform.offsetMax = Vector2.zero;
		}

		bool needOriginGroup = false;
		int originCount = listGrantInfo.Count + listTrpInfo.Count;
		if (originCount == 0)
		{
			// 구분선 없이 pp리스트만 출력한다.
			gainCharacterText.SetLocalizedText(UIString.instance.GetString("ShopUI_PpReward"));
		}
		else
		{
			gainCharacterText.SetLocalizedText(UIString.instance.GetString("ShopUI_CharacterReward"));
			needOriginGroup = true;
		}

		for (int i = 0; i < _listResultListItem.Count; ++i)
			_listResultListItem[i].gameObject.SetActive(false);
		_listResultListItem.Clear();
		for (int i = 0; i < _listOriginResultItem.Count; ++i)
			_listOriginResultItem[i].gameObject.SetActive(false);
		_listOriginResultItem.Clear();

		for (int i = 0; i < listGrantInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			int powerLevel = 0;
			int transcendLevel = 0;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listGrantInfo[i]);
			if (characterData != null)
			{
				powerLevel = characterData.powerLevel;
				transcendLevel = characterData.transcendLevel;
			}
			resultListItem.characterListItem.Initialize(listGrantInfo[i], powerLevel, SwapCanvasListItem.GetPowerLevelColorState(characterData), transcendLevel, 0, null, null, null);
			resultListItem.Initialize("ShopUI_NewCharacter", 0);
			_listOriginResultItem.Add(resultListItem);
		}

		for (int i = 0; i < listTrpInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			int powerLevel = 0;
			int transcendLevel = 0;
			bool transcendCondition = false;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listTrpInfo[i].actorId);
			if (characterData != null)
			{
				powerLevel = characterData.powerLevel;
				transcendLevel = characterData.transcendLevel;
				transcendCondition = (characterData.transcendPoint >= CharacterData.GetTranscendPoint(characterData.transcendLevel + 1));
			}
			resultListItem.characterListItem.Initialize(listTrpInfo[i].actorId, powerLevel, SwapCanvasListItem.GetPowerLevelColorState(characterData), transcendLevel, 0, null, null, null);
			resultListItem.Initialize(transcendCondition ? "ShopUI_TranscendReward" : "", 0);
			_listOriginResultItem.Add(resultListItem);
		}

		

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		// DropManager.instance.ClearLobbyDropInfo(); 대신 
		AnalysisData.instance.ClearCachedInfo();
	}
	*/
}