using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Portal : MonoBehaviour
{
	public Collider portalTrigger;
	public float fillGaugeRadius = 2.0f;
	public float fillGaugeTime = 3.0f;
	public float portalOpenTime = 0.2f;
	public Renderer portalPlaneRenderer;
	public Color offColor;
	public Color onColor;

	public Vector3 targetPosition { get; set; }

	void OnEnable()
	{
		portalTrigger.enabled = false;
		_currentGaugeRatio = 0.0f;
		_currentColor = _targetColor = offColor;
		portalPlaneRenderer.material.SetColor(BattleInstanceManager.instance.GetShaderPropertyId("_TintColor"), offColor);
	}

	void OnDisable()
	{
		DisablePortalGauge();
	}

	PortalGauge _portalGauge;
	float _currentGaugeRatio = 0.0f;
	void UpdateGauge()
	{
		if (BattleInstanceManager.instance.playerActor == null)
			return;

		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - cachedTransform.position;
		diff.y = 0.0f;
		if (diff.sqrMagnitude > fillGaugeRadius * fillGaugeRadius)
		{
			if (_currentGaugeRatio == 0.0f)
				return;
			else if (_currentGaugeRatio < 1.0f)
			{
				_currentGaugeRatio = 0.0f;
				DisablePortalGauge();
				return;
			}
		}

		if (_currentGaugeRatio >= 1.0f)
			return;

		_currentGaugeRatio += Time.deltaTime / fillGaugeTime;

		if (_portalGauge == null)
		{
			_portalGauge = UIInstanceManager.instance.GetCachedPortalGauge(BattleManager.instance.portalGaugePrefab);
			_portalGauge.InitializeGauge(cachedTransform.position);
		}
		_portalGauge.OnChanged(_currentGaugeRatio);

		if (_currentGaugeRatio >= 1.0f)
		{
			_currentGaugeRatio = 1.0f;
			_targetColor = onColor;
			BattleInstanceManager.instance.OnOpenedPortal(this);
			DOTween.To(() => _currentColor, x => _currentColor = x, onColor, portalOpenTime).SetEase(Ease.OutQuad).OnComplete(() => portalTrigger.enabled = true);
			DisablePortalGauge();
		}
	}

	void DisablePortalGauge()
	{
		if (_portalGauge == null)
			return;

		_portalGauge.gameObject.SetActive(false);
		_portalGauge = null;
	}

	Color _currentColor;
	Color _targetColor;
	void UpdateColor()
	{
		if (_currentColor == _targetColor)
			return;

		portalPlaneRenderer.material.SetColor(BattleInstanceManager.instance.GetShaderPropertyId("_TintColor"), _currentColor);
	}

	void Update()
	{
		UpdateGauge();
		UpdateColor();
	}

	// 서있어도 이동해야하므로 Stay에서 처리. OnTriggerEnter 호출되는 프레임부터 같이 호출되기 때문에 Stay에서만 처리해도 괜찮다.
	static int s_ignoreEnterFrameCount = 0;
	void OnTriggerStay(Collider other)
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

		ClosePortal();
	}

	public void ClosePortal()
	{
		portalTrigger.enabled = false;
		_currentGaugeRatio = 0.0f;
		DisablePortalGauge();
		_targetColor = offColor;
		DOTween.To(() => _currentColor, x => _currentColor = x, offColor, portalOpenTime).SetEase(Ease.OutQuad);
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