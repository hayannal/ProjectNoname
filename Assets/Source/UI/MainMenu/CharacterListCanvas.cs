using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.Text;

public class CharacterListCanvas : MonoBehaviour
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

		RefreshGrid(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
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
	public void OnClickYesButton()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		// DB

		// StackCanvas인데 이렇게 그냥 꺼버리면 스택에서 빼라는거로 인식해서 뒤로 돌아오려고 할때 이쪽으로 못오게 된다.
		// 그러니 진짜로 뒤로 가는게 아닌이상 호출하면 안된다.
		//gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("CharacterInfoCanvas", () =>
		{
			CharacterInfoCanvas.instance.ShowCanvas(_selectedActorId);
		});
	}
}