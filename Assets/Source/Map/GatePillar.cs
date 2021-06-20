﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class GatePillar : MonoBehaviour
{
	public static GatePillar instance;

	public GameObject meshColliderObject;
	public GameObject particleRootObject;
	public GameObject changeEffectParticleRootObject;

	// 동적 로드하는 것들을 외부로 뺄까 했는데 크기가 크지 않고 자주 쓰이는거라면 굳이 뺄필요가 없어서 안빼기로 한다.
	ObjectIndicatorCanvas _objectIndicatorCanvas;
	public GameObject descriptionObjectIndicatorPrefab;
	public float descriptionObjectIndicatorShowDelayTime = 5.0f;
	public float energyGaugeShowDelayTime = 0.2f;
	const float purifyShowDelayTime = 0.3f;
	const float chaosPurifierShowDelayTime = 0.15f;

	public Canvas worldCanvas;
	public Text floorText;
	public GameObject chaosRootObject;
	public Image[] chaosPurifyImageList;
	public Sprite purifyFillSprite;
	public Sprite purifyStrokeSprite;
	public Color purifyNormalColor;
	public Color purifyHighlightColor;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	float _spawnTime;
	void OnEnable()
	{
		// 보스 게이트필라가 추가되면서 instance가 두개 생겼다.
		// instance 구조를 뽑아버릴까 하다가
		// 항상 하나의 게이트필라만 켜지는 구조라서 OnEnable에서 덮어쓰는거로 처리해본다.
		instance = this;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		floorText.text = "";

		_spawnTime = Time.time;
		chaosRootObject.SetActive(false);
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
		{
			if (PlayerData.instance.currentChaosMode)
			{
				_purifyCountShowRemainTime = purifyShowDelayTime;
				_maxPurify = (PlayerData.instance.purifyCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("PurifyMaxCount"));
			}
			if (ContentsManager.IsTutorialChapter() == false && PlayerData.instance.lobbyDownloadState == false && EnergyGaugeCanvas.instance == null)
			{
				// 일부러 조금 뒤에 보이게 한다. 초기 로딩 줄이기 위해.
				_energyGaugeShowRemainTime = energyGaugeShowDelayTime;
			}
			if (ContentsManager.IsTutorialChapter() == false && ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chapter) == false)
				return;
		}
		// 보스 인디케이터는 빠르게 등장한다.
		if (string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName))
			_descriptionObjectIndicatorShowRemainTime = descriptionObjectIndicatorShowDelayTime;
		else
			_descriptionObjectIndicatorShowRemainTime = 0.001f;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false && StageManager.instance.playStage != StageManager.instance.GetCurrentMaxStage())
			floorText.text = (StageManager.instance.playStage + 1).ToString();
	}

	void OnDisable()
	{
		raycastCount = 0;
		particleRootObject.SetActive(false);
		changeEffectParticleRootObject.SetActive(false);
		
		_checkedStageSwapSuggest = false;

		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void ResetFlagForServerFailure()
	{
		_spawnTime = Time.time;
		raycastCount = 0;
		changeEffectParticleRootObject.SetActive(false);
	}

	float _descriptionObjectIndicatorShowRemainTime;
	float _energyGaugeShowRemainTime;
	float _purifyCountShowRemainTime;
	float _chaosPurifierShowRemainTime;
	bool _maxPurify;
	void Update()
	{
		// 타이틀 캔버스와 상관없이 에너지 게이지를 띄워야한다면 시간 체크 후 띄운다.
		if (_energyGaugeShowRemainTime > 0.0f)
		{
			_energyGaugeShowRemainTime -= Time.deltaTime;
			if (_energyGaugeShowRemainTime <= 0.0f)
			{
				_energyGaugeShowRemainTime = 0.0f;
				AddressableAssetLoadManager.GetAddressableGameObject("EnergyGaugeCanvas", "Canvas", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
				});
				return;
			}
		}

		if (_purifyCountShowRemainTime > 0.0f)
		{
			_purifyCountShowRemainTime -= Time.deltaTime;
			if (_purifyCountShowRemainTime <= 0.0f)
			{
				_purifyCountShowRemainTime = 0.0f;
				RefreshPurify();
			}
		}

		if (OpenChaosEventGatePillar.instance != null && OpenChaosEventGatePillar.instance.gameObject.activeSelf)
			return;
		if (OpenTimeSpacePortal.instance != null && EventInputLockCanvas.instance != null && EventInputLockCanvas.instance.gameObject.activeSelf)
			return;

		if (_chaosPurifierShowRemainTime > 0.0f)
		{
			_chaosPurifierShowRemainTime -= Time.deltaTime;
			if (_chaosPurifierShowRemainTime <= 0.0f)
			{
				_chaosPurifierShowRemainTime = 0.0f;
				AddressableAssetLoadManager.GetAddressableGameObject("ChaosPurifierLever", "Map", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
				});
			}
		}

		if (TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf && PlayerData.instance.currentChallengeMode == false)
			return;
		if (RandomBoxScreenCanvas.instance != null && RandomBoxScreenCanvas.instance.gameObject.activeSelf)
			return;

		// 설명 인디케이터는 타이틀 있을 경우엔 안나오는게 맞다. 지나가고 시간 재는게 맞다.
		if (_descriptionObjectIndicatorShowRemainTime > 0.0f)
		{
			_descriptionObjectIndicatorShowRemainTime -= Time.deltaTime;
			if (_descriptionObjectIndicatorShowRemainTime <= 0.0f)
			{
				_descriptionObjectIndicatorShowRemainTime = 0.0f;
				_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(descriptionObjectIndicatorPrefab);
				_objectIndicatorCanvas.targetTransform = cachedTransform;

				RefreshChapterText();
			}
		}
	}

	#region Gacha
	// 원래는 없었다가 가차 하면서 생긴 함수. 임시로 인디케이터 하이드 시키는 기능이다.
	public bool IsShowIndicatorCanvas()
	{
		if (_objectIndicatorCanvas == null)
			return false;
		if (_objectIndicatorCanvas.gameObject == null)
			return false;
		return _objectIndicatorCanvas.gameObject.activeSelf;
	}

	public void HideIndicatorCanvas(bool hide)
	{
		_objectIndicatorCanvas.gameObject.SetActive(!hide);
	}
	#endregion

	public void RefreshChapterText()
	{
		string text = "";
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
		{
			if (ContentsManager.IsTutorialChapter())
				text = UIString.instance.GetString("GameUI_TouchToMove");
			else if (PlayerData.instance.currentChaosMode)
				text = UIString.instance.GetString("GameUI_ChapterInIndicator", PlayerData.instance.selectedChapter, UIString.instance.GetString("GameUI_ChapterIndicatorParam"));
			else if (PlayerData.instance.currentChallengeMode && EventManager.instance.IsCompleteServerEvent(EventManager.eServerEvent.chaos))
				text = UIString.instance.GetString("GameUI_ChapterInIndicator", PlayerData.instance.selectedChapter, UIString.instance.GetString("GameUI_ChapterIndicatorChallenge"));
			else
				text = UIString.instance.GetString("GameUI_ChapterInIndicator", PlayerData.instance.selectedChapter, "");
		}
		else
		{
			if (string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName))
				text = UIString.instance.GetString("GameUI_TouchToMove");
		}
		if (string.IsNullOrEmpty(text))
			return;

		DescriptionObjectIndicatorCanvas descriptionObjectIndicatorCanvas = _objectIndicatorCanvas as DescriptionObjectIndicatorCanvas;
		if (descriptionObjectIndicatorCanvas != null)
			descriptionObjectIndicatorCanvas.contextText.SetLocalizedText(text);
	}

	public void RefreshPurify(bool onlyRefreshPurifyImage = false)
	{
		_maxPurify = (PlayerData.instance.purifyCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("PurifyMaxCount"));
		for (int i = 0; i < chaosPurifyImageList.Length; ++i)
		{
			chaosPurifyImageList[i].sprite = (i < PlayerData.instance.purifyCount) ? purifyFillSprite : purifyStrokeSprite;
			chaosPurifyImageList[i].color = _maxPurify ? purifyHighlightColor : purifyNormalColor;
		}
		chaosRootObject.SetActive(true);

		if (onlyRefreshPurifyImage)
			return;

		SetChaosPurifierLeverShowRemainTime();
	}

	public void SetChaosPurifierLeverShowRemainTime()
	{
		// 마지막 챕터에서는 어차피 도전모드로 갈수없기 때문에 레버를 만들 필요가 없다.
		int chapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		if (_maxPurify == false && PlayerData.instance.selectedChapter < chapterLimit)
			_chaosPurifierShowRemainTime = chaosPurifierShowDelayTime;
	}

	void OnCollisionEnter(Collision collision)
	{
		if (_processing)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			if (CheckCollider(contact.otherCollider) == false)
				continue;
			if (CheckNextMap() == false)
				continue;

			Timing.RunCoroutine(NextMapProcess());
			break;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_processing)
			return;
		if (CheckCollider(other) == false)
			return;
		if (CheckNextMap() == false)
			return;

		Timing.RunCoroutine(NextMapProcess());
	}

	public int raycastCount { get; set; }
	bool CheckCollider(Collider collider)
	{
		if (collider == null)
			return false;
		HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(collider);
		// _disableSelfObjectAfterCollision 켜지는 HitObject들은 하필 게이트 필라에 다다르는 순간 바로 꺼져버린다.
		// 풀링에서 빠져버리기 때문에 위 함수로 얻어올 수 없게 되는데
		// 그렇다고 hitObject 컬리더 함수에서 직접 하기엔 괜히 로직만 복잡해지고
		// 그렇다고 한프레임 살려두다 죽이는것도 지저분하다.
		// 그래서 어차피 성능에 영향 심하게 주는 함수도 아니고, 몹 다 잡고난 다음이니 GetComponent 호출하게 한다.
		if (hitObject == null)
			hitObject = collider.GetComponent<HitObject>();
		if (hitObject == null)
			return false;
		if (hitObject.statusStructForHitObject.teamId == (int)Team.eTeamID.DefaultMonster)
			return false;
		if (hitObject.GetGatePillarCompareTime() < _spawnTime)
			return false;
		if (raycastCount == 0)
			return false;
		if (SwapCanvas.instance != null && SwapCanvas.instance.gameObject.activeSelf)
			return false;
		return true;
	}

	public void CheckHitObject(int teamId, float gatePillarCompareTime, Collider collider)
	{
		if (collider.gameObject != meshColliderObject)
			return;
		if (teamId == (int)Team.eTeamID.DefaultMonster)
			return;
		if (gatePillarCompareTime < _spawnTime)
			return;
		if (raycastCount == 0)
			return;
		if (CheckNextMap() == false)
			return;

		Timing.RunCoroutine(NextMapProcess());
	}

	#region InProgressGame
	public void EnterInProgressGame()
	{
		// 이미 호출하는 부분에서 스테이지 셋팅은 해놨을거다. 여기선 평소에 하던대로 이동하면 된다.
		Timing.RunCoroutine(NextMapProcess(true));
	}
	#endregion

	// 전엔 OptionManager에 두고 클라가 한번 켜진 상태에선 기억해두려고 했는데
	// 이랬더니 임시교체가 아니라 진짜 교체를 해야했다.
	// 근데 교체메뉴가 따로 있는데 Swap창에서 메인캐릭터가 교체되는게 이상해서 안하려다보니
	// 씬 이동해서 되돌아올때 다시 물어보는 절차가 필요해졌다.
	// 그래서 이렇게 GatePillar가 마지막 suggest한 챕터를 멤버로 가지고 있기로 한다.
	int _suggestedChapter;
	bool CheckNextMap()
	{
		if (SwapCanvas.instance != null && SwapCanvas.instance.gameObject.activeSelf)
			return false;
		if (ConfirmSpendCanvas.instance != null && ConfirmSpendCanvas.instance.gameObject.activeSelf)
			return false;
		if (FullChaosSelectCanvas.instance != null && FullChaosSelectCanvas.instance.gameObject.activeSelf)
			return false;
		if (DelayedLoadingCanvas.IsShow())
			return false;
		if (_processing)
			return false;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return false;
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.processing)
			return false;
		if (RandomBoxScreenCanvas.instance != null && RandomBoxScreenCanvas.instance.gameObject.activeSelf)
			return false;
		if (QuestSelectCanvas.instance != null && QuestSelectCanvas.instance.gameObject.activeSelf)
			return false;
		if (QuestInfoCanvas.instance != null && QuestInfoCanvas.instance.gameObject.activeSelf)
			return false;
		if (QuestEndCanvas.instance != null && QuestEndCanvas.instance.gameObject.activeSelf)
			return false;
		if (StackCanvas.IsStacked())
			return false;

		if (MainSceneBuilder.instance.lobby)
		{
			// check download
			if (PlayerData.instance.lobbyDownloadState)
			{
				// show download info canvas
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterDownload"), 2.0f);
				return false;
			}

			if (_maxPurify)
			{
				UIInstanceManager.instance.ShowCanvasAsync("FullChaosSelectCanvas", null);
				return false;
			}

			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return false;
			}

			// check lobby energy
			if (CheckEnergy() == false)
				return false;

			// check lobby suggest
			ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
			if (_suggestedChapter != StageManager.instance.playChapter && PlayerData.instance.swappable && chapterTableData != null && ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chapter))
			{
				CharacterData mainCharacterData = PlayerData.instance.GetCharacterData(PlayerData.instance.mainCharacterId);
				bool showSwapCanvas = false;
				string descStringId = "";
				if (mainCharacterData.powerLevel < chapterTableData.suggestedPowerLevel)
				{
					showSwapCanvas = true;
					descStringId = "GameUI_EnterInfoDesc";
				}
				if (mainCharacterData.powerLevel > chapterTableData.suggestedMaxPowerLevel)
				{
					showSwapCanvas = true;
					descStringId = "GameUI_EnterTooPowerfulDesc";
				}
				if (showSwapCanvas == false && PlayerData.instance.currentChaosMode == false && CheckStagePenalty())
				{
					showSwapCanvas = true;
					descStringId = "GameUI_EnterPenaltyDesc";
				}
				if (showSwapCanvas)
				{
					_spawnTime = Time.time;
					raycastCount = 0;
					FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_EnterInfo"), UIString.instance.GetString(descStringId), () => {
						_suggestedChapter = StageManager.instance.playChapter;
						FullscreenYesNoCanvas.instance.ShowCanvas(false, "", "", null);
						UIInstanceManager.instance.ShowCanvasAsync("SwapCanvas", null);
					}, () => {
						_suggestedChapter = StageManager.instance.playChapter;
					});
					return false;
				}
			}
		}
		else
		{
			// swappable이면서
			// 다음번 보스의 suggest 캐릭터가 아니면서
			// 다음번 보스의 suggest 캐릭터를 보유하고 있다면 팝업을 띄운다.
			// 해당 캐릭터의 파워레벨이 권장파워레벨 이상인지는 체크하지 않는게 이걸 해버리면 1렙짜리 뽑아놨을땐 팝업이 뜨지 않게되서 모르고 지나쳐버리게 된다.
			if (_checkedStageSwapSuggest == false && StageManager.instance.currentStageTableData != null && StageManager.instance.currentStageTableData.swap && PlayerData.instance.swappable)
			{
				MapTableData nextMapTableData = StageManager.instance.nextMapTableData;
				if (nextMapTableData != null && string.IsNullOrEmpty(nextMapTableData.bossName) == false &&
					CheckSuggestedActor(nextMapTableData.suggestedActorId, BattleInstanceManager.instance.playerActor.actorId) == false && HasSuggestedActor(nextMapTableData.suggestedActorId))
				{
					_spawnTime = Time.time;
					raycastCount = 0;
					FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_EnterInfo"), UIString.instance.GetString("GameUI_EnterRecommendDesc"), () => {
						_checkedStageSwapSuggest = true;
						FullscreenYesNoCanvas.instance.ShowCanvas(false, "", "", null);
						UIInstanceManager.instance.ShowCanvasAsync("SwapCanvas", null);
					}, () => {
						_checkedStageSwapSuggest = true;
					});
					return false;
				}
			}
		}

		return true;
	}

	public void OnCompleteStageSwap()
	{
		_checkedStageSwapSuggest = true;
	}

	bool CheckStagePenalty()
	{
		// 로비에서 도전모드일때 스테이지 패널티 체크하는 함수다.
		// 아직 적용되어있지 않은 상태이기 때문에 nextStage 그러니까 1층 데이터에서 가져와서 체크해야한다.
		// 
		if (StageDataManager.instance.nextStageTableData == null)
			return false;

		// 아마 도전모드에선 1개만 들어있을거라 랜덤을 돌리든 [0]으로 접근하든 똑같을거다.
		string stagePenaltyId = "";
		if (StageDataManager.instance.nextStageTableData != null && StageDataManager.instance.nextStageTableData.stagePenaltyId.Length > 0)
			stagePenaltyId = StageDataManager.instance.nextStageTableData.stagePenaltyId[Random.Range(0, StageDataManager.instance.nextStageTableData.stagePenaltyId.Length)];

		if (string.IsNullOrEmpty(stagePenaltyId))
			return false;

		StagePenaltyTableData stagePenaltyTableData = TableDataManager.instance.FindStagePenaltyTableData(stagePenaltyId);
		if (stagePenaltyTableData == null)
			return false;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(BattleInstanceManager.instance.playerActor.actorId);
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
					if (actorTableData.powerSource == intValue)
						return true;
				}
			}
		}
		return false;
	}

	#region Energy
	bool CheckEnergy()
	{
		if (PlayerData.instance.clientOnly)
			return true;

		if (ContentsManager.IsTutorialChapter())
			return true;

		if (CurrencyData.instance.energy < BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay"))
		{
			// 선 클라 처리. 오히려 이건 쉽다.
			ShowRefillEnergyCanvas();
			return false;
		}

		return true;
	}

	void ShowRefillEnergyCanvas()
	{
		_spawnTime = Time.time;
		raycastCount = 0;
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			string title = UIString.instance.GetString("SystemUI_Info");
			int energyToPlay = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay");
			string message = UIString.instance.GetString("GameUI_RefillEnergy", energyToPlay, energyToPlay);
			int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RefillEnergyDiamond");
			ConfirmSpendCanvas.instance.ShowCanvas(true, title, message, CurrencyData.eCurrencyType.Diamond, price, true, () =>
			{
				if (CurrencyData.instance.dia < price)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
					return;
				}
				PlayFabApiManager.instance.RequestRefillEnergy(price, energyToPlay, () =>
				{
					CurrencySmallInfoCanvas.RefreshInfo();
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
				});
			});
		});
	}

	// 클라이언트 에너지 선처리. 패킷을 날려놓고 페이드아웃쯤에 오는 서버 응답에 따라 처리가 나뉜다.
	bool _waitEnergyServerResponse;
	bool _enterGameServerFailure;
	bool _networkFailure;
	void PrepareUseEnergy()
	{
		if (PlayerData.instance.clientOnly)
			return;

		int useAmount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay");
		if (ContentsManager.IsTutorialChapter())
			useAmount = 0;

		// 클라이언트에서 먼저 삭제한 다음
		if (useAmount > 0)
		{
			CurrencyData.instance.UseEnergy(useAmount);
			if (EnergyGaugeCanvas.instance != null)
				EnergyGaugeCanvas.instance.RefreshEnergy();
		}
		// 입장패킷 보내서 서버로부터 제대로 응답오는지 기다려야한다.
		PlayFabApiManager.instance.RequestEnterGame(false, "", (serverFailure) =>
		{
			if (_waitEnergyServerResponse)
			{
				// 에너지 부족
				_enterGameServerFailure = serverFailure;
				_waitEnergyServerResponse = false;
			}
		}, () =>
		{
			if (_waitEnergyServerResponse)
			{
				// 그외 접속불가 네트워크 에러
				_networkFailure = true;
				_waitEnergyServerResponse = false;
			}
		});
		_waitEnergyServerResponse = true;
	}

	void PrepareInProgressGame()
	{
		if (PlayerData.instance.clientOnly)
			return;

		string enterFlag = ClientSaveData.instance.GetCachedEnterFlag();
		PlayFabApiManager.instance.RequestEnterGame(true, enterFlag, (serverFailure) =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnergyServerResponse)
			{
				// 에너지 부족
				_enterGameServerFailure = serverFailure;
				_waitEnergyServerResponse = false;
			}
		}, () =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnergyServerResponse)
			{
				// 그외 접속불가 네트워크 에러
				_networkFailure = true;
				_waitEnergyServerResponse = false;
			}
		});
		_waitEnergyServerResponse = true;
	}
	#endregion

	bool _checkedStageSwapSuggest = false;
	bool HasSuggestedActor(string[] suggestedActorIdList)
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return false;

		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			CharacterData characterData = listCharacterData[i];
			//if (characterData.powerLevel < chapterTableData.suggestedPowerLevel)
			//	continue;
			if (BattleInstanceManager.instance.playerActor.actorId == characterData.actorId)
				continue;
			if (CheckSuggestedActor(suggestedActorIdList, characterData.actorId))
				return true;
		}
		return false;
	}

	public static bool CheckSuggestedActor(string[] suggestedActorIdList, string actorId)
	{
		if (suggestedActorIdList == null)
			return false;
		for (int i = 0; i < suggestedActorIdList.Length; ++i)
		{
			if (suggestedActorIdList[i] == actorId)			
				return true;
		}
		return false;
	}

	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> NextMapProcess(bool inProgressGame = false)
	{
		if (_processing)
			yield break;

		_processing = true;

		if (MainSceneBuilder.instance.lobby)
		{
			if (inProgressGame)
				PrepareInProgressGame();
			else
				PrepareUseEnergy();

			LobbyCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.5f, false, false);
		}
		else
			LobbyCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.5f, true, false);

		yield return Timing.WaitForSeconds(0.2f);
		changeEffectParticleRootObject.SetActive(true);
		SoundManager.instance.PlaySFX("GatePillar");

		// avoid gc
		if (this == null)
			yield break;

#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(gameObject);
#endif
		CustomRenderer.instance.bloom.AdjustDirtIntensity(1.5f);

		yield return Timing.WaitForSeconds(0.5f);

		// avoid gc
		if (this == null)
			yield break;

		FadeCanvas.instance.FadeOut(0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		// avoid gc
		if (this == null)
			yield break;

		bool lobby = MainSceneBuilder.instance.lobby;
		if (MainSceneBuilder.instance.lobby)
		{
			while (_waitEnergyServerResponse)
				yield return Timing.WaitForOneFrame;
			if (_enterGameServerFailure || _networkFailure)
			{
				ResetFlagForServerFailure();
				CustomRenderer.instance.bloom.ResetDirtIntensity();
				FadeCanvas.instance.FadeIn(0.4f);
				if (_enterGameServerFailure)
					ShowRefillEnergyCanvas();
				if (_networkFailure)
				{
					// 네트워크 오류라면 선처리로 차감했던 에너지를 복구해준다.
					if (ContentsManager.IsTutorialChapter() == false)
					{
						int useAmount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay");
						CurrencyData.instance.OnRecvRefillEnergy(useAmount);
					}
				}
				_enterGameServerFailure = false;
				_networkFailure = false;
				// 알파가 어느정도 빠지면 _processing을 풀어준다.
				yield return Timing.WaitForSeconds(0.2f);
				_processing = false;				
				yield break;
			}
			if (ClientSaveData.instance.IsLoadingInProgressGame() && ClientSaveData.instance.standbySwapBattleActor)
			{
				while (ClientSaveData.instance.IsLoadedPlayerActor == false)
					yield return Timing.WaitForOneFrame;
				ClientSaveData.instance.ChangeBattleActor();
				// 한번도 여기서 캐릭터를 바꾼적이 없어서 그동안은 필요없던 코드긴 한데
				// 처음 캐릭을 생성하고나서 Start가 호출되고 나야 ActorStatus를 사용할 수 있다. 그러기 위해서 생성하고나서 한프레임 쉬고 가도록 한다.
				yield return Timing.WaitForOneFrame;
			}
			if (PlayerData.instance.clientOnly == false)
				Timing.RunCoroutine(CurrencyData.instance.DelayedSyncEnergyRechargeTime(5.0f));
			while (MainSceneBuilder.instance.IsDoneLateInitialized() == false)
				yield return Timing.WaitForOneFrame;
			if (TitleCanvas.instance != null)
				TitleCanvas.instance.gameObject.SetActive(false);
			if (NodeWarPortal.instance != null && NodeWarPortal.instance.gameObject.activeSelf)
				NodeWarPortal.instance.gameObject.SetActive(false);
			MainSceneBuilder.instance.OnExitLobby();
			BattleManager.instance.OnStartBattle();
			BattleInstanceManager.instance.AddBattlePlayer(BattleInstanceManager.instance.playerActor.actorId);
			SoundManager.instance.PlayBattleBgm(BattleInstanceManager.instance.playerActor.actorId);
		}
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return Timing.WaitForOneFrame;
		CustomRenderer.instance.bloom.ResetDirtIntensity();
		StageManager.instance.MoveToNextStage();
		gameObject.SetActive(false);

		FadeCanvas.instance.FadeIn(0.4f);
		if (lobby) LobbyCanvas.instance.FadeInQuestInfoGroup(1.0f, 0.4f, false, true);
		else LobbyCanvas.instance.FadeInQuestInfoGroup(1.0f, 0.4f, true, true);

		_processing = false;
	}





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
