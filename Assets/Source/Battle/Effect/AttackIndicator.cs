using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AttackIndicator : MonoBehaviour
{
	public enum eIndicatorType
	{
		Prefab,
		Line,
	}

	public Transform lineRootTransform;

	static int s_tintColorPropertyID;

	MeAttackIndicator _meAttackIndicator;
	List<Material> _listCachedMaterial;
	List<Color> _listCachedColor;
	float _currentAlpha;
	float _targetAlpha;
	public void InitializeAttackIndicator(MeAttackIndicator meAttackIndicator)
	{
		if (s_tintColorPropertyID == 0) s_tintColorPropertyID = Shader.PropertyToID("_TintColor");

		_meAttackIndicator = meAttackIndicator;

		if (_listCachedMaterial == null)
		{
			_listCachedMaterial = new List<Material>();
			_listCachedColor = new List<Color>();
			Renderer[] renderers = GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderers.Length; ++i)
			{
				for (int j = 0; j < renderers[i].materials.Length; ++j)
				{
					if (renderers[i].materials[j].HasProperty(s_tintColorPropertyID))
					{
						_listCachedMaterial.Add(renderers[i].materials[j]);
						_listCachedColor.Add(renderers[i].materials[j].GetColor(s_tintColorPropertyID));
					}
				}
			}
		}

		if (_listCachedMaterial.Count == 0)
		{
			//Debug.Log("")
			return;
		}

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].SetColor(s_tintColorPropertyID, new Color(_listCachedColor[i].r, _listCachedColor[i].g, _listCachedColor[i].b, 0.0f));
		}

		switch (meAttackIndicator.indicatorType)
		{
			case eIndicatorType.Prefab:
				cachedTransform.localScale = new Vector3(meAttackIndicator.areaRadius, meAttackIndicator.areaRadius, meAttackIndicator.areaRadius);
				break;
			case eIndicatorType.Line:
				lineRootTransform.localScale = new Vector3(meAttackIndicator.lineWidth, lineRootTransform.localScale.y, lineRootTransform.localScale.z);
				break;
		}

		_currentAlpha = 0.0f;
		_targetAlpha = 1.0f;
		DOTween.To(() => _currentAlpha, x => _currentAlpha = x, _targetAlpha, 0.2f).SetEase(Ease.OutQuad);
	}

	public void FinalizeAttackIndicator()
	{
		_targetAlpha = 0.0f;
		DOTween.To(() => _currentAlpha, x => _currentAlpha = x, _targetAlpha, 0.2f).SetEase(Ease.OutQuad).OnComplete(() => gameObject.SetActive(false));
	}

	void Update()
	{
		UpdateAlpha();
		UpdateLine();
		UpdateLifeTime();
	}

	void UpdateAlpha()
	{
		if (_listCachedMaterial == null)
			return;

		if (_currentAlpha == _targetAlpha)
			return;

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].SetColor(s_tintColorPropertyID, new Color(_listCachedColor[i].r, _listCachedColor[i].g, _listCachedColor[i].b, _listCachedColor[i].a * _currentAlpha));
		}
	}

	RaycastHit[] _raycastHitList = null;
	void UpdateLine()
	{
		if (_meAttackIndicator == null)
			return;
		if (_meAttackIndicator.indicatorType != eIndicatorType.Line)
			return;

		if (_raycastHitList == null)
			_raycastHitList = new RaycastHit[100];

		int resultCount = Physics.SphereCastNonAlloc(cachedTransform.position, _meAttackIndicator.sphereCastRadius, cachedTransform.forward, _raycastHitList, _meAttackIndicator.lineMaxDistance, 1);
		float reservedNearestDistance = _meAttackIndicator.lineMaxDistance;
		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= _raycastHitList.Length)
				break;

			bool planeCollided = false;
			bool groundQuadCollided = false;
			bool wallCollided = false;
			Vector3 wallNormal = Vector3.forward;

			Collider col = _raycastHitList[i].collider;
			if (col.isTrigger)
				continue;

			if (BattleInstanceManager.instance.planeCollider != null && BattleInstanceManager.instance.planeCollider == col)
			{
				planeCollided = true;
				wallNormal = _raycastHitList[i].normal;
			}

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = _raycastHitList[i].normal;
			}

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null)
			{
				//if (Team.CheckTeamFilter(statusForHitObject.teamId, col, meHit.teamCheckType))
				//	monsterCollided = true;
			}
			else if (planeCollided == false && groundQuadCollided == false)
			{
				wallCollided = true;
				wallNormal = _raycastHitList[i].normal;
			}

			if (planeCollided)
			{
				if (reservedNearestDistance > _raycastHitList[i].distance)
					reservedNearestDistance = _raycastHitList[i].distance;
			}

			if (groundQuadCollided && _meAttackIndicator.quadThrough == false)
			{
				if (reservedNearestDistance > _raycastHitList[i].distance)
					reservedNearestDistance = _raycastHitList[i].distance;
			}

			if (wallCollided && _meAttackIndicator.wallThrough == false)
			{
				if (reservedNearestDistance > _raycastHitList[i].distance)
					reservedNearestDistance = _raycastHitList[i].distance;
			}
		}

		lineRootTransform.position = new Vector3(cachedTransform.position.x, 0.05f, cachedTransform.position.z);
		lineRootTransform.localScale = new Vector3(lineRootTransform.localScale.x, lineRootTransform.localScale.y, reservedNearestDistance);
	}

	public void SetLifeTime(float lifeTime)
	{
		// Range시그널의 범위를 무시하고 lifeTime만큼 보여주고 싶을때 사용하는 방법이다.
		// 이때는 Indicator스스로가 라이프타임을 세고있다가 사라지게 한다.
		_remainLifeTime = lifeTime;
	}

	float _remainLifeTime = 0.0f;
	void UpdateLifeTime()
	{
		if (_remainLifeTime == 0.0f)
			return;

		_remainLifeTime -= Time.deltaTime;
		if (_remainLifeTime <= 0.0f)
		{
			_remainLifeTime = 0.0f;
			FinalizeAttackIndicator();
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