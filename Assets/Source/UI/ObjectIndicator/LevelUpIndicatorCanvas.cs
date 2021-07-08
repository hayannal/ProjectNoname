﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using MEC;
using PlayFab;
using CodeStage.AntiCheat.ObscuredTypes;

public class LevelUpIndicatorCanvas : ObjectIndicatorCanvas
{
	static LevelUpIndicatorCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.levelUpIndicatorPrefab).GetComponent<LevelUpIndicatorCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static LevelUpIndicatorCanvas _instance = null;

	public static void Show(bool show, Transform targetTransform, int levelUpCount, int levelPackCount, int noHitLevelPackCount)
	{
		if (show)
		{
			// 이미 보여지는 중이라면 예약을 걸어두기만 한다.
			if (IsShow())
			{
				_instance.ReserveCount(levelUpCount, levelPackCount, noHitLevelPackCount);
				return;
			}

			instance.targetTransform = targetTransform;
			instance.ShowLevelUpIndicator(levelUpCount, levelPackCount, noHitLevelPackCount);
			instance.gameObject.SetActive(true);
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}

	#region Exclusive Info
	public static void ShowExclusive(bool show, Transform targetTransform, string exclusivePackId, int level)
	{
		if (show)
		{
			// 이미 보여지는 중이라면 예약을 걸어두기만 한다.
			if (IsShow())
			{
				_instance.ReserveExclusive(exclusivePackId, level);
				return;
			}

			instance.targetTransform = targetTransform;
			instance.ShowLevelUpIndicator(exclusivePackId, level);
			instance.gameObject.SetActive(true);
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}
	#endregion

	public static bool IsShow()
	{
		if (_instance != null && _instance.gameObject.activeSelf && _instance._close == false)
			return true;
		return false;
	}

	public static void OnSelectLevelPack(string levelPackId)
	{
		if (IsShow() == false)
			return;
		_instance.OnSelectLevelUpPack(levelPackId);
	}

	#region Exclusive Info
	public static void OnClickExclusiveCloseButton()
	{
		if (IsShow() == false)
			return;
		_instance.OnClickExclusiveOkButton();
	}
	#endregion

	// 타겟의 수치만큼 레벨업을 해야 모든 레벨업 기회를 적용한거다. 이게 충족되야 다음 맵으로 넘어갈 수 있다.
	// 드랍으로만 레벨팩이 나오면 LevelUpIndicator가 보여지지 않기 때문에 static으로 기억해둔다.
	static int _targetLevelUpCount;
	public static void SetTargetLevelUpCount(int targetCount)
	{
		// 애초에 막겠지만 혹시라도 중복호출 될때를 대비해서 조건문을 걸어둔다.
		if (_targetLevelUpCount == 0)
			_targetLevelUpCount = targetCount;
		else
			Debug.LogErrorFormat("Invalid call. _targetLevelUpCount is not zero. {0} {1}", _targetLevelUpCount, targetCount);
	}

	public CanvasGroup canvasGroup;
	public GraphicRaycaster graphicRaycaster;
	public GameObject buttonRootObject;
	public LevelUpIndicatorButton[] buttonList;
	public LevelUpIndicatorButton exclusiveButton;
	public GameObject exclusiveOkButtonObject;
	public GameObject titleTextObject;
	public Text titleText;
	public GameObject challengeRetryBonusTextObject;
	public Text challengeRetryBonusText;
	public GameObject currentClearPointGroupObject;
	public Text currentClearPointText;
	public GameObject refreshButtonGroupObject;
	public Image refreshIconImage;
	public Image refreshPriceImage;
	public Text refreshPriceText;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	void OnDisable()
	{
		titleTextObject.SetActive(false);
		challengeRetryBonusTextObject.SetActive(false);
		buttonRootObject.SetActive(false);
		for (int i = 0; i < buttonList.Length; ++i)
			buttonList[i].gameObject.SetActive(false);
		exclusiveButton.gameObject.SetActive(false);
		exclusiveOkButtonObject.SetActive(false);
		currentClearPointGroupObject.SetActive(false);
		refreshButtonGroupObject.SetActive(false);
	}

	void OnDestroy()
	{
		_targetLevelUpCount = 0;
	}

	// Update is called once per frame
	void Update()
	{
		UpdateObjectIndicator();
		UpdateCloseAlphaAnimation();
	}

	// 레벨업은 0 노말 레벨팩 보상은 1로 전용은 2로 리스트에 넣어둔다.
	enum eLevelUpType
	{
		LevelUp,
		LevelPack,
		NoHitLevelPack,
	}
	List<int> _listReservedLevelUpType = new List<int>();
	void ReserveCount(int levelUpCount, int levelPackCount, int noHitLevelPackCount)
	{
		for (int i = 0; i < levelUpCount; ++i)
			_listReservedLevelUpType.Add((int)eLevelUpType.LevelUp);
		for (int i = 0; i < levelPackCount; ++i)
			_listReservedLevelUpType.Add((int)eLevelUpType.LevelPack);
		for (int i = 0; i < noHitLevelPackCount; ++i)
			_listReservedLevelUpType.Add((int)eLevelUpType.NoHitLevelPack);
	}

	int _levelUpType = 0;
	void ShowLevelUpIndicator(int levelUpCount, int levelPackCount, int noHitLevelPackCount)
	{
		if (_close)
		{
			_close = false;
			graphicRaycaster.enabled = true;
			canvasGroup.alpha = 1.0f;
		}

		// 셋중에 하나만 값이 0 초과로 들어온다.
		// noHitLevelPack 일때는 3개의 레벨팩이 전부 상급으로 나올거다.
		_levelUpType = 0;
		if (levelUpCount > 0 && levelPackCount == 0 && noHitLevelPackCount == 0)
		{
			ReserveCount(levelUpCount - 1, levelPackCount, noHitLevelPackCount);
		}
		if (levelUpCount == 0 && levelPackCount > 0 && noHitLevelPackCount == 0)
		{
			ReserveCount(levelUpCount, levelPackCount - 1, noHitLevelPackCount);
			_levelUpType = (int)eLevelUpType.LevelPack;
		}
		if (levelUpCount == 0 && levelPackCount == 0 && noHitLevelPackCount > 0)
		{
			ReserveCount(levelUpCount, levelPackCount, noHitLevelPackCount - 1);
			_levelUpType = (int)eLevelUpType.NoHitLevelPack;
		}

		RefreshLevelPackList();
	}

	List<string> _listRandomLevelPackId = new List<string>();
	void RefreshLevelPackList()
	{
		_exclusiveMode = false;

		switch (_levelUpType)
		{
			case 0: titleText.SetLocalizedText(UIString.instance.GetString("GameUI_SelectLevelPack")); break;
			case 1: titleText.SetLocalizedText(UIString.instance.GetString("GameUI_BossClearReward")); break;
			case 2: titleText.SetLocalizedText(UIString.instance.GetString("GameUI_NoHitClearReward")); break;
		}

		// 복구 모드일때는 저장된 값이 있는지 확인해야한다.
		// 1회만 해야하는데 어차피 한번 셋팅하고 Loading플래그는 바로 풀릴거라 따로 호출할건 없이 여기서만 로드해두면 된다.
		if (ClientSaveData.instance.IsLoadingInProgressGame())
		{
			_refreshStackCount = ClientSaveData.instance.GetCachedRefreshStackCount();

			string jsonRandomLevelPackData = ClientSaveData.instance.GetCachedRandomLevelPackData();
			if (string.IsNullOrEmpty(jsonRandomLevelPackData) == false)
			{
				var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
				_listRandomLevelPackId = serializer.DeserializeObject<List<string>>(jsonRandomLevelPackData);
				if (_listRandomLevelPackId != null && _listRandomLevelPackId.Count == buttonList.Length)
				{
					for (int i = 0; i < buttonList.Length; ++i)
					{
						string levelPackId = _listRandomLevelPackId[i];
						LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(levelPackId);
						buttonList[i].SetInfo(levelPackTableData, BattleInstanceManager.instance.playerActor.skillProcessor.GetLevelPackStackCount(levelPackId) + 1);
					}
					// 모든 조건이 맞아서 파싱 될때만 리턴하고 아니면 원래 로직대로 랜덤하게 뽑아온다.
					return;
				}
			}
		}

		_listSelectedIndex.Clear();
		_listRandomLevelPackId.Clear();
		List<LevelPackDataManager.RandomLevelPackInfo> listRandomLevelPackInfo = LevelPackDataManager.instance.GetRandomLevelPackTableDataList(BattleInstanceManager.instance.playerActor, _levelUpType == (int)eLevelUpType.NoHitLevelPack, StageManager.instance.playChapter);
		for (int i = 0; i < buttonList.Length; ++i)
		{
			int index = SelectRandomIndex(listRandomLevelPackInfo);
			buttonList[i].SetInfo(listRandomLevelPackInfo[index].levelPackTableData, BattleInstanceManager.instance.playerActor.skillProcessor.GetLevelPackStackCount(listRandomLevelPackInfo[index].levelPackTableData.levelPackId) + 1);
			_listRandomLevelPackId.Add(listRandomLevelPackInfo[index].levelPackTableData.levelPackId);
		}
		listRandomLevelPackInfo.Clear();
		ClientSaveData.instance.OnChangedRandomLevelPackData(GetCachedRandomLevelPackData());
	}

	List<int> _listSelectedIndex = new List<int>();
	int SelectRandomIndex(List<LevelPackDataManager.RandomLevelPackInfo> listRandomLevelPackInfo)
	{
		int loopCount = 0;
		while (true)
		{
			++loopCount;
			if (loopCount > 100)
			{
				Debug.LogError("Something wrong. while loop invalid");
				return FindIndex(listRandomLevelPackInfo, Random.value);
			}

			float currentRandom = Random.Range(0.0f, 1.0f);
			int findIndex = FindIndex(listRandomLevelPackInfo, currentRandom);
			if (findIndex == -1)
				continue;
			bool duplicated = false;
			for (int i = 0; i < _listSelectedIndex.Count; ++i)
			{
				if (_listSelectedIndex[i] == findIndex)
				{
					duplicated = true;
					break;
				}
			}
			if (duplicated)
				continue;
			_listSelectedIndex.Add(findIndex);
			return findIndex;
		}
	}

	public static int FindIndex(List<LevelPackDataManager.RandomLevelPackInfo> listRandomLevelPackInfo, float random)
	{
		for (int i = 0; i < listRandomLevelPackInfo.Count; ++i)
		{
			if (random <= listRandomLevelPackInfo[i].rate)
				return i;
		}
		return -1;
	}

	#region Exclusive Info
	bool _exclusiveMode = false;
	List<string> _listReservedExclusiveLevelPackId = new List<string>();
	List<int> _listReservedExclusiveLevel = new List<int>();
	void ReserveExclusive(string exclusiveLevelPackId, int level)
	{
		_listReservedExclusiveLevelPackId.Add(exclusiveLevelPackId);
		_listReservedExclusiveLevel.Add(level);
	}

	void ShowLevelUpIndicator(string exclusiveLevelPackId, int level)
	{
		LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(exclusiveLevelPackId);
		if (levelPackTableData == null)
			return;

		if (_close)
		{
			_close = false;
			graphicRaycaster.enabled = true;
			canvasGroup.alpha = 1.0f;
		}

		titleText.SetLocalizedText(UIString.instance.GetString("GameUI_GetExclusiveLevelPack", level));
		exclusiveButton.SetInfo(levelPackTableData, BattleInstanceManager.instance.playerActor.skillProcessor.GetLevelPackStackCount(exclusiveLevelPackId));
		_exclusiveMode = true;
	}
	#endregion

	public void OnCompleteLineAnimation()
	{
		titleTextObject.SetActive(true);
		buttonRootObject.SetActive(true);

		if (_exclusiveMode)
			Timing.RunCoroutine(ExclusiveAppearProcess());
		else
			Timing.RunCoroutine(ButtonAppearProcess(false));

		if (BattleManager.instance != null && BattleManager.instance.GetAppliedChallengeRetryBonusClearPoint() > 0 && ClientSaveData.instance.inProgressGame == false)
		{
			string descStringId = "";
			if (BattleManager.instance.IsDefaultBattle())
				descStringId = PlayerData.instance.chaosMode ? "GameUI_ClearPointColleagues" : "GameUI_ClearPointBonus";
			else if (BattleManager.instance.IsInvasion())
				descStringId = "GameUI_ClearPointColleaguesPower";
			challengeRetryBonusText.SetLocalizedText(UIString.instance.GetString(descStringId, BattleManager.instance.GetAppliedChallengeRetryBonusClearPoint()));
			challengeRetryBonusTextObject.SetActive(true);
			BattleManager.instance.ResetAppliedChallengeRetryBonusClearPoint();
		}
	}

	IEnumerator<float> ButtonAppearProcess(bool ignoreShowClearPoint)
	{
		// preset
		int snapshotClearPoint = BattleManager.instance.GetClearPoint();
		int snapshotPrice = GetRefreshPrice();

		for (int i = 0; i < buttonList.Length; ++i)
		{
			buttonList[i].gameObject.SetActive(true);

			if (i != (buttonList.Length - 1))
				yield return Timing.WaitForSeconds(0.3f);

			// avoid gc
			if (this == null)
				yield break;
		}

		if (ContentsManager.IsTutorialChapter())
			yield break;
		if (ignoreShowClearPoint)
			yield break;

		yield return Timing.WaitForSeconds(0.5f);

		// avoid gc
		if (this == null)
			yield break;

		RefreshClearPoint(snapshotClearPoint, snapshotPrice);
	}

	void RefreshClearPoint(int clearPoint, int refreshPrice)
	{
		currentClearPointText.text = clearPoint.ToString("N0");
		refreshPriceText.text = refreshPrice.ToString();
		currentClearPointGroupObject.SetActive(true);
		refreshButtonGroupObject.SetActive(true);
		bool disablePrice = (clearPoint < refreshPrice);
		refreshIconImage.color = refreshPriceImage.color = refreshPriceText.color = !disablePrice ? Color.white : Color.gray;
	}

	ObscuredInt _refreshStackCount = 0;
	int GetRefreshPrice()
	{
		if (_levelUpType == (int)eLevelUpType.NoHitLevelPack)
		{
			switch (_refreshStackCount)
			{
				case 0: return 5;
				case 1: return 10;
				case 2: return 15;
				default: return 20;
			}
		}

		switch (_refreshStackCount)
		{
			case 0: return 2;
			case 1: return 4;
			case 2: return 6;
			case 3: return 8;
			default: return 10;
		}
	}

	#region Exclusive Info
	IEnumerator<float> ExclusiveAppearProcess()
	{
		exclusiveButton.gameObject.SetActive(true);

		yield return Timing.WaitForSeconds(0.3f);

		// avoid gc
		if (this == null)
			yield break;

		exclusiveOkButtonObject.SetActive(true);
	}
	#endregion

	int _selectCount = 0;
	void OnSelectLevelUpPack(string levelPackId)
	{
		++_selectCount;
		LevelPackDataManager.instance.AddLevelPack(BattleInstanceManager.instance.playerActor.actorId, levelPackId);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack(levelPackId, false, 0);
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.levelPackGainEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);

		// 레벨팩 데이터로부터 리스트를 저장해두고 굴려야할 횟수를 차감해둔다.
		// Exclusive 선택땐 아무것도 안해도 되는게 어차피 이건 알아서 자동으로 획득처리 하니 저장할 필요가 없다.
		ClientSaveData.instance.OnChangedLevelPackData(LevelPackDataManager.instance.GetCachedLevelPackData());
		switch (_levelUpType)
		{
			case (int)eLevelUpType.LevelUp: ClientSaveData.instance.OnAddedRemainLevelUpCount(-1); break;
			case (int)eLevelUpType.LevelPack: ClientSaveData.instance.OnAddedRemainLevelPackCount(-1); break;
			case (int)eLevelUpType.NoHitLevelPack: ClientSaveData.instance.OnAddedRemainNoHitLevelPackCount(-1); break;
		}

		// 갱신 횟수도 초기화
		_refreshStackCount = 0;
		ClientSaveData.instance.OnChangedRefreshStackCount(_refreshStackCount);

		// 예약이 되어있다면 창을 닫지 않고 항목만 갱신
		if (_listReservedLevelUpType.Count > 0)
		{
			_levelUpType = _listReservedLevelUpType[0];
			_listReservedLevelUpType.RemoveAt(0);
			RefreshLevelPackList();

			for (int i = 0; i < buttonList.Length; ++i)
				buttonList[i].gameObject.SetActive(false);
			Timing.RunCoroutine(ButtonAppearProcess(true));

			if (ContentsManager.IsTutorialChapter())
				return;

			// 아이콘 3개가 보이고 나서 갱신되면 헷갈린다. OnClickRefreshButton에서 처리했던거처럼 바로 갱신해주는게 좋아보인다.
			RefreshClearPoint(BattleManager.instance.GetClearPoint(), GetRefreshPrice());
			return;
		}

		ClientSaveData.instance.OnChangedRandomLevelPackData("");

		// 굴려야할 모든 레벨업 항목을 굴렸다면 BattleManager에게 Clear를 알린다.
		if (_selectCount == _targetLevelUpCount)
		{
			_selectCount = _targetLevelUpCount = 0;
			BattleManager.instance.OnClearStage();
		}

		// 예약이 없다면 창을 닫는다.
		//gameObject.SetActive(false);
		_close = true;
		graphicRaycaster.enabled = false;
	}

	#region Exclusive Info
	public void OnClickExclusiveOkButton()
	{
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.levelPackGainEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);

		// exclusive가 두개 연속 쌓일 가능성이 없진 않다. 한번에 10렙까지 도달한다면 가능해짐.
		if (_listReservedExclusiveLevelPackId.Count > 0)
		{
			string exclusiveLevelPackId = _listReservedExclusiveLevelPackId[0];
			int level = _listReservedExclusiveLevel[0];
			_listReservedExclusiveLevelPackId.RemoveAt(0);
			_listReservedExclusiveLevel.RemoveAt(0);
			ShowLevelUpIndicator(exclusiveLevelPackId, level);

			exclusiveButton.gameObject.SetActive(false);
			exclusiveOkButtonObject.SetActive(false);
			OnCompleteLineAnimation();
			return;
		}

		// 아마도 레벨업 카운트가 예약되어있을거다.
		if (_listReservedLevelUpType.Count > 0)
		{
			_levelUpType = _listReservedLevelUpType[0];
			_listReservedLevelUpType.RemoveAt(0);
			RefreshLevelPackList();

			exclusiveButton.gameObject.SetActive(false);
			exclusiveOkButtonObject.SetActive(false);
			OnCompleteLineAnimation();
			return;
		}

		// 예약이 없다면 창을 닫는다.
		//gameObject.SetActive(false);
		_close = true;
		graphicRaycaster.enabled = false;
	}
	#endregion

	bool _close = false;
	void UpdateCloseAlphaAnimation()
	{
		if (_close == false)
			return;

		canvasGroup.alpha -= Time.deltaTime * 2.0f;
		if (canvasGroup.alpha <= 0.0f)
		{
			canvasGroup.alpha = 1.0f;
			graphicRaycaster.enabled = true;
			_close = false;
			gameObject.SetActive(false);
		}
	}


	#region Clear Point
	public void OnClickRefreshButton()
	{
		if (BattleManager.instance.GetClearPoint() < GetRefreshPrice())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughPoint"), 2.0f);
			return;
		}
		BattleManager.instance.AddClearPoint(-GetRefreshPrice());

		RefreshLevelPackList();

		for (int i = 0; i < buttonList.Length; ++i)
			buttonList[i].gameObject.SetActive(false);
		Timing.RunCoroutine(ButtonAppearProcess(true));

		++_refreshStackCount;
		ClientSaveData.instance.OnChangedRefreshStackCount(_refreshStackCount);
		RefreshClearPoint(BattleManager.instance.GetClearPoint(), GetRefreshPrice());
	}
	#endregion


	#region InProgressGame
	public string GetCachedRandomLevelPackData()
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.SerializeObject(_listRandomLevelPackId);
	}
	#endregion
}