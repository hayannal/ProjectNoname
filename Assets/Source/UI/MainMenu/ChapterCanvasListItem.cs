using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChapterCanvasListItem : MonoBehaviour
{
	public LayoutElement layoutElement;
	public RectTransform contentRectTransform;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text chapterText;
	public Text stageText;
	public GameObject clearObject;
	public GameObject selectObject;
	public GameObject blackObject;

	public Transform descRootTransform;
	public CanvasGroup descObjectCanvasGroup;
	public Text descText;

	float _defaultLayoutPreferredHeightMin;
	float _defaultLayoutPreferredHeightMax;
	void Awake()
	{
		_defaultLayoutPreferredHeightMin = layoutElement.minHeight;
		_defaultLayoutPreferredHeightMax = layoutElement.preferredHeight;
	}

	public int chapter { get; set; }
	string _notiId = "";
	public void Initialize(int chapter, string notiId)
	{
		this.chapter = chapter;
		_notiId = notiId;

		string romanNumberString = UIString.instance.GetString(string.Format("GameUI_RomanNumber{0}", chapter));
		chapterText.text = UIString.instance.GetString("GameUI_MenuChapter", romanNumberString);

		bool disableChapter = (chapter > PlayerData.instance.highestPlayChapter);
		bool clearChapter = (chapter < PlayerData.instance.highestPlayChapter);
		stageText.gameObject.SetActive(disableChapter || clearChapter == false);
		clearObject.SetActive(clearChapter);
		blackObject.SetActive(disableChapter);

		int chapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		if (chapter >= chapterLimit || disableChapter)
			stageText.text = "???";
		else
		{
			int stage = 0;
			if (chapter == PlayerData.instance.highestPlayChapter)
				stage = PlayerData.instance.highestClearStage;
			stageText.text = UIString.instance.GetString("GameUI_StageFraction", stage, StageManager.instance.GetMaxStage(chapter, false));
		}

		if (chapter == 0)
		{
			blurImage.color = new Color(0.623f, 0.623f, 0.443f, 0.0f);
			gradient.color1 = new Color(0.411f, 0.411f, 0.094f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}
		else if (disableChapter)
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

		if (string.IsNullOrEmpty(notiId) == false)
			descText.SetLocalizedText(UIString.instance.GetString(notiId));

		selectObject.SetActive(false);
	}

	public void OnClickButton()
	{
		ChapterCanvas.instance.OnClickListItem(chapter);
		SoundManager.instance.PlaySFX("GridOn");
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
		UpdateDescTransform();
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

	void UpdateDescTransform()
	{
		if (selectObject.activeSelf && string.IsNullOrEmpty(_notiId) == false)
		{
			if (layoutElement.preferredHeight != _defaultLayoutPreferredHeightMax)
			{
				layoutElement.preferredHeight = Mathf.Lerp(layoutElement.preferredHeight, _defaultLayoutPreferredHeightMax, Time.deltaTime * 15.0f);
				float diff = layoutElement.preferredHeight - _defaultLayoutPreferredHeightMax;
				if (Mathf.Abs(diff) < 0.1f)
					layoutElement.preferredHeight = _defaultLayoutPreferredHeightMax;

				float ratio = (layoutElement.preferredHeight - _defaultLayoutPreferredHeightMin) / (_defaultLayoutPreferredHeightMax - _defaultLayoutPreferredHeightMin);
				descRootTransform.localScale = new Vector3(1.0f, ratio, 1.0f);
				ratio -= 0.9f;
				if (ratio < 0.0f) ratio = 0.0f;
				ratio *= (1.0f / 0.1f);
				descObjectCanvasGroup.alpha = ratio;
			}
		}
		else
		{
			if (layoutElement.preferredHeight != _defaultLayoutPreferredHeightMin)
			{
				layoutElement.preferredHeight = Mathf.Lerp(layoutElement.preferredHeight, _defaultLayoutPreferredHeightMin, Time.deltaTime * 15.0f);
				float diff = layoutElement.preferredHeight - _defaultLayoutPreferredHeightMin;
				if (Mathf.Abs(diff) < 0.1f)
					layoutElement.preferredHeight = _defaultLayoutPreferredHeightMin;

				float ratio = (layoutElement.preferredHeight - _defaultLayoutPreferredHeightMin) / (_defaultLayoutPreferredHeightMax - _defaultLayoutPreferredHeightMin);
				descRootTransform.localScale = new Vector3(1.0f, ratio, 1.0f);
				ratio -= 0.9f;
				if (ratio < 0.0f) ratio = 0.0f;
				ratio *= (1.0f / 0.1f);
				descObjectCanvasGroup.alpha = ratio;
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