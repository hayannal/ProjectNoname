using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BalanceSortButton : SortButton
{
	public new enum eSortType
	{
		Pp,
		Up,

		Amount,
	}

	eSortType _sortType = eSortType.Pp;
	public new Action<eSortType> onChangedCallback;

	public new void OnClickButton()
	{
		int index = (int)_sortType;
		++index;
		if (index >= (int)eSortType.Amount)
			index = 0;
		_sortType = (eSortType)index;

		string stringId = "";
		switch (_sortType)
		{
			case eSortType.Pp: stringId = "GameUI_OrderByPowerPoint"; break;
			case eSortType.Up: stringId = "GameUI_OrderByLevelUp"; break;
		}
		sortText.SetLocalizedText(UIString.instance.GetString(stringId));

		textCanvasGroup.alpha = 1.0f;
		_lastClickRemainTime = 2.0f;
		_fadeTime = 0.0f;

		if (onChangedCallback != null)
			onChangedCallback.Invoke(_sortType);
	}

	public void SetSortType(eSortType sortType) { _sortType = sortType; }




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

	public Comparison<CharacterData> comparisonUp = delegate (CharacterData x, CharacterData y)
	{
		// 최고 pp 캐릭터는 제일 마지막으로 빼야한다.
		if (BalanceCanvas.instance != null)
		{
			if (BalanceCanvas.instance.highestPpCharacterId == x.actorId && BalanceCanvas.instance.highestPpCharacterId != y.actorId) return 1;
			else if (BalanceCanvas.instance.highestPpCharacterId != x.actorId && BalanceCanvas.instance.highestPpCharacterId == y.actorId) return -1;
		}

		// 최고 pp 캐릭터의 도달 가능 레벨과 동일하다면 렙업을 할 수 없을테니 제외시켜야한다.
		if (BalanceCanvas.instance != null)
		{
			int xDiff = BalanceCanvas.instance.highestPpCharacterBaseLevel - BalanceCanvas.GetReachablePowerLevel(x.pp, 0);
			int yDiff = BalanceCanvas.instance.highestPpCharacterBaseLevel - BalanceCanvas.GetReachablePowerLevel(y.pp, 0);
			if (xDiff == 0 && yDiff > 0) return 1;
			else if (xDiff > 0 && yDiff == 0) return -1;
		}

		// 다음 레벨업까지 필요한 pp의 수량이 작은 순서
		if (BalanceCanvas.GetNeedPpOfNextReachablePowerLevel(x.pp) > BalanceCanvas.GetNeedPpOfNextReachablePowerLevel(y.pp)) return 1;
		else if (BalanceCanvas.GetNeedPpOfNextReachablePowerLevel(x.pp) < BalanceCanvas.GetNeedPpOfNextReachablePowerLevel(y.pp)) return -1;

		// 나머지부터는 comparisonPp 순서를 따른다.
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
}