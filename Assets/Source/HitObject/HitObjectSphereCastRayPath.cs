using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.ThunderAndLightning;

public class HitObjectSphereCastRayPath : MonoBehaviour
{
	public LightningBoltPathScript lightningBoltPathScript;
	public Transform lightningBoltPathEndTransform;
	List<LineRenderer> _listLineRenderer = null;

	HitObject _hitObject;
	Actor _parentActor;
	int _fullPathHash;

	public void InitializeSignal(HitObject hitObject, Actor parentActor)
	{
		lightningBoltPathScript.ManualMode = false;

		if (_listLineRenderer == null)
		{
			_listLineRenderer = new List<LineRenderer>();
			GetComponentsInChildren<LineRenderer>(_listLineRenderer);
			for (int i = 0; i < _listLineRenderer.Count; ++i)
				_listLineRenderer[i].useWorldSpace = true;
		}

		_hitObject = hitObject;
		_parentActor = parentActor;
		if (parentActor.actionController.animator != null)
			_fullPathHash = parentActor.actionController.animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
	}

	public void SetEndPosition(Vector3 position)
	{
		lightningBoltPathEndTransform.position = position;
	}

	void Update()
	{
		if (_fadeOutStarted && _fadeOutRemainTime > 0.0f)
		{
			_fadeOutRemainTime -= Time.deltaTime;
			if (_fadeOutRemainTime <= 0.0f)
			{
				_fadeOutRemainTime = 0.0f;
				_fadeOutStarted = false;
				_hitObject.OnFinalizeByLifeTime();
			}
			return;
		}

		if (CheckChangeState())
			DisableRayPath();

		for (int i = 0; i < _listLineRenderer.Count; ++i)
		{
			_listLineRenderer[i].SetPosition(1, lightningBoltPathEndTransform.position);
		}
	}

	protected bool CheckChangeState()
	{
		if (_parentActor.actionController.animator.IsInTransition(0))
			return true;

		if (_parentActor.actionController.animator == null)
			return false;
		if (_parentActor.actionController.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == _fullPathHash)
			return false;

		return true;
	}

	bool _fadeOutStarted = false;
	float _fadeOutRemainTime;
	public void DisableRayPath()
	{
		if (lightningBoltPathScript != null && lightningBoltPathScript.FadePercent > 0.0f && lightningBoltPathScript.FadeOutMultiplier > 0.0f)
		{
			// 이 조건에서만 천천히 삭제되면 된다.
			lightningBoltPathScript.ManualMode = true;
			cachedTransform.parent = null;
			_fadeOutRemainTime = lightningBoltPathScript.FadePercent;
			_fadeOutStarted = true;
			return;
		}

		_hitObject.OnFinalizeByLifeTime();
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
