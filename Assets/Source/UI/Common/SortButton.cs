using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SortButton : MonoBehaviour
{
	public enum eSortType
	{
		PowerLevel,
		PowerLevelDescending,
		PowerSource,

		Amount,
	}

	public Button button;
	public Text sortText;
	public CanvasGroup textCanvasGroup;
	public Canvas buttonTextCanvas;
	public Canvas backgroundImageCanvas;

	eSortType _sortType = eSortType.PowerLevel;
	public Action<eSortType> onChangedCallback;

	void Awake()
	{
		_parentCanvas = cachedRectTransform.parent.GetComponentInParent<Canvas>();
	}

	Canvas _parentCanvas;
	void OnEnable()
	{
		if (_parentCanvas != null)
		{
			buttonTextCanvas.sortingOrder = _parentCanvas.sortingOrder + 2;
			backgroundImageCanvas.sortingOrder = _parentCanvas.sortingOrder + 1;
		}

		textCanvasGroup.alpha = 0.0f;
	}

	public void OnClickButton()
	{
		int index = (int)_sortType;
		++index;
		if (index >= (int)eSortType.Amount)
			index = 0;
		_sortType = (eSortType)index;

		string stringId = "";
		switch (_sortType)
		{
			case eSortType.PowerLevel: stringId = "GameUI_OrderByPowerLevel"; break;
			case eSortType.PowerLevelDescending: stringId = "GameUI_OrderByPowerLevelDescending"; break;
			case eSortType.PowerSource: stringId = "GameUI_OrderByPowerSource"; break;
		}
		sortText.SetLocalizedText(UIString.instance.GetString(stringId));

		textCanvasGroup.alpha = 1.0f;
		_lastClickRemainTime = 2.0f;
		_fadeTime = 0.0f;

		if (onChangedCallback != null)
			onChangedCallback.Invoke(_sortType);
	}

	void Update()
	{
		UpdateLastClickTime();
		UpdateFade();
	}

	float _lastClickRemainTime;
	void UpdateLastClickTime()
	{
		if (_lastClickRemainTime > 0.0f)
		{
			_lastClickRemainTime -= Time.deltaTime;
			if (_lastClickRemainTime <= 0.0f)
			{
				_lastClickRemainTime = 0.0f;
				_fadeTime = 0.5f;
			}
		}
	}

	float _fadeTime;
	void UpdateFade()
	{
		if (_fadeTime > 0.0f)
		{
			_fadeTime -= Time.deltaTime;
			textCanvasGroup.alpha = _fadeTime * 2.0f;
			if (_fadeTime <= 0.0f)
			{
				_fadeTime = 0.0f;
				textCanvasGroup.alpha = 0.0f;
			}
		}
	}

	public void SetSortType(eSortType sortType) { _sortType = sortType; }




	public Comparison<CharacterData> comparisonPowerLevel = delegate (CharacterData x, CharacterData y)
	{
		if (x.powerLevel > y.powerLevel) return -1;
		else if (x.powerLevel < y.powerLevel) return 1;	
		ActorTableData xActorTableData = TableDataManager.instance.FindActorTableData(x.actorId);
		ActorTableData yActorTableData = TableDataManager.instance.FindActorTableData(y.actorId);
		if (xActorTableData != null && yActorTableData != null)
		{
			if (xActorTableData.grade > yActorTableData.grade) return -1;
			else if (xActorTableData.grade < yActorTableData.grade) return 1;
			if (xActorTableData.orderIndex < yActorTableData.orderIndex) return -1;
			else if (xActorTableData.orderIndex > yActorTableData.orderIndex) return 1;
		}
		return 0;
	};

	public Comparison<CharacterData> comparisonPowerLevelDescending = delegate (CharacterData x, CharacterData y)
	{
		if (x.powerLevel > y.powerLevel) return 1;
		else if (x.powerLevel < y.powerLevel) return -1;
		ActorTableData xActorTableData = TableDataManager.instance.FindActorTableData(x.actorId);
		ActorTableData yActorTableData = TableDataManager.instance.FindActorTableData(y.actorId);
		if (xActorTableData != null && yActorTableData != null)
		{
			if (xActorTableData.grade > yActorTableData.grade) return -1;
			else if (xActorTableData.grade < yActorTableData.grade) return 1;
			if (xActorTableData.orderIndex < yActorTableData.orderIndex) return -1;
			else if (xActorTableData.orderIndex > yActorTableData.orderIndex) return 1;
		}
		return 0;
	};

	public Comparison<CharacterData> comparisonPowerSource = delegate (CharacterData x, CharacterData y)
	{
		ActorTableData xActorTableData = TableDataManager.instance.FindActorTableData(x.actorId);
		ActorTableData yActorTableData = TableDataManager.instance.FindActorTableData(y.actorId);
		if (xActorTableData != null && yActorTableData != null)
		{
			if (xActorTableData.powerSource < yActorTableData.powerSource) return -1;
			else if (xActorTableData.powerSource > yActorTableData.powerSource) return 1;
			if (xActorTableData.grade > yActorTableData.grade) return -1;
			else if (xActorTableData.grade < yActorTableData.grade) return 1;
			if (xActorTableData.orderIndex < yActorTableData.orderIndex) return -1;
			else if (xActorTableData.orderIndex > yActorTableData.orderIndex) return 1;
		}
		return 0;
	};





	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}