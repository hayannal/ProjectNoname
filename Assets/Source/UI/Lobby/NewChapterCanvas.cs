using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewChapterCanvas : MonoBehaviour
{
	public static NewChapterCanvas instance;

	public Text chapterRomanNumberText;
	public Text chapterNameText;

	private void Awake()
	{
		instance = this;
	}

	public void RefreshChapterInfo(int chapter)
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(chapter);
		if (chapterTableData == null)
			return;

		chapterRomanNumberText.text = SwapCanvas.GetChapterRomanNumberString(chapter);
		chapterNameText.SetLocalizedText(UIString.instance.GetString(chapterTableData.nameId));
	}
}