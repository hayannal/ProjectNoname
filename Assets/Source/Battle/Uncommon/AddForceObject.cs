using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddForceObject : MonoBehaviour
{
	public float explosionCenterHeight = 1.5f;
	public float explosionForce = 20;
	public float explosionRadius = 3;
	public float explosionUpwardsModifier = 0;

	public bool dropUncommonObject;
	public GameObject uncommonDropPrefab;
	public float dropForce = 20;

	public static int ONLY_DEFAULT_LAYER;

	List<Transform> _listTransform = new List<Transform>();
	List<Rigidbody> _listRigidbody = new List<Rigidbody>();
	List<Vector3> _listPosition = new List<Vector3>();
	List<Quaternion> _listRotation = new List<Quaternion>();
	void Awake()
	{
		if (ONLY_DEFAULT_LAYER == 0) ONLY_DEFAULT_LAYER = LayerMask.NameToLayer("OnlyDefault");

		Transform[] transformList = GetComponentsInChildren<Transform>();
		for (int i = 0; i < transformList.Length; ++i)
		{
			if (transformList[i] == transform)
				continue;
			Rigidbody childRigidbody = transformList[i].GetComponent<Rigidbody>();
			if (childRigidbody == null)
				continue;
			_listTransform.Add(transformList[i]);
			_listRigidbody.Add(childRigidbody);
			_listPosition.Add(transformList[i].localPosition);
			_listRotation.Add(transformList[i].localRotation);
			transformList[i].gameObject.layer = ONLY_DEFAULT_LAYER;
		}
	}

	void OnEnable()
	{
		Vector3 explosionCenter = cachedTransform.position + Vector3.up * explosionCenterHeight;
		for (int i = 0; i < _listTransform.Count; ++i)
		{
			_listTransform[i].localPosition = _listPosition[i];
			_listTransform[i].localRotation = _listRotation[i];
			_listRigidbody[i].AddExplosionForce(explosionForce, explosionCenter, explosionRadius, explosionUpwardsModifier, ForceMode.Impulse);
		}

		if (dropUncommonObject)
		{
			Vector3 dropPosition = cachedTransform.position;
			dropPosition.y = 1.0f;
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(uncommonDropPrefab, dropPosition, Quaternion.identity);
			newObject.layer = ONLY_DEFAULT_LAYER;
			Rigidbody rigidbody = newObject.GetComponent<Rigidbody>();
			Vector3 dropCenter = dropPosition;
			Vector3 sideCenter = Random.onUnitSphere;
			sideCenter.y = 0.0f;
			sideCenter = sideCenter.normalized * 1.0f;
			dropCenter.x += sideCenter.x;
			dropCenter.z += sideCenter.z;
			float force = dropForce;
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar()) force = NodeWarProcessor.SpawnDistance * 1.5f;
			rigidbody.AddExplosionForce(force, dropCenter, explosionRadius, 0.0f, ForceMode.Impulse);
			BattleInstanceManager.instance.OnInitializeSummonObject(newObject);
		}
	}

	void OnDisable()
	{
		// 애니에 의해서 꺼질때 이 조각난 오브젝트 루트도 꺼놔야 다음에 재활용될때 OnEnable이 실행되지 않는다.
		gameObject.SetActive(false);
	}








	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}