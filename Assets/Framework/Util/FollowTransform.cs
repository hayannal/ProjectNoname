using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
	#region staticFunction
	public static void Follow(Transform rootTransform, Transform targetTransform, Vector3 worldOffset, bool disableOnDisableTarget = false)
	{
		FollowTransform followTransform = rootTransform.GetComponent<FollowTransform>();
		if (followTransform == null) followTransform = rootTransform.gameObject.AddComponent<FollowTransform>();
		followTransform.Follow(targetTransform, worldOffset, disableOnDisableTarget);
	}
	#endregion

	void OnDisable()
	{
		_follow = false;
	}

	Transform _targetTransform;
	Vector3 _worldOffset;
	bool _disableOnDisableTarget;
	bool _follow;
	public void Follow(Transform targetTransform, Vector3 worldOffset, bool disableOnDisableTarget)
	{
		_targetTransform = targetTransform;
		_worldOffset = worldOffset;
		_disableOnDisableTarget = disableOnDisableTarget;
		_follow = true;
	}

	void Update()
	{
		if (_follow == false || _targetTransform == null)
			return;

		cachedTransform.position = _targetTransform.position + _worldOffset;

		if (_disableOnDisableTarget)
		{
			if (_targetTransform.gameObject.activeSelf == false)
				gameObject.SetActive(false);
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