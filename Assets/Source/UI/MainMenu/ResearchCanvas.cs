using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchCanvas : MonoBehaviour
{
	public static ResearchCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject screenSpaceCanvasPrefab;
	public GameObject researchGroundObjectPrefab;
	public GameObject inputLockObject;

	void Awake()
	{
		instance = this;
	}

	GameObject _screenSpaceCanvasObject;
	GameObject _researchGroundObject;
	void Start()
	{
		_screenSpaceCanvasObject = Instantiate<GameObject>(screenSpaceCanvasPrefab);
		_researchGroundObject = Instantiate<GameObject>(researchGroundObjectPrefab, _rootOffsetPosition, Quaternion.identity);
	}

	void OnEnable()
	{
		if (_screenSpaceCanvasObject != null)
			_screenSpaceCanvasObject.SetActive(true);
		if (_researchGroundObject != null)
			_researchGroundObject.SetActive(true);

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		
		if (restore)
			return;

		SetInfoCameraMode(true);
	}

	void OnDisable()
	{
		_screenSpaceCanvasObject.SetActive(false);
		_researchGroundObject.SetActive(false);

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		SetInfoCameraMode(false);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		LobbyCanvas.Home();
	}



	// CharacterShowCanvasBase 처럼 ResearchShowCanvasBase같은걸 만들까 하다가 연구는 여러개로 나눠서 상속받을 일이 없을거 같아서
	// 하나의 클래스 안에 넣기로 한다.
	public Transform infoCameraTransform;
	public float infoCameraFov = 43.0f;

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
	
	Transform _groundTransform;
	GameObject _prevEnvironmentSettingObject;
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
			_lastBackgroundColor = UIInstanceManager.instance.GetCachedCameraMain().backgroundColor;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

			// ground setting
			_prevEnvironmentSettingObject = StageManager.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
				_groundTransform = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.characterInfoGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
			else
				_groundTransform.gameObject.SetActive(true);
			CharacterInfoGround.instance.stoneObject.SetActive(false);

			if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
				TimeSpaceGround.instance.EnableObjectDeformer(false);

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

			CharacterInfoGround.instance.stoneObject.SetActive(true);
			_groundTransform.gameObject.SetActive(false);
			_prevEnvironmentSettingObject.SetActive(true);

			CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = _lastBackgroundColor;
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
	#endregion
}