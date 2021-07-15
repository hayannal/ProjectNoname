using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BalanceSelectCanvas : MonoBehaviour
{
	public static BalanceSelectCanvas instance;

	public BalanceSortButton balanceSortButton;
	BalanceSortButton.eSortType _currentSortType;

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
		if (balanceSortButton.onChangedCallback == null)
		{
			int sortType = PlayerPrefs.GetInt("_BalanceListSort", 0);
			_currentSortType = (BalanceSortButton.eSortType)sortType;
			balanceSortButton.SetSortType(_currentSortType);
			balanceSortButton.onChangedCallback = OnChangedSortType;
		}

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		// 창을 닫을때 알람을 제거해둔다. 재활용 위해.
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].ShowAlarm(false);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	void OnChangedSortType(BalanceSortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_BalanceListSort", sortTypeValue);

		// 여기서는 정렬 바꿀때 새로 선택되는 캐릭터가 뒤죽박죽 바뀔 수 있으므로 제일 위에있는 캐릭터가 선택되도록 해준다.
		RefreshGrid("");
	}

	string _highestActorId;
	int _highestPp;
	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	public void RefreshGrid(string selectActorId)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		switch (_currentSortType)
		{
			case BalanceSortButton.eSortType.Pp:
				listCharacterData.Sort(balanceSortButton.comparisonPp);
				break;
			case BalanceSortButton.eSortType.Up:
				listCharacterData.Sort(balanceSortButton.comparisonUp);
				break;
		}

		_highestActorId = "";
		_highestPp = 0;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(listCharacterData[i].actorId, listCharacterData[i].powerLevel, SwapCanvasListItem.GetPowerLevelColorState(listCharacterData[i]), listCharacterData[i].transcendLevel, 0, null, null, OnClickListItem);

			// 여기서는 플러스 알람만 표시해도 되지 않을까.
			swapCanvasListItem.ShowAlarm(false);
			if (listCharacterData[i].IsPlusAlarmState())
				swapCanvasListItem.ShowAlarm(true, true);

			_listSwapCanvasListItem.Add(swapCanvasListItem);

			if (listCharacterData[i].pp > _highestPp)
			{
				_highestActorId = listCharacterData[i].actorId;
				_highestPp = listCharacterData[i].pp;
			}
		}

		string currentSelectActorId = selectActorId;
		if (string.IsNullOrEmpty(currentSelectActorId))
		{
			for (int i = 0; i < listCharacterData.Count; ++i)
			{
				if (listCharacterData[i].actorId != _highestActorId)
				{
					currentSelectActorId = listCharacterData[i].actorId;
					break;
				}
			}
		}
		OnClickListItem(currentSelectActorId);
	}

	public void OnClickListItem(string actorId)
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		if (characterData.actorId == _highestActorId || characterData.pp == _highestPp)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BalanceUI_CannotSelectBest"), 2.0f);
			return;
		}

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
		LobbyCanvas.Home();
	}



	string _selectedActorId;
	public string selectedActorId { get { return _selectedActorId; } }
	public void OnClickYesButton()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		BalanceCanvas.instance.RefreshTargetActor(_selectedActorId);
		gameObject.SetActive(false);
	}
}