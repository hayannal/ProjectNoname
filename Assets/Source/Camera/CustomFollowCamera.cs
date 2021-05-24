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

	public float smoothTime = 0.3f;

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
	Vector3 _velocity = Vector3.zero;
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

		// 카메라에는 Time.deltaTime 대신 Time.smoothDeltaTime쓰는게 좋다.
		// 이동량의 정확도보다 부드러운게 더 중요하기 때문. 캐릭이랑 다르다.
		//cachedTransform.position = Vector3.Lerp(cachedTransform.position, cameraRelativePosition, followSpeed * Time.smoothDeltaTime);

		// 30프레임으로 낮춰도 안끊기게 하려고 다방면으로 테스트 했는데 캐릭터가 리지드바디쓰면 거의 답이없어보인다.
		// 캐릭 이동과 카메라 이동을 통일해라도 해보고
		// FixedUpdate에서 직접 보간처리도 해봤는데 여전히 폰에서 30프레임으로 하면 끊긴다.
		// 그래서 보니 롤 모바일은 리지드바디를 안쓰고 직접 구현한거 같고
		// 원신은 리지드바디를 써서 그런지(튕기는거보면 딱 리지드바디다) 30프레임 하면 끊기는게 확 느껴진다.
		// 그래서 결국 2021 이후 버전에서 고쳐지면 그때가서 다시 테스트해봐야 할거 같다.
		//cachedTransform.position = cameraRelativePosition;
		//cachedTransform.position = Vector3.Lerp(cachedTransform.position, cameraRelativePosition, lerpFactor);
		//cachedTransform.position = Vector3.Lerp(cachedTransform.position, cameraRelativePosition, 1 - Mathf.Exp(-2 * Time.deltaTime));
		//cachedTransform.position = Vector3.SmoothDamp(cachedTransform.position, cameraRelativePosition, ref _velocity, smoothTime, Mathf.Infinity, Time.smoothDeltaTime);
		//cachedTransform.position = Vector3.SmoothDamp(cachedTransform.position, cameraRelativePosition, ref _velocity, smoothTime, _followSpeed * 2.0f, Time.smoothDeltaTime);
		//cachedTransform.position = Vector3.SmoothDamp(cachedTransform.position, cameraRelativePosition, ref _velocity, smoothTime, _followSpeed * 2.0f, Time.fixedDeltaTime);

		// 그나마 이 방법이 기존에 쓰던 Lerp보다 조금은 더 나은거 같아서 쓰기로 한다.
		cachedTransform.position = Vector3.SmoothDamp(cachedTransform.position, cameraRelativePosition, ref _velocity, smoothTime, Mathf.Infinity, Time.smoothDeltaTime);

		LateUpdateTargetFrameRate();
	}

#if UNITY_IOS
	const int AdjustTargetFrameRate = 60;
#else
	const int AdjustTargetFrameRate = 50;
#endif
	bool _appliedAdjust = false;
	void LateUpdateTargetFrameRate()
	{
		// 이미 높게 설정되어있는 상태라면 아무것도 하지 않는다.
#if UNITY_IOS
		if (OptionManager.instance.frame >= 6)
			return;
#else
		if (OptionManager.instance.frame >= 4)
			return;
#endif

		bool applyAdjust = (Mathf.Abs(cachedTransform.position.z - cameraRelativePosition.z) > 0.1f);
		if (applyAdjust)
		{
			if (_appliedAdjust == false)
			{
				_appliedAdjust = true;
				Application.targetFrameRate = AdjustTargetFrameRate;
			}
		}
		else
		{
			if (_appliedAdjust)
			{
				_appliedAdjust = false;
				OptionManager.instance.frame = OptionManager.instance.frame;
			}
		}
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
	public float cachedQuadUp { get { return _quadUp; } }
	public float cachedQuadDown { get { return _quadDown; } }
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