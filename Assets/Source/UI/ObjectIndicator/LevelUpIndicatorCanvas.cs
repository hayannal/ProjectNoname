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

	public static void Show(bool show, Transform targetTransform, int levelUpCount, int exclusiveLevelUpCount)
	{
		if (show)
		{
			// 이미 보여지는 중이라면 예약을 걸어두기만 한다.
			if (IsShow())
			{
				_instance.ReserveCount(levelUpCount, exclusiveLevelUpCount);
				return;
			}

			instance.targetTransform = targetTransform;
			instance.ShowLevelUpIndicator(levelUpCount, exclusiveLevelUpCount);
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
		if (_instance != null && _instance.gameObject.activeSelf)
			return true;
		return false;
	}

	// 타겟의 수치만큼 레벨업을 해야 모든 레벨업 기회를 적용한거다. 이게 충족되야 다음 맵으로 넘어갈 수 있다.
	// 드랍으로만 레벨팩이 나오면 LevelUpIndicator가 보여지지 않기 때문에 static으로 기억해둔다.
	static int _targetLevelUpCount;
	public static void SetTargetLevelUpCount(int targetCount)
	{
		_targetLevelUpCount = targetCount;
	}

	public GameObject buttonRootObject;
	public LevelUpIndicatorButton[] buttonList;
	public GameObject titleTextObject;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		InitializeTarget(targetTransform);
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
	}

	// 노말은 false로 전용은 true로 리스트에 넣어둔다.
	List<bool> _listReservedLevelUp = new List<bool>();
	void ReserveCount(int levelUpCount, int exclusiveLevelUpCount)
	{
		for (int i = 0; i < levelUpCount; ++i)
			_listReservedLevelUp.Add(false);
		for (int i = 0; i < exclusiveLevelUpCount; ++i)
			_listReservedLevelUp.Add(true);
	}

	bool _exclusive = false;
	void ShowLevelUpIndicator(int levelUpCount, int exclusiveLevelUpCount)
	{
		// 둘 중에 하나는 0으로 들어온다. 1보다 큰 값에 대해선 예약으로 걸어둔다.
		_exclusive = false;
		if (levelUpCount > 0 && exclusiveLevelUpCount == 0)
		{
			ReserveCount(levelUpCount - 1, exclusiveLevelUpCount);
		}
		if (levelUpCount == 0 && exclusiveLevelUpCount > 0)
		{
			ReserveCount(levelUpCount, exclusiveLevelUpCount - 1);
			_exclusive = true;
		}

		RefreshLevelPackList();
		// 레벨팩 3개 나오는거 2개 나오는거 구분할 필요가 있는가. 이땐 애니는 어떻게 바뀌나. 아이템 오브젝트는 리스트 형태인가. 캐시드 아이템으로 처리할건가 등등

		// 처음 두번은 렙업 카운트로 일반꺼 하고
		// 그 다음 보스몹 한대도 안맞은거 레벨팩 들어올때 전용 2개짜리 돌려야하는건가. 레벨팩 드랍템은 천천히 들어오는건가????? 전투 시작해버리면 어쩌나.

		// 전용팩만 2개 나오는거 구분할 필요가 있는가.
	}

	class RandomLevelPackInfo
	{
		public LevelPackTableData levelPackTableData;
		public float rate;
	}

	List<RandomLevelPackInfo> _listRandomLevelPackInfo = new List<RandomLevelPackInfo>();
	void RefreshLevelPackList()
	{
		List<LevelPackTableData> listLevelPackTableData = LevelPackDataManager.instance.FindActorLevelPackList(BattleInstanceManager.instance.playerActor.actorId);
		float sumWeight = 0.0f;
		for (int i = 0; i < listLevelPackTableData.Count; ++i)
		{
			if (_exclusive && listLevelPackTableData[i].exclusive == false)
				continue;
			if (BattleInstanceManager.instance.playerActor.skillProcessor.GetLevelPackStackCount(listLevelPackTableData[i].levelPackId) >= listLevelPackTableData[i].defaultMax)
				continue;

			sumWeight += listLevelPackTableData[i].dropWeight;
			RandomLevelPackInfo newInfo = new RandomLevelPackInfo();
			newInfo.levelPackTableData = listLevelPackTableData[i];
			newInfo.rate = sumWeight;
			_listRandomLevelPackInfo.Add(newInfo);
		}
		if (_listRandomLevelPackInfo.Count == 0)
		{
			Debug.LogError("Invalid Result : There are no level packs available.");
			return;
		}

		for (int i = 0; i < _listRandomLevelPackInfo.Count; ++i)
			_listRandomLevelPackInfo[i].rate = _listRandomLevelPackInfo[i].rate / sumWeight;
		for (int i = 0; i < buttonList.Length; ++i)
		{
			float currentRandom = Random.value;
			for (int j = 0; j < _listRandomLevelPackInfo.Count; ++j)
			{
				if (currentRandom <= _listRandomLevelPackInfo[j].rate)
				{
					buttonList[i].SetInfo(_listRandomLevelPackInfo[j].levelPackTableData);
					break;
				}
			}

			if (_exclusive && i == 1)
				break;
		}
		_listRandomLevelPackInfo.Clear();
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

			yield return Timing.WaitForSeconds(0.3f);

			// avoid gc
			if (this == null)
				yield break;

			if (_exclusive && i == 1)
				yield break;
		}
	}

	public void OnClickButton1()
	{

	}

	public void OnClickButton2()
	{

	}

	public void OnClickButton3()
	{

	}

	int _selectCount = 0;
	void OnSelectLevelUpPack()
	{
		++_selectCount;

		// 예약이 되어있다면 창을 닫지 않고 항목만 갱신

		// 예약이 없다면 창을 닫는다.

		// 굴려야할 모든 레벨업 항목을 굴렸다면 BattleManager에게 Clear를 알린다.
		if (_selectCount == _targetLevelUpCount)
		{
			_selectCount = _targetLevelUpCount = 0;
			BattleManager.instance.OnClearStage();
		}
	}
}