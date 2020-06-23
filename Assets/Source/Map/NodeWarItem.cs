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
		BoostOrb,
		SpecialPack,
	}

	public eItemType itemType;
	public float getRange = 0.2f;

	public GameObject mainObject;
	public Transform areaEffectTransform;
	public ParticleSystemRenderer particleSystemRenderer;
	public Transform autoGetEffectTranform;

	// 몬스터를 잡아서 얻어야하므로 DropObject에서 점프 코드만 가져와서 쓴다.
	[Space(10)]
	public bool useJumpAnimation;
	public Transform jumpTransform;
	public float jumpPower = 1.0f;
	public float jumpStartY = 0.5f;
	public float jumpEndY = 0.0f;
	public float jumpDuration = 1.0f;
	public float secondJumpPower = 0.5f;
	public float secondJumpDuration = 0.5f;

	// 아무래도 수집인데 pull 없이 자동획득되니 이상하다. pull 넣어본다.
	[Space(10)]
	public float pullStartDelay = 0.5f;
	public float pullStartSpeed = 3.0f;
	public float pullAcceleration = 2.0f;
	public Transform trailTransform;

	static int s_particleColorPropertyID;
	Color _particleDefaultColor;

	void OnEnable()
	{
		OnInitialized(this);

		if (mainObject != null)
			mainObject.SetActive(true);
		if (areaEffectTransform != null)
			areaEffectTransform.localScale = Vector3.one;
		if (particleSystemRenderer != null && _started)
			particleSystemRenderer.material.SetColor(s_particleColorPropertyID, _particleDefaultColor);
		if (trailTransform != null)
			trailTransform.gameObject.SetActive(false);

		InitializeJump();
	}

	void OnDisable()
	{
		_waitEndAnimation = false;

		OnFinalized(this);
	}

	bool _started = false;
	void Start()
	{
		if (s_particleColorPropertyID == 0) s_particleColorPropertyID = Shader.PropertyToID("_TintColor");
		if (particleSystemRenderer != null)
			_particleDefaultColor = particleSystemRenderer.material.GetColor(s_particleColorPropertyID);
		_started = true;
	}

	void Update()
	{
		UpdateJump();
		//UpdateAutoGet();
		UpdatePull();
		UpdateDistance();
		UpdateAlpha();
	}

	const float ValidDistance = 30.0f;
	void UpdateDistance()
	{
		if (_jumpRemainTime > 0)
			return;
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
		else if (sqrMagnitude > NodeWarProcessor.ItemValidDistance * NodeWarProcessor.ItemValidDistance)
			gameObject.SetActive(false);
	}

	const float AlphaTime = 0.25f;
	float _alphaRemainTime;
	void UpdateAlpha()
	{
		if (particleSystemRenderer == null)
			return;
		if (_waitEndAnimation == false)
			return;
		if (_alphaRemainTime > 0.0f)
		{
			_alphaRemainTime -= Time.deltaTime;
			if (_alphaRemainTime <= 0.0f)
				_alphaRemainTime = 0.0f;
			float ratio = _alphaRemainTime / AlphaTime;
			particleSystemRenderer.material.SetColor(s_particleColorPropertyID, new Color(_particleDefaultColor.r, _particleDefaultColor.g, _particleDefaultColor.b, _particleDefaultColor.a * ratio));
		}
	}

	void InitializeJump()
	{
		if (useJumpAnimation == false)
			return;

		jumpTransform.localPosition = new Vector3(0.0f, jumpStartY, 0.0f);
		jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), jumpPower, 1, jumpDuration).SetEase(Ease.Linear);
		_lastJump = (secondJumpPower == 0.0f || secondJumpDuration == 0.0f) ? true : false;
		_jumpRemainTime = jumpDuration;
	}

	float _jumpRemainTime = 0.0f;
	Vector3 _rotateEuler;
	bool _lastJump = false;
	void UpdateJump()
	{
		if (_jumpRemainTime <= 0.0f)
			return;

		_jumpRemainTime -= Time.deltaTime;

		if (_jumpRemainTime <= 0.0f)
		{
			if (_lastJump)
			{
				_jumpRemainTime = 0.0f;

				_pullStarted = true;
				_pullDelay = pullStartDelay;
				_pullSpeed = pullStartSpeed;
				if (trailTransform != null) trailTransform.gameObject.SetActive(true);
			}
			else
			{
				jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), secondJumpPower, 1, secondJumpDuration).SetEase(Ease.Linear);
				_jumpRemainTime = secondJumpDuration;
				_lastJump = true;
			}
		}
	}

	float _autoGetRemainTime = 0.0f;
	void UpdateAutoGet()
	{
		if (_autoGetRemainTime > 0.0f)
		{
			_autoGetRemainTime -= Time.deltaTime;
			if (_autoGetRemainTime <= 0.0f)
			{
				_autoGetRemainTime = 0.0f;
				BattleManager.instance.OnGetSoul(cachedTransform.position);
				gameObject.SetActive(false);
			}
		}
	}

	bool _pullStarted = false;
	float _pullSpeed = 0.0f;
	float _pullDelay = 0.0f;
	void UpdatePull()
	{
		if (_jumpRemainTime > 0.0f)
			return;
		if (_pullStarted == false)
			return;

		_pullSpeed += pullAcceleration * Time.deltaTime;

		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		if (diff.magnitude < _pullSpeed * Time.deltaTime)
			cachedTransform.Translate(new Vector3(diff.x, 0.0f, diff.y));
		else
			cachedTransform.Translate(new Vector3(diff.normalized.x, 0.0f, diff.normalized.y) * _pullSpeed * Time.deltaTime);
	}

	bool _waitEndAnimation;
	void GetDropObject()
	{
		switch (itemType)
		{
			case eItemType.Soul:
				BattleManager.instance.OnGetSoul(cachedTransform.position);
				gameObject.SetActive(false);
				//_waitEndAnimation = true;
				break;
			case eItemType.HealOrb:
				BattleManager.instance.OnGetHealOrb(cachedTransform.position);
				_waitEndAnimation = true;
				break;
			case eItemType.BoostOrb:
				BattleManager.instance.OnGetBoostOrb(cachedTransform.position);
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
				areaEffectTransform.DOScale(0.8f, AlphaTime).SetEase(Ease.OutQuad).OnComplete(OnCompleteAnimation);
			if (particleSystemRenderer != null)
				_alphaRemainTime = AlphaTime;
		}
	}

	void OnCompleteAnimation()
	{
		gameObject.SetActive(false);
	}



	static List<NodeWarItem> s_listInitializedNodeWarItem;
	static void OnInitialized(NodeWarItem nodeWarItem)
	{
		if (s_listInitializedNodeWarItem == null)
			s_listInitializedNodeWarItem = new List<NodeWarItem>();

		if (s_listInitializedNodeWarItem.Contains(nodeWarItem) == false)
			s_listInitializedNodeWarItem.Add(nodeWarItem);
	}

	static void OnFinalized(NodeWarItem nodeWarItem)
	{
		if (s_listInitializedNodeWarItem == null)
			return;

		s_listInitializedNodeWarItem.Remove(nodeWarItem);
	}

	public static void DisableAllItem()
	{
		if (s_listInitializedNodeWarItem == null)
			return;

		for (int i = s_listInitializedNodeWarItem.Count - 1; i >= 0; --i)
			s_listInitializedNodeWarItem[i].gameObject.SetActive(false);
		s_listInitializedNodeWarItem.Clear();
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