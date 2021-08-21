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
	public GameObject rankSusTextObject;

	public GameObject editButtonObject;
	public RectTransform alarmRootTransform;

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
	public void RefreshGrid()
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

	public void RefreshInfo()
	{
		bool noName = string.IsNullOrEmpty(PlayerData.instance.displayName);
		//noName = true;
		editButtonObject.SetActive(noName);
		if (noName)
		{
			myNameText.text = string.Format("Nameless_{0}", PlayFabApiManager.instance.playFabId.Substring(0, 5));
			AlarmObject.Show(alarmRootTransform);
		}
		else
		{
			myNameText.text = PlayerData.instance.displayName;
			AlarmObject.Hide(alarmRootTransform);
		}

		if (RankingData.instance.listDisplayStageRankingInfo.Count == 0)
		{
			myRankText.fontSize = _defaultFontSize;
			myRankText.color = _defaultFontColor;
			myRankText.text = "-";
			return;
		}

		if (PlayerData.instance.cheatRankSus > 0)
		{
			myRankText.text = "";
			myOutOfRankTextObject.SetActive(false);
			rankSusTextObject.SetActive(true);
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
			rankSusTextObject.SetActive(false);
			return;
		}
		myRankText.text = myRanking.ToString();
		myOutOfRankTextObject.SetActive(false);
		rankSusTextObject.SetActive(false);

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
		UIInstanceManager.instance.ShowCanvasAsync("InputNameCanvas", null);
	}
}