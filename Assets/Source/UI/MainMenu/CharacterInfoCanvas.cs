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
	bool _infoCameraMode = false;
	float _lastFov;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;
	Vector3 _lastCharacterPosition;
	Quaternion _lastCharacterRotation;
	Transform _groundTransform;
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
			_lastFov = UIInstanceManager.instance.GetCachedCameraMain().fieldOfView;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;
			_lastCharacterPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
			_lastCharacterRotation = BattleInstanceManager.instance.playerActor.cachedTransform.rotation;

			// ground setting
			if (_groundTransform == null)
			{
				_groundTransform = Instantiate<GameObject>(infoCameraGroundPrefab).transform;
			}
			else
			{
				_groundTransform.gameObject.SetActive(true);
			}

			// player setting
			BattleInstanceManager.instance.playerActor.cachedTransform.position = Vector3.zero;
			BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.Euler(0.0f, charactorY, 0.0f);
			TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 5);

			// setting
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
			CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;


		}
		else
		{
			if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
				return;

			_groundTransform.gameObject.SetActive(false);

			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;
			BattleInstanceManager.instance.playerActor.cachedTransform.position = _lastCharacterPosition;
			BattleInstanceManager.instance.playerActor.cachedTransform.rotation = _lastCharacterRotation;
			TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 5);

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

		BattleInstanceManager.instance.playerActor.cachedTransform.Rotate(0.0f, -pointerEventData.delta.x, 0.0f, Space.Self);
	}
	#endregion
}
