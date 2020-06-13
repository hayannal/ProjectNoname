using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarItem : MonoBehaviour
{
	public enum eItemType
	{
		Soul,
		HealOrb,
		SpecialPack,
	}

	public eItemType itemType;
	public float getRange = 0.2f;

	void Update()
	{
		UpdateDistance();
	}

	const float ValidDistance = 30.0f;
	void UpdateDistance()
	{
		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		float playerRadius = BattleInstanceManager.instance.playerActor.actorRadius;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		float sqrMagnitude = diff.x * diff.x + diff.y * diff.y;
		if (sqrMagnitude < (getRange + playerRadius) * (getRange + playerRadius))
			GetDropObject();
		else if (sqrMagnitude > NodeWarProcessor.ValidDistance * NodeWarProcessor.ValidDistance)
			gameObject.SetActive(false);
	}

	void GetDropObject()
	{
		switch (itemType)
		{
			case eItemType.Soul:
				BattleManager.instance.OnGetSoul(cachedTransform.position);
				break;
			case eItemType.HealOrb:
				BattleManager.instance.OnGetHealOrb(cachedTransform.position);
				break;
			case eItemType.SpecialPack:

				break;
		}
		gameObject.SetActive(false);
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