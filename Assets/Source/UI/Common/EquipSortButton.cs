using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipSortButton : SortButton
{
	public new enum eSortType
	{
		Grade,
		Attack,
		Enhance,

		Amount,
	}

	eSortType _sortType = eSortType.Grade;
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
			case eSortType.Grade: stringId = "GameUI_OrderByGrade"; break;
			case eSortType.Attack: stringId = "GameUI_OrderByAttack"; break;
			case eSortType.Enhance: stringId = "GameUI_OrderByEnhance"; break;
		}
		sortText.SetLocalizedText(UIString.instance.GetString(stringId));

		textCanvasGroup.alpha = 1.0f;
		_lastClickRemainTime = 2.0f;
		_fadeTime = 0.0f;

		if (onChangedCallback != null)
			onChangedCallback.Invoke(_sortType);
	}
	
	public void SetSortType(eSortType sortType) { _sortType = sortType; }




	public new Comparison<EquipData> comparisonGrade = delegate (EquipData x, EquipData y)
	{
		if (x.newEquip && y.newEquip == false) return -1;
		else if (x.newEquip == false && y.newEquip) return 1;
		if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
		{
			if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return -1;
			else if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return 1;
			if (x.mainStatusValue > y.mainStatusValue) return -1;
			else if (x.mainStatusValue < y.mainStatusValue) return 1;
			if (x.enhanceLevel > y.enhanceLevel) return -1;
			else if (x.enhanceLevel < y.enhanceLevel) return 1;
		}
		return 0;
	};

	public Comparison<EquipData> comparisonAttack = delegate (EquipData x, EquipData y)
	{
		if (x.newEquip && y.newEquip == false) return -1;
		else if (x.newEquip == false && y.newEquip) return 1;
		if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
		{
			if (x.mainStatusValue > y.mainStatusValue) return -1;
			else if (x.mainStatusValue < y.mainStatusValue) return 1;
			if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return -1;
			else if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return 1;
			if (x.enhanceLevel > y.enhanceLevel) return -1;
			else if (x.enhanceLevel < y.enhanceLevel) return 1;
		}
		return 0;
	};

	public Comparison<EquipData> comparisonEnhance = delegate (EquipData x, EquipData y)
	{
		if (x.newEquip && y.newEquip == false) return -1;
		else if (x.newEquip == false && y.newEquip) return 1;
		if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
		{
			if (x.enhanceLevel > y.enhanceLevel) return -1;
			else if (x.enhanceLevel < y.enhanceLevel) return 1;
			if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return -1;
			else if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return 1;
			if (x.mainStatusValue > y.mainStatusValue) return -1;
			else if (x.mainStatusValue < y.mainStatusValue) return 1;
		}
		return 0;
	};
}