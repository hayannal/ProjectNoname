using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayFab.ClientModels;

public class BattleResultCanvas : MonoBehaviour
{
	public static BattleResultCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.battleResultPrefab).GetComponent<BattleResultCanvas>();
			}
			return _instance;
		}
	}
	static BattleResultCanvas _instance = null;

	public Text chapterRomanNumberText;
	public Text chapterNameText;
	public Button chapterInfoButton;
	public Image chapterInfoImage;

	public Text stageValueText;
	public Text stageValueMaxText;
	public Text clearText;
	public Text clearRewardText;
	public GameObject newRecordTextObject;
	public GameObject newRecordBonusTextObject;

	public GameObject goldGroupObject;
	public DOTweenAnimation goldImageTweenAnimation;
	public Text goldValueText;

	public GameObject sealGroupObject;
	public RectTransform sealInfoButtonTransform;
	public RectTransform sealIconTransform;
	public RectTransform sealIconSubTransform;
	public Text sealCompletedText;
	public GameObject sealImageGroupObject;
	public GameObject[] sealCountImageList;
	public GameObject sealUnacquiredTextObject;

	public GameObject chaosFragmentGroupObject;
	public RectTransform chaosFragmentInfoButtonTransform;
	public Text chaosFragmentValueText;

	public GameObject itemGroupObject;
	public Slider itemLineSlider;
	public GameObject gainItemTextObject;
	public GameObject itemScrollViewObject;
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public EquipListStatusInfo equipSmallStatusInfo;
	public RectTransform smallStatusBackBlurImage;

	public GameObject exitGroupObject;
	public GameObject challengeFailTextObject;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

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
		UpdateStageText();
		UpdateGoldText();
		UpdateChaosFragmentText();
		UpdateEquipSmallStatusInfo();
	}

	bool _clear = false;
	bool _newRecord = false;
	List<ItemInstance> _listGrantItem;
	public void RefreshChapterInfo(string jsonItemGrantResults)
	{
		_clear = true;
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie())
			_clear = false;

		int playStage = StageManager.instance.playStage;
		int maxStage = StageManager.instance.GetCurrentMaxStage();
		if (_clear && playStage != maxStage)
			_clear = false;

		if (_clear == false)
		{
			// 다른 곳과 달리 PlayerData.instance.currentChallengeMode를 사용하면 안되는 곳이다.
			// 카오스가 열리기 전 챕터도 포함시켜야하므로 직접 검사하기로 한다.
			if (ContentsManager.IsTutorialChapter() == false && PlayerData.instance.selectedChapter == PlayerData.instance.highestPlayChapter && PlayerData.instance.chaosMode == false)
			{
				if (PlayerData.instance.highestClearStage < playStage - 1)
					_newRecord = true;
			}
		}

		stageValueMaxText.text = string.Format("/ {0}", maxStage);

		if (StageManager.instance.playChapter == 0 && PlayerData.instance.ContainsActor("Actor1002") == false)
		{
			clearRewardText.SetLocalizedText(UIString.instance.GetString(_clear ? "GameUI_Chp0ClearRewardGot" : "GameUI_Chp0ClearReward", CharacterData.GetLowNameByActorId("Actor1002")));
			clearRewardText.gameObject.SetActive(true);
		}

		if (StageManager.instance.playChapter == 1 && PlayerData.instance.ContainsActor("Actor2103") == false && _newRecord == false)
		{
			clearRewardText.SetLocalizedText(UIString.instance.GetString(_clear ? "GameUI_Chp1ClearRewardGot" : "GameUI_Chp1ClearReward", CharacterData.GetLowNameByActorId("Actor2103")));
			clearRewardText.gameObject.SetActive(true);
		}

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		chapterRomanNumberText.text = SwapCanvas.GetChapterRomanNumberString(StageManager.instance.playChapter);
		_currentChaosMode = PlayerData.instance.currentChaosMode;
		if (_currentChaosMode)
			chapterNameText.SetLocalizedText(UIString.instance.GetString("GameUI_ChaosMode"));
		else
			chapterNameText.SetLocalizedText(UIString.instance.GetString(chapterTableData.nameId));

		if (PlayerData.instance.currentChallengeMode && _clear == false && EventManager.instance.IsCompleteServerEvent(EventManager.eServerEvent.chaos))
			challengeFailTextObject.SetActive(true);

		if (jsonItemGrantResults != "")
			_listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		gameObject.SetActive(true);

		StartCoroutine(BgmProcess());
	}

	IEnumerator BgmProcess()
	{
		yield return new WaitForSecondsRealtime(0.8f);

		SoundManager.instance.PlaySFX(_clear ? "BattleWin" : "BattleEnd");

		yield return new WaitForSecondsRealtime((_clear ? 12.0f : 10.0f) + 3.0f);

		SoundManager.instance.PlayBgm("BGM_BattleEnd", 3.0f);
	}

	bool _currentChaosMode = false;
	public void OnClickChapterInfoButton()
	{
		string descriptionId = "";
		if (_currentChaosMode)
			descriptionId = "GameUI_ChaosModeDesc";
		else
		{
			ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
			if (chapterTableData != null)
				descriptionId = chapterTableData.descriptionId;
		}

		if (string.IsNullOrEmpty(descriptionId))
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(descriptionId), 300, chapterInfoButton.transform, new Vector2(0.0f, -35.0f));
	}

	#region Stage
	public void OnEventIncreaseStage()
	{
		_stageChangeRemainTime = stageChangeTime;
		_stageChangeSpeed = (StageManager.instance.playStage - 1) / _stageChangeRemainTime;
		_currentStage = 1.0f;
		_updateStageText = true;

		StartCoroutine(StageProcess());
	}

	IEnumerator StageProcess()
	{
		yield return new WaitForSecondsRealtime(stageChangeTime);

		if (_clear)
			clearText.gameObject.SetActive(true);
		else if (_newRecord)
			newRecordTextObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.2f);

		if (_clear == false && _newRecord && clearRewardText.gameObject.activeSelf == false)
			newRecordBonusTextObject.SetActive(true);

		goldGroupObject.SetActive(true);
	}

	const float stageChangeTime = 0.4f;
	float _stageChangeRemainTime;
	float _stageChangeSpeed;
	float _currentStage;
	int _lastStage;
	bool _updateStageText;
	void UpdateStageText()
	{
		if (_updateStageText == false)
			return;

		_currentStage += _stageChangeSpeed * Time.unscaledDeltaTime;
		int currentStageInt = (int)_currentStage;
		if (currentStageInt >= StageManager.instance.playStage)
		{
			currentStageInt = StageManager.instance.playStage;
			_updateStageText = false;
		}
		if (currentStageInt != _lastStage)
		{
			_lastStage = currentStageInt;
			stageValueText.text = _lastStage.ToString();
		}
	}
	#endregion

	#region Gold
	public void OnEventIncreaseGold()
	{
		_goldChangeRemainTime = goldChangeTime;
		_goldChangeSpeed = DropManager.instance.GetStackedDropGold() / _goldChangeRemainTime;
		_currentGold = 0.0f;
		_updateGoldText = true;

		goldImageTweenAnimation.DOPlay();

		StartCoroutine(GoldProcess());
	}

	IEnumerator GoldProcess()
	{
		yield return new WaitForSecondsRealtime(goldChangeTime);
		
		if (StageManager.instance.playChapter == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		if (PlayerData.instance.sharedDailyBoxOpened)
		{
			sealCompletedText.gameObject.SetActive(true);
			sealIconTransform.gameObject.SetActive(false);
		}
		else
		{
			int sealCount = DropManager.instance.GetStackedDropSeal();
			if (sealCount == 0)
			{
				sealUnacquiredTextObject.SetActive(true);
			}
			else if (sealCount <= 6)
			{
				sealImageGroupObject.SetActive(true);
			}
			else
			{
				sealImageGroupObject.SetActive(true);
				sealIconTransform.anchoredPosition = sealIconSubTransform.anchoredPosition;
			}
		}

		sealGroupObject.SetActive(true);

		yield return null;
		yield return null;

		sealInfoButtonTransform.pivot = new Vector2(0.5f, sealInfoButtonTransform.pivot.y);
		sealInfoButtonTransform.anchoredPosition = new Vector2(sealInfoButtonTransform.sizeDelta.x * 0.5f, sealInfoButtonTransform.anchoredPosition.y);
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
		if (currentGoldInt >= DropManager.instance.GetStackedDropGold())
		{
			currentGoldInt = DropManager.instance.GetStackedDropGold();
			_updateGoldText = false;
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}
	#endregion

	#region Seal
	public void OnEventIncreaseSeal()
	{
		StartCoroutine(SealProcess());
	}

	IEnumerator SealProcess()
	{
		bool nextStep = false;
		if (sealCompletedText.gameObject.activeSelf || sealUnacquiredTextObject.activeSelf)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			nextStep = true;
		}

		if (sealImageGroupObject.activeSelf && nextStep == false)
		{
			int sealCount = DropManager.instance.GetStackedDropSeal();
			if (sealCount > sealCountImageList.Length)
				sealCount = sealCountImageList.Length;
			for (int i = 0; i < sealCount; ++i)
			{
				sealCountImageList[i].SetActive(true);
				yield return new WaitForSecondsRealtime(0.2f);
			}
			nextStep = true;
		}

		// 인장 모여주고나서 다음 스텝으로 넘어가지 않는 유저들이 생겨서 강제로 켜기로 해본다.
		if (nextStep == false)
			nextStep = true;

		if (nextStep)
		{
			if (_currentChaosMode)
			{
				chaosFragmentGroupObject.SetActive(true);

				yield return null;
				yield return null;

				chaosFragmentInfoButtonTransform.pivot = new Vector2(0.5f, chaosFragmentInfoButtonTransform.pivot.y);
				chaosFragmentInfoButtonTransform.anchoredPosition = new Vector2(chaosFragmentInfoButtonTransform.sizeDelta.x * 0.5f, chaosFragmentInfoButtonTransform.anchoredPosition.y);
			}
			else
			{
				itemGroupObject.SetActive(true);
				StartCoroutine(ItemProcess());
			}
		}
	}

	public void OnClickSealInfoButton()
	{
		string text = UIString.instance.GetString("GameUI_SealDesc");

		// check highest
		if (PlayerData.instance.highestPlayChapter != PlayerData.instance.selectedChapter && PlayerData.instance.sharedDailyBoxOpened == false)
			text = string.Format("{0}.\n\n{1}.", text, UIString.instance.GetString("GameUI_SealAddedDesc"));

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 200, sealInfoButtonTransform, new Vector2(25.0f, -35.0f));
	}
	#endregion

	#region Chaos Fragment
	public void OnEventIncreaseChaosFragment()
	{
		_chaosFragmentChangeRemainTime = chaosFragmentChangeTime;
		_chaosFragmentChangeSpeed = DropManager.instance.GetStackedDropChaosFragment() / _chaosFragmentChangeRemainTime;
		_currentChaosFragment = 0.0f;
		_updateChaosFragmentText = true;

		StartCoroutine(ChaosFragmentProcess());
	}

	IEnumerator ChaosFragmentProcess()
	{
		yield return new WaitForSecondsRealtime(chaosFragmentChangeTime);

		itemGroupObject.SetActive(true);
		StartCoroutine(ItemProcess());
	}

	const float chaosFragmentChangeTime = 0.4f;
	float _chaosFragmentChangeRemainTime;
	float _chaosFragmentChangeSpeed;
	float _currentChaosFragment;
	int _lastChaosFragment;
	bool _updateChaosFragmentText;
	void UpdateChaosFragmentText()
	{
		if (_updateChaosFragmentText == false)
			return;

		_currentChaosFragment += _chaosFragmentChangeSpeed * Time.unscaledDeltaTime;
		int currentChaosFragmentInt = (int)_currentChaosFragment;
		if (currentChaosFragmentInt >= DropManager.instance.GetStackedDropChaosFragment())
		{
			currentChaosFragmentInt = DropManager.instance.GetStackedDropChaosFragment();
			_updateChaosFragmentText = false;
		}
		if (currentChaosFragmentInt != _lastChaosFragment)
		{
			_lastChaosFragment = currentChaosFragmentInt;
			chaosFragmentValueText.text = _lastChaosFragment.ToString("N0");
		}
	}

	public void OnClickChaosFragmentInfoButton()
	{
		string text = UIString.instance.GetString("GameUI_ChaosFragmentDesc");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 200, chaosFragmentInfoButtonTransform, new Vector2(25.0f, -35.0f));
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
		bool result = EventManager.instance.OnExitBattleResult();
		if (result == false)
			SceneManager.LoadScene(0);
	}
}