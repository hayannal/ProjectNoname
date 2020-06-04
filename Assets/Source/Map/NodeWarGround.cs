using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarGround : MonoBehaviour
{
	public static NodeWarGround instance;

	public Canvas worldCanvas;

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

	// 로딩속도를 위해 만들어지고 Plane 프리팹을 Async로드하는게 아니라 생성될때 함께 로드된 Plane 프리팹을 전달받아서 사용하기로 한다.
	Dictionary<string, Collider> _dicCurrentPlaneCollider = new Dictionary<string, Collider>();
	public void InitializeGround(GameObject planePrefab)
	{
		_planePrefab = planePrefab;

		// 최초에는 중심에만 하나 만들고 주변 8칸은 한프레임씩 늦게 만들기로 한다.
		GameObject planeObject = BattleInstanceManager.instance.GetCachedObject(planePrefab, Vector3.zero, Quaternion.identity);
		Collider planeCollider = planeObject.GetComponent<Collider>();
		_dicCurrentPlaneCollider.Add("0x0", planeCollider);
	}

	public bool CheckPlaneCollider(Collider col)
	{
		Dictionary<string, Collider>.Enumerator e = _dicCurrentPlaneCollider.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.Value == null)
				continue;
			if (e.Current.Value.gameObject == null)
				continue;
			if (e.Current.Value.gameObject.activeSelf == false)
				continue;
			if (e.Current.Value != col)
				continue;
			return true;
		}
		return false;
	}
}