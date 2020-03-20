using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChapterCanvasListItem : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text chapterText;
	public Text stageText;
	public GameObject clearObject;
	public GameObject selectObject;
	public GameObject blackObject;

	public int chapter { get; set; }
	public void Initialize(int chapter)
	{
		this.chapter = chapter;

		string romanNumberString = UIString.instance.GetString(string.Format("GameUI_RomanNumber{0}", chapter));
		chapterText.text = UIString.instance.GetString("GameUI_MenuChapter", romanNumberString);

		bool disableChapter = (chapter > PlayerData.instance.highestPlayChapter);
		bool clearChapter = (chapter < PlayerData.instance.highestPlayChapter);
		stageText.gameObject.SetActive(disableChapter || clearChapter == false);
		int stage = 0;
		if (chapter == PlayerData.instance.highestPlayChapter)
			stage = PlayerData.instance.highestClearStage;
		stageText.text = UIString.instance.GetString("GameUI_StageFraction", stage, StageManager.instance.GetMaxStage(chapter));
		clearObject.SetActive(clearChapter);
		blackObject.SetActive(disableChapter);

		if (disableChapter)
		{
			blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
			gradient.color1 = Color.white;
			gradient.color2 = Color.black;
		}
		else if (clearChapter)
		{
			blurImage.color = new Color(0.192f, 0.866f, 0.819f, 0.0f);
			gradient.color1 = new Color(0.117f, 0.914f, 0.914f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}
		else
		{
			blurImage.color = new Color(0.792f, 0.776f, 0.615f, 0.0f);
			gradient.color1 = new Color(1.0f, 0.0f, 0.0f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}

		selectObject.SetActive(false);
	}

	public void OnClickButton()
	{
		ChapterCanvas.instance.OnClickListItem(chapter);
	}

	public void OnClickStoryButton()
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(chapter);
		if (chapterTableData == null)
			return;

		bool up = false;
		if (contentRectTransform.position.y < Screen.height * 0.5f)
			up = true;

		string text = string.Format("<size=18>{0}</size>\n\n{1}", UIString.instance.GetString(chapterTableData.nameId), UIString.instance.GetString(chapterTableData.descriptionId));
		TooltipCanvas.Show(true, up ? TooltipCanvas.eDirection.Top: TooltipCanvas.eDirection.Bottom, text, 300, contentRectTransform.transform, new Vector2(0.0f, up ? 45.0f : -45.0f));
	}

	public void ShowSelectObject(bool show)
	{
		selectObject.SetActive(show);
	}

	void Update()
	{
		UpdateSelectPosition();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-13.0f, 8.0f);
		if (selectObject.activeSelf)
		{
			if (contentRectTransform.anchoredPosition != selectOffset)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}




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