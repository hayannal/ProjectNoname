using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect25Controller : MonoBehaviour
{
	public float totalLifeTime;

	public float mainParticleLifeTime;
	public ParticleSystemRenderer[] mainParticleSystemRendererList;

	public float trailLifeTime;
	public RFX4_ParticleTrail[] trailList;

	void OnEnable()
	{
		_totalRemainTime = totalLifeTime;

		_mainParticleRemainTime = mainParticleLifeTime;
		for (int i = 0; i < mainParticleSystemRendererList.Length; ++i)
			mainParticleSystemRendererList[i].enabled = true;

		_trailRemainTime = trailLifeTime;
		for (int i = 0; i < trailList.Length; ++i)
			trailList[i].stopUpdate = false;
	}

	// Update is called once per frame
	float _totalRemainTime;
	float _mainParticleRemainTime;
	float _trailRemainTime;
    void Update()
    {
		if (_totalRemainTime > 0.0f)
		{
			_totalRemainTime -= Time.deltaTime;
			if (_totalRemainTime <= 0.0f)
			{
				_totalRemainTime = 0.0f;
				gameObject.SetActive(false);
			}
		}

        if (_mainParticleRemainTime > 0.0f)
		{
			_mainParticleRemainTime -= Time.deltaTime;
			if (_mainParticleRemainTime <= 0.0f)
			{
				_mainParticleRemainTime = 0.0f;
				for (int i = 0; i < mainParticleSystemRendererList.Length; ++i)
					mainParticleSystemRendererList[i].enabled = false;
			}
		}

		if (_trailRemainTime > 0.0f)
		{
			_trailRemainTime -= Time.deltaTime;
			if (_trailRemainTime <= 0.0f)
			{
				_trailRemainTime = 0.0f;
				for (int i = 0; i < trailList.Length; ++i)
				{
					trailList[i].stopUpdate = true;
					if (trailList[i].transform.childCount > 0)
					{
						Transform collisionEffectTransform = trailList[i].transform.GetChild(0);
						DisableParticleEmission.DisableEmission(collisionEffectTransform);
					}
				}
			}
		}
    }
}