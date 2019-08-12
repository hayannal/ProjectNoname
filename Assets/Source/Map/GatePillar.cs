using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class GatePillar : MonoBehaviour
{
	public static GatePillar instance;

	public GameObject meshColliderObject;
	public GameObject particleRootObject;
	public GameObject changeEffectParticleRootObject;

	// 이건 나중에 동적 로드하는 구조로 바꿔야할듯 싶다.
	public GameObject descriptionObjectIndicatorPrefab;
	public float descriptionObjectIndicatorShowDelayTime = 5.0f;
	GameObject _indicatorObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (string.IsNullOrEmpty(StageManager.instance.currentGatePillarPreview))
			_descriptionObjectIndicatorShowRemainTime = descriptionObjectIndicatorShowDelayTime;
	}

	void OnDisable()
	{
		particleRootObject.SetActive(false);
		changeEffectParticleRootObject.SetActive(false);

		if (_indicatorObject != null)
		{
			_indicatorObject.SetActive(false);
			_indicatorObject = null;
		}
	}

	float _descriptionObjectIndicatorShowRemainTime;
	void Update()
	{
		if (_descriptionObjectIndicatorShowRemainTime > 0.0f)
		{
			_descriptionObjectIndicatorShowRemainTime -= Time.deltaTime;
			if (_descriptionObjectIndicatorShowRemainTime <= 0.0f)
			{
				_descriptionObjectIndicatorShowRemainTime = 0.0f;
				_indicatorObject = BattleInstanceManager.instance.GetCachedObject(descriptionObjectIndicatorPrefab, Vector3.zero, Quaternion.identity);
				_indicatorObject.GetComponent<ObjectIndicatorCanvas>().targetTransform = cachedTransform;
			}
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		if (_processing)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(col);
			if (hitObject == null)
				continue;

			Timing.RunCoroutine(NextMapProcess());
			break;
		}
	}

	bool _processing = false;
	IEnumerator<float> NextMapProcess()
	{
		if (_processing)
			yield break;

		_processing = true;

		yield return Timing.WaitForSeconds(0.2f);
		changeEffectParticleRootObject.SetActive(true);

		yield return Timing.WaitForSeconds(0.5f);

		FadeCanvas.instance.FadeOut(0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		StageManager.instance.NextStage();
		gameObject.SetActive(false);

		FadeCanvas.instance.FadeIn(0.4f);

		_processing = false;
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
