using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchCanvas : MonoBehaviour
{
	public static ResearchCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject[] innerMenuPrefabList;
	public MenuButton[] menuButtonList;
	public GameObject menuRootObject;

	// 2개의 메뉴 다 알람을 쓰니 CharacterInfoCanvas 에서 했던거처럼 관리
	public RectTransform researchAlarmRootTransform;
	public RectTransform researchShadowAlarmRootTransform;
	public RectTransform analysisAlarmRootTransform;
	public RectTransform analysisShadowAlarmRootTransform;

	public GameObject researchGroundObjectPrefab;
	public GameObject inputLockObject;

	void Awake()
	{
		instance = this;
	}

	GameObject _researchGroundObject;
	void Start()
	{
		_researchGroundObject = Instantiate<GameObject>(researchGroundObjectPrefab, _rootOffsetPosition, Quaternion.identity);

		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(0);
	}

	void OnEnable()
	{
		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(_lastIndex == i);
		}

		if (_researchGroundObject != null)
			_researchGroundObject.SetActive(true);

		RefreshAlarmObjectList();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		
		if (restore)
			return;

		SetInfoCameraMode(true);
	}

	void OnDisable()
	{
		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(false);
		}
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

	public void RefreshAlarmObjectList()
	{
		if (ResearchInfoGrowthCanvas.CheckResearch(PlayerData.instance.researchLevel + 1))
		{
			AlarmObject.Show(researchAlarmRootTransform);
			AlarmObject.Show(researchShadowAlarmRootTransform, true, false, false, true);
		}
		else
		{
			AlarmObject.Hide(researchAlarmRootTransform);
			AlarmObject.Hide(researchShadowAlarmRootTransform);
		}

		if (ResearchInfoAnalysisCanvas.CheckAnalysis())
		{
			AlarmObject.Show(analysisAlarmRootTransform);
			AlarmObject.Show(analysisShadowAlarmRootTransform, true, false, false, true);
		}
		else
		{
			AlarmObject.Hide(analysisAlarmRootTransform);
			AlarmObject.Hide(analysisShadowAlarmRootTransform);
		}
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


	#region Menu Button
	public void OnClickMenuButton1() { OnValueChangedToggle(0); }
	public void OnClickMenuButton2() { OnValueChangedToggle(1); }

	List<Transform> _listMenuTransform = new List<Transform>();
	int _lastIndex = -1;
	void OnValueChangedToggle(int index)
	{
		if (index == _lastIndex)
			return;

		if (_listMenuTransform.Count == 0)
		{
			for (int i = 0; i < menuButtonList.Length; ++i)
				_listMenuTransform.Add(null);
		}

		if (_listMenuTransform[index] == null && innerMenuPrefabList[index] != null)
		{
			GameObject newObject = Instantiate<GameObject>(innerMenuPrefabList[index]);
			_listMenuTransform[index] = newObject.transform;
		}

		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			menuButtonList[i].isOn = (index == i);
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(index == i);
		}

		_lastIndex = index;
	}
	#endregion



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
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = new Color(0.183f, 0.19f, 0.208f);
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