using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarTrap : MonoBehaviour
{
	public float multiAtk;
	public float hitStayInterval;

	// 어차피 일반 Trap과 같이 쓰이지 않으니 99 그대로 써도 상관없다.
	public int hitStayIdForIgnoreDuplicate = 99;
	//public string[] affectorValueIdList;
	const int _tempActorInstanceId = 99;

	// 일반 Trap과 달리 수명을 가지고 있다.
	public float lifeTime;
	// 자연스럽게 사라지게 하기 위해 필요
	public EffectSettings effectSettings;
	public FadeInOutShaderColor fadeInOutShaderColor;
	// fadeInOutShaderColor에서 설정한 값에서 보정할 수치
	public float adjustStartDamageDelay = -1.0f;

	void Start()
	{
		Team.SetTeamLayer(gameObject, Team.eTeamLayer.TEAM1_HITOBJECT_LAYER);
	}

	void OnEnable()
	{
		OnInitialized(this);

		effectSettings.IsVisible = true;

		// 처음 켜지고나서 일정시간동안엔 발동되지 않는다.
		_startDelayRemainTime = fadeInOutShaderColor.StartDelay + fadeInOutShaderColor.FadeInSpeed + adjustStartDamageDelay;
		_totalLifeTime = _startDelayRemainTime + lifeTime;

	}

	void OnDisable()
	{
		OnFinalized(this);
	}

	float _startDelayRemainTime;
	float _totalLifeTime;
	float _fadeOutRemainTime;
	void Update()
	{
		if (_startDelayRemainTime > 0.0f)
		{
			_startDelayRemainTime -= Time.deltaTime;
			if (_startDelayRemainTime <= 0.0f)
				_startDelayRemainTime = 0.0f;
		}

		if (_totalLifeTime > 0.0f)
		{
			_totalLifeTime -= Time.deltaTime;
			if (_totalLifeTime <= 0.0f)
			{
				_totalLifeTime = 0.0f;
				effectSettings.IsVisible = false;
				_fadeOutRemainTime = fadeInOutShaderColor.FadeOutSpeed;
			}
		}

		if (_fadeOutRemainTime > 0.0f)
		{
			_fadeOutRemainTime -= Time.deltaTime;
			if (_fadeOutRemainTime <= 0.0f)
			{
				_fadeOutRemainTime = 0.0f;
				gameObject.SetActive(false);
			}
		}
	}

	Dictionary<AffectorProcessor, float> _dicHitStayTime = null;
	void OnTriggerStay(Collider other)
	{
		if (_startDelayRemainTime > 0.0f)
			return;
		if (effectSettings.IsVisible == false)
			return;

		if (other.isTrigger)
			return;

		Collider col = other;
		if (col == null)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;

		if (affectorProcessor.actor.IsPlayerActor() == false)
			return;
		PlayerActor playerActor = affectorProcessor.actor as PlayerActor;
		if (playerActor == null)
			return;

		if (_dicHitStayTime == null)
			_dicHitStayTime = new Dictionary<AffectorProcessor, float>();

		if (affectorProcessor.CheckHitStayInterval(hitStayIdForIgnoreDuplicate, hitStayInterval, _tempActorInstanceId))
		{
			eAffectorType affectorType = eAffectorType.CollisionDamage;
			AffectorValueLevelTableData collisionDamageAffectorValue = new AffectorValueLevelTableData();
			collisionDamageAffectorValue.fValue1 = multiAtk;
			collisionDamageAffectorValue.iValue1 = 1;
			affectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, collisionDamageAffectorValue, null, false);

			//if (meHit.showHitEffect)
			//	HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, hitParameter.statusStructForHitObject.weaponIDAtCreation);
		}
	}


	static List<NodeWarTrap> s_listInitializedNodeWarTrap;
	static void OnInitialized(NodeWarTrap nodeWarTrap)
	{
		if (s_listInitializedNodeWarTrap == null)
			s_listInitializedNodeWarTrap = new List<NodeWarTrap>();

		if (s_listInitializedNodeWarTrap.Contains(nodeWarTrap) == false)
			s_listInitializedNodeWarTrap.Add(nodeWarTrap);
	}

	static void OnFinalized(NodeWarTrap nodeWarTrap)
	{
		if (s_listInitializedNodeWarTrap == null)
			return;

		s_listInitializedNodeWarTrap.Remove(nodeWarTrap);
	}

	public static bool IsExistInRange(Vector3 desirePosition, float range)
	{
		if (BattleInstanceManager.instance.playerActor == null)
			return false;
		if (s_listInitializedNodeWarTrap == null)
			return false;

		for (int i = 0; i < s_listInitializedNodeWarTrap.Count; ++i)
		{
			Vector3 diff = s_listInitializedNodeWarTrap[i].cachedTransform.position - desirePosition;
			if (diff.sqrMagnitude < range * range)
				return true;
		}
		return false;
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