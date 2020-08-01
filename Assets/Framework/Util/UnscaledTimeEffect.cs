using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UnscaledTimeEffect : MonoBehaviour
{
	#region staticFunction
	public static void Unscaled(Transform rootTransform)
	{
		UnscaledTimeEffect unscaledTimeEffect = rootTransform.GetComponent<UnscaledTimeEffect>();
		if (unscaledTimeEffect == null) unscaledTimeEffect = rootTransform.gameObject.AddComponent<UnscaledTimeEffect>();
		unscaledTimeEffect.Unscaled();
	}
	#endregion

	List<ParticleSystem> _listChangedParticleSystem;
	// DOTweenAnimation도 하는게 정석이긴 한데 생각보다 많이 쓰이지 않으니 굳이 안해도 되지 않을까
	//List<DOTweenAnimation> _listTweenAnimation;
	void OnEnable()
	{
		if (_listChangedParticleSystem == null)
		{ }
		else
		{
			Reset(false);
		}
	}

	public void Unscaled()
	{
		if (_listChangedParticleSystem == null)
		{
			ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
			_listChangedParticleSystem = new List<ParticleSystem>();
			for (int i = 0; i < particleSystems.Length; ++i)
			{
				// 애초에 꺼있는건 설정할 필요 없으니 패스
				if (particleSystems[i].emission.enabled == false)
					continue;

				_listChangedParticleSystem.Add(particleSystems[i]);
			}
		}

		Reset(true);
	}

	void Reset(bool enabled)
	{
		for (int i = 0; i < _listChangedParticleSystem.Count; ++i)
		{
			ParticleSystem.MainModule mainModule = _listChangedParticleSystem[i].main;
			mainModule.useUnscaledTime = enabled;
		}
	}
}