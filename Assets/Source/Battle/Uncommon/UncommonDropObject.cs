using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class UncommonDropObject : MonoBehaviour
{
	public enum eUncommonDropType
	{
		DroneCore,
		PowerLightning,
	}

	public eUncommonDropType uncommonDropType;
	public float getRange = 0.5f;
	public RectTransform nameCanvasRectTransform;


	Rigidbody _rigidbody;
	void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
	}

	bool _getable = false;
	void OnEnable()
	{
		_getable = false;
		nameCanvasRectTransform.gameObject.SetActive(false);
	}

	void Update()
	{
		if (_getable)
		{
			UpdateDistance();

			if (nameCanvasRectTransform.rotation != Quaternion.identity)
				nameCanvasRectTransform.rotation = Quaternion.identity;
		}
		else
		{
			if (_rigidbody.velocity.sqrMagnitude < 1.0f)
			{
				_getable = true;
				nameCanvasRectTransform.rotation = Quaternion.identity;
				nameCanvasRectTransform.gameObject.SetActive(true);
			}
		}
	}

	void UpdateDistance()
	{
		switch (uncommonDropType)
		{
			case eUncommonDropType.DroneCore:
				if (BattleInstanceManager.instance.playerActor.actorStatus.GetSPRatio() >= 1.0f)
					return;
				break;
			case eUncommonDropType.PowerLightning:
				break;
		}

		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		float playerRadius = BattleInstanceManager.instance.playerActor.actorRadius;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		if (diff.x * diff.x + diff.y * diff.y < (getRange + playerRadius) * (getRange + playerRadius))
		{
			switch (uncommonDropType)
			{
				case eUncommonDropType.DroneCore:
					BattleInstanceManager.instance.playerActor.actorStatus.AddSP(BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.MaxSp));
					break;
				case eUncommonDropType.PowerLightning:
					ChangeAttackStateAffector.CheckBulletBoost(BattleInstanceManager.instance.playerActor.affectorProcessor);
					break;
			}
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