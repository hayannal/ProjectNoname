using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterInfoCanvas : MonoBehaviour
{
	public GameObject infoCameraGroundPrefab;
	public Transform infoCameraTransform;
	public float infoCameraFov = 60.0f;
	public float charactorY = 180.0f;

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		SetInfoCameraMode(true);
	}

	void OnDisable()
	{
		SetInfoCameraMode(false);
	}




	#region Info Camera
	Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, 75.0f);
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
	float _defaultLightIntensity;
	ActorInfoTableData _cachedActorInfoTableData;
	void SetInfoCameraMode(bool enable)
	{
		if (_infoCameraMode == enable)
			return;

		if (enable)
		{
			LobbyCanvas.instance.OnEnterMainMenu(true);

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
			_lastCharacterPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
			_lastCharacterRotation = BattleInstanceManager.instance.playerActor.cachedTransform.rotation;

			// table override
			_cachedActorInfoTableData = TableDataManager.instance.FindActorInfoTableData(BattleInstanceManager.instance.playerActor.actorId);

			// ground setting
			StageManager.instance.EnableEnvironmentSettingForUI(false);
			if (_groundTransform == null)
			{
				_groundTransform = Instantiate<GameObject>(infoCameraGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
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
			BattleInstanceManager.instance.playerActor.cachedTransform.position = _rootOffsetPosition;
			BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.Euler(0.0f, charactorY, 0.0f);
			TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 15);
			if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.useInfoIdle)
				BattleInstanceManager.instance.playerActor.actionController.animator.Play("InfoIdle");

			// setting
			CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = 0.8f;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = Color.black;
			CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition + _rootOffsetPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;
		}
		else
		{
			if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
				return;

			_groundTransform.gameObject.SetActive(false);
			StageManager.instance.EnableEnvironmentSettingForUI(true);

			CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = _lastBackgroundColor;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;
			BattleInstanceManager.instance.playerActor.cachedTransform.position = _lastCharacterPosition;
			BattleInstanceManager.instance.playerActor.cachedTransform.rotation = _lastCharacterRotation;
			TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 15);

			if (_cachedActorInfoTableData != null)
			{
				if (_cachedActorInfoTableData.useInfoIdle)
					BattleInstanceManager.instance.playerActor.actionController.PlayActionByActionName("Idle");
				_cachedActorInfoTableData = null;
			}

			CameraFovController.instance.enabled = true;
			CustomFollowCamera.instance.enabled = true;
			LobbyCanvas.instance.OnEnterMainMenu(false);
		}
		_infoCameraMode = enable;
	}
	#endregion

	#region Character
	public void OnDragRect(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;

		float ratio = -pointerEventData.delta.x * 2.54f;
		ratio /= Screen.dpi;
		ratio *= 70.0f;	// rotate speed
		BattleInstanceManager.instance.playerActor.cachedTransform.Rotate(0.0f, ratio, 0.0f, Space.Self);
	}
	#endregion
}
