using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ObjectIndicatorCanvas : MonoBehaviour
{
	public Transform targetTransform;
	public RectTransform contentGroupRectTransform;
	public RectTransform bottomLineImageRectTransform;
	public RectTransform bottomLeftEndRectTransform;
	public RectTransform bottomRightEndRectTransform;
	public RectTransform lineImageRectTransform;
	public Image lineImage;
	public Vector2 lineOffset = new Vector2(0.0f, 0.0f);
	public Vector2 contentOffset = new Vector2(0.75f, 1.0f);
	public bool rightPosition = true;
	bool useTweenOutBack = false;
	public bool useLeftRightSwapByAxisX = false;
	public float leftRightSwapThreshold = 1.5f;
	public float swapSuppressingRange = 2.5f;

	// Start is called before the first frame update
	void Start()
    {
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		InitializeTarget(targetTransform);
    }

	protected void InitializeTarget(Transform t)
	{
		if (t == null)
			return;

		_prevTargetTransform = targetTransform = t;
		GetTargetHeightAndRadius(targetTransform);
		bottomLineImageRectTransform.pivot = new Vector2(rightPosition ? 0.0f : 1.0f, bottomLineImageRectTransform.pivot.y);
		_immediatelyUpdate = true;
		_lastRightPosition = rightPosition;
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	// Update is called once per frame
    void Update()
    {
		UpdateObjectIndicator();
	}

	Transform _prevTargetTransform;
	protected void UpdateObjectIndicator()
	{
		if (_prevTargetTransform != targetTransform && targetTransform != null)
		{
			InitializeTarget(targetTransform);
		}

		UpdateTargetPosition();
		UpdateTextImagePosition();

		if (useLeftRightSwapByAxisX)
			UpdateLeftRightSwapByAxisX();
	}

	void GetTargetHeightAndRadius(Transform t)
	{
		Collider collider = t.GetComponentInChildren<Collider>();
		if (collider == null)
			return;

		_targetHeight = ColliderUtil.GetHeight(collider);
		_targetRadius = ColliderUtil.GetRadius(collider);
	}

	Vector3 _targetPrevPosition;
	void UpdateTargetPosition()
	{
		if (targetTransform == null)
			return;

		if (_targetPrevPosition != targetTransform.position)
		{
			// 3D 포지션 Lerp로 바꾸면서 타겟 위치가 바뀌었다고 즉시 갱신할 필요가 없어졌다.
			//_immediatelyUpdate = true;
			_targetPrevPosition = targetTransform.position;
		}
	}

	bool _immediatelyUpdate;
	float _immediatelyAlphaDistance;
	float _targetHeight;
	float _targetRadius;
	bool _lastRightPosition = false;
	void UpdateTextImagePosition()
	{
		if (targetTransform == null)
			return;

		Vector3 desiredPosition = targetTransform.position;
		desiredPosition.y += _targetHeight;
		desiredPosition.y += lineOffset.y;
		float deltaX = _targetRadius + lineOffset.x;
		lineImageRectTransform.position = desiredPosition + new Vector3(_lastRightPosition ? deltaX : -deltaX, 0.0f);

		if (useLeftRightSwapByAxisX)
		{
			if (rightPosition)
			{
				if (desiredPosition.x > CustomFollowCamera.instance.cachedQuadRight - leftRightSwapThreshold - swapSuppressingRange)
					desiredPosition.x = CustomFollowCamera.instance.cachedQuadRight - leftRightSwapThreshold - swapSuppressingRange;
			}
			else
			{
				if (desiredPosition.x < CustomFollowCamera.instance.cachedQuadLeft + leftRightSwapThreshold + swapSuppressingRange)
					desiredPosition.x = CustomFollowCamera.instance.cachedQuadLeft + leftRightSwapThreshold + swapSuppressingRange;
			}
		}

		desiredPosition.y += contentOffset.y;
		deltaX = _targetRadius + lineOffset.x + contentOffset.x + contentGroupRectTransform.sizeDelta.x * 0.5f;
		desiredPosition.x += rightPosition ? deltaX : -deltaX;

		if (_immediatelyUpdate)
		{
			contentGroupRectTransform.position = desiredPosition;
			_immediatelyUpdate = false;
			_immediatelyAlphaDistance = Mathf.Abs(contentGroupRectTransform.position.x - targetTransform.position.x);
		}
		else
		{
			// z값을 고정시켜서 하는 방법도 있겠지만
			// 이렇게 x, y, z 전부다 Lerp해야 진짜 3D 공간처럼 보인다.
			//textGroupRectTransform.position = new Vector3(textGroupRectTransform.position.x, textGroupRectTransform.position.y, desiredPosition.z);

			// 상황별 체크를 위해 분기
			SetTargetPosition(desiredPosition);
		}

		// 선은 rightPosition으로 바로 체크하지 않고
		// 마지막으로 설정된걸 쓰다가 수직정도로 이동이 이루어질때 자동으로 바꾸게 한다.
		Vector3 diff = (_lastRightPosition ? bottomLeftEndRectTransform.position : bottomRightEndRectTransform.position) - lineImageRectTransform.position;
		// 일반적 방향벡터와 달리 여기서 쓰는 lineImage는 y축(0, 1, 0)을 바라보는 벡터이기 때문에 LookRotation을 쓰고난 후 90를 돌려줘야 맞아떨어진다.
		lineImageRectTransform.rotation = Quaternion.LookRotation(diff) * Quaternion.Euler(90.0f, 0.0f, 0.0f);
		lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude);

		#region Change RightPosition
		if (_lastRightPosition != rightPosition)
		{
			if (_lastRightPosition)
			{
				if (contentGroupRectTransform.position.x - targetTransform.position.x < 0.0f)
					_lastRightPosition = rightPosition;
			}
			else
			{
				if (contentGroupRectTransform.position.x - targetTransform.position.x > 0.0f)
					_lastRightPosition = rightPosition;
			}
		}
		#endregion

		#region lineImage Alpha
		float lineAlphaDistance = Mathf.Abs(contentGroupRectTransform.position.x - targetTransform.position.x);
		if (lineAlphaDistance < _immediatelyAlphaDistance - 0.5f)
		{
			float alpha = Mathf.Lerp(0.0f, 1.0f, (lineAlphaDistance / (_immediatelyAlphaDistance - 0.5f)));
			lineImage.color = new Color(lineImage.color.r, lineImage.color.g, lineImage.color.b, alpha * alpha);
		}
		else
		{
			lineImage.color = new Color(lineImage.color.r, lineImage.color.g, lineImage.color.b, 1.0f);
		}
		#endregion
	}

	void SetTargetPosition(Vector3 desiredPosition)
	{
		Vector3 diff = contentGroupRectTransform.position - desiredPosition;
		if (diff.sqrMagnitude < 2.0f * 2.0f || useTweenOutBack == false)
		{
			bool useLerp = false;
			if (_isTweening == false) useLerp = true;
			if (_isTweening && _lastTweenDesiredPosition != desiredPosition) useLerp = true;
			if (useTweenOutBack == false) useLerp = true;
			if (useLerp)
			{
				contentGroupRectTransform.position = Vector3.Lerp(contentGroupRectTransform.position, desiredPosition, Time.deltaTime * 5.0f);
			}
		}
		else
		{
			if (_isTweening && _lastTweenDesiredPosition == desiredPosition)
				return;

			contentGroupRectTransform.DOKill();
			contentGroupRectTransform.DOMove(desiredPosition, 1.2f).SetEase(Ease.OutBack, 2.0f, 0.0f).OnComplete(OnEaseComplete);
			_lastTweenDesiredPosition = desiredPosition;
			_isTweening = true;
		}
	}

	Vector3 _lastTweenDesiredPosition = -Vector3.up;
	bool _isTweening = false;
	void OnEaseComplete()
	{
		_isTweening = false;
	}

	void UpdateLeftRightSwapByAxisX()
	{
		if (rightPosition)
		{
			if (_targetPrevPosition.x > CustomFollowCamera.instance.cachedQuadRight - leftRightSwapThreshold)
				rightPosition = false;
		}
		else
		{
			if (_targetPrevPosition.x < CustomFollowCamera.instance.cachedQuadLeft + leftRightSwapThreshold)
				rightPosition = true;
		}
	}
}
