using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoInnerCanvas : MonoBehaviour
{
	public static CharacterInfoInnerCanvas instance;

	public Button swapButton;
	public Button experienceButton;

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
	public void RefreshInfo()
	{
		string actorId = CharacterInfoCanvas.instance.currentActorId;


		// tooltip
		// CharStory + "\n\n" + CharDesc

		// 뽑기창에서는 이와 다르게
		// Char CharDesc는 기본으로 나오고 돋보기로만 Story를 본다.
	}
	#endregion

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

		// 선처리하고 에러가 나면 그때 에러팝업 뜨는 형태로 바꾸기로 한다.
		ChangeMainCharacter(CharacterInfoCanvas.instance.currentActorId);
		PlayFabApiManager.instance.RequestSelectMainCharacter(CharacterInfoCanvas.instance.currentActorId, null);
	}

	void ChangeMainCharacter(string actorId)
	{
		// 이미 이 버튼을 누를 수 있다는건 액터로딩이 끝나있단 얘기다. 찾으면 있어야한다.
		PlayerActor newPlayerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (newPlayerActor == null)
			return;

		newPlayerActor.OnChangedMainCharacter();
		PlayerData.instance.mainCharacterId = newPlayerActor.actorId;
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterChanged"), 2.0f);
	}

	public void OnClickExperience()
	{

	}
}