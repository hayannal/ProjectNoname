using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BalanceSelectCanvas : MonoBehaviour
{
	public static BalanceSelectCanvas instance;

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

	public Comparison<CharacterData> comparisonPp = delegate (CharacterData x, CharacterData y)
	{
		if (x.pp > y.pp) return 1;
		else if (x.pp < y.pp) return -1;
		if (x.powerLevel > y.powerLevel) return 1;
		else if (x.powerLevel < y.powerLevel) return -1;
		ActorTableData xActorTableData = TableDataManager.instance.FindActorTableData(x.actorId);
		ActorTableData yActorTableData = TableDataManager.instance.FindActorTableData(y.actorId);
		if (xActorTableData != null && yActorTableData != null)
		{
			if (xActorTableData.grade > yActorTableData.grade) return 1;
			else if (xActorTableData.grade < yActorTableData.grade) return -1;
		}
		if (x.transcendLevel > y.transcendLevel) return 1;
		else if (x.transcendLevel < y.transcendLevel) return -1;
		if (xActorTableData != null && yActorTableData != null)
		{
			if (xActorTableData.orderIndex < yActorTableData.orderIndex) return 1;
			else if (xActorTableData.orderIndex > yActorTableData.orderIndex) return -1;
		}
		return 0;
	};

	string _highestActorId;
	int _highestPp;
	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	public void RefreshGrid(string selectActorId)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();
		
		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		listCharacterData.Sort(comparisonPp);

		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(listCharacterData[i].actorId, listCharacterData[i].powerLevel, SwapCanvasListItem.GetPowerLevelColorState(listCharacterData[i]), listCharacterData[i].transcendLevel, 0, null, OnClickListItem);

			// 여기서는 플러스 알람만 표시해도 되지 않을까.
			swapCanvasListItem.ShowAlarm(false);
			if (listCharacterData[i].IsPlusAlarmState())
				swapCanvasListItem.ShowAlarm(true, true);

			_listSwapCanvasListItem.Add(swapCanvasListItem);

			if (i == (listCharacterData.Count - 1))
			{
				_highestActorId = listCharacterData[i].actorId;
				_highestPp = listCharacterData[i].pp;
			}
		}

		// 적어도 캐릭터는 셋 이상 있을테니 첫번째꺼 선택하게 하면 된다.
		OnClickListItem(string.IsNullOrEmpty(selectActorId) ? _listSwapCanvasListItem[0].actorId : selectActorId);
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