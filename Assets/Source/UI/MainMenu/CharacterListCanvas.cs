using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.Text;

public class CharacterListCanvas : CharacterShowCanvasBase
{
	public static CharacterListCanvas instance;
	
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

		bool restore = StackCanvas.Push(gameObject);

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
	void RefreshGrid(bool onEnable)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
		if (chapterTableData == null)
			return;
		
		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		switch (_currentSortType)
		{
			case SortButton.eSortType.PowerLevel:
				listCharacterData.Sort(sortButton.comparisonPowerLevel);
				break;
			case SortButton.eSortType.PowerLevelDescending:
				listCharacterData.Sort(sortButton.comparisonPowerLevelDescending);
				break;
			case SortButton.eSortType.PowerSource:
				listCharacterData.Sort(sortButton.comparisonPowerSource);
				break;
		}

		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(listCharacterData[i], chapterTableData.suggestedPowerLevel, null, OnClickListItem);
			_listSwapCanvasListItem.Add(swapCanvasListItem);
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
		UIInstanceManager.instance.ShowCanvasAsync("CharacterInfoCanvas", () =>
		{
			CharacterInfoCanvas.instance.RefreshInfo(_selectedActorId);
		});
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
		ShowCharacterInfoCanvas();
	}
}