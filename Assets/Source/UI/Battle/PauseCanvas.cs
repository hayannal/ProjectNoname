using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseCanvas : MonoBehaviour
{
	public static PauseCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.pauseCanvasPrefab).GetComponent<PauseCanvas>();
			}
			return _instance;
		}
	}
	static PauseCanvas _instance = null;

	public Text levelPackNameText;
	public Text levelPackDescText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<PauseCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

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

	List<PauseCanvasListItem> _listPauseCanvasListItem = new List<PauseCanvasListItem>();
	List<SkillProcessor.LevelPackInfo> _listLevelPackInfo = new List<SkillProcessor.LevelPackInfo>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listPauseCanvasListItem.Count; ++i)
			_listPauseCanvasListItem[i].gameObject.SetActive(false);
		_listPauseCanvasListItem.Clear();

		_listLevelPackInfo.Clear();
		SkillProcessor skillProcessor = BattleInstanceManager.instance.playerActor.skillProcessor;
		Dictionary<string, SkillProcessor.LevelPackInfo>.Enumerator e = skillProcessor.dicLevelPack.GetEnumerator();
		while (e.MoveNext())
		{
			SkillProcessor.LevelPackInfo levelPackInfo = e.Current.Value;
			if (levelPackInfo == null)
				continue;
			if (levelPackInfo.stackCount == 0)
				continue;

			_listLevelPackInfo.Add(levelPackInfo);
		}
		_listLevelPackInfo.Sort(delegate (SkillProcessor.LevelPackInfo x, SkillProcessor.LevelPackInfo y)
		{
			if (x.stackCount > y.stackCount) return -1;
			else if (x.stackCount < y.stackCount) return 1;
			if (x.exclusive && y.exclusive == false) return -1;
			else if (x.exclusive == false && y.exclusive) return 1;
			return 0;
		});

		for (int i = 0; i < _listLevelPackInfo.Count; ++i)
		{
			PauseCanvasListItem pauseCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			pauseCanvasListItem.Initialize(_listLevelPackInfo[i]);
			_listPauseCanvasListItem.Add(pauseCanvasListItem);
		}
		if (_listPauseCanvasListItem.Count > 0)
		{
			OnClickListItem(_listLevelPackInfo[0]);
			_listPauseCanvasListItem[0].ShowSelectObject(true);
		}
	}

	public void OnClickListItem(SkillProcessor.LevelPackInfo levelPackInfo)
	{
		for (int i = 0; i < _listPauseCanvasListItem.Count; ++i)
			_listPauseCanvasListItem[i].ShowSelectObject(false);

		levelPackNameText.SetLocalizedText(UIString.instance.GetString(levelPackInfo.nameId));
		if (levelPackInfo.descriptionParameterList == null)
			levelPackDescText.SetLocalizedText(UIString.instance.GetString(levelPackInfo.descriptionId));
		else
			levelPackDescText.SetLocalizedText(UIString.instance.GetString(levelPackInfo.descriptionId, levelPackInfo.descriptionParameterList));
	}


	public void OnClickHomeButton()
	{
		FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			SceneManager.LoadScene(0);
		});
	}
}