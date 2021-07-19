using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeInvasionDifficultyCanvas : MonoBehaviour
{
	public static ChangeInvasionDifficultyCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<ChangeInvasionDifficultyCanvasListItem>
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

	List<ChangeInvasionDifficultyCanvasListItem> _listChangeInvasionDifficultyCanvasListItem = new List<ChangeInvasionDifficultyCanvasListItem>();
	public void RefreshInfo(int highestDifficulty, int selectedDifficulty, int selectedActorPowerLevel)
	{
		// 선택한 캐릭터가 없을땐 1이라도 표시해둔다.
		if (selectedActorPowerLevel == 0)
			selectedActorPowerLevel = 1;

		for (int i = 0; i < _listChangeInvasionDifficultyCanvasListItem.Count; ++i)
			_listChangeInvasionDifficultyCanvasListItem[i].gameObject.SetActive(false);
		_listChangeInvasionDifficultyCanvasListItem.Clear();

		for (int i = 1; i <= highestDifficulty; ++i)
		{
			ChangeInvasionDifficultyCanvasListItem changeInvasionDifficultyCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			changeInvasionDifficultyCanvasListItem.Initialize(i, selectedActorPowerLevel, i == selectedDifficulty);
			_listChangeInvasionDifficultyCanvasListItem.Add(changeInvasionDifficultyCanvasListItem);
		}
	}
}