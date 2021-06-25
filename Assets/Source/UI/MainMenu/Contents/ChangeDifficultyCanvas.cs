using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeDifficultyCanvas : MonoBehaviour
{
	public static ChangeDifficultyCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<ChangeDifficultyCanvasListItem>
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

	List<ChangeDifficultyCanvasListItem> _listChangeDifficultyCanvasListItem = new List<ChangeDifficultyCanvasListItem>();
	public void RefreshInfo(int startChapter, int selectedDifficulty, int clearDifficulty)
	{
		for (int i = 0; i < _listChangeDifficultyCanvasListItem.Count; ++i)
			_listChangeDifficultyCanvasListItem[i].gameObject.SetActive(false);
		_listChangeDifficultyCanvasListItem.Clear();
		

		if (clearDifficulty == 0)
		{
			// 클리어 기록이 없다면 이쪽으로 들어오지 않았을거다.
			return;
		}

		// 클리어 한거에서 1개까지만 추가해서 표기
		for (int i = startChapter; i <= clearDifficulty + 1; ++i)
		{
			ChangeDifficultyCanvasListItem changeDifficultyCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			changeDifficultyCanvasListItem.Initialize(i, (i <= clearDifficulty), i == selectedDifficulty);
			_listChangeDifficultyCanvasListItem.Add(changeDifficultyCanvasListItem);
		}
	}
}