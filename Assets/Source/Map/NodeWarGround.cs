using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeWarGround : MonoBehaviour
{
	public static NodeWarGround instance;
	
	public Canvas worldCanvas;
	public int planeSize = 40;
	public LineRenderer safeAreaLineRenderer;
	public GameObject splitLineRendererObject;
	public Text safeAreaInfoText;

	public CanvasGroup levelTextCanvasGroup;

	public Text centerLevelText;
	public Text centerChallengeText;
	public Text leftLevelText;
	public Text leftChallengeText;
	public Text rightLevelText;
	public Text rightChallengeText;

	public GameObject boostSignObject;
	public GameObject boostAppliedObject;

	public GameObject[] monsterPrefabList;
	public GameObject trapPrefab;
	public GameObject soulPrefab;
	public GameObject soulGetEffectPrefab;
	public GameObject healOrbPrefab;
	public GameObject healOrbGetEffectPrefab;
	public GameObject boostOrbPrefab;
	public GameObject boostOrbGetEffectPrefab;
	public GameObject nodeWarExitPortalPrefab;
	public GameObject nodeWarEndSafeAreaPrefab;

	GameObject _planePrefab;
	bool _centerLevel;
	int _leftLevel;
	int _rightLevel;
	ObjectIndicatorCanvas _objectIndicatorCanvas;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		SetSafeLineRadius(3.0f, 80);

		int clearLevel = PlayerData.instance.nodeWarClearLevel;
		int currentLevel = PlayerData.instance.nodeWarCurrentLevel;
		if (currentLevel == 0)
		{
			// 현재가 0이면 처음 NodeWar 진행하는 유저일거다.
			_centerLevel = true;
			currentLevel = 1;
			centerChallengeText.SetLocalizedText(UIString.instance.GetString("GameUI_NodeWarChallenge"));
			_leftLevel = _rightLevel = currentLevel;
		}
		else if (currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxNodeWarLevel"))
		{
			_centerLevel = true;
			centerChallengeText.SetLocalizedText(UIString.instance.GetString("GameUI_NodeWarRepeat"));
			_leftLevel = _rightLevel = currentLevel;
		}
		else
		{
			_centerLevel = false;
			_leftLevel = currentLevel;
			_rightLevel = currentLevel + 1;
		}

		if (_centerLevel)
		{
			centerLevelText.text = string.Format("LEVEL {0}", currentLevel);
		}
		else
		{
			// 왼쪽은 깬거니 이름만 적으면 될거다.
			leftLevelText.text = string.Format("LEVEL {0}", _leftLevel);
			leftChallengeText.SetLocalizedText(UIString.instance.GetString("GameUI_NodeWarRepeat"));

			// 우측은 도전하는 레벨. ClearLevel과 비교해서 처리하면 된다.
			rightLevelText.text = string.Format("LEVEL {0}", _rightLevel);
			rightChallengeText.SetLocalizedText(UIString.instance.GetString(_rightLevel > clearLevel ? "GameUI_NodeWarChallenge" : "GameUI_NodeWarRepeat"));
		}

		splitLineRendererObject.SetActive(!_centerLevel);
		centerLevelText.gameObject.SetActive(_centerLevel);
		leftLevelText.gameObject.SetActive(!_centerLevel);
		rightLevelText.gameObject.SetActive(!_centerLevel);

		_defaultSafeAreaMaterialAlpha = safeAreaLineRenderer.material.GetColor("_TintColor").a;
		_listSafeAreaLineMaterial.Add(safeAreaLineRenderer.material);
		Renderer[] splitLineRenderers = splitLineRendererObject.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < splitLineRenderers.Length; ++i)
			_listSafeAreaLineMaterial.Add(splitLineRenderers[i].material);

		// 시작하자마자 보상정보는 보이지 않는다.
		levelTextCanvasGroup.alpha = 0.0f;

		// 0.1초만 기다렸다가 바로 레벨업 이펙트부터 보여준다.
		_levelUpEffectDelayTime = 0.1f;

		// 난이도 단계 설명은 레벨업 연출이 끝나고 조금 후에 보여주도록 한다.
		_levelTextDelayTime = 1.5f;

		// 첫번째 몬스터 한마리를 미리 만들어놓고 하이드 시킨다. 첫 스폰 캐싱을 위함
		if (monsterPrefabList.Length > 0)
		{
			GameObject firstMonsterObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefabList[0], new Vector3(100.0f, 0.0f, 100.0f), Quaternion.identity);
			firstMonsterObject.SetActive(false);
		}

		// 교체가능은 항상 띄운다.
		PlayerIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform);

		// 부스트 알림 인디케이터 역시 항상 띄워야한다.
		AddressableAssetLoadManager.GetAddressableGameObject("NodeWarBoostIndicator", "Canvas", (prefab) =>
		{
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeInHierarchy == false) return;
			if (_outOfSafeArea) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = boostSignObject.transform;
		});

		RefreshNodeWarBoostApplyState();
	}

	public void RefreshNodeWarBoostApplyState()
	{
		boostAppliedObject.SetActive(PlayerData.instance.nodeWarBoostRemainCount > 0);
	}

	void Update()
	{
		UpdateLateInitialize();
		UpdateSeamlessGround();

		UpdateLevelUp();
		UpdateLevelText();
		UpdateSafeLineArea();
		UpdateInfoAlpha();
	}

	void SetSafeLineRadius(float radius, int segments)
	{
		safeAreaLineRenderer.positionCount = segments + 1;
		safeAreaLineRenderer.useWorldSpace = false;

		float x = 0.0f;
		float z = 0.0f;
		float deltaTheta = (float)(2.0 * Mathf.PI) / segments;
		float theta = 0.0f;

		for (int i = 0; i < segments + 1; ++i)
		{
			x = radius * Mathf.Cos(theta);
			z = radius * Mathf.Sin(theta);
			Vector3 pos = new Vector3(x, 0.0f, z);
			safeAreaLineRenderer.SetPosition(i, pos);
			theta += deltaTheta;
		}
	}


	static List<string> _listRandomPlane;
	public static string GetRandomPlaneAddress()
	{
		if (_listRandomPlane == null)
			_listRandomPlane = new List<string>();
		_listRandomPlane.Clear();

		int targetLevel = PlayerData.instance.nodeWarClearLevel;
		targetLevel += 1;
		for (int i = 0; i < TableDataManager.instance.nodeWarTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.nodeWarTable.dataArray[i].level > targetLevel)
				continue;

			for (int j = 0; j < TableDataManager.instance.nodeWarTable.dataArray[i].addPlane.Length; ++j)
				_listRandomPlane.Add(TableDataManager.instance.nodeWarTable.dataArray[i].addPlane[j]);
		}
		if (_listRandomPlane.Count == 0)
			return "";
		return _listRandomPlane[Random.Range(0, _listRandomPlane.Count)];
	}

	public static string GetRandomEnvAddress()
	{
		int targetLevel = PlayerData.instance.nodeWarClearLevel;
		targetLevel += 1;
		for (int i = TableDataManager.instance.nodeWarTable.dataArray.Length - 1; i >= 0; --i)
		{
			if (TableDataManager.instance.nodeWarTable.dataArray[i].level > targetLevel)
				continue;

			if (TableDataManager.instance.nodeWarTable.dataArray[i].environmentSetting.Length == 0)
				continue;

			return TableDataManager.instance.nodeWarTable.dataArray[i].environmentSetting[Random.Range(0, TableDataManager.instance.nodeWarTable.dataArray[i].environmentSetting.Length)];
		}
		return "";
	}


	// 지면은 9개로 나눠서 플레이어 주변에 배치해두기만 하면 된다.
	List<Collider> _listCurrentPlaneCollider = new List<Collider>();

	// 로딩속도를 위해 만들어지고 Plane 프리팹을 Async로드하는게 아니라 생성될때 함께 로드된 Plane 프리팹을 전달받아서 사용하기로 한다.
	public void InitializeGround(GameObject planePrefab, GameObject environmentSettingObject)
	{
		_planePrefab = planePrefab;

		// 9개의 슬롯은 미리 만들어둔다.
		for (int j = -1; j <= 1; ++j)
		{
			for (int i = -1; i <= 1; ++i)
			{
				_listCurrentPlaneCollider.Add(null);
			}
		}

		int middleIndex = 3 * 1 + 2 - 1;

		// 최초에는 중심에만 하나 만들고 주변 8칸은 한프레임씩 늦게 만들기로 한다. 한번에 8개 다 만들면 로딩 느릴까봐 이렇게 해본다.
		BattleInstanceManager.instance.GetCachedObject(environmentSettingObject, null);
		GameObject planeObject = BattleInstanceManager.instance.GetCachedObject(_planePrefab, Vector3.zero, Quaternion.identity);
		Collider planeCollider = planeObject.GetComponent<Collider>();
		_listCurrentPlaneCollider[middleIndex] = planeCollider;
		_createRemainCount = 8;
		_invalidChunk = true;
	}

	bool _skipFrame;
	int _createRemainCount;
	void UpdateLateInitialize()
	{
		if (_createRemainCount == 0)
			return;
		if (_skipFrame)
		{
			_skipFrame = false;
			return;
		}

		for (int j = 0; j <= 2; ++j)
		{
			for (int i = 0; i <= 2; ++i)
			{
				int index = j * 3 + i;
				if (_listCurrentPlaneCollider[index] != null)
					continue;

				float planePositionX = (_lastChunkX + i - 1) * planeSize;
				float planePositionZ = (_lastChunkZ + j - 1) * planeSize;
				GameObject planeObject = BattleInstanceManager.instance.GetCachedObject(_planePrefab, new Vector3(planePositionX, 0.0f, planePositionZ), Quaternion.identity);
				Collider planeCollider = planeObject.GetComponent<Collider>();
				_listCurrentPlaneCollider[index] = planeCollider;
				_createRemainCount -= 1;
				_skipFrame = true;
				return;
			}
		}
	}

	bool _invalidChunk = false;
	int _lastChunkX;
	int _lastChunkZ;
	void UpdateSeamlessGround()
	{
		Vector3 position = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		// 현재 위치로부터 청크 위치를 계산한 후
		float calX = (position.x + planeSize * 0.5f) / planeSize;
		float calZ = (position.z + planeSize * 0.5f) / planeSize;
		
		int x = (calX < 0.0f) ? (int)(calX - 1.0f) : (int)calX;
		int z = (calZ < 0.0f) ? (int)(calZ - 1.0f) : (int)calZ;

		bool refresh = false;
		if (_lastChunkX != x || _lastChunkZ != z || _invalidChunk)
			refresh = true;

		if (refresh == false)
			return;

		//Debug.LogFormat("Refresh Seamless Map. Chunk X : {0} / Chunk Z : {1}", x, z);
		_lastChunkX = x;
		_lastChunkZ = z;
		_invalidChunk = false;

		// 중앙청크 한개와 인근 8개의 청크를 계산해서 월드에 배치한다.
		for (int j = 0; j <= 2; ++j)
		{
			for (int i = 0; i <= 2; ++i)
			{
				int index = j * 3 + i;
				if (_listCurrentPlaneCollider[index] == null)
					continue;
				float planePositionX = (_lastChunkX + i - 1) * planeSize;
				float planePositionZ = (_lastChunkZ + j - 1) * planeSize;
				_listCurrentPlaneCollider[index].transform.position = new Vector3(planePositionX, 0.0f, planePositionZ);
			}
		}
	}

	float _levelUpEffectDelayTime;
	void UpdateLevelUp()
	{
		if (_levelUpEffectDelayTime > 0.0f)
		{
			_levelUpEffectDelayTime -= Time.deltaTime;
			if (_levelUpEffectDelayTime <= 0.0f)
			{
				_levelUpEffectDelayTime = 0.0f;
				BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerLevelUpEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);
				// 이미 레벨팩은 OnStartBattle에서 다 넣어둔 상태고 연출만 여기서 처리해준다.
				// 15번 하면 너무 많아서 잘 안보이니 적당히 많게 8번으로 해둔다.
				LobbyCanvas.instance.RefreshExpPercent(1.0f, 8);
				LobbyCanvas.instance.RefreshLevelText(StageManager.instance.GetMaxStageLevel());
				BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarInitialMind"), 2.5f);
			}
		}
	}

	float _levelTextDelayTime;
	float _targetLevelTextCanvasGroupAlpha;
	void UpdateLevelText()
	{
		if (_levelTextDelayTime > 0.0f)
		{
			_levelTextDelayTime -= Time.deltaTime;
			if (_levelTextDelayTime <= 0.0f)
			{
				_levelTextDelayTime = 0.0f;
				_targetLevelTextCanvasGroupAlpha = 1.0f;
			}
		}

		if (levelTextCanvasGroup.alpha == _targetLevelTextCanvasGroupAlpha)
			return;

		float diff = _targetLevelTextCanvasGroupAlpha - levelTextCanvasGroup.alpha;
		if (Mathf.Abs(diff) < 0.01f)
		{
			levelTextCanvasGroup.alpha = _targetLevelTextCanvasGroupAlpha;
			return;
		}
		levelTextCanvasGroup.alpha = Mathf.Lerp(levelTextCanvasGroup.alpha, _targetLevelTextCanvasGroupAlpha, Time.deltaTime * 3.0f);
	}

	bool _outOfSafeArea;
	void UpdateSafeLineArea()
	{
		if (_outOfSafeArea)
			return;

		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		if (diff.sqrMagnitude < 3.0f * 3.0f)
			return;

		_outOfSafeArea = true;
		_levelTextDelayTime = 0.0f;
		_targetLevelTextCanvasGroupAlpha = 0.0f;
		_targetInfoAlpha = 0.0f;

		int selectedLevel = 0;
		if (_centerLevel)
		{
			selectedLevel = _leftLevel;
		}
		else
		{
			if (diff.x > 0.0f) selectedLevel = _rightLevel;
			else selectedLevel = _leftLevel;
		}
		PlayerIndicatorCanvas.Show(false, null);
		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}
		boostSignObject.SetActive(false);
		BattleManager.instance.OnSelectedNodeWarLevel(selectedLevel);
	}

	float _targetInfoAlpha = 1.0f;
	void UpdateInfoAlpha()
	{
		// SafeArea Line 및 그 설명 텍스트에 적용하면 된다.
		if (safeAreaInfoText.color.a == _targetInfoAlpha)
			return;

		float diff = _targetInfoAlpha - safeAreaInfoText.color.a;
		if (Mathf.Abs(diff) < 0.01f)
		{
			safeAreaInfoText.color = new Color(1.0f, 1.0f, 1.0f, _targetInfoAlpha);
			SetLineRendererAlpha(_targetInfoAlpha);

			// 사실 여기서 사라질테니 아예 없애둔다.
			worldCanvas.gameObject.SetActive(false);
			safeAreaLineRenderer.gameObject.SetActive(false);
			splitLineRendererObject.SetActive(false);
			return;
		}
		safeAreaInfoText.color = Color.Lerp(safeAreaInfoText.color, new Color(1.0f, 1.0f, 1.0f, _targetInfoAlpha), Time.deltaTime * 3.0f);
		SetLineRendererAlpha(safeAreaInfoText.color.a);
	}

	List<Material> _listSafeAreaLineMaterial = new List<Material>();
	float _defaultSafeAreaMaterialAlpha;
	void SetLineRendererAlpha(float alphaRatio)
	{
		for (int i = 0; i < _listSafeAreaLineMaterial.Count; ++i)
		{
			_listSafeAreaLineMaterial[i].SetColor("_TintColor", new Color(1.0f, 1.0f, 1.0f, _defaultSafeAreaMaterialAlpha * alphaRatio));
		}
	}

	public GameObject GetMonsterPrefab(string monsterId)
	{
		for (int i = 0; i < monsterPrefabList.Length; ++i)
		{
			if (monsterPrefabList[i].name == monsterId)
				return monsterPrefabList[i];
		}
		Debug.LogErrorFormat("Not found NodeWar Monster Prefab. monsterId = {0}", monsterId);
		return null;
	}



	public bool CheckPlaneCollider(Collider col)
	{
		for (int i = 0; i < _listCurrentPlaneCollider.Count; ++i)
		{
			if (_listCurrentPlaneCollider[i] == null || _listCurrentPlaneCollider[i].gameObject == null)
				continue;
			if (_listCurrentPlaneCollider[i] != col)
				continue;
			return true;
		}
		return false;
	}
}