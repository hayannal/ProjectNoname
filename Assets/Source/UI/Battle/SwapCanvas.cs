using System.Collections;
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
		if (MainSceneBuilder.instance.lobby)
			RefreshChapterInfo();
		else if (PlayerData.instance.chaosMode && string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName))
			RefreshChapterInfo();
		else
			RefreshSwapInfo();
		RefreshGrid();

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
		// 챕터 디버프 어펙터는 로비 바로 다음 스테이지에서 뽑아와서 표시해준다.(여기서 넣는거 아니다. 보여주기만 한다.)
		// 없으면 표시하지 않는다.
		// 실제로 넣는건 해당 시점에서 하니 여기서는 신경쓰지 않아도 된다.
		// 카오스에서는 여러개 들어있을 수도 있는데 이땐 아마 설명창에 여러개 중 하나가 되는 식이라고 표시될거다. 통합 스트링 제공.
		// 사실 챕터에 넣을 수 있지만 스테이지에 연결해두는 이유가
		// 언젠가 나중에 챕터 중간에도 이 디버프를 변경시킬 상황이 올까봐 미리 확장시켜서 여기에 두는 것이다.
		stagePenaltyText.gameObject.SetActive(false);
		if (StageDataManager.instance.existNextStageInfo)
		{
			string penaltyString = "";
			if (!string.IsNullOrEmpty(StageDataManager.instance.nextStageTableData.penaltyRepresentative))
			{
				string[] penaltyParameterList = UIString.instance.ParseParameterString(StageDataManager.instance.nextStageTableData.repreParameter);
				penaltyString = UIString.instance.GetString(StageDataManager.instance.nextStageTableData.penaltyRepresentative, penaltyParameterList);
			}
			else
			{
				if (StageDataManager.instance.nextStageTableData.stagePenaltyId.Length == 1)
				{
					// 패널티가 하나만 있을땐 직접 구해와서 표시해준다.
					StagePenaltyTableData stagePenaltyTableData = TableDataManager.instance.FindStagePenaltyTableData(StageDataManager.instance.nextStageTableData.stagePenaltyId[0]);
					if (stagePenaltyTableData != null)
					{
						string[] nameParameterList = UIString.instance.ParseParameterString(stagePenaltyTableData.nameParameter);
						penaltyString = UIString.instance.GetString(stagePenaltyTableData.penaltyName, nameParameterList);
					}
				}
			}
			if (string.IsNullOrEmpty(penaltyString) == false)
			{
				stagePenaltyText.SetLocalizedText(penaltyString);
				stagePenaltyText.gameObject.SetActive(true);
			}
		}

		selectResultText.text = "";

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		// 파워레벨은 항상 표시
		string rangeString = UIString.instance.GetString("GameUI_NumberRange", chapterTableData.suggestedPowerLevel, chapterTableData.suggestedMaxPowerLevel);
		suggestPowerLevelText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("GameUI_SuggestedPowerLevel"), rangeString));
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
		if (PlayerData.instance.chaosMode)
		{
			chapterNameText.SetLocalizedText(UIString.instance.GetString("GameUI_ChaosMode"));
			chapterInfoButton.interactable = false;
			chapterInfoImage.gameObject.SetActive(false);
		}
		else
		{
			chapterNameText.SetLocalizedText(UIString.instance.GetString(chapterTableData.nameId));
			chapterInfoButton.interactable = true;
			chapterInfoImage.gameObject.SetActive(true);
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

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		string[] suggestedActorIdList = null;
		if (MainSceneBuilder.instance.lobby == false)
		{
			MapTableData nextBossMapTableData = StageManager.instance.nextBossMapTableData;
			if (nextBossMapTableData != null)
				suggestedActorIdList = nextBossMapTableData.suggestedActorId;
		}

		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		listCharacterData.Sort(delegate (CharacterData x, CharacterData y)
		{
			if (x.powerLevel > y.powerLevel) return -1;
			else if (x.powerLevel < y.powerLevel) return 1;
			ActorTableData xActorTableData = TableDataManager.instance.FindActorTableData(x.actorId);
			ActorTableData yActorTableData = TableDataManager.instance.FindActorTableData(y.actorId);
			if (xActorTableData != null && yActorTableData != null)
			{
				if (xActorTableData.grade > yActorTableData.grade) return -1;
				else if (xActorTableData.grade < yActorTableData.grade) return 1;
				if (xActorTableData.orderIndex < yActorTableData.orderIndex) return -1;
				else if (xActorTableData.orderIndex > yActorTableData.orderIndex) return 1;
			}
			return 0;
		});
		int firstIndex = -1;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(listCharacterData[i], chapterTableData.suggestedPowerLevel, suggestedActorIdList);
			_listSwapCanvasListItem.Add(swapCanvasListItem);

			if (firstIndex == -1 && listCharacterData[i].actorId != BattleInstanceManager.instance.playerActor.actorId)
				firstIndex = i;
		}
		if (firstIndex != -1)
			OnClickListItem(_listSwapCanvasListItem[firstIndex].actorId);

		// 항목이 적을땐 가운데 정렬 하려고 했는데 안쓰게 되면서 지울까 하다가 혹시 몰라서 코드는 남겨둔다.
		//RefreshContentPosition();
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

		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].ShowSelectObject(_listSwapCanvasListItem[i].actorId == actorId);

		if (!MainSceneBuilder.instance.lobby)
		{
			if (StageManager.instance.IsInBattlePlayerList(actorId))
				selectResultText.SetLocalizedText(UIString.instance.GetString("GameUI_FirstSwapHealNotApplied"));
			else
				selectResultText.text = "";
		}
	}

	public void OnClickChapterInfoButton()
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(chapterTableData.descriptionId), 200, chapterInfoButton.transform, new Vector2(0.0f, -35.0f));
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
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_selectedActorId), "", OnLoadedPlayerActor);
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

		// PlayerData 에 등록
		if (MainSceneBuilder.instance.lobby)
			PlayerData.instance.mainCharacterId = newPlayerActor.actorId;

		// 걸린 시간 표시
		float deltaTime = Time.time - _buttonClickTime;
		Debug.LogFormat("Change Time : {0}", deltaTime);

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
			if (PlayerData.instance.ContainsActor(actorId) == false)
				continue;
			if (_stringBuilderActor.Length > 0)
				_stringBuilderActor.Append(", ");
			_stringBuilderActor.Append("<color=#00AB00>");
			_stringBuilderActor.Append(CharacterData.GetNameByActorId(actorId));
			_stringBuilderActor.Append("</color>");
		}
		if (_stringBuilderActor.Length == 0)
		{
			_stringBuilderActor.Append("<color=#00AB00>");
			_stringBuilderActor.Append(CharacterData.GetNameByActorId(suggestedActorIdList[0]));
			_stringBuilderActor.Append("</color>");
		}
		_stringBuilderFull.AppendFormat(UIString.instance.GetString(descriptionId), _stringBuilderActor.ToString());
		return _stringBuilderFull.ToString();
	}
}