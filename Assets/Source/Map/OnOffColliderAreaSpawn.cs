using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnOffColliderAreaSpawn : MonoBehaviour
{
	OnOffColliderArea _onOffColliderArea = null;

	bool _started = false;
	void Start()
	{
		InitializeOnOffColliderArea();
		_started = true;
	}

	void OnEnable()
	{
		if (_started)
			InitializeOnOffColliderArea();
	}

	void InitializeOnOffColliderArea()
	{
		_onOffColliderArea = BattleInstanceManager.instance.GetCachedOnOffColliderArea(cachedTransform.position, Quaternion.identity);
	}

	void OnDisable()
	{
		if (_onOffColliderArea != null)
		{
			_onOffColliderArea.gameObject.SetActive(false);
			_onOffColliderArea = null;
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