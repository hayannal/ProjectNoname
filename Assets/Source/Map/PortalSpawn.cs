using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalSpawn : MonoBehaviour
{
	public float openDelay;
	public Transform targetTransform;

	Portal _portal = null;
	void OnEnable()
	{
		_portal = BattleInstanceManager.instance.GetCachedPortal(cachedTransform.position, Quaternion.identity);
		_portal.targetPosition = targetTransform.position;
		_portal.StartPortalEffect();
		_openRemainTime = openDelay;
	}

	void OnDisable()
	{
		if (_portal != null)
		{
			_portal.gameObject.SetActive(false);
			_portal = null;
		}
	}

	float _openRemainTime;
	void Update()
	{
		if (_openRemainTime > 0.0f)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime += openDelay;
				_portal.StartPortalEffect();
			}
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
