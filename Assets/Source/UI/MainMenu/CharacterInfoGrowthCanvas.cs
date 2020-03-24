using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoGrowthCanvas : MonoBehaviour
{
	public static CharacterInfoGrowthCanvas instance;

	public Text gradeText;
	public Text nameText;
	public Button experienceButton;
	public Button swapButton;
	public Text swapButtonText;

	public Text powerSourceText;
	public Image ultimateSkillIconImage;
	public Transform exclusivePackRootTransform;
	public Text powerLevelText;
	public Text hpText;
	public Text atkText;
	public Slider ppSlider;
	public Text ppText;
	public Text priceText;

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
		string actorId = CharacterInfoCanvas.instance.currentActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);

		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", actorTableData.grade)));
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));

		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));

		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
		{
			powerLevelText.text = UIString.instance.GetString("GameUI_CharPower", playerActor.actorStatus.powerLevel);
			hpText.text = playerActor.actorStatus.GetDisplayMaxHp().ToString();
			atkText.text = playerActor.actorStatus.GetDisplayAttack().ToString();
		}
		ppText.text = UIString.instance.GetString("GameUI_StageFraction", 2, 20);
		priceText.text = 1000.ToString("N0");

		_actorId = actorId;
	}
	#endregion

	public void OnClickExperience()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ExperienceCanvas", null);
	}

	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		string story = UIString.instance.GetString(actorTableData.storyId);
		string desc = UIString.instance.GetString(actorTableData.descId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, string.Format("{0}\n\n{1}", story, desc), 300, nameText.transform, new Vector2(0.0f, -35.0f));

		// 뽑기창에서는 이와 다르게
		// Char CharDesc는 기본으로 나오고 돋보기로만 Story를 본다.
	}

	public void OnClickMainCharacter()
	{
		if (CharacterInfoCanvas.instance.currentActorId == BattleInstanceManager.instance.playerActor.actorId)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterAlready"), 2.0f);
			return;
		}

		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chapter) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_Chp1SwapUnavailable"), 2.0f);
			return;
		}

		CharacterData characterData = PlayerData.instance.GetCharacterData(CharacterInfoCanvas.instance.currentActorId);
		if (characterData == null)
			return;

		// 토스트만 미리 띄우고 서버 응답 후 바꾸도록 한다.
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterChanged"), 2.0f);
		PlayFabApiManager.instance.RequestSelectMainCharacter(CharacterInfoCanvas.instance.currentActorId, () =>
		{
			ChangeMainCharacter(CharacterInfoCanvas.instance.currentActorId);
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

	}

	public void OnClickLevelUpButton()
	{

	}
}