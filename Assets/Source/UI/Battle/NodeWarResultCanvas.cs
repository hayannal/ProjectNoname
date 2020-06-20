using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayFab.ClientModels;

public class NodeWarResultCanvas : MonoBehaviour
{
	public static NodeWarResultCanvas instance;

	public Text levelNumberText;
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
		UpdateEquipSmallStatusInfo();
	}

	bool _clear = false;
	NodeWarTableData _currentNodeWarTableData;
	bool _challengeMode;
	bool _boostApplied;
	List<ItemInstance> _listGrantItem;
	public void RefreshInfo(bool clear, NodeWarTableData currentNodeWarTableData, bool challengeMode, string jsonItemGrantResults)
	{
		_clear = clear;
		_currentNodeWarTableData = currentNodeWarTableData;
		_challengeMode = challengeMode;
		_boostApplied = (PlayerData.instance.nodeWarBoostRemainCount > 0);

		levelNumberText.text = string.Format("LEVEL {0}", currentNodeWarTableData.level);
		resultText.text = UIString.instance.GetString(clear ? "GameUI_Success" : "GameUI_Failure");
		resultText.color = clear ? new Color(0.0f, 0.733f, 0.792f) : new Color(0.792f, 0.152f, 0.0f);

		if (jsonItemGrantResults != "")
			_listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		gameObject.SetActive(true);

		// NodeWarResultCanvas 닫는 타이밍엔 씬이 날아가는 상태니 미리 RandomBoxScreenCanvas는 켤때 닫아두기로 한다.
		if (clear)
			RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}

	#region NodeWar Result
	int _firstRewardAmount;
	public void OnEventSuccessResult()
	{
		if (_clear)
		{
			if (_challengeMode)
			{
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

	#region NoReward
	public void OnEventNoReward()
	{
		// 보상없음 켜지면 조건에 따라 단계 하락 보여주면서
		if (_challengeMode == false && _currentNodeWarTableData.level > 1)
		{
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_NodeWarLevelDown"), () =>
			{
				PlayFabApiManager.instance.RequestDownNodeWarLevel(() =>
				{
					PlayerData.instance.nodeWarCurrentLevel -= 1;
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarLevelDownToast"), 2.0f);
				});
			});
		}

		// 나가기 버튼 보여준다.
		exitGroupObject.SetActive(true);
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
		_repeatRewardAmount = _currentNodeWarTableData.repeatRewardGold;
		if (_boostApplied)
			_repeatRewardAmount *= BattleInstanceManager.instance.GetCachedGlobalConstantInt("NodeWarRepeatBoost");
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

		if (_boostApplied)
		{
			yield return new WaitForSecondsRealtime(0.3f);
			goldBoostText.text = UIString.instance.GetString("GameUI_NodeWarBoostTimes", BattleInstanceManager.instance.GetCachedGlobalConstantInt("NodeWarRepeatBoost"));
			goldBoostText.gameObject.SetActive(true);
		}
		
		itemGroupObject.SetActive(true);
		StartCoroutine(ItemProcess());
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

	#region Item
	IEnumerator ItemProcess()
	{
		DOTween.To(() => itemLineSlider.value, x => itemLineSlider.value = x, 1.0f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

		yield return new WaitForSecondsRealtime(0.3f);

		gainItemTextObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.1f);

		// 드랍매니저가 가지고 있는 아이템 리스트에는 아이디밖에 들어있지 않아서 정보를 구성할 수 없기 때문에 사용할 수 없다.
		// 결과패킷으로 받은 실제 아이템 리스트를 사용해야한다.
		if (_listGrantItem == null || _listGrantItem.Count == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		itemScrollViewObject.SetActive(true);

		for (int i = 0; i < _listGrantItem.Count; ++i)
		{
			EquipData newEquipData = new EquipData();
			newEquipData.equipId = _listGrantItem[i].ItemId;
			newEquipData.Initialize(_listGrantItem[i].CustomData);

			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(newEquipData, OnClickListItem);
			yield return new WaitForSecondsRealtime(0.2f);
		}

		if (_boostApplied)
		{
			itemBoostText.text = UIString.instance.GetString("GameUI_NodeWarBoostPlus");
			itemBoostText.gameObject.SetActive(true);
			yield return new WaitForSecondsRealtime(0.3f);
		}

		exitGroupObject.SetActive(true);
	}

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
		SceneManager.LoadScene(0);
	}
}