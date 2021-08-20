using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingCanvas : MonoBehaviour
{
	public static RankingCanvas instance;

	public GameObject emptyRankingObject;

	public Text myNameText;
	public Text myRankText;
	public GameObject myOutOfRankTextObject;

	public GameObject editButtonObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<RankingCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	int _defaultFontSize;
	Color _defaultFontColor;
	void Awake()
	{
		instance = this;
		_defaultFontSize = myRankText.fontSize;
		_defaultFontColor = myRankText.color;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		RefreshGrid();
		RefreshInfo();

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		LobbyCanvas.Home();
	}

	List<RankingCanvasListItem> _listRankingCanvasListItem = new List<RankingCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listRankingCanvasListItem.Count; ++i)
			_listRankingCanvasListItem[i].gameObject.SetActive(false);
		_listRankingCanvasListItem.Clear();

		if (RankingData.instance.listDisplayStageRankingInfo.Count == 0)
		{
			emptyRankingObject.SetActive(true);
			return;
		}

		emptyRankingObject.SetActive(false);
		for (int i = 0; i < RankingData.instance.listDisplayStageRankingInfo.Count; ++i)
		{
			RankingCanvasListItem rankingCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			rankingCanvasListItem.Initialize(RankingData.instance.listDisplayStageRankingInfo[i].ranking, RankingData.instance.listDisplayStageRankingInfo[i].displayName, RankingData.instance.listDisplayStageRankingInfo[i].value);
			_listRankingCanvasListItem.Add(rankingCanvasListItem);
		}
	}

	void RefreshInfo()
	{
		myNameText.text = "";

		if (RankingData.instance.listDisplayStageRankingInfo.Count == 0)
		{
			myRankText.fontSize = _defaultFontSize;
			myRankText.color = _defaultFontColor;
			myRankText.text = "-";
			return;
		}

		int myRanking = 0;
		for (int i = 0; i < RankingData.instance.listDisplayStageRankingInfo.Count; ++i)
		{
			if (RankingData.instance.listDisplayStageRankingInfo[i].playFabId != PlayFabApiManager.instance.playFabId)
				continue;

			myRanking = RankingData.instance.listDisplayStageRankingInfo[i].ranking;
			break;
		}
		if (myRanking == 0)
		{
			myRankText.text = "";
			myOutOfRankTextObject.SetActive(true);
			return;
		}
		myRankText.text = myRanking.ToString();
		myOutOfRankTextObject.SetActive(false);

		int fontSize = _defaultFontSize;
		Color fontColor = _defaultFontColor;
		switch (myRanking)
		{
			case 1:
				fontSize = 30;
				fontColor = new Color(1.0f, 0.95f, 0.0f);
				break;
			case 2:
				fontSize = 27;
				fontColor = new Color(1.0f, 0.95f, 0.0f);
				break;
			case 3:
				fontSize = 24;
				fontColor = new Color(1.0f, 0.95f, 0.0f);
				break;
		}
		myRankText.fontSize = fontSize;
		myRankText.color = fontColor;
	}

	public void OnClickEditButton()
	{

	}
}