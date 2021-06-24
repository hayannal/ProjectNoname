using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayFab.ClientModels;

public class BossBattleResultCanvas : MonoBehaviour
{
	public static BossBattleResultCanvas instance;

	public Text difficultyText;
	public Text resultText;

	public GameObject noRewardGroupObject;

	public GameObject firstRewardGroupObject;
	public Text firstRewardTypeText;
	public GameObject[] firstRewardTypeObjectList;
	public DOTweenAnimation[] firstRewardTweenAnimation;
	public Text firstRewardValueText;

	public GameObject goldGroupObject;
	public DOTweenAnimation goldImageTweenAnimation;
	public Text goldValueText;
	public Text goldBoostText;

	public GameObject itemGroupObject;
	public Slider itemLineSlider;
	public GameObject gainItemTextObject;
	public Text itemBoostText;
	public GameObject itemScrollViewObject;
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public EquipListStatusInfo equipSmallStatusInfo;
	public RectTransform smallStatusBackBlurImage;

	public GameObject exitGroupObject;

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
		//UpdateEquipSmallStatusInfo();
	}

	bool _clear = false;
	int _selectedDifficulty;
	bool _firstClear;
	List<ItemInstance> _listGrantItem;
	public void RefreshInfo(bool clear, int selectedDifficulty, bool firstClear, string jsonItemGrantResults)
	{
		_clear = clear;
		_selectedDifficulty = selectedDifficulty;
		_firstClear = firstClear;

		difficultyText.text = string.Format("DIFFICULTY {0}", selectedDifficulty);
		resultText.text = UIString.instance.GetString(clear ? "GameUI_Success" : "GameUI_Failure");
		resultText.color = clear ? new Color(0.0f, 0.733f, 0.792f) : new Color(0.792f, 0.152f, 0.0f);

		if (jsonItemGrantResults != "")
			_listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(jsonItemGrantResults);

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
	int _firstRewardAmount;
	public void OnEventSuccessResult()
	{
		if (_clear)
		{
			if (_firstClear)
			{
				/*
				CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
				int amount = _currentNodeWarTableData.firstRewardDiamond;
				if (_currentNodeWarTableData.firstRewardGold > 0)
				{
					currencyType = CurrencyData.eCurrencyType.Gold;
					amount = _currentNodeWarTableData.firstRewardGold;
				}
				_firstRewardAmount = amount;
				for (int i = 0; i < firstRewardTypeObjectList.Length; ++i)
					firstRewardTypeObjectList[i].SetActive((int)currencyType == i);
				firstRewardTypeText.SetLocalizedText(UIString.instance.GetString((currencyType == CurrencyData.eCurrencyType.Diamond) ? "ShopUI_DiamondReward" : "ShopUI_GoldReward"));
				firstRewardGroupObject.SetActive(true);
				*/
				firstRewardGroupObject.SetActive(true);
			}
			else
			{
				// 첫 클리어 보상이 없을땐 repeat보상만 켜면 된다.
				SetRepeatRewardInfo();
				goldGroupObject.SetActive(true);
			}
		}
		else
		{
			// 실패했다면 보상 없음 표시를 켜고
			noRewardGroupObject.SetActive(true);
		}
	}
	#endregion

	#region First Reward
	public void OnEventIncreaseFirst()
	{
		_firstRewardChangeRemainTime = firstRewardChangeTime;
		_firstRewardChangeSpeed = _firstRewardAmount / _firstRewardChangeRemainTime;
		_currentFirstReward = 0.0f;
		_updateFirstRewardText = true;

		for (int i = 0; i < firstRewardTypeObjectList.Length; ++i)
		{
			if (firstRewardTypeObjectList[i].activeSelf)
				firstRewardTweenAnimation[i].DOPlay();
		}

		StartCoroutine(FirstRewardProcess());
	}

	IEnumerator FirstRewardProcess()
	{
		yield return new WaitForSecondsRealtime(firstRewardChangeTime);

		SetRepeatRewardInfo();
		goldGroupObject.SetActive(true);
	}

	const float firstRewardChangeTime = 0.4f;
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

	#region Gold
	int _repeatRewardAmount;
	void SetRepeatRewardInfo()
	{
		//_repeatRewardAmount = _currentNodeWarTableData.repeatRewardGold;
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
		yield return new WaitForSecondsRealtime(0.2f);
		itemGroupObject.SetActive(true);
		//StartCoroutine(ItemProcess());

		exitGroupObject.SetActive(true);
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

	public void OnClickExitButton()
	{
		SceneManager.LoadScene(0);
	}
}