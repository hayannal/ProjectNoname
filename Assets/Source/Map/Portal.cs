using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
	public Collider portalTrigger;
	public float portalOpenStartTime;
	public float portalOpenEndTime;
	public ParticleSystem _portalParticleSystem;

	public Vector3 targetPosition { get; set; }

	void OnEnable()
	{
		portalTrigger.enabled = false;
	}

	void OnDisable()
	{
		_portalEffectStarted = false;
	}

	bool _portalEffectStarted = false;
	float _portalEffectTime = 0.0f;
	public void StartPortalEffect()
	{
		_portalEffectStarted = true;
		_portalEffectTime = 0.0f;
		if (_portalParticleSystem != null)
			_portalParticleSystem.Play(true);
		_opened = false;
	}

	bool _opened = false;
	void Update()
	{
		if (_portalEffectStarted == false)
			return;

		_portalEffectTime += Time.deltaTime;
		if (_opened)
		{
			if (_portalEffectTime > portalOpenEndTime)
			{
				_opened = false;
				portalTrigger.enabled = false;
			}
		}
		else
		{
			if (_portalEffectTime >= portalOpenStartTime && _portalEffectTime <= portalOpenEndTime)
			{
				_opened = true;
				portalTrigger.enabled = true;
			}
		}
	}

	static int s_ignoreEnterFrameCount = 0;
	void OnTriggerEnter(Collider other)
	{
		if (s_ignoreEnterFrameCount != 0 && Time.frameCount < s_ignoreEnterFrameCount)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		//Debug.Log("Portal On");

		// 제대로 하려면 타겟 지점의 포탈이 켜있는지 체크하고
		// 해당 포탈이 켜있다면 이동하는 물체에 대해 Enter 무시를 1회 설정하고 이동시켜야하지만
		// 이러기엔 포탈이 있는지부터 검사해서 오픈되어있는지까지 체크해야한다.
		// 그래서 차라리 두어프레임만 무시하게 해놓고 이동 즉시 켜있어도 다시 되돌아오지 않게 처리하겠다.
		s_ignoreEnterFrameCount = Time.frameCount + 3;
		affectorProcessor.actor.cachedTransform.position = targetPosition;

		// Tail Animator for playerActor
		if (BattleInstanceManager.instance.playerActor == affectorProcessor.actor)
			TailAnimatorUpdater.UpdateAnimator(affectorProcessor.actor.cachedTransform, 5);

		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.portalMoveEffectPrefab, targetPosition, Quaternion.identity);
	}

	// Exit에선 딱히 안해도 Enter로만 처리 가능하다.
	//void OnTriggerExit(Collider other)
	//{
	//	// 트리거 위에 서있는채로 collider.enabled = false 해도 exit가 오지 않으니 별도로 처리해줘야한다.
	//	Debug.Log("Portal Exit");
	//}

	

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