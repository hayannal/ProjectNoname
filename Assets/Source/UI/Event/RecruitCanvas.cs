using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine.SceneManagement;

public class RecruitCanvas : CharacterShowCanvasBase
{
	public static RecruitCanvas instance;

	public Text characterNameText;
	public Button characterNameTextButton;
	public Image characterNameButtonImage;
	public Text characterDescText;
	public Transform acceptingDescTransform;
	public GameObject detailButtonObject;
	public GameObject effectPrefab;

	void Awake()
	{
		instance = this;
	}

	// 전투결과에서 홈으로 돌아가는거라면 어차피 새로 만들어질테니 호출할 필요가 없다.
	//void OnDisable()
	//{
	//	SetInfoCameraMode(false, "");
	//}

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
			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId), "Character", OnLoadedPlayerActor);
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

		GameObject effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, _rootOffsetPosition, Quaternion.identity, null);
		ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < particleSystems.Length; ++i)
		{
			// 어차피 씬 이동할거니 캐싱은 전부 삭제될거다. 그래서 복구 루틴도 없다.
			ParticleSystem.MainModule main = particleSystems[i].main;
			main.useUnscaledTime = true;
		}
	}

	#region Info
	void RefreshInfo(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;

		bool clearChapter0 = (actorId == "Actor1002");

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

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(actorTableData.storyId), 300, characterNameText.transform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickDetailButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Top, UIString.instance.GetString("GameUI_Chp1AcceptingDesc"), 300, acceptingDescTransform, new Vector2(0.0f, 30.0f));
	}

	public void OnClickRecruitButton()
	{
		// 클라이벤트로 바뀌면서 이벤트 확인패킷이 필요없게 되었다.
		// 이미 메인캐릭터도 바꿔둔 상태니 메인씬으로 돌아가면 끝이다.
		SceneManager.LoadScene(0);
	}
}