using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using MEC;

public class CharacterListCanvas : CharacterShowCanvasBase
{
	public static CharacterListCanvas instance;
	
	public SortButton sortButton;
	SortButton.eSortType _currentSortType;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	// 가짜 라인을 만들기 위한 프리팹
	public GameObject contentFakeItemPrefab;

	public class CustomItemContainer : CachedItemHave<SwapCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	public class CustomItemSubContainer : CachedItemHave<CharacterListCanvasFakeItem>
	{
	}
	CustomItemSubContainer _subContainer = new CustomItemSubContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
		contentFakeItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		if (sortButton.onChangedCallback == null)
		{
			int sortType = PlayerPrefs.GetInt("_CharacterListSort", 0);
			_currentSortType = (SortButton.eSortType)sortType;
			sortButton.SetSortType(_currentSortType);
			sortButton.onChangedCallback = OnChangedSortType;
		}

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		// forceShow상태가 false인 창들은 스택안에 있는채로 다른 창에 가려져있다 라는 의미이므로
		// OnEnable처리중에 일부를 건너뛰어(여기선 저 아래 RefreshGrid 함수)
		// 마지막 정보가 그대로 남아있는채로 다시 보여줘야한다.(마치 어디론가 이동시켜놓고 있다가 다시 보여주는거처럼)
		if (restore)
			return;

		_playerActor = BattleInstanceManager.instance.playerActor;
		SetInfoCameraMode(true, _playerActor.actorId);
		RefreshGrid(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		// 인포창 같은거에 stacked 되어서 disable 상태중에 Home키를 누르면 InfoCamera 모드를 복구해야한다.
		// 이걸 위해 OnPop Action으로 감싸고 Push할때 넣어둔다.
		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (_playerActor == null || _playerActor.gameObject == null)
			return;

		// 캐릭터 교체하다보면 _playerActor가 바뀌어져있을거다. 복구시켜준다.
		if (_playerActor != BattleInstanceManager.instance.playerActor)
		{
			_playerActor.gameObject.SetActive(false);
			_playerActor = BattleInstanceManager.instance.playerActor;
			_playerActor.gameObject.SetActive(true);
		}
		SetInfoCameraMode(false, "");
	}

	void OnChangedSortType(SortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_CharacterListSort", sortTypeValue);
		RefreshGrid(false);
	}

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	List<CharacterListCanvasFakeItem> _listFakeItem = new List<CharacterListCanvasFakeItem>();
	class CharacterInfo
	{
		public ActorTableData actorTableData;
		public CharacterData characterData;
	}
	List<CharacterInfo> _listAllCharacterInfo = new List<CharacterInfo>();
	void RefreshGrid(bool onEnable)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();
		for (int i = 0; i < _listFakeItem.Count; ++i)
			_listFakeItem[i].gameObject.SetActive(false);
		_listFakeItem.Clear();

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;

		// 이 창은 유일하게 얻지않은 캐릭들도 다 나오는 창이다.
		if (onEnable)
		{
			_listAllCharacterInfo.Clear();
			for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
			{
				CharacterInfo characterInfo = new CharacterInfo();
				characterInfo.actorTableData = TableDataManager.instance.actorTable.dataArray[i];
				characterInfo.characterData = PlayerData.instance.GetCharacterData(characterInfo.actorTableData.actorId);
				_listAllCharacterInfo.Add(characterInfo);
			}
		}

		_listAllCharacterInfo.Sort(delegate (CharacterInfo x, CharacterInfo y)
		{
			if (x.characterData != null && y.characterData == null) return -1;
			else if (x.characterData == null && y.characterData != null) return 1;

			if (x.characterData != null && y.characterData != null)
			{
				if (_currentSortType == SortButton.eSortType.PowerLevel)
				{
					if (x.characterData.powerLevel > y.characterData.powerLevel) return -1;
					else if (x.characterData.powerLevel < y.characterData.powerLevel) return 1;
				}
				else if (_currentSortType == SortButton.eSortType.PowerLevelDescending)
				{
					if (x.characterData.powerLevel > y.characterData.powerLevel) return 1;
					else if (x.characterData.powerLevel < y.characterData.powerLevel) return -1;
				}
			}

			if (_currentSortType == SortButton.eSortType.PowerSource)
			{
				if (x.actorTableData.powerSource < y.actorTableData.powerSource) return -1;
				else if (x.actorTableData.powerSource > y.actorTableData.powerSource) return 1;
			}

			if (x.actorTableData.grade > y.actorTableData.grade) return -1;
			else if (x.actorTableData.grade < y.actorTableData.grade) return 1;
			if (x.actorTableData.orderIndex < y.actorTableData.orderIndex) return -1;
			else if (x.actorTableData.orderIndex > y.actorTableData.orderIndex) return 1;
			return 0;
		});

		bool existNotYetJoined = (PlayerData.instance.listCharacterData.Count < _listAllCharacterInfo.Count);
		for (int i = 0; i < _listAllCharacterInfo.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			int powerLevel = 0;
			if (_listAllCharacterInfo[i].characterData != null)
				powerLevel = _listAllCharacterInfo[i].characterData.powerLevel;
			swapCanvasListItem.Initialize(_listAllCharacterInfo[i].actorTableData.actorId, powerLevel, chapterTableData.suggestedPowerLevel, null, OnClickListItem);
			_listSwapCanvasListItem.Add(swapCanvasListItem);
			// 빈슬롯과 함께 포함되어있는채로 재활용 해야하니 형제들 중 가장 마지막으로 밀어서 순서를 맞춘다.
			swapCanvasListItem.cachedRectTransform.SetAsLastSibling();

			// 줄이 바뀔만큼의 빈 슬롯을 넣는다.
			if (existNotYetJoined && i == (PlayerData.instance.listCharacterData.Count - 1))
			{
				int needCount = 3;
				int remainder = PlayerData.instance.listCharacterData.Count % 3;
				if (remainder > 0) needCount += (needCount - remainder);

				for (int j = 0; j < needCount; ++j)
				{
					CharacterListCanvasFakeItem fakeItem = _subContainer.GetCachedItem(contentFakeItemPrefab, contentRootRectTransform);
					// 마지막줄 중앙 아이템에만 라인이 보이도록 설정
					fakeItem.Initialize(j == (needCount - 2));
					_listFakeItem.Add(fakeItem);
					// 이거 역시 형제들 중 가장 마지막으로 밀어서 순서를 맞춰야한다.
					fakeItem.cachedRectTransform.SetAsLastSibling();
				}
			}
		}
		if (onEnable)
			OnClickListItem(BattleInstanceManager.instance.playerActor.actorId);
		else
			OnClickListItem(_selectedActorId);
	}

	public void OnClickListItem(string actorId)
	{
		_selectedActorId = actorId;

		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].ShowSelectObject(_listSwapCanvasListItem[i].actorId == actorId);

		// 선택을 누를때마다 조금이라도 로딩시간 줄이기 위해 어드레서블 로드를 걸어둔다.
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId));
	}


	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		StackCanvas.Home();
	}



	string _selectedActorId;
	float _buttonClickTime;
	bool _wait = false;
	public string selectedActorId { get { return _selectedActorId; } }
	public void OnClickYesButton()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;
		if (_wait)
			return;

		// 캐릭터 교체는 이 캔버스 담당이다.
		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거니 가져다쓴다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_selectedActorId);
		if (playerActor != null)
		{
			if (playerActor != _playerActor)
			{
				// 현재 캐릭터 하이드 시키고
				_playerActor.gameObject.SetActive(false);
				_playerActor = playerActor;
				_playerActor.gameObject.SetActive(true);
				base.OnLoadedPlayerActor(true);
			}
			ShowCharacterInfoCanvas();
		}
		else
		{
			// 없다면 로딩 걸어두고 SetInfoCameraMode를 호출해둔다.
			// SetInfoCameraMode 안에는 이미 캐릭터가 없을때를 대비해서 코드가 짜여져있긴 하다.
			_wait = true;
			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_selectedActorId), "", OnLoadedPlayerActor);
		}
	}

	void ShowCharacterInfoCanvas()
	{
		// StackCanvas인데 이렇게 그냥 꺼버리면 스택에서 빼라는거로 인식해서 뒤로 돌아오려고 할때 이쪽으로 못오게 된다.
		// 그러니 진짜로 뒤로 가는게 아닌이상 호출하면 안된다.
		//gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("CharacterInfoCanvas", null);
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		_wait = false;
		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		if (playerActor != _playerActor)
		{
			_playerActor.gameObject.SetActive(false);
			_playerActor = playerActor;
		}
		base.OnLoadedPlayerActor(true);

		// 하필 캐릭을 만들어내는 타이밍에 캔버스가 이미 만들어져서 대기중이라면
		// PlayerActor의 Start는 호출되지 않았는데 Canvas들의 OnEnable에서 Refresh를 하게 된다.
		// 이 결과 BattleInstanceManager에 등록되지 않은 PlayerActor로 되서 검색이 안되기 때문에
		// 차라리 플레이어 만든 직후라면 한프레임 쉬고 호출하는거로 해본다.
		// 여기서 처리 안하면 각각의 창에서 예외처리 해야해서 이쪽에서 한번에 하기로 한다.
		Timing.RunCoroutine(DelayedShowCharacterInfoCanvas());
	}

	IEnumerator<float> DelayedShowCharacterInfoCanvas()
	{
		yield return Timing.WaitForOneFrame;
		ShowCharacterInfoCanvas();
	}
}