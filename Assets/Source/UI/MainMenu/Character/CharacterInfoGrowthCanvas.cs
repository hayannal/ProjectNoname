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

	public GameObject limitBreakRectObject;
	public Text limitBreakLevelText;
	public Text powerLevelText;
	public Text hpText;
	public Text atkText;
	public GameObject sliderRectObject;
	public Slider ppSlider;
	public Text ppText;
	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;
	public RectTransform alarmRootTransform;

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
		swapButtonImage.color = contains ? Color.white : ColorUtil.halfGray;
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
		RefreshRequired();
	}

	void RefreshStatus()
	{
		// 구조 바꾸면서 플레이 중에 못찾는건 없어졌는데 Canvas켜둔채 종료하니 자꾸 뜬다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_actorId);
		if (playerActor == null)
			return;

		int limitBreakLevel = 0;
		CharacterData characterData = PlayerData.instance.GetCharacterData(_actorId);
		if (characterData != null)
			limitBreakLevel = characterData.limitBreakLevel;
		limitBreakRectObject.SetActive(limitBreakLevel > 0);
		if (limitBreakLevel > 0)
		{
			limitBreakLevelText.SetLocalizedText(UIString.instance.GetString("GameUI_CharLimitBreak", limitBreakLevel));
			powerLevelText.text = UIString.instance.GetString("GameUI_CharLbPower", playerActor.actorStatus.powerLevel, characterData.maxPowerLevelOfCurrentLimitBreak);
		}
		else
			powerLevelText.text = UIString.instance.GetString("GameUI_CharPower", playerActor.actorStatus.powerLevel);
		hpText.text = playerActor.actorStatus.GetDisplayMaxHp().ToString("N0");
		atkText.text = playerActor.actorStatus.GetDisplayAttack().ToString("N0");
	}

	bool _overMaxMode = false;
	bool _limitBreakMode = false;
	int _price;
	bool _needPp;
	bool _needLimitBreakPoint;
	void RefreshRequired()
	{
		AlarmObject.Hide(alarmRootTransform);

		int powerLevel = 1;
		int pp = 0;
		bool dontHave = true;
		CharacterData characterData = PlayerData.instance.GetCharacterData(_actorId);
		if (characterData != null)
		{
			powerLevel = characterData.powerLevel;
			pp = characterData.pp;
			dontHave = false;
		}

		_overMaxMode = false;
		_limitBreakMode = false;
		_needPp = false;
		_needLimitBreakPoint = false;
		if (powerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
		{
			_overMaxMode = true;
			ppText.text = UIString.instance.GetString("GameUI_OverPp", pp - characterData.maxPp);
			ppSlider.value = 1.0f;
			sliderRectObject.SetActive(true);
			priceButtonObject.SetActive(false);

			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			maxButtonObject.SetActive(true);
		}
		else
		{
			int current = 0;
			int max = 0;
			PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel);
			PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel + 1);
			if (characterData != null && characterData.needLimitBreak)
			{
				_limitBreakMode = true;
				current = characterData.limitBreakPoint - powerLevelTableData.requiredLimitBreak;
				max = 1;
				_needLimitBreakPoint = current < max;
			}
			else
			{
				current = pp - powerLevelTableData.requiredAccumulatedPowerPoint;
				max = nextPowerLevelTableData.requiredPowerPoint;
				_needPp = current < max;
			}

			if (!dontHave)
			{
				ppText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
				ppSlider.value = Mathf.Min(1.0f, (float)current / (float)max);
			}
			sliderRectObject.SetActive(!dontHave);

			int requiredGold = nextPowerLevelTableData.requiredGold;
			if (_limitBreakMode) requiredGold = nextPowerLevelTableData.requiredLimitBreakGold;
			priceText.text = requiredGold.ToString("N0");

			bool disablePrice = (dontHave || CurrencyData.instance.gold < requiredGold || current < max);
			priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			priceText.color = !disablePrice ? Color.white : Color.gray;
			goldGrayscaleEffect.enabled = disablePrice;
			priceButtonObject.SetActive(true);
			maxButtonObject.SetActive(false);
			_price = requiredGold;

			if (current >= max)
				AlarmObject.Show(alarmRootTransform, false, false, true);
		}
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
		string text = "";
		if (_overMaxMode)
		{
			float percent = 0.0f;			
			if (PlayerData.instance.ContainsActor(_actorId))
			{
				PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_actorId);
				if (playerActor != null)
					percent = playerActor.actorStatus.GetAttackAddRateByOverPP() * 100.0f;
			}
			text = UIString.instance.GetString("GameUI_OverMaxDesc", percent);
		}
		else if (_limitBreakMode)
		{
			int pp = 0;
			int maxPpOfCurrentLimitBreak = 0;
			CharacterData characterData = PlayerData.instance.GetCharacterData(_actorId);
			if (characterData != null)
			{
				pp = characterData.pp;
				maxPpOfCurrentLimitBreak = characterData.maxPpOfCurrentLimitBreak;
			}
			text = string.Format("{0}\n\n{1}", UIString.instance.GetString("GameUI_CharTranscendenceDesc"), UIString.instance.GetString("GameUI_NowPp", pp - characterData.maxPpOfCurrentLimitBreak));
		}
		else
			text = UIString.instance.GetString("GameUI_CharGaugeDesc");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, ppSlider.transform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickLevelUpButton()
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(_actorId);
		if (characterData == null)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterDontHave"), 2.0f);
			return;
		}

		if (characterData.powerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		if (_needLimitBreakPoint)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughLbp"), 2.0f);
			return;
		}

		if (_needPp)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughPp"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		if (characterData.needLimitBreak)
		{
			UIInstanceManager.instance.ShowCanvasAsync("CharacterLimitBreakCanvas", () =>
			{
				CharacterLimitBreakCanvas.instance.ShowCanvas(true, characterData, _price);
			});
		}
		else
		{
			UIInstanceManager.instance.ShowCanvasAsync("CharacterPowerLevelUpCanvas", () =>
			{
				CharacterPowerLevelUpCanvas.instance.ShowCanvas(true, characterData, _price);
			});
		}
	}
}