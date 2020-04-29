using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class CharacterBoxShowCanvas : CharacterShowCanvasBase
{
	public static CharacterBoxShowCanvas instance;

	public Text characterNameText;
	public Text characterDescText;
	public GameObject effectPrefab;

	GameObject _effectObject;

	void Awake()
	{
		instance = this;
	}

	void OnDisable()
	{
		if (_effectObject != null)
			_effectObject.SetActive(false);
		if (_playerActor != null)
			_playerActor.gameObject.SetActive(false);

		SetInfoCameraMode(false, "");
	}

	string _actorId;
	System.Action _okAction;
	public void ShowCanvas(string actorId, System.Action okAction)
	{
		// 연속해서 호출될 수 있으므로 미리 꺼놔야한다.
		if (_effectObject != null)
			_effectObject.SetActive(false);
		if (_playerActor != null)
			_playerActor.gameObject.SetActive(false);

		_actorId = actorId;
		_okAction = okAction;

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

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;

		characterNameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		characterDescText.SetLocalizedText(UIString.instance.GetString(actorTableData.descId));
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

		_effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, _rootOffsetPosition, Quaternion.identity, null);
	}

	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(actorTableData.storyId), 300, characterNameText.transform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickConfirmButton()
	{
		if (_okAction != null)
			_okAction();
	}
}