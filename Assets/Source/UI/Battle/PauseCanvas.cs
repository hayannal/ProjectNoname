//#define CHEAT_RESURRECT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Michsky.UI.Hexart;

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

	public GameObject baseRoot;
	public GameObject battlePauseSimpleMenuRoot;

	public Slider systemVolumeSlider;
	public Text systemVolumeText;
	public Slider bgmVolumeSlider;
	public Text bgmVolumeText;
	public SwitchAnim doubleTabSwitch;
	public Text doubleTabOnOffText;
	public SwitchAnim lockIconSwitch;
	public Text lockIconOnOffText;

	public GameObject levelPackNameLineObject;
	public Text levelPackNameText;
	public Text levelPackDescText;
	public GameObject emptyLevelPackObject;

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

	// 씬 종료때는 BattleInstanceManager에 접근하기 애매해서 임시변수 만들어두고 쓴다.
	PlayerActor _prevPlayerActor;
	bool _prevPlayerActorAnimatorUnscaledTime;
	float _prevTimeScale;
	void OnEnable()
	{
		if (LobbyCanvas.instance != null)
			LobbyCanvas.instance.battlePauseButton.gameObject.SetActive(false);
		if (TutorialLinkAccountCanvas.instance != null && TutorialLinkAccountCanvas.instance.gameObject.activeSelf)
			TutorialLinkAccountCanvas.instance.buttonObject.SetActive(false);

		_prevPlayerActorAnimatorUnscaledTime = (BattleInstanceManager.instance.playerActor.actionController.animator.updateMode == AnimatorUpdateMode.UnscaledTime);
		if (_prevPlayerActorAnimatorUnscaledTime)
		{
			_prevPlayerActor = BattleInstanceManager.instance.playerActor;
			_prevPlayerActor.actionController.animator.updateMode = AnimatorUpdateMode.Normal;
		}
		_prevTimeScale = Time.timeScale;
		Time.timeScale = (BattleInstanceManager.instance.playerActor.actorId == "Actor1039") ? 0.000001f : 0.0f;

		LoadOption();
		RefreshGrid();

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (LobbyCanvas.instance != null)
			LobbyCanvas.instance.battlePauseButton.gameObject.SetActive(true);
		if (TutorialLinkAccountCanvas.instance != null && TutorialLinkAccountCanvas.instance.gameObject.activeSelf)
			TutorialLinkAccountCanvas.instance.buttonObject.SetActive(true);

		if (_prevPlayerActorAnimatorUnscaledTime && _prevPlayerActor != null)
			_prevPlayerActor.actionController.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
		Time.timeScale = _prevTimeScale;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void OnDestroy()
	{
		if (Time.timeScale != 1.0f)
			Time.timeScale = 1.0f;
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
		if (skillProcessor.dicLevelPack == null)
		{
			levelPackNameLineObject.SetActive(false);
			emptyLevelPackObject.SetActive(true);
			return;
		}
		levelPackNameLineObject.SetActive(true);
		emptyLevelPackObject.SetActive(false);
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
			if (x.exclusive && y.exclusive == false) return -1;
			else if (x.exclusive == false && y.exclusive) return 1;
			if (x.stackCount > y.stackCount) return -1;
			else if (x.stackCount < y.stackCount) return 1;
			if (x.colored && y.colored == false) return -1;
			else if (x.colored == false && y.colored) return 1;
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

		levelPackNameText.SetLocalizedText(UIString.instance.GetString(levelPackInfo.nameId).Replace("\n", " "));
		if (levelPackInfo.descriptionParameterList == null)
			levelPackDescText.SetLocalizedText(UIString.instance.GetString(levelPackInfo.descriptionId));
		else
			levelPackDescText.SetLocalizedText(UIString.instance.GetString(levelPackInfo.descriptionId, levelPackInfo.descriptionParameterList));
	}

	public void OnClickBackButton()
	{
		SaveOption();
		gameObject.SetActive(false);
	}

	public void OnClickHomeButton()
	{
		bool isNodeWar = BattleManager.instance.IsNodeWar();
		bool needCancelChallengeMode = (PlayerData.instance.currentChallengeMode && EventManager.instance.IsCompleteServerEvent(EventManager.eServerEvent.chaos));
		if (isNodeWar == false && needCancelChallengeMode)
		{
			string message = string.Format("{0}\n{1}", UIString.instance.GetString("GameUI_BackToLobbyDescription"), UIString.instance.GetString("GameUI_BackToLobbyDescriptionChallenge"));
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), message, () =>
			{
				// 도전모드일때는 서버에도 플레이어 데이터를 갱신해야해서 응답을 받고 처리해야한다.
				PlayFabApiManager.instance.RequestCancelChallenge(() =>
				{
					SaveOption();
					SceneManager.LoadScene(0);
				});
			});
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString(isNodeWar ? "GameUI_CancelNodeWarDescription" : "GameUI_BackToLobbyDescription"), () => {
			if (isNodeWar)
				PlayFabApiManager.instance.RequestCancelNodeWar();
			else
				PlayFabApiManager.instance.RequestCancelGame();
			SaveOption();
			SceneManager.LoadScene(0);
		});
	}


	#region Option
	void LoadOption()
	{
		systemVolumeSlider.value = OptionManager.instance.systemVolume;
		bgmVolumeSlider.value = OptionManager.instance.bgmVolume;
		doubleTabSwitch.isOn = (OptionManager.instance.useDoubleTab == 1);
		lockIconSwitch.isOn = (OptionManager.instance.lockIcon == 1);
	}

	void SaveOption()
	{
		OptionManager.instance.SavePlayerPrefs();
	}

	public void OnValueChangedSystem(float value)
	{
		OptionManager.instance.systemVolume = value;
		systemVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnValueChangedBgm(float value)
	{
		OptionManager.instance.bgmVolume = value;
		bgmVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnSwitchOnDoubleTab()
	{
		OptionManager.instance.useDoubleTab = 1;
		doubleTabOnOffText.text = "ON";
		doubleTabOnOffText.color = Color.white;
	}

	public void OnSwitchOffDoubleTab()
	{
		OptionManager.instance.useDoubleTab = 0;
		doubleTabOnOffText.text = "OFF";
		doubleTabOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}

	public void OnSwitchOnLockIcon()
	{
		OptionManager.instance.lockIcon = 1;
		lockIconOnOffText.text = "ON";
		lockIconOnOffText.color = Color.white;

#if CHEAT_RESURRECT
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie() == false)
			BattleInstanceManager.instance.playerActor.actorStatus.cheatDontDie ^= true;
#endif
	}

	public void OnSwitchOffLockIcon()
	{
		OptionManager.instance.lockIcon = 0;
		lockIconOnOffText.text = "OFF";
		lockIconOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}
	#endregion

	#region Simple Menu
	public void ShowBattlePauseSimpleMenu(bool show)
	{
		baseRoot.SetActive(!show);
		battlePauseSimpleMenuRoot.SetActive(show);
	}

	public void OnClickSimpleSettingButton()
	{
		baseRoot.SetActive(true);
		battlePauseSimpleMenuRoot.SetActive(false);
	}

	public void OnClickSimplePlayButton()
	{
		gameObject.SetActive(false);
		baseRoot.SetActive(true);
		battlePauseSimpleMenuRoot.SetActive(false);
	}
	#endregion
}