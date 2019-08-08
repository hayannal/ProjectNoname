using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectIndicatorCanvas : MonoBehaviour
{
	public Transform targetTransform;
	public RectTransform textGroupRectTransform;
	public RectTransform textBackImageRectTransform;
	public RectTransform textBottomLineRectTransform;
	public RectTransform textBottomLeftEndRectTransform;
	public RectTransform textBottomRightEndRectTransform;
	public RectTransform lineImageRectTransform;
	public Image lineImage;
	public Text contextText;
	public float offsetY = 1.0f;
	public float offsetX = 0.75f;
	public bool rightPosition = true;

	// Start is called before the first frame update
	void Start()
    {
		GetComponent<Canvas>().worldCamera = BattleInstanceManager.instance.GetCachedCameraMain();

		if (targetTransform != null)
			InitializeTarget(targetTransform);
    }

	void InitializeTarget(Transform t)
	{
		_prevTargetTransform = targetTransform = t;
		GetTargetHeightAndRadius(targetTransform);
		textBottomLineRectTransform.pivot = new Vector2(rightPosition ? 0.0f : 1.0f, textBottomLineRectTransform.pivot.y);
		_immediatelyUpdate = true;
		_lastRightPosition = rightPosition;
	}

	void OnEnable()
	{
		if (targetTransform != null)
			InitializeTarget(targetTransform);
	}

	// Update is called once per frame
	Transform _prevTargetTransform;
    void Update()
    {
        if (_prevTargetTransform != targetTransform && targetTransform != null)
		{
			InitializeTarget(targetTransform);
		}

		UpdateTargetPosition();
		UpdateTextImagePosition();
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
			_immediatelyUpdate = true;
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
		desiredPosition.x += _lastRightPosition ? _targetRadius : -_targetRadius;
		lineImageRectTransform.position = desiredPosition;

		// 선은 rightPosition으로 바로 체크하지 않고
		// 마지막으로 설정된걸 쓰다가 수직정도로 이동이 이루어질때 자동으로 바꾸게 한다.
		Vector2 diff = (_lastRightPosition ? textBottomLeftEndRectTransform.position : textBottomRightEndRectTransform.position) - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude);

		desiredPosition.y += offsetY;
		float deltaX = offsetX + textBackImageRectTransform.sizeDelta.x * 0.5f;
		desiredPosition.x += rightPosition ? deltaX : -deltaX;

		if (_immediatelyUpdate)
		{
			textGroupRectTransform.position = desiredPosition;
			_immediatelyUpdate = false;
			_immediatelyAlphaDistance = Mathf.Abs(textGroupRectTransform.position.x - targetTransform.position.x);
		}
		else
		{
			textGroupRectTransform.position = Vector3.Lerp(textGroupRectTransform.position, desiredPosition, Time.deltaTime * 5.0f);
		}

		#region Change RightPosition
		if (_lastRightPosition != rightPosition)
		{
			if (_lastRightPosition)
			{
				if (textGroupRectTransform.position.x - targetTransform.position.x < 0.0f)
					_lastRightPosition = rightPosition;
			}
			else
			{
				if (textGroupRectTransform.position.x - targetTransform.position.x > 0.0f)
					_lastRightPosition = rightPosition;
			}
		}
		#endregion

		#region lineImage Alpha
		float lineAlphaDistance = Mathf.Abs(textGroupRectTransform.position.x - targetTransform.position.x);
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
}
