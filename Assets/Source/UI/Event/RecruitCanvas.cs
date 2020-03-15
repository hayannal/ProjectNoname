using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class RecruitCanvas : CharacterShowCanvasBase
{
	public static RecruitCanvas instance;

	public Text characterNameText;
	public Button characterNameTextButton;
	public Image characterNameButtonImage;
	public Text characterDescText;
	public Transform acceptingDescTransform;
	public GameObject detailButtonObject;

	void Awake()
	{
		instance = this;
	}

	void OnDisable()
	{
		SetInfoCameraMode(false, "");
	}

	string _actorId;
	public void ShowCanvas(string actorId)
	{
		_actorId = actorId;

		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
			_playerActor = playerActor;
		else
		{
			// 없다면 로딩 걸어두고 SetInfoCameraMode를 호출해둔다.
			// SetInfoCameraMode 안에는 이미 캐릭터가 없을때를 대비해서 코드가 짜여져있긴 하다.
			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId), "", OnLoadedPlayerActor);
		}
		SetInfoCameraMode(true, actorId);
		RefreshInfo(actorId);
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		_playerActor = playerActor;
		base.OnLoadedPlayerActor();
	}

	#region Info
	void RefreshInfo(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;

		bool clearChapter0 = (actorId == "Actor002");

		characterNameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		characterNameTextButton.interactable = (!clearChapter0);
		characterNameButtonImage.gameObject.SetActive(!clearChapter0);
		characterDescText.SetLocalizedText(UIString.instance.GetString(actorTableData.descId));

		acceptingDescTransform.gameObject.SetActive(clearChapter0);
		detailButtonObject.SetActive(clearChapter0);
	}
	#endregion

	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(actorTableData.storyId), 200, characterNameText.transform, new Vector2(0.0f, -30.0f));
	}

	public void OnClickDetailButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Top, UIString.instance.GetString("GameUI_Chp1AcceptingDesc"), 200, acceptingDescTransform, new Vector2(0.0f, 35.0f));
	}

	public void OnClickRecruitButton()
	{
		// 영입버튼을 누르면 이벤트 확인 패킷을 보내며
		// 이때 자동으로 셀렉트도 바꾸고
		// 이벤트 플래그도 끄고 등등.
	}
}