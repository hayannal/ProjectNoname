using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class EquipShowCanvasBase : MonoBehaviour
{
	public Transform infoCameraTransform;
	public float infoCameraFov = 43.0f;
	public GameObject equipInfoGroundPrefab;

	protected Vector3 _rootOffsetPosition = new Vector3(-75.0f, 0.0f, 0.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }
	bool _infoCameraMode = false;
	float _lastRendererResolutionFactor;
	float _lastBloomResolutionFactor;
	float _lastFov;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;
	Transform _groundTransform;
	EnvironmentSetting _environmentSetting;
	GameObject _prevEnvironmentSettingObject;
	TweenerCore<Vector3, Vector3, VectorOptions> _tweenReferenceForLocalMove;
	protected void SetInfoCameraMode(bool enable)
	{
		if (_infoCameraMode == enable)
			return;

		if (enable)
		{
			if (MainSceneBuilder.instance.lobby)
				LobbyCanvas.instance.OnEnterMainMenu(true);

			// disable prev component
			CameraFovController.instance.enabled = false;
			CustomFollowCamera.instance.enabled = false;

			// save prev info
			_lastRendererResolutionFactor = CustomRenderer.instance.RenderTextureResolutionFactor;
			_lastBloomResolutionFactor = CustomRenderer.instance.bloom.RenderTextureResolutoinFactor;
			_lastFov = UIInstanceManager.instance.GetCachedCameraMain().fieldOfView;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

			// ground setting
			_prevEnvironmentSettingObject = StageManager.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
			{
				_groundTransform = BattleInstanceManager.instance.GetCachedObject(equipInfoGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
				_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
			}
			else
			{
				_groundTransform.gameObject.SetActive(true);
			}

			if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
				TimeSpaceGround.instance.EnableObjectDeformer(false);

			// setting
			CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = 0.8f;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
			CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition + _rootOffsetPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;

			CustomFollowCamera.instance.cachedTransform.Translate(0.0f, 0.0f, -1.0f, Space.Self);
			_tweenReferenceForLocalMove = CustomFollowCamera.instance.cachedTransform.DOLocalMove(infoCameraTransform.localPosition + _rootOffsetPosition, 0.4f);
		}
		else
		{
			if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
				return;
			if (CustomFollowCamera.instance.gameObject == null)
				return;
			if (StageManager.instance == null)
				return;
			if (BattleInstanceManager.instance.playerActor == null || BattleInstanceManager.instance.playerActor.gameObject == null)
				return;

			_groundTransform.gameObject.SetActive(false);
			_prevEnvironmentSettingObject.SetActive(true);

			if (_tweenReferenceForLocalMove != null)
				_tweenReferenceForLocalMove.Kill();

			CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

			if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
				TimeSpaceGround.instance.EnableObjectDeformer(true);

			CameraFovController.instance.enabled = true;
			CustomFollowCamera.instance.enabled = true;
			LobbyCanvas.instance.OnEnterMainMenu(false);
		}
		_infoCameraMode = enable;
	}
}