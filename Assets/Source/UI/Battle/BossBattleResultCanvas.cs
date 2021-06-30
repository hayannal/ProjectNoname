using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using PlayFab.ClientModels;

public class BossBattleResultCanvas : MonoBehaviour
{
	public static BossBattleResultCanvas instance;

	public Text difficultyText;
	public Text resultText;

	public GameObject firstRewardGroupObject;
	public DOTweenAnimation firstRewardTweenAnimation;
	public Text firstRewardValueText;

	public GameObject firstRewardContentItemPrefab;
	public RectTransform firstRewardContentRootRectTransform;

	public GameObject repeatOnlyRectObject;
	public GameObject goldGroupObject;
	public DOTweenAnimation goldImageTweenAnimation;
	public Text goldValueText;

	public Text xpLevelText;
	public Text xpLevelExpText;
	public Image xpLevelExpImage;
	public GameObject xpLevelUpInfoObject;

	public GameObject itemGroupObject;
	public Slider itemLineSlider;
	public GameObject gainItemTextObject;

	public GameObject itemScrollViewObject;
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public EquipListStatusInfo equipSmallStatusInfo;
	public RectTransform smallStatusBackBlurImage;

	public GameObject exitGroupObject;

	public class CustomItemFirstRewardContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _firstRewardContainer = new CustomItemContainer();

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		firstRewardContentItemPrefab.SetActive(false);
		contentItemPrefab.SetActive(false);
		smallStatusBackBlurImage.SetAsFirstSibling();
	}

	void OnEnable()
	{
		Time.timeScale = 0.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDestroy()
	{
		Time.timeScale = 1.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void Update()
	{
		UpdateFirstRewardText();
		UpdateGoldText();
		UpdateEquipSmallStatusInfo();
	}

	bool _clear = false;
	int _selectedDifficulty;
	bool _firstClear;
	List<ItemInstance> _listFirstGrantItem;
	List<ItemInstance> _listDropGrantItem;
	public void RefreshInfo(bool clear, int selectedDifficulty, bool firstClear, string jsonFirstItemGrantResults, string jsonDropItemGrantResults)
	{
		_clear = clear;
		_selectedDifficulty = selectedDifficulty;
		_firstClear = firstClear;

		difficultyText.text = string.Format("DIFFICULTY {0}", selectedDifficulty);
		resultText.text = UIString.instance.GetString(clear ? "GameUI_Success" : "GameUI_Failure");
		resultText.color = clear ? new Color(0.0f, 0.733f, 0.792f) : new Color(0.792f, 0.152f, 0.0f);

		if (jsonFirstItemGrantResults != "")
			_listFirstGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(jsonFirstItemGrantResults);
		if (jsonDropItemGrantResults != "")
			_listDropGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(jsonDropItemGrantResults);

		gameObject.SetActive(true);

		StartCoroutine(BgmProcess());
	}

	IEnumerator BgmProcess()
	{
		yield return new WaitForSecondsRealtime(_clear ? 0.1f : 1.5f);

		SoundManager.instance.PlaySFX(_clear ? "BattleWin" : "BattleLose");

		yield return new WaitForSecondsRealtime((_clear ? 12.0f : 11.0f) + 3.0f);

		SoundManager.instance.PlayBgm("BGM_BattleEnd", 3.0f);
	}

	#region NodeWar Result
	int _firstRewardAmount = 0;
	public void OnEventSuccessResult()
	{
		if (_clear && _firstClear)
		{
			_firstRewardAmount = BattleManager.instance.GetCachedBossRewardTableData().firstEnergy;
			firstRewardGroupObject.SetActive(true);
			return;
		}

		// 첫 클리어 보상이 없을땐 일반보상만 켜면 된다.
		SetRepeatRewardInfo();
		repeatOnlyRectObject.SetActive(true);
		goldGroupObject.SetActive(true);
	}
	#endregion

	#region First Reward
	public void OnEventIncreaseFirst()
	{
		_firstRewardChangeRemainTime = firstRewardChangeTime;
		_firstRewardChangeSpeed = _firstRewardAmount / _firstRewardChangeRemainTime;
		_currentFirstReward = 0.0f;
		_updateFirstRewardText = true;
		firstRewardTweenAnimation.DOPlay();

		StartCoroutine(FirstRewardProcess());
	}

	IEnumerator FirstRewardProcess()
	{
		yield return new WaitForSecondsRealtime(firstRewardChangeTime);

		// 없을리는 없겠지만 체크
		if (_listFirstGrantItem != null || _listFirstGrantItem.Count > 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);

			for (int i = 0; i < _listFirstGrantItem.Count; ++i)
			{
				EquipData newEquipData = new EquipData();
				newEquipData.equipId = _listFirstGrantItem[i].ItemId;
				newEquipData.Initialize(_listFirstGrantItem[i].CustomData);

				EquipCanvasListItem equipCanvasListItem = _firstRewardContainer.GetCachedItem(firstRewardContentItemPrefab, firstRewardContentRootRectTransform);
				equipCanvasListItem.Initialize(newEquipData, OnClickListItem);
				yield return new WaitForSecondsRealtime(0.2f);
			}
		}

		SetRepeatRewardInfo();
		goldGroupObject.SetActive(true);
	}

	const float firstRewardChangeTime = 0.2f;
	float _firstRewardChangeRemainTime;
	float _firstRewardChangeSpeed;
	float _currentFirstReward;
	int _lastFirstReward;
	bool _updateFirstRewardText;
	void UpdateFirstRewardText()
	{
		if (_updateFirstRewardText == false)
			return;

		_currentFirstReward += _firstRewardChangeSpeed * Time.unscaledDeltaTime;
		int currentFirstRewardInt = (int)_currentFirstReward;
		if (currentFirstRewardInt >= _firstRewardAmount)
		{
			currentFirstRewardInt = _firstRewardAmount;
			_updateFirstRewardText = false;
		}
		if (currentFirstRewardInt != _lastFirstReward)
		{
			_lastFirstReward = currentFirstRewardInt;
			firstRewardValueText.text = _lastFirstReward.ToString("N0");
		}
	}
	#endregion

	#region Xp
	bool _maxXpLevel = false;
	float _nextPercent = 0.0f;
	bool _nextIsLevelUp = false;
	int _current = 0;
	int _max = 0;
	void RefreshBossBattleCount(int count)
	{
		// 현재 카운트가 속하는 테이블 구해와서 레벨 및 경험치로 표시.
		int maxXpLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleLevel");
		int level = 0;
		float percent = 0.0f;
		int currentPeriodExp = 0;
		int currentPeriodExpMax = 0;
		for (int i = 1; i < TableDataManager.instance.bossExpTable.dataArray.Length; ++i)
		{
			if (count < TableDataManager.instance.bossExpTable.dataArray[i].requiredAccumulatedExp)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				percent = (float)currentPeriodExp / (float)currentPeriodExpMax;
				level = TableDataManager.instance.bossExpTable.dataArray[i].xpLevel - 1;
				break;
			}
			if (TableDataManager.instance.bossExpTable.dataArray[i].xpLevel >= maxXpLevel)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				level = maxXpLevel;
				percent = 1.0f;
				break;
			}
		}
		_current = currentPeriodExp;
		_max = currentPeriodExpMax;

		string xpLevelString = "";
		if (level == maxXpLevel)
		{
			_maxXpLevel = true;
			xpLevelString = UIString.instance.GetString("GameUI_Lv", "Max");
			xpLevelExpImage.color = DailyFreeItem.GetGoldTextColor();
		}
		else
		{
			xpLevelString = UIString.instance.GetString("GameUI_Lv", level);
			xpLevelExpImage.color = Color.white;
			_nextPercent = (float)(currentPeriodExp + 1) / currentPeriodExpMax;
			_nextIsLevelUp = (currentPeriodExp + 1) == currentPeriodExpMax;
		}
		xpLevelText.text = string.Format("XP {0}", xpLevelString);
		xpLevelExpText.text = string.Format("{0} / {1}", currentPeriodExp, currentPeriodExpMax);
		xpLevelExpImage.fillAmount = percent;
	}
	#endregion

	#region Gold
	int _repeatRewardAmount;
	void SetRepeatRewardInfo()
	{
		_repeatRewardAmount = BattleManager.instance.GetCachedBossRewardTableData().enterGold;
		RefreshBossBattleCount(BossBattleEnterCanvas.instance.GetXp());
	}

	public void OnEventIncreaseGold()
	{
		_goldChangeRemainTime = goldChangeTime;
		_goldChangeSpeed = _repeatRewardAmount / _goldChangeRemainTime;
		_currentGold = 0.0f;
		_updateGoldText = true;

		goldImageTweenAnimation.DOPlay();

		StartCoroutine(GoldProcess());
	}

	IEnumerator GoldProcess()
	{
		yield return new WaitForSecondsRealtime(goldChangeTime);

		// Exp Process
		yield return new WaitForSecondsRealtime(0.2f);

		if (_maxXpLevel == false)
		{
			TweenerCore<float, float, FloatOptions> tweenRef = DOTween.To(() => xpLevelExpImage.fillAmount, x => xpLevelExpImage.fillAmount = x, _nextPercent, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

			yield return new WaitForSecondsRealtime(0.4f);

			if (tweenRef != null)
				tweenRef.Kill();

			// _nextIsLevelUp 플래그가 간혹 동작하지 않는 버그가 있어서 fillAmount도 추가로 검사하기로 해본다.
			if (_nextIsLevelUp || xpLevelExpImage.fillAmount >= 1.0f)
			{
				RefreshBossBattleCount(BossBattleEnterCanvas.instance.GetXp() + 1);
				xpLevelUpInfoObject.SetActive(true);
			}
			else
				xpLevelExpText.text = string.Format("{0} / {1}", _current + 1, _max);

			yield return new WaitForSecondsRealtime(0.2f);
		}

		itemGroupObject.SetActive(true);
		StartCoroutine(DropItemProcess());
	}

	const float goldChangeTime = 0.4f;
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
		if (currentGoldInt >= _repeatRewardAmount)
		{
			currentGoldInt = _repeatRewardAmount;
			_updateGoldText = false;
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}
	#endregion

	#region Drop Item
	IEnumerator DropItemProcess()
	{
		DOTween.To(() => itemLineSlider.value, x => itemLineSlider.value = x, 1.0f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

		yield return new WaitForSecondsRealtime(0.3f);

		gainItemTextObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.1f);

		// 아이템이 없으면 바로 exitGroupObject
		if (_listDropGrantItem == null || _listDropGrantItem.Count == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		itemScrollViewObject.SetActive(true);

		for (int i = 0; i < _listDropGrantItem.Count; ++i)
		{
			EquipData newEquipData = new EquipData();
			newEquipData.equipId = _listDropGrantItem[i].ItemId;
			newEquipData.Initialize(_listDropGrantItem[i].CustomData);

			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(newEquipData, OnClickListItem);
			yield return new WaitForSecondsRealtime(0.2f);
		}

		exitGroupObject.SetActive(true);
	}
	#endregion

	#region Item Common
	public void OnClickListItem(EquipData equipData)
	{
		equipSmallStatusInfo.RefreshInfo(equipData, false);
		equipSmallStatusInfo.gameObject.SetActive(false);
		equipSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}

	float _materialSmallStatusInfoShowRemainTime;
	void UpdateEquipSmallStatusInfo()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.unscaledDeltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				equipSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}
	#endregion

	public void OnClickExitButton()
	{
		// PlayerData에 기록을 남겨둔다.
		PlayerData.instance.readyToReopenBossEnterCanvas = true;

		SceneManager.LoadScene(0);
	}
}