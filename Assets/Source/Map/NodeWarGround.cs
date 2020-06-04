using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarGround : MonoBehaviour
{
	public static NodeWarGround instance;
	
	public Canvas worldCanvas;
	public int planeSize = 40;

	GameObject _planePrefab;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void Update()
	{
		UpdateLateInitialize();
		UpdateSeamlessGround();
	}

	
	// 지면은 9개로 나눠서 플레이어 주변에 배치해두기만 하면 된다.
	List<Collider> _listCurrentPlaneCollider = new List<Collider>();

	// 로딩속도를 위해 만들어지고 Plane 프리팹을 Async로드하는게 아니라 생성될때 함께 로드된 Plane 프리팹을 전달받아서 사용하기로 한다.
	public void InitializeGround(GameObject planePrefab)
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