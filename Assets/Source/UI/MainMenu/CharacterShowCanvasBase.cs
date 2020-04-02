using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterShowCanvasBase : MonoBehaviour
{
	public Transform infoCameraTransform;
	public float infoCameraFov = 43.0f;
	public float charactorY = 180.0f;

	protected PlayerActor _playerActor;

	#region Info Camera
	protected Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, 75.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }
	bool _infoCameraMode = false;
	float _lastRendererResolutionFactor;
	float _lastBloomResolutionFactor;
	float _lastFov;
	Color _lastBackgroundColor;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;
	Vector3 _lastCharacterPosition;
	Quaternion _lastCharacterRotation;
	Transform _groundTransform;
	EnvironmentSetting _environmentSetting;
	GameObject _prevEnvironmentSettingObject;
	float _defaultLightIntensity;
	ActorInfoTableData _cachedActorInfoTableData;
	protected void SetInfoCameraMode(bool enable, string actorId)
	{
		if (_infoCameraMode == enable)
			return;

		if (enable)
		{
			if (MainSceneBuilder.instance.lobby)
				LobbyCanvas.instance.OnEnterMainMenu(true);
			else
			{
				// lobby가 아닐때란건 아마 전투 후 열리는 영입창이란 얘기다. 불필요한 캔버스들을 다 가려둔다.
				LobbyCanvas.instance.gameObject.SetActive(false);
				SkillSlotCanvas.instance.gameObject.SetActive(false);
				if (BattleResultCanvas.instance != null)
					BattleResultCanvas.instance.gameObject.SetActive(false);
			}

			// disable prev component
			CameraFovController.instance.enabled = false;
			CustomFollowCamera.instance.enabled = false;

			// save prev info
			_lastRendererResolutionFactor = CustomRenderer.instance.RenderTextureResolutionFactor;
			_lastBloomResolutionFactor = CustomRenderer.instance.bloom.RenderTextureResolutoinFactor;
			_lastFov = UIInstanceManager.instance.GetCachedCameraMain().fieldOfView;
			_lastBackgroundColor = UIInstanceManager.instance.GetCachedCameraMain().backgroundColor;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;
			if (BattleInstanceManager.instance.playerActor == _playerActor)
			{
				_lastCharacterPosition = _playerActor.cachedTransform.position;
				_lastCharacterRotation = _playerActor.cachedTransform.rotation;
			}

			// table override
			_cachedActorInfoTableData = TableDataManager.instance.FindActorInfoTableData(actorId);

			// ground setting
			_prevEnvironmentSettingObject = StageManager.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
			{
				_groundTransform = Instantiate<GameObject>(StageManager.instance.characterInfoGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
				_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
				_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;

				// override setting
				if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoLightIntensity > 0.0f)
					_environmentSetting.SetDefaultLightIntensity(_cachedActorInfoTableData.infoLightIntensity);
			}
			else
			{
				// override setting
				if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoLightIntensity > 0.0f)
					_environmentSetting.SetDefaultLightIntensity(_cachedActorInfoTableData.infoLightIntensity);
				else
					_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);

				_groundTransform.gameObject.SetActive(true);
			}

			// player setting
			if (_playerActor != null)
				OnLoadedPlayerActor();

			// setting
			CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = 0.8f;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = new Color(0.1174f, 0.1255f, 0.1412f);
			CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition + _rootOffsetPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;
		}
		else
		{
			if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
				return;
			if (CustomFollowCamera.instance.gameObject == null)
				return;
			if (StageManager.instance == null)
				return;
			if (BattleInstanceManager.instance.playerActor.gameObject == null)
				return;

			_groundTransform.gameObject.SetActive(false);
			_prevEnvironmentSettingObject.SetActive(true);

			CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = _lastBackgroundColor;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;
			if (BattleInstanceManager.instance.playerActor == _playerActor)
			{
				BattleInstanceManager.instance.playerActor.cachedTransform.position = _lastCharacterPosition;
				BattleInstanceManager.instance.playerActor.cachedTransform.rotation = _lastCharacterRotation;
				TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 15);
			}

			if (_cachedActorInfoTableData != null)
			{
				if (_cachedActorInfoTableData.useInfoIdle)
					_playerActor.actionController.PlayActionByActionName("Idle");
				_cachedActorInfoTableData = null;
			}

			CameraFovController.instance.enabled = true;
			CustomFollowCamera.instance.enabled = true;
			LobbyCanvas.instance.OnEnterMainMenu(false);
		}
		_infoCameraMode = enable;
	}

	// 해당 Canvas보다 늦게 로딩될걸 대비해서 캐릭터 OnLoaded함수를 만들어놓는다.
	protected void OnLoadedPlayerActor(bool refreshActorInfoTable = false)
	{
		if (refreshActorInfoTable)
		{
			_cachedActorInfoTableData = TableDataManager.instance.FindActorInfoTableData(_playerActor.actorId);

			// override setting
			if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoLightIntensity > 0.0f)
				_environmentSetting.SetDefaultLightIntensity(_cachedActorInfoTableData.infoLightIntensity);
			else
				_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
		}

		_playerActor.cachedTransform.position = _rootOffsetPosition;
		float yaw = charactorY;
		if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoRotate != 0.0f)
			yaw = _cachedActorInfoTableData.infoRotate;
		_playerActor.cachedTransform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
		TailAnimatorUpdater.UpdateAnimator(_playerActor.cachedTransform, 15);
		if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.useInfoIdle)
			_playerActor.actionController.animator.Play("InfoIdle");
		if (MainSceneBuilder.instance.lobby == false)
		{
			// lobby가 아닐때란건 아마 전투 후 열리는 영입창이란 얘기다. TimeScale 걸려있을테니 Unscaled로 바꿔둔다. 어차피 씬 이동 할테니 복구코드는 없다.
			_playerActor.actionController.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
			_playerActor.playerAI.enabled = false;
		}
	}
	#endregion

	#region Character
	public void OnDragRect(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;
		if (_playerActor == null)
			return;

		float ratio = -pointerEventData.delta.x * 2.54f;
		ratio /= Screen.dpi;
		ratio *= 80.0f; // rotate speed
		_playerActor.cachedTransform.Rotate(0.0f, ratio, 0.0f, Space.Self);
	}
	#endregion

	#region Experience
	public void ChangeExperience()
	{
		CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
		CustomFollowCamera.instance.targetTransform = _playerActor.cachedTransform;
		_playerActor.actionController.PlayActionByActionName("Idle");
	}

	public void ResetExperience()
	{
		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
			return;

		// disable prev component
		CameraFovController.instance.enabled = false;
		CustomFollowCamera.instance.enabled = false;
		CustomFollowCamera.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;

		// player setting
		if (_playerActor != null)
			OnLoadedPlayerActor();

		// setting
		CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
		UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
		CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition + _rootOffsetPosition;
		CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;
	}

	public PlayerActor selectedPlayerActor { get { return _playerActor; } }

	public float GetLastFov()
	{
		return _lastFov;
	}

	public Vector3 GetLastCameraOffset()
	{
		Vector3 diff = _lastCameraPosition - _lastCharacterPosition;
		diff.x = 0.0f;
		return diff;
	}

	public Quaternion GetLastCameraRotation()
	{
		return _lastCameraRotation;
	}
	#endregion
}