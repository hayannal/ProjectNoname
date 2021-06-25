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
	void OnEnable()
	{
		for (int i = 0; i < _listChangeDifficultyCanvasListItem.Count; ++i)
			_listChangeDifficultyCanvasListItem[i].gameObject.SetActive(false);
		_listChangeDifficultyCanvasListItem.Clear();


		int currentBossId = PlayerData.instance.bossBattleId;
		int clearDifficulty = PlayerData.instance.GetBossBattleClearDifficulty(currentBossId.ToString());
		int selectedDifficulty = PlayerData.instance.GetBossBattleSelectedDifficulty(currentBossId.ToString());
		if (selectedDifficulty > (clearDifficulty + 1))
			selectedDifficulty = (clearDifficulty + 1);

		BossBattleTableData bossBattleTableData = TableDataManager.instance.FindBossBattleData(currentBossId);
		if (bossBattleTableData == null)
			return;

		// 클리어 한거에서 1개까지만 추가해서 표기
		for (int i = 0; i < clearDifficulty + 1; ++i)
		{
			ChangeDifficultyCanvasListItem changeDifficultyCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			changeDifficultyCanvasListItem.Initialize(1 + i, BossBattleEnterCanvas.GetVisualDifficulty(1 + i, bossBattleTableData), (i < clearDifficulty), (1 + i) == selectedDifficulty);
			_listChangeDifficultyCanvasListItem.Add(changeDifficultyCanvasListItem);
		}
	}
}