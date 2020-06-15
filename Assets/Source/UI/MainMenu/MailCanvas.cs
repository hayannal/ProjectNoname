using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailCanvas : MonoBehaviour
{
	public static MailCanvas instance;

	public GameObject emptyMailObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<MailCanvasListItem>
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
		RefreshGrid();

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


	List<MailCanvasListItem> _listMailCanvasListItem = new List<MailCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listMailCanvasListItem.Count; ++i)
			_listMailCanvasListItem[i].gameObject.SetActive(false);
		_listMailCanvasListItem.Clear();

		List<MailData.MyMailData> listMyMailData = MailData.instance.listMyMailData;
		if (listMyMailData == null || listMyMailData.Count == 0)
		{
			emptyMailObject.SetActive(true);
			return;
		}

		// j == 0 은 공지 타입. j == 1은 나머지 타입
		for (int j = 0; j < 2; ++j)
		{
			for (int i = 0; i < listMyMailData.Count; ++i)
			{
				string id = listMyMailData[i].id;
				if (id == "un")
				{
					// 클라이언트에서 숨기는 시스템 메일이다. "un"을 약자로 쓴다.
					continue;
				}

				// 메일을 보여주면 안되는 상황들이 있다.
				// 만료기한이 넘었거나
				// 혹은 아이템을 이미 수령했거나 등등
				if (listMyMailData[i].got != 0)
					continue;

				MailData.MailCreateInfo createInfo = MailData.instance.FindCreateMailInfo(id);
				if (createInfo == null)
					continue;

				bool noAttachMail = string.IsNullOrEmpty(createInfo.tp);
				if (j == 0)
				{
					if (noAttachMail == false)
						continue;
				}
				if (j == 1)
				{
					if (noAttachMail)
						continue;
				}

				int receiveDay = 0;
				DateTime receiveTime = new DateTime();
				DateTime validTime = new DateTime();
				if (DateTime.TryParse(listMyMailData[i].rcvDat, out receiveTime))
				{
					DateTime universalTime = receiveTime.ToUniversalTime();
					validTime = universalTime;
					validTime = validTime.AddDays(createInfo.ti);
					receiveDay = universalTime.Day;
					if (j == 1)
					{
						if (ServerTime.UtcNow > validTime)
							continue;
					}
				}

				MailCanvasListItem mailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				mailCanvasListItem.Initialize(createInfo, listMyMailData[i], receiveDay, validTime);
				_listMailCanvasListItem.Add(mailCanvasListItem);
			}
		}
		emptyMailObject.SetActive(_listMailCanvasListItem.Count == 0);

		_selectedId = "";
		_selectedReceiveDay = 0;
		OnClickListItem(_selectedId, _selectedReceiveDay);
	}

	public void OnClickListItem(string id, int receiveDay)
	{
		_selectedId = id;
		_selectedReceiveDay = receiveDay;

		for (int i = 0; i < _listMailCanvasListItem.Count; ++i)
			_listMailCanvasListItem[i].ShowSelectObject(_listMailCanvasListItem[i].id == id && _listMailCanvasListItem[i].receiveDay == receiveDay);
	}




	string _selectedId;
	// ev 구별을 위해서 수령날짜를 비교하는거다. ev는 매일 받는거라 여러개가 들어있을 수 있다.
	int _selectedReceiveDay;
}