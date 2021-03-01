using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MEC;

public class RandomPositionGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("RandomPositionGenerator")]
	public float interval;

	public bool useRandomWorldAxisDirection;
	public bool useRandomWorldDiagonalDirection;
	public bool addOppositeSideDirection;

	int _remainCreateCount;
	float _remainIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = 0.0f;

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		if (CheckChangeState())
		{
			gameObject.SetActive(false);
			return;
		}
		//if (_parentActor.actorStatus.IsDie())
		//{
		//	gameObject.SetActive(false);
		//	return;
		//}
		//if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		//	return;

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f && _remainCreateCount > 0)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;

			// check Quad Position
			Vector3 position = GetRandomPosition();
			position.y = 1.0f;
			Quaternion rotation = cachedTransform.rotation;
			if (useRandomWorldAxisDirection)
			{
				int random = Random.Range(0, 4);
				Vector3 randomDirection = Vector3.forward;
				switch (random)
				{
					case 0: randomDirection = Vector3.forward; break;
					case 1: randomDirection = Vector3.back; break;
					case 2: randomDirection = Vector3.right; break;
					case 3: randomDirection = Vector3.left; break;
				}
				rotation = Quaternion.LookRotation(randomDirection);
			}
			if (useRandomWorldDiagonalDirection)
			{
				int random = Random.Range(0, 4);
				Vector3 randomDirection = Vector3.forward;
				switch (random)
				{
					case 0: randomDirection = Vector3.forward + Vector3.right; break;
					case 1: randomDirection = Vector3.back + Vector3.left; break;
					case 2: randomDirection = Vector3.right - Vector3.forward; break;
					case 3: randomDirection = Vector3.left - Vector3.back; break;
				}
				rotation = Quaternion.LookRotation(randomDirection);
			}
			Generate(position, rotation, true);

			if (addOppositeSideDirection)
			{
				rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f) * rotation;
				Generate(position, rotation, true);
			}
			
			if (_remainCreateCount <= 0)
				gameObject.SetActive(false);
		}
	}

	Vector3 GetRandomPosition()
	{
		Vector3 result = Vector3.zero;
		Vector3 desirePosition = cachedTransform.position;
		int tryBreakCount = 0;
		while (true)
		{
			// 이걸 쓰게될 보스는 스테이지 보스라서 currentGround가 있다고 가정하고 처리해둔다.
			if (BattleInstanceManager.instance.currentGround != null)
				desirePosition = BattleInstanceManager.instance.currentGround.GetRandomPositionInQuadBound(1.0f);

			// 하필 고른 위치가 플레이어 바로 옆이라면 패스해야하니 검사
			Vector3 diff = desirePosition - BattleInstanceManager.instance.playerActor.cachedTransform.position;
			if (diff.sqrMagnitude > 1.0f)
			{
				result = desirePosition;
				break;
			}
			
			// exception handling
			++tryBreakCount;
			if (tryBreakCount > 50)
			{
				Debug.LogError("RandomPositionGenerator RandomPosition Error. Not found valid random position.");
				return desirePosition;
			}
		}
		return result;
	}
}