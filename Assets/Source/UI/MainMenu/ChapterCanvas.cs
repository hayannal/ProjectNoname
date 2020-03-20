using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChapterCanvas : MonoBehaviour
{
	public static ChapterCanvas instance;

	public Text suggestPowerLevelText;
	public Text stagePenaltyText;
	public Text selectResultText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<ChapterCanvasListItem>
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
		StackCanvas.Home();
	}
	

	List<ChapterCanvasListItem> _listChapterCanvasListItem = new List<ChapterCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listChapterCanvasListItem.Count; ++i)
			_listChapterCanvasListItem[i].gameObject.SetActive(false);
		_listChapterCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.chapterTable.dataArray.Length; ++i)
		{
			ChapterCanvasListItem chapterCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			chapterCanvasListItem.Initialize(TableDataManager.instance.chapterTable.dataArray[i].chapter);
			_listChapterCanvasListItem.Add(chapterCanvasListItem);

			// 현재 챕터 넘어서는거 1개까지만 표기하고 break
			if (TableDataManager.instance.chapterTable.dataArray[i].chapter > PlayerData.instance.highestPlayChapter)
				break;
		}
		_selectedChapter = PlayerData.instance.selectedChapter;
		OnClickListItem(_selectedChapter);
	}

	public void OnClickListItem(int chapter)
	{
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
	}

	void RefreshChapterInfo()
	{
		stagePenaltyText.gameObject.SetActive(false);
		bool chaosMode = PlayerData.instance.chaosMode;
		if (_selectedChapter < PlayerData.instance.highestPlayChapter)
			chaosMode = false;
		StageTableData stageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_selectedChapter, 1, chaosMode);
		string penaltyString = SwapCanvas.GetPenaltyString(stageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}

		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(_selectedChapter);
		if (chapterTableData == null)
			return;

		// 파워레벨은 항상 표시
		string rangeString = UIString.instance.GetString("GameUI_NumberRange", chapterTableData.suggestedPowerLevel, chapterTableData.suggestedMaxPowerLevel);
		suggestPowerLevelText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("GameUI_SuggestedPowerLevel"), rangeString));

		selectResultText.text = "";
		if (_selectedChapter < PlayerData.instance.highestPlayChapter)
			selectResultText.SetLocalizedText(UIString.instance.GetString("GameUI_ChapterTooLow", PlayerData.instance.highestPlayChapter - 1));
	}




	int _selectedChapter;
	public void OnClickYesButton()
	{
		if (_selectedChapter == 0)
			return;

		if (_selectedChapter == PlayerData.instance.selectedChapter)
		{
			OnClickHomeButton();
			return;
		}

		if (_selectedChapter > PlayerData.instance.highestPlayChapter)
			return;

		// Request
		PlayFabApiManager.instance.RequestChangeChapter(_selectedChapter, () =>
		{
			DelayedLoadingCanvas.Show(true);
			SceneManager.LoadScene(0);
		});
	}
}