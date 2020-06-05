using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class NodeWarGround : MonoBehaviour
{
	public static NodeWarGround instance;
	
	public Canvas worldCanvas;
	public int planeSize = 40;
	public LineRenderer safeAreaLineRenderer;
	public GameObject splitLineRendererObject;

	public Text centerLevelText;
	public GameObject centerFirstRewardObject;
	public GameObject[] centerPriceTypeObjectList;
	public Text centerRewardValueText;

	public Text leftLevelText;
	public GameObject leftFirstRewardObject;
	public GameObject[] leftPriceTypeObjectList;
	public Text leftRewardValueText;

	public Text rightLevelText;
	public GameObject rightFirstRewardObject;
	public GameObject[] rightPriceTypeObjectList;
	public Text rightRewardValueText;

	GameObject _planePrefab;
	bool _centerLevel;
	int _leftLevel;
	int _rightLevel;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		SetSafeLineRadius(3.0f, 80);

		int currentLevel = PlayerData.instance.nodeWarClearLevel;
		if (currentLevel == 0)
		{
			_centerLevel = true;
			currentLevel = 1;
			centerFirstRewardObject.SetActive(true);
			_leftLevel = _rightLevel = currentLevel;
		}
		else if (currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxNodeWarLevel"))
		{
			_centerLevel = true;
			centerFirstRewardObject.SetActive(false);
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
			if (centerFirstRewardObject.activeSelf)
			{
				NodeWarTableData nodeWarTableData = TableDataManager.instance.FindNodeWarTableData(currentLevel);
				if (nodeWarTableData != null)
				{
					// 재화는 둘중에 하나다.
					CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
					int rewardAmount = nodeWarTableData.firstRewardDiamond;
					if (nodeWarTableData.firstRewardGold > 0)
					{
						currencyType = CurrencyData.eCurrencyType.Gold;
						rewardAmount = nodeWarTableData.firstRewardGold;
					}
					for (int i = 0; i < centerPriceTypeObjectList.Length; ++i)
						centerPriceTypeObjectList[i].SetActive((int)currencyType == i);
					centerRewardValueText.text = rewardAmount.ToString("N0");
				}
			}
		}
		else
		{
			// 왼쪽은 깬거니 이름만 적으면 될거다.
			leftLevelText.text = string.Format("LEVEL {0}", _leftLevel);
			leftFirstRewardObject.SetActive(false);

			// 우측은 도전하는 레벨
			rightLevelText.text = string.Format("LEVEL {0}", _rightLevel);
			rightFirstRewardObject.SetActive(true);
			NodeWarTableData nodeWarTableData = TableDataManager.instance.FindNodeWarTableData(currentLevel);
			if (nodeWarTableData != null)
			{
				// 재화는 둘중에 하나다.
				CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
				int rewardAmount = nodeWarTableData.firstRewardDiamond;
				if (nodeWarTableData.firstRewardGold > 0)
				{
					currencyType = CurrencyData.eCurrencyType.Gold;
					rewardAmount = nodeWarTableData.firstRewardGold;
				}
				for (int i = 0; i < rightPriceTypeObjectList.Length; ++i)
					rightPriceTypeObjectList[i].SetActive((int)currencyType == i);
				rightRewardValueText.text = rewardAmount.ToString("N0");
			}
		}

		splitLineRendererObject.SetActive(!_centerLevel);
		centerLevelText.gameObject.SetActive(_centerLevel);
		leftLevelText.gameObject.SetActive(!_centerLevel);
		rightLevelText.gameObject.SetActive(!_centerLevel);
	}

	void Update()
	{
		UpdateLateInitialize();
		UpdateSeamlessGround();

		UpdateSafeLineArea();
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

	bool _outOfRange;
	void UpdateSafeLineArea()
	{
		if (_outOfRange)
			return;

		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		if (diff.sqrMagnitude < 3.0f * 3.0f)
			return;

		Timing.RunCoroutine(ScreenStartEffectProcess());
		_outOfRange = true;

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
		BattleManager.instance.OnSelectedNodeWarLevel(selectedLevel);
	}

	IEnumerator<float> ScreenStartEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.8f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		worldCanvas.gameObject.SetActive(false);
		safeAreaLineRenderer.gameObject.SetActive(false);
		splitLineRendererObject.SetActive(false);

		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("PowerSourceUI_Heal"), 2.5f);
		FadeCanvas.instance.FadeIn(0.8f);
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