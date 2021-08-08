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
	bool _forResearchCanvas;
	Vector3 _playerPrevPosition;
	Quaternion _playerPrevRotation;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		// 분석에서 오리진을 뽑아서 연출을 보여줘야하는 상황이 생겨서 예외처리 해둔다.
		if (ResearchCanvas.instance != null && ResearchCanvas.instance.gameObject.activeSelf)
		{
			_playerActor = BattleInstanceManager.instance.playerActor;
			_playerActor.gameObject.SetActive(false);
			_playerPrevPosition = _playerActor.cachedTransform.position;
			_playerPrevRotation = _playerActor.cachedTransform.rotation;
			_playerActor = null;
			_forResearchCanvas = true;
			return;
		}

		// CharacterShowCanvasBase 클래스를 기반으로 만들어져있는데 이게 사실은 CharacterListCanvas에 맞춰서 만들어진건데
		// CharacterListCanvas뿐만 아니라 ExperienceCanvas까지 다 엮여있어서 이제와서 고치기가 너무 어렵다.
		// 그래서 CharacterShowCanvasBase를 수정하지 않는 선에서 어떤식으로 호출되어도 캐릭터가 잘 보여지도록 처리해보기로 한다.
		// 
		// 우선 현재 캐릭터 기반으로 카메라 모드를 변경시키고
		_playerActor = BattleInstanceManager.instance.playerActor;
		SetInfoCameraMode(true, _playerActor.actorId);

		// 현재 캐릭터를 꺼둔다.
		_playerActor.gameObject.SetActive(false);

		// 그리고 아예 null로 바꿔서 곧바로 OnDisable이 호출되더라도-그럴일은 없겠지만
		// 잘 복구되도록 한다.
		_playerActor = null;
	}

	void OnDisable()
	{
		if (_effectObject != null)
			_effectObject.SetActive(false);

		// 캐릭터 여러개 보여지다보면 _playerActor가 바뀌어져있을거다. 복구시켜준다.
		if (_playerActor != BattleInstanceManager.instance.playerActor)
		{
			_playerActor.gameObject.SetActive(false);
			_playerActor = BattleInstanceManager.instance.playerActor;
			_playerActor.gameObject.SetActive(true);
		}

		SetInfoCameraMode(false, "");

		// SetInfoCameraMode 까지 해제하고 나면 혹시 메인캐릭터 위치가 변경되었을 수도 있으니 복구해둔다. 분석에서 자기 자신 뽑을때의 예외처리다.
		if (_forResearchCanvas)
		{
			_playerActor.cachedTransform.position = _playerPrevPosition;
			_playerActor.cachedTransform.rotation = _playerPrevRotation;
			TailAnimatorUpdater.UpdateAnimator(_playerActor.cachedTransform, 15);
			_forResearchCanvas = false;
		}
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
		{
			_playerActor = playerActor;
			_playerActor.gameObject.SetActive(true);
			base.OnLoadedPlayerActor(true);
			OnAfterLoaded();
		}
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
		base.OnLoadedPlayerActor(true);
		OnAfterLoaded();
	}

	void OnAfterLoaded()
	{
		_effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, _rootOffsetPosition, Quaternion.identity, null);
	}

	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Top, UIString.instance.GetString(actorTableData.storyId), 300, characterNameText.transform, new Vector2(0.0f, 35.0f));
	}

	public void OnClickConfirmButton()
	{
		if (_okAction != null)
			_okAction();
	}
}