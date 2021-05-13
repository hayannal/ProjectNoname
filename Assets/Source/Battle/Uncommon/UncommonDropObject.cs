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
	bool _experienceMode = false;
	void OnEnable()
	{
		_getable = false;
		nameCanvasRectTransform.gameObject.SetActive(false);

		_experienceMode = (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf);
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
		PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
		if (_experienceMode && CharacterListCanvas.instance.selectedPlayerActor != null)
			playerActor = CharacterListCanvas.instance.selectedPlayerActor;

		switch (uncommonDropType)
		{
			case eUncommonDropType.DroneCore:
				if (playerActor.actorStatus.GetSPRatio() >= 1.0f)
					return;
				break;
			case eUncommonDropType.PowerLightning:
				break;
		}

		Vector3 playerPosition = playerActor.cachedTransform.position;
		float playerRadius = playerActor.actorRadius;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		if (diff.x * diff.x + diff.y * diff.y < (getRange + playerRadius) * (getRange + playerRadius))
		{
			switch (uncommonDropType)
			{
				case eUncommonDropType.DroneCore:
					playerActor.actorStatus.AddSP(playerActor.actorStatus.GetValue(eActorStatus.MaxSp));
					break;
				case eUncommonDropType.PowerLightning:
					ChangeAttackStateAffector.CheckBulletBoost(playerActor.affectorProcessor);
					break;
			}
			SoundManager.instance.PlaySFX("DropObject");
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