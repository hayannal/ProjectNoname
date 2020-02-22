using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalSpawn : MonoBehaviour
{
	public Transform targetTransform;

	Portal _portal = null;

	bool _started = false;
	void Start()
	{
		InitializePortal();
		_started = true;
	}

	// 사실은 OnEnable에서만 해도 되는건데 BattleScene에서 BattleManager보다 나중에 만들어지게 하기 위해 이렇게 처리한다.
	void OnEnable()
	{
		if (_started)
			InitializePortal();
	}

	void InitializePortal()
	{
		_portal = BattleInstanceManager.instance.GetCachedPortal(cachedTransform.position, Quaternion.identity);
		_portal.targetPosition = targetTransform.position;
	}

	void OnDisable()
	{
		if (_portal != null)
		{
			_portal.gameObject.SetActive(false);
			_portal = null;
		}
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
