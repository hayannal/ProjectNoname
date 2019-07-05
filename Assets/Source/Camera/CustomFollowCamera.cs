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
			Vector3 result = targetTransform.position - transform.forward * distanceToTarget;
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
		transform.position = cameraRelativePosition;
	}

	public void LateUpdate()
	{
		transform.position = Vector3.Lerp(transform.position, cameraRelativePosition, followSpeed * Time.deltaTime);
	}

	#endregion

	const float LEFT_LIMIT = -7.5f;
	const float RIGHT_LIMIT = 7.5f;
	const float UP_LIMIT = 10.5f;
	const float DOWN_LIMIT = -12.5f;
	float _quadUp;
	float _quadDown;
	float _quadLeft;
	float _quadRight;
	bool _quadLoaded = false;
	public void OnLoadPlaneObject(GameObject quadRootObject)
	{
		Transform quadRootTransform = quadRootObject.transform;
		_quadUp = quadRootTransform.Find("QuadUp").localPosition.z;
		_quadDown = quadRootTransform.Find("QuadDown").localPosition.z;
		_quadLeft = quadRootTransform.Find("QuadLeft").localPosition.x;
		_quadRight = quadRootTransform.Find("QuadRight").localPosition.x;
		_quadLoaded = true;
	}
}