using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.ThunderAndLightning;

public class HitObjectSphereCastRayPath : MonoBehaviour
{
	public LightningBoltPathScript lightningBoltPathScript;
	public Transform lightningBoltPathEndTransform;
	List<LineRenderer> _listLineRenderer = new List<LineRenderer>();

	HitObject _hitObject;
	Actor _parentActor;
	int _fullPathHash;

	public void InitializeSignal(HitObject hitObject, Actor parentActor)
	{
		if (_listLineRenderer.Count == 0)
		{
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
		if (CheckChangeState())
			_hitObject.OnFinalizeByLifeTime();

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
