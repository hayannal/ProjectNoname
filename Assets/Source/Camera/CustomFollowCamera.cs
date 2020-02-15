using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomFollowCamera : MonoBehaviour
{
	public static CustomFollowCamera instance;

	public bool checkPlaneLeftRightQuad = true;
	public bool checkPlaneUpDownQuad = false;

	[SerializeField]
	private Transform _targetTransform;

	[SerializeField]
	private float _distanceToTarget = 30.0f;

	[SerializeField]
	private float _followSpeed = 3.0f;

	#region PROPERTIES

	public Transform targetTransform
	{
		get { return _targetTransform; }
		set { _targetTransform = value; }
	}

	public float distanceToTarget
	{
		get { return _distanceToTarget; }
		set { _distanceToTarget = Mathf.Max(0.0f, value); }
	}

	public float followSpeed
	{
		get { return _followSpeed; }
		set { _followSpeed = Mathf.Max(0.0f, value); }
	}

	private Vector3 cameraRelativePosition
	{
		get
		{
			Vector3 result = targetTransform.position - cachedTransform.forward * distanceToTarget;
			if (_quadLoaded && checkPlaneLeftRightQuad)
			{
				if (result.x < _quadLeft - LEFT_LIMIT)
					result.x = _quadLeft - LEFT_LIMIT;
				if (result.x > _quadRight - RIGHT_LIMIT)
					result.x = _quadRight - RIGHT_LIMIT;
			}
			return result;
		}
	}

	#endregion

	#region MONOBEHAVIOUR

	public void OnValidate()
	{
		distanceToTarget = _distanceToTarget;
		followSpeed = _followSpeed;
	}

	public void Awake()
	{
		instance = this;

		if (targetTransform != null)
			cachedTransform.position = cameraRelativePosition;
	}

	Transform _prevTargetTransform;
	void Update()
	{
		if (_prevTargetTransform != targetTransform && targetTransform != null)
		{
			_immediatelyUpdate = true;
			_prevTargetTransform = targetTransform;
		}
	}

	public bool immediatelyUpdate { set { _immediatelyUpdate = value; } }
	bool _immediatelyUpdate;
	public void LateUpdate()
	{
		if (targetTransform == null)
			return;

		if (_immediatelyUpdate)
		{
			cachedTransform.position = cameraRelativePosition;
			_immediatelyUpdate = false;
			return;
		}

		cachedTransform.position = Vector3.Lerp(cachedTransform.position, cameraRelativePosition, followSpeed * Time.deltaTime);
	}

	#endregion

	const float LEFT_LIMIT = -3.93f;
	const float RIGHT_LIMIT = 3.93f;
	const float UP_LIMIT = 10.5f;
	const float DOWN_LIMIT = -12.5f;
	float _quadUp;
	float _quadDown;
	float _quadLeft;
	float _quadRight;
	bool _quadLoaded = false;
	public float cachedQuadLeft { get { return _quadLeft; } }
	public float cachedQuadRight { get { return _quadRight; } }
	public void OnLoadPlaneObject(float quadUp, float quadDown, float quadLeft, float quadRight)
	{
		_quadUp = quadUp;
		_quadDown = quadDown;
		_quadLeft = quadLeft;
		_quadRight = quadRight;
		_quadLoaded = true;
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