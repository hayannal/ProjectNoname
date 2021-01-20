﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.Text;

public class SwapCanvas : MonoBehaviour
{
	public static SwapCanvas instance;

	public GameObject chapterBackgroundObject;
	public GameObject swapBackgroundObject;
	public GameObject chapterRootObject;
	public GameObject swapRootObject;
	public Text chapterRomanNumberText;
	public Text chapterNameText;
	public Text suggestPowerLevelText;
	public Button chapterInfoButton;
	public Image chapterInfoImage;
	public Transform previewRootTransform;
	public Text bossNameText;
	public Button bossInfoButton;
	public Text stagePenaltyText;
	public Text selectResultText;

	public SortButton sortButton;
	SortButton.eSortType _currentSortType;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SwapCanvasListItem>
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

		// 생성되는 프레임에도 제대로 동작하려면 start에서도 호출해야한다.
		//RefreshContentPosition();
	}

	void OnEnable()
	{
		if (sortButton.onChangedCallback == null)
		{
			int sortType = PlayerPrefs.GetInt("_SwapSort", 0);
			_currentSortType = (SortButton.eSortType)sortType;
			sortButton.SetSortType(_currentSortType);
			sortButton.onChangedCallback = OnChangedSortType;
		}

		if (MainSceneBuilder.instance.lobby)
			RefreshChapterInfo();
		else if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			RefreshNodeWarInfo();
		else if (PlayerData.instance.currentChaosMode && string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName))
			RefreshChapterInfo();
		else
			RefreshSwapInfo();
		RefreshGrid(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void RefreshCommonInfo()
	{
		// 플레이 중에는 현재 적용중인 패널티를 보여주면 된다. 로비에서 뜰때는 보여주기가 애매해서 띄우지 않는다.
		stagePenaltyText.gameObject.SetActive(false);
		string penaltyString = "";
		if (MainSceneBuilder.instance.lobby == false && BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData != null)
		{
			StagePenaltyTableData stagePenaltyTableData = BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData;
			string[] nameParameterList = UIString.instance.ParseParameterString(stagePenaltyTableData.nameParameter);
			penaltyString = UIString.instance.GetString(stagePenaltyTableData.penaltyName, nameParameterList);
		}
		// 이렇게 게이트필라 쳐서 나오는 SwapCanvas에서는 1층에 설정된거 뽑아와서 보여줄 수도 있긴 한데(카오스에 설정된 것도 보여줄 수 있다.)
		// 카오스는 이런거 신경쓰지 않고 입장하기로 했기 때문에
		// 그리고 레벨이 낮을때만 뜨는것도 이상한거 같아서(챕터 선택창에 뜨기도 했었으니) 아예 로비에서 뜨는 SwapCanvas에서는 보여주지 않기로 결정했다.
		//else		
		//	penaltyString = ChapterCanvas.GetPenaltyString(StageDataManager.instance.nextStageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}

		selectResultText.text = "";

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		// 파워레벨은 항상 표시
		string rangeString = UIString.instance.GetString("GameUI_NumberRange", chapterTableData.suggestedPowerLevel, chapterTableData.suggestedMaxPowerLevel);
		suggestPowerLevelText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("GameUI_SuggestedPowerLevel"), rangeString));
	}

	void RefreshNodeWarInfo()
	{
		chapterBackgroundObject.SetActive(true);
		swapBackgroundObject.SetActive(false);
		chapterRootObject.SetActive(true);
		swapRootObject.SetActive(false);

		int currentLevel = PlayerData.instance.nodeWarCurrentLevel;
		if (currentLevel == 0)
			currentLevel = 1;
		else if (currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxNodeWarLevel"))
		{
		}
		else
			currentLevel = currentLevel + 1;
		chapterRomanNumberText.text = string.Format("LEVEL {0}", currentLevel);
		chapterNameText.SetLocalizedText(UIString.instance.GetString("GameUI_NodeWarMode"));

		stagePenaltyText.gameObject.SetActive(false);
		selectResultText.text = "";
		suggestPowerLevelText.text = "";
	}

	void RefreshChapterInfo()
	{
		chapterBackgroundObject.SetActive(true);
		swapBackgroundObject.SetActive(false);
		chapterRootObject.SetActive(true);
		swapRootObject.SetActive(false);

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		chapterRomanNumberText.text = GetChapterRomanNumberString(StageManager.instance.playChapter);
		if (PlayerData.instance.currentChaosMode)
		{
			chapterNameText.SetLocalizedText(UIString.instance.GetString("GameUI_ChaosMode"));
			//chapterInfoButton.interactable = false;
			//chapterInfoImage.gameObject.SetActive(false);
		}
		else
		{
			chapterNameText.SetLocalizedText(UIString.instance.GetString(chapterTableData.nameId));
			//chapterInfoButton.interactable = true;
			//chapterInfoImage.gameObject.SetActive(true);
		}

		RefreshCommonInfo();
	}

	public static string GetChapterRomanNumberString(int chapter)
	{
		string romanNumberString = UIString.instance.GetString(string.Format("GameUI_RomanNumber{0}", chapter));
		return UIString.instance.GetString("GameUI_Chapter", romanNumberString);
	}

	GameObject _cachedPreviewObject;
	void RefreshSwapInfo()
	{
		chapterBackgroundObject.SetActive(false);
		swapBackgroundObject.SetActive(true);
		chapterRootObject.SetActive(false);
		swapRootObject.SetActive(true);

		if (_cachedPreviewObject != null)
		{
			_cachedPreviewObject.SetActive(false);
			_cachedPreviewObject = null;
		}

		MapTableData nextBossMapTableData = StageManager.instance.nextBossMapTableData;
		if (nextBossMapTableData == null)
			return;

		if (string.IsNullOrEmpty(nextBossMapTableData.bossName) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", nextBossMapTableData.bossName), "Preview", (prefab) =>
			{
				_cachedPreviewObject = UIInstanceManager.instance.GetCachedObject(prefab, previewRootTransform);
			});
		}
		bossNameText.SetLocalizedText(UIString.instance.GetString(nextBossMapTableData.nameId));

		RefreshCommonInfo();
	}


	void OnChangedSortType(SortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_SwapSort", sortTypeValue);
		RefreshGrid(false);
	}

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	void RefreshGrid(bool onEnable)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		string[] suggestedActorIdList = null;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar()) { }
		else if (MainSceneBuilder.instance.lobby == false)
		{
			MapTableData nextMapTableData = StageManager.instance.nextMapTableData;
			if (nextMapTableData != null && string.IsNullOrEmpty(nextMapTableData.bossName) == false)
			{
				suggestedActorIdList = nextMapTableData.suggestedActorId;

				// 전에는 여기서 suggestedActorIdList만 넘겼었는데 이젠 penalty타입도 계산해놨다가 표시하는 곳에서 회색으로 알려줘야한다.
				RefreshPenaltyPowerSource();
			}
		}

		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		switch (_currentSortType)
		{
			case SortButton.eSortType.PowerLevel:
				listCharacterData.Sort(sortButton.comparisonPowerLevel);
				break;
			case SortButton.eSortType.PowerLevelDescending:
				listCharacterData.Sort(sortButton.comparisonPowerLevelDescending);
				break;
			case SortButton.eSortType.Transcend:
				listCharacterData.Sort(sortButton.comparisonTranscendLevel);
				break;
			case SortButton.eSortType.PowerSource:
				listCharacterData.Sort(sortButton.comparisonPowerSource);
				break;
			case SortButton.eSortType.Grade:
				listCharacterData.Sort(sortButton.comparisonGrade);
				break;
		}

		int firstIndex = -1;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(listCharacterData[i].actorId, listCharacterData[i].powerLevel, SwapCanvasListItem.GetPowerLevelColorState(listCharacterData[i]), listCharacterData[i].transcendLevel, chapterTableData.suggestedPowerLevel, suggestedActorIdList, _listCachedPenaltyPowerSource, OnClickListItem);
			_listSwapCanvasListItem.Add(swapCanvasListItem);

			if (firstIndex == -1 && listCharacterData[i].actorId != BattleInstanceManager.instance.playerActor.actorId)
				firstIndex = i;
		}
		if (onEnable && firstIndex != -1)
			OnClickListItem(_listSwapCanvasListItem[firstIndex].actorId);
		else
			OnClickListItem(_selectedActorId);

		// 항목이 적을땐 가운데 정렬 하려고 했는데 안쓰게 되면서 지울까 하다가 혹시 몰라서 코드는 남겨둔다.
		//RefreshContentPosition();
	}

	List<int> _listCachedPenaltyPowerSource = new List<int>();
	void RefreshPenaltyPowerSource()
	{
		_listCachedPenaltyPowerSource.Clear();

		if (MainSceneBuilder.instance.lobby)
			return;
		if (BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData == null)
			return;
		
		StagePenaltyTableData stagePenaltyTableData = BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData;
		for (int i = 0; i < stagePenaltyTableData.affectorValueId.Length; ++i)
		{
			AffectorValueLevelTableData affectorValueLevelTableData = TableDataManager.instance.FindAffectorValueLevelTableData(stagePenaltyTableData.affectorValueId[i], 1);
			if (affectorValueLevelTableData == null)
				continue;

			for (int j = 0; j < affectorValueLevelTableData.conditionValueId.Length; ++j)
			{
				ConditionValueTableData conditionValueTableData = TableDataManager.instance.FindConditionValueTableData(affectorValueLevelTableData.conditionValueId[j]);
				if (conditionValueTableData == null)
					continue;

				if ((Condition.eConditionType)conditionValueTableData.conditionId == Condition.eConditionType.DefenderPowerSource && (Condition.eCompareType)conditionValueTableData.compareType == Condition.eCompareType.Equal)
				{
					int.TryParse(conditionValueTableData.value, out int intValue);
					if (_listCachedPenaltyPowerSource.Contains(intValue) == false)
						_listCachedPenaltyPowerSource.Add(intValue);
				}
			}
		}
	}

	RectTransform _contentParentRectTransform;
	void RefreshContentPosition()
	{
		if (_contentParentRectTransform == null) _contentParentRectTransform = contentRootRectTransform.parent.GetComponent<RectTransform>();
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootRectTransform);
		bool centerPivot = (contentRootRectTransform.rect.height < _contentParentRectTransform.rect.height);
		contentRootRectTransform.pivot = new Vector2(contentRootRectTransform.pivot.x, centerPivot ? 0.5f : 1.0f);
		contentRootRectTransform.anchoredPosition = new Vector2(contentRootRectTransform.anchoredPosition.x, centerPivot ? _contentParentRectTransform.rect.height * -0.5f : 0.0f);
	}

	public void OnClickListItem(string actorId)
	{
		if (BattleInstanceManager.instance.playerActor.actorId == actorId)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NowPlayingCharacter"), 1.0f);
			return;
		}

		_selectedActorId = actorId;

		bool recommanded = false;
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
		{
			bool showSelectObject = (_listSwapCanvasListItem[i].actorId == actorId);
			_listSwapCanvasListItem[i].ShowSelectObject(showSelectObject);
			if (showSelectObject)
				recommanded = _listSwapCanvasListItem[i].recommandedText.gameObject.activeSelf;
		}

		string firstText = "";
		if (MainSceneBuilder.instance.lobby == false && BattleInstanceManager.instance.IsInBattlePlayerList(actorId))
			firstText = UIString.instance.GetString("GameUI_FirstSwapHealNotApplied");

		string secondText = "";
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar()) { }
		else if (chapterTableData != null)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
			if (characterData.powerLevel > chapterTableData.suggestedMaxPowerLevel)
				secondText = UIString.instance.GetString("GameUI_TooPowerfulToReward");
			else if (characterData.powerLevel < chapterTableData.suggestedPowerLevel && recommanded)
				secondText = UIString.instance.GetString("GameUI_TooWeakToBoss");
		}
		bool firstResult = string.IsNullOrEmpty(firstText);
		bool secondResult = string.IsNullOrEmpty(secondText);
		if (firstResult && secondResult)
			selectResultText.text = "";
		else if (firstResult == false && secondResult)
			selectResultText.SetLocalizedText(firstText);
		else if (firstResult && secondResult == false)
			selectResultText.SetLocalizedText(secondText);
		else
			selectResultText.SetLocalizedText(string.Format("{0}\n{1}", firstText, secondText));
	}

	public void OnClickChapterInfoButton()
	{
		string descriptionId = "";
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			descriptionId = "GameUI_NodeWarModeDesc";
		else if (PlayerData.instance.currentChaosMode)
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

	public void OnClickBossInfoButton()
	{
		MapTableData nextBossMapTableData = StageManager.instance.nextBossMapTableData;
		if (nextBossMapTableData == null)
			return;

		string suggestString = GetSuggestString(nextBossMapTableData.descriptionId, nextBossMapTableData.suggestedActorId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, suggestString, 200, bossInfoButton.transform, new Vector2(0.0f, -35.0f));
	}










	string _selectedActorId;
	float _buttonClickTime;
	public void OnClickYesButton()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		// DB


		_buttonClickTime = Time.time;
		DelayedLoadingCanvas.Show(true);

		// 이미 만들었던 플레이어 캐릭터라면 다시 만들필요 없으니 가져다쓰고 없으면 어드레스 로딩을 시작해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_selectedActorId);
		if (playerActor != null)
		{
			SwapCharacter(playerActor);
			return;
		}
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_selectedActorId), "Character", OnLoadedPlayerActor);
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
		// 새 캐릭터 생성 후
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		SwapCharacter(playerActor);
	}

	void SwapCharacter(PlayerActor newPlayerActor)
	{
		// 먼저 교체가능 UI를 끈다.
		PlayerIndicatorCanvas.Show(false, null);

		// PlayerActor에 Swap을 알린다.
		BattleInstanceManager.instance.standbySwapPlayerActor = true;

		// 미리 꺼두면 레벨팩 이전받을 캐릭을 찾지 못해서 안된다. 다 이전시키고 스왑 처리하는 곳에서 알아서 끌테니 그냥 두면 된다.
		//BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);

		// 그리고 기존 캐릭터를 위치 정보 얻어온 후 꺼두고
		Vector3 position = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		// 포지션 맞춰주고
		newPlayerActor.cachedTransform.position = position;

		// 새 캐릭터 재활성화
		if (newPlayerActor.gameObject.activeSelf == false)
			newPlayerActor.gameObject.SetActive(true);

		/////////////////////////////////////////////////////////////////////
		// 여기서 제일 문제가..
		// 생성 직후엔 Start가 호출되지 않은 상태라서 스탯 계산이 아직 안되어있다.
		// 같은 프레임이긴 한데 여기 Swap코드보다 나중에 호출되는 구조다.
		// 결국 순서 맞추려면 PlayerActor Start할때 RegisterBattleInstance 호출될때 Initialize 다 끝난 후
		// 기존 플레이어액터 정보 구해와서 피, 스왑힐,레벨팩 이전 하는게 가장 맞다.
		// 여기서는 아무것도 하지 않는다.
		/////////////////////////////////////////////////////////////////////
		
		// 교체 누른게 아니기때문에 바꾸지 않기로 한다.
		//if (MainSceneBuilder.instance.lobby)
		//	PlayerData.instance.mainCharacterId = newPlayerActor.actorId;

		// 걸린 시간 표시
		float deltaTime = Time.time - _buttonClickTime;
		Debug.LogFormat("Change Time : {0}", deltaTime);

		// OnComplete
		if (MainSceneBuilder.instance.lobby == false)
		{
			// 게이트 필라에게 스왑을 알려서 SwapSuggest를 하지 말라고 알려야한다.
			if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
				GatePillar.instance.OnCompleteStageSwap();
			BattleInstanceManager.instance.FinalizeAllSummonObject();
			ClientSaveData.instance.OnChangedCloseSwap(true);
		}

		// 로딩 대기창 닫는다.
		DelayedLoadingCanvas.Show(false);

		// SwapCanvas를 닫는다.
		gameObject.SetActive(false);
	}

	StringBuilder _stringBuilderFull = new StringBuilder();
	StringBuilder _stringBuilderActor = new StringBuilder();
	string GetSuggestString(string descriptionId, string[] suggestedActorIdList)
	{
		_stringBuilderFull.Remove(0, _stringBuilderFull.Length);
		_stringBuilderActor.Remove(0, _stringBuilderActor.Length);
		for (int i = 0; i < suggestedActorIdList.Length; ++i)
		{
			string actorId = suggestedActorIdList[i];
			string actorName = CharacterData.GetNameByActorId(actorId);
			if (string.IsNullOrEmpty(actorName))
				continue;

			bool applyPenalty = false;
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
			if (_listCachedPenaltyPowerSource != null && _listCachedPenaltyPowerSource.Contains(actorTableData.powerSource)) applyPenalty = true;

			//if (PlayerData.instance.ContainsActor(actorId) == false)
			//	continue;
			if (_stringBuilderActor.Length > 0)
				_stringBuilderActor.Append(", ");
			_stringBuilderActor.Append(applyPenalty ? "<color=#707070>" : "<color=#00AB00>");
			_stringBuilderActor.Append(actorName);
			_stringBuilderActor.Append("</color>");
		}
		if (_stringBuilderActor.Length == 0)
		{
			bool applyPenalty = false;
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(suggestedActorIdList[0]);
			if (_listCachedPenaltyPowerSource != null && _listCachedPenaltyPowerSource.Contains(actorTableData.powerSource)) applyPenalty = true;

			_stringBuilderActor.Append(applyPenalty ? "<color=#707070>" : "<color=#00AB00>");
			_stringBuilderActor.Append(CharacterData.GetNameByActorId(suggestedActorIdList[0]));
			_stringBuilderActor.Append("</color>");
		}
		_stringBuilderFull.AppendFormat(UIString.instance.GetString(descriptionId), _stringBuilderActor.ToString());
		return _stringBuilderFull.ToString();
	}
}