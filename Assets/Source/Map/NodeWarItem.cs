using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

	public GameObject mainObject;
	public Transform areaEffectTransform;

	void OnEnable()
	{
		if (mainObject != null)
			mainObject.SetActive(true);
		if (areaEffectTransform != null)
			areaEffectTransform.localScale = Vector3.one;
	}

	void OnDisable()
	{
		_waitEndAnimation = false;
	}

	void Update()
	{
		UpdateDistance();
	}

	const float ValidDistance = 30.0f;
	void UpdateDistance()
	{
		if (_waitEndAnimation)
			return;

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

	bool _waitEndAnimation;
	void GetDropObject()
	{
		switch (itemType)
		{
			case eItemType.Soul:
				BattleManager.instance.OnGetSoul(cachedTransform.position);
				_waitEndAnimation = true;
				break;
			case eItemType.HealOrb:
				BattleManager.instance.OnGetHealOrb(cachedTransform.position);
				_waitEndAnimation = true;
				break;
			case eItemType.SpecialPack:
				// 이렇게 먹자마자 사라지는 아이템도 있을 수 있다.
				//gameObject.SetActive(false);
				break;
		}

		if (_waitEndAnimation)
		{
			if (mainObject != null)
				mainObject.SetActive(false);
			if (areaEffectTransform != null)
				areaEffectTransform.DOScale(0.0f, 0.25f).SetEase(Ease.OutQuad).OnComplete(OnCompleteAnimation);
		}
	}

	void OnCompleteAnimation()
	{
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