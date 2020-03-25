using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoGrowthCanvas : MonoBehaviour
{
	public static CharacterInfoGrowthCanvas instance;

	public Image gradeBackImage;
	public Text gradeText;
	public Text nameText;
	public Button experienceButton;
	public Button swapButton;
	public Image swapButtonImage;
	public Text swapButtonText;

	public Text powerSourceText;
	public Image ultimateSkillIconImage;
	public GameObject noExclusivePackObject;
	public GameObject exclusivePackObject;
	public CharacterInfoPackIcon[] packIconList;
	public Transform[] packIconContentTransformList;

	public Text powerLevelText;
	public Text hpText;
	public Text atkText;
	public Slider ppSlider;
	public Text ppText;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	string _actorId;
	public void RefreshInfo()
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);

		switch (actorTableData.grade)
		{
			case 0:
				gradeBackImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				gradeBackImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 2:
				gradeBackImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", actorTableData.grade)));
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));

		bool contains = PlayerData.instance.ContainsActor(actorId);
		swapButtonImage.color = contains ? Color.white : Color.gray;
		swapButtonText.color = contains ? Color.white : Color.gray;

		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));
		ultimateSkillIconImage.sprite = null;
		ultimateSkillIconImage.sprite = CommonCanvasGroup.instance.powerSourceIconSpriteList[actorTableData.powerSource];

		int maxStageLevel = StageManager.instance.GetMaxStageLevel();
		int exclusiveLevelPackCount = 0;
		for (int i = 0; i < packIconList.Length; ++i)
			packIconList[i].gameObject.SetActive(false);
		for (int i = 1; i <= maxStageLevel; ++i)
		{
			string exclusiveLevelPackId = TableDataManager.instance.FindActorLevelPackByLevel(actorId, i);
			if (string.IsNullOrEmpty(exclusiveLevelPackId))
				continue;
			LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(exclusiveLevelPackId);
			if (levelPackTableData == null)
				continue;

			packIconList[exclusiveLevelPackCount].Initialize(levelPackTableData, i);
			packIconList[exclusiveLevelPackCount].gameObject.SetActive(true);
			if (exclusiveLevelPackCount == 0) _exclusiveLevelPack1 = exclusiveLevelPackId;
			else if (exclusiveLevelPackCount == 1) _exclusiveLevelPack2 = exclusiveLevelPackId;
			++exclusiveLevelPackCount;
			if (exclusiveLevelPackCount == packIconList.Length)
				break;
		}
		noExclusivePackObject.SetActive(exclusiveLevelPackCount == 0);
		exclusivePackObject.SetActive(exclusiveLevelPackCount > 0);

		_actorId = actorId;
		RefreshStatus();
	}

	void RefreshStatus()
	{
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_actorId);
		if (playerActor != null)
		{
			// 구조 바꾸면서 플레이 중에 못찾는건 없어졌는데 Canvas켜둔채 종료하니 자꾸 뜬다.
			powerLevelText.text = UIString.instance.GetString("GameUI_CharPower", playerActor.actorStatus.powerLevel);
			hpText.text = playerActor.actorStatus.GetDisplayMaxHp().ToString();
			atkText.text = playerActor.actorStatus.GetDisplayAttack().ToString();
		}

		ppText.text = UIString.instance.GetString("GameUI_StageFraction", 2, 20);
		priceText.text = 1000.ToString("N0");
	}
	#endregion

	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		string story = UIString.instance.GetString(actorTableData.storyId);
		string desc = UIString.instance.GetString(actorTableData.descId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", story, desc), 300, nameText.transform, new Vector2(0.0f, -35.0f));

		// 뽑기창에서는 이와 다르게
		// Char CharDesc는 기본으로 나오고 돋보기로만 Story를 본다.
	}

	public void OnClickExperience()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ExperienceCanvas", null);
	}

	public void OnClickMainCharacter()
	{
		if (CharacterListCanvas.instance.selectedActorId == BattleInstanceManager.instance.playerActor.actorId)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterAlready"), 2.0f);
			return;
		}

		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chapter) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_Chp1SwapUnavailable"), 2.0f);
			return;
		}

		CharacterData characterData = PlayerData.instance.GetCharacterData(CharacterListCanvas.instance.selectedActorId);
		if (characterData == null)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterDontHave"), 2.0f);
			return;
		}

		// 토스트만 미리 띄우고 서버 응답 후 바꾸도록 한다.
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterChanged"), 2.0f);
		PlayFabApiManager.instance.RequestSelectMainCharacter(CharacterListCanvas.instance.selectedActorId, () =>
		{
			ChangeMainCharacter(CharacterListCanvas.instance.selectedActorId);
		});
	}

	void ChangeMainCharacter(string actorId)
	{
		// 이미 이 버튼을 누를 수 있다는건 액터로딩이 끝나있단 얘기다. 찾으면 있어야한다.
		PlayerActor newPlayerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (newPlayerActor == null)
			return;

		newPlayerActor.OnChangedMainCharacter();
	}

	public void OnClickUltimate()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString(actorTableData.ultimateId), 250, ultimateSkillIconImage.transform, new Vector2(10.0f, -45.0f));
	}

	string _exclusiveLevelPack1;
	public void OnClickLevelPack1Button()
	{
		LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(_exclusiveLevelPack1);
		if (levelPackTableData == null)
			return;

		string name = UIString.instance.GetString(levelPackTableData.nameId);
		name = name.Replace("FFC080", "DE7100");
		name = string.Format("<size=16>{0}</size>", name);
		string desc = UIString.instance.GetString(levelPackTableData.descriptionId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", name, desc), 250, packIconContentTransformList[0].transform, new Vector2(10.0f, -40.0f));
	}

	string _exclusiveLevelPack2;
	public void OnClickLevelPack2Button()
	{
		LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(_exclusiveLevelPack2);
		if (levelPackTableData == null)
			return;

		string name = UIString.instance.GetString(levelPackTableData.nameId);
		name = name.Replace("FFC080", "DE7100");
		name = string.Format("<size=16>{0}</size>", name);
		string desc = UIString.instance.GetString(levelPackTableData.descriptionId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", name, desc), 250, packIconContentTransformList[1].transform, new Vector2(10.0f, -40.0f));
	}

	public void OnClickGaugeDetailButton()
	{

	}

	public void OnClickLevelUpButton()
	{

	}
}