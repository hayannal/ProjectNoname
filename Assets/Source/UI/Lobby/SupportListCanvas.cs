using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SupportListCanvas : MonoBehaviour
{
	public static SupportListCanvas instance;

	public GameObject emptySupportObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SupportCanvasListItem>
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

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}

	public void OnClickHomeButton()
	{
		gameObject.SetActive(false);
	}


	List<SupportCanvasListItem> _listSupportCanvasListItem = new List<SupportCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listSupportCanvasListItem.Count; ++i)
			_listSupportCanvasListItem[i].gameObject.SetActive(false);
		_listSupportCanvasListItem.Clear();

		List<MailData.MyMailData> listMyMailData = MailData.instance.listMyMailData;
		if (listMyMailData == null || listMyMailData.Count == 0)
		{
			emptySupportObject.SetActive(true);
			return;
		}

		/*
		int chapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		for (int i = 0; i < TableDataManager.instance.chapterTable.dataArray.Length; ++i)
		{
			ChapterCanvasListItem chapterCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			chapterCanvasListItem.Initialize(TableDataManager.instance.chapterTable.dataArray[i].chapter);
			_listChapterCanvasListItem.Add(chapterCanvasListItem);

			// 현재 챕터 넘어서는거 1개까지만 표기하고 break
			if (TableDataManager.instance.chapterTable.dataArray[i].chapter > PlayerData.instance.highestPlayChapter)
				break;
			if (TableDataManager.instance.chapterTable.dataArray[i].chapter >= chapterLimit)
				break;
		}
		*/
	}

	public void OnClickListItem(int chapter)
	{
		// 보기창이 열려야한다.

		/*
		if (chapter > PlayerData.instance.highestPlayChapter)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotGoChapter", PlayerData.instance.highestPlayChapter), 1.0f);
			return;
		}
		if (chapter == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotGoTrainingChapter"), 1.0f);
			return;
		}

		_selectedChapter = chapter;
		RefreshChapterInfo();

		for (int i = 0; i < _listChapterCanvasListItem.Count; ++i)
			_listChapterCanvasListItem[i].ShowSelectObject(_listChapterCanvasListItem[i].chapter == chapter);
		*/
	}



	
	public void OnClickWriteButton()
	{
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SupportWriteCanvas", null);
	}
}