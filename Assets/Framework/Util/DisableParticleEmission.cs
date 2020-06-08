using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableParticleEmission : MonoBehaviour
{
	#region staticFunction
	public static void DisableEmission(Transform rootTransform, bool disableOnlyLoopParticle = false)
	{
		DisableParticleEmission disableParticleEmission = rootTransform.GetComponent<DisableParticleEmission>();
		if (disableParticleEmission == null) disableParticleEmission = rootTransform.gameObject.AddComponent<DisableParticleEmission>();
		disableParticleEmission.DisableEmission(disableOnlyLoopParticle);
	}
	#endregion

	bool _disableOnlyLoopParticle;
	List<ParticleSystem> _listChangedParticleSystem;
	List<ParticleSystem> _listChangedLoopParticleSystem;

	void OnEnable()
	{
		if (_listChangedParticleSystem == null || _listChangedLoopParticleSystem == null)
		{ }
		else
		{
			Reset(true);
			_checkAlive = false;
		}
	}

	public void DisableEmission(bool disableOnlyLoopParticle)
	{
		_disableOnlyLoopParticle = disableOnlyLoopParticle;			
		if (_listChangedParticleSystem == null || _listChangedLoopParticleSystem == null)
		{
			ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
			_listChangedParticleSystem = new List<ParticleSystem>();
			_listChangedLoopParticleSystem = new List<ParticleSystem>();
			for (int i = 0; i < particleSystems.Length; ++i)
			{
				// 애초에 꺼있는건 다시 켜지 않기 위해 리스트에 넣지 않는다.
				if (particleSystems[i].emission.enabled == false)
					continue;

				if (particleSystems[i].main.loop)
					_listChangedLoopParticleSystem.Add(particleSystems[i]);
				else
					_listChangedParticleSystem.Add(particleSystems[i]);
			}
		}

		Reset(false);
		_waitActive = true;
		_checkAlive = false;
	}

	void Reset(bool enabled)
	{
		for (int i = 0; i < _listChangedLoopParticleSystem.Count; ++i)
		{
			ParticleSystem.EmissionModule emissionModule = _listChangedLoopParticleSystem[i].emission;
			emissionModule.enabled = enabled;
		}

		if (_disableOnlyLoopParticle)
			return;

		for (int i = 0; i < _listChangedParticleSystem.Count; ++i)
		{
			ParticleSystem.EmissionModule emissionModule = _listChangedParticleSystem[i].emission;
			emissionModule.enabled = enabled;
		}
	}

	bool _waitActive = false;
	bool _checkAlive = false;
	void Update()
	{
		if (_listChangedLoopParticleSystem == null || _listChangedParticleSystem == null)
			return;

		bool alive = false;
		for (int i = 0; i < _listChangedLoopParticleSystem.Count; ++i)
		{
			if (_listChangedLoopParticleSystem[i].particleCount > 0)
			{
				alive = true;
				break;
			}
		}
		if (alive == false)
		{
			for (int i = 0; i < _listChangedParticleSystem.Count; ++i)
			{
				if (_listChangedParticleSystem[i].particleCount > 0)
				{
					alive = true;
					break;
				}
			}
		}
		if (alive)
		{
			if (_waitActive)
			{
				_waitActive = false;
				_checkAlive = true;
			}
			return;
		}
		if (_checkAlive == false)
			return;

		gameObject.SetActive(false);
		_checkAlive = false;
	}
}
