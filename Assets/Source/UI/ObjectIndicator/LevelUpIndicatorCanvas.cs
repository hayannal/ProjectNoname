using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using MEC;

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

	public static void Show(bool show, Transform targetTransform, int levelUpCount, int levelPackCount, int exclusiveLevelPackCount)
	{
		if (show)
		{
			// 이미 보여지는 중이라면 예약을 걸어두기만 한다.
			if (IsShow())
			{
				_instance.ReserveCount(levelUpCount, levelPackCount, exclusiveLevelPackCount);
				return;
			}

			instance.targetTransform = targetTransform;
			instance.ShowLevelUpIndicator(levelUpCount, levelPackCount, exclusiveLevelPackCount);
			instance.gameObject.SetActive(true);
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}

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

	// 타겟의 수치만큼 레벨업을 해야 모든 레벨업 기회를 적용한거다. 이게 충족되야 다음 맵으로 넘어갈 수 있다.
	// 드랍으로만 레벨팩이 나오면 LevelUpIndicator가 보여지지 않기 때문에 static으로 기억해둔다.
	static int _targetLevelUpCount;
	public static void SetTargetLevelUpCount(int targetCount)
	{
		_targetLevelUpCount = targetCount;
	}

	public CanvasGroup canvasGroup;
	public GraphicRaycaster graphicRaycaster;
	public GameObject buttonRootObject;
	public LevelUpIndicatorButton[] buttonList;
	public GameObject titleTextObject;
	public Text titleText;

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
		buttonRootObject.SetActive(false);
		for (int i = 0; i < buttonList.Length; ++i)
			buttonList[i].gameObject.SetActive(false);
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
	void ReserveCount(int levelUpCount, int levelPackCount, int exclusiveLevelPackCount)
	{
		for (int i = 0; i < levelUpCount; ++i)
			_listReservedLevelUpType.Add((int)eLevelUpType.LevelUp);
		for (int i = 0; i < levelPackCount; ++i)
			_listReservedLevelUpType.Add((int)eLevelUpType.LevelPack);
		for (int i = 0; i < exclusiveLevelPackCount; ++i)
			_listReservedLevelUpType.Add((int)eLevelUpType.NoHitLevelPack);
	}

	int _levelUpType = 0;
	void ShowLevelUpIndicator(int levelUpCount, int levelPackCount, int exclusiveLevelPackCount)
	{
		if (_close)
		{
			_close = false;
			graphicRaycaster.enabled = true;
			canvasGroup.alpha = 1.0f;
		}

		// 셋중에 하나만 값이 0 초과로 들어온다.
		// exclusiveLevelPack 일때는 3개의 레벨팩 중 마지막꺼가 무조건 exclusive에서 뽑혀진다.
		_levelUpType = 0;
		if (levelUpCount > 0 && levelPackCount == 0 && exclusiveLevelPackCount == 0)
		{
			ReserveCount(levelUpCount - 1, levelPackCount, exclusiveLevelPackCount);
		}
		if (levelUpCount == 0 && levelPackCount > 0 && exclusiveLevelPackCount == 0)
		{
			ReserveCount(levelUpCount, levelPackCount - 1, exclusiveLevelPackCount);
			_levelUpType = (int)eLevelUpType.LevelPack;
		}
		if (levelUpCount == 0 && levelPackCount == 0 && exclusiveLevelPackCount > 0)
		{
			ReserveCount(levelUpCount, levelPackCount, exclusiveLevelPackCount - 1);
			_levelUpType = (int)eLevelUpType.NoHitLevelPack;
		}

		RefreshLevelPackList();
	}

	void RefreshLevelPackList()
	{
		switch (_levelUpType)
		{
			case 0: titleText.SetLocalizedText(UIString.instance.GetString("GameUI_SelectLevelPack")); break;
			case 1: titleText.SetLocalizedText(UIString.instance.GetString("GameUI_BossClearReward")); break;
			case 2: titleText.SetLocalizedText(UIString.instance.GetString("GameUI_NoHitClearReward")); break;
		}

		List<LevelPackDataManager.RandomLevelPackInfo> listRandomLevelPackInfo = LevelPackDataManager.instance.GetRandomLevelPackTableDataList(BattleInstanceManager.instance.playerActor);
		for (int i = 0; i < buttonList.Length; ++i)
		{
			float currentRandom = Random.value;
			for (int j = 0; j < listRandomLevelPackInfo.Count; ++j)
			{
				if (currentRandom <= listRandomLevelPackInfo[j].rate)
				{
					buttonList[i].SetInfo(listRandomLevelPackInfo[j].levelPackTableData, BattleInstanceManager.instance.playerActor.skillProcessor.GetLevelPackStackCount(listRandomLevelPackInfo[j].levelPackTableData.levelPackId) + 1);
					break;
				}
			}
			if (_levelUpType == (int)eLevelUpType.NoHitLevelPack && i == 1)
				break;
		}
		listRandomLevelPackInfo.Clear();

		if (_levelUpType != (int)eLevelUpType.NoHitLevelPack)
			return;

		// last is exclusive
		LevelPackTableData randomExclusiveLevelPackTableData = LevelPackDataManager.instance.GetRandomExclusiveLevelPackTableData(BattleInstanceManager.instance.playerActor);
		buttonList[buttonList.Length - 1].SetInfo(randomExclusiveLevelPackTableData, BattleInstanceManager.instance.playerActor.skillProcessor.GetLevelPackStackCount(randomExclusiveLevelPackTableData.levelPackId) + 1);
	}

	public void OnCompleteLineAnimation()
	{
		titleTextObject.SetActive(true);
		buttonRootObject.SetActive(true);
		Timing.RunCoroutine(ButtonAppearProcess());
	}

	IEnumerator<float> ButtonAppearProcess()
	{
		for (int i = 0; i < buttonList.Length; ++i)
		{
			buttonList[i].gameObject.SetActive(true);

			if (i != (buttonList.Length - 1))
				yield return Timing.WaitForSeconds(0.3f);

			// avoid gc
			if (this == null)
				yield break;
		}
	}

	int _selectCount = 0;
	void OnSelectLevelUpPack(string levelPackId)
	{
		++_selectCount;
		LevelPackDataManager.instance.AddLevelPack(BattleInstanceManager.instance.playerActor.actorId, levelPackId);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack(levelPackId);
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.levelPackGainEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);

		// 예약이 되어있다면 창을 닫지 않고 항목만 갱신
		if (_listReservedLevelUpType.Count > 0)
		{
			_levelUpType = _listReservedLevelUpType[0];
			_listReservedLevelUpType.RemoveAt(0);
			RefreshLevelPackList();

			for (int i = 0; i < buttonList.Length; ++i)
				buttonList[i].gameObject.SetActive(false);
			OnCompleteLineAnimation();
			return;
		}

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
}