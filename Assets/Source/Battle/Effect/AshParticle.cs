using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AshParticle : MonoBehaviour
{
	static float DefaultRateOverTimeMultiplier = 200.0f;
	static float DefaultSmokeRateOverTimeMultiplier = 12.0f;

	public ParticleSystem flakeParticleSystem;
	public ParticleSystem emberParticleSystem;
	public ParticleSystem smokeParticleSystem;

	public void SetParticleInfo(SkinnedMeshRenderer skinnedMeshRenderer, float duration, float flakeMultiplier)
	{
		ParticleSystem.ShapeModule shape = flakeParticleSystem.shape;
		shape.skinnedMeshRenderer = skinnedMeshRenderer;
		shape = emberParticleSystem.shape;
		shape.skinnedMeshRenderer = skinnedMeshRenderer;
		shape = smokeParticleSystem.shape;
		shape.skinnedMeshRenderer = skinnedMeshRenderer;

		ParticleSystem.EmissionModule emission = flakeParticleSystem.emission;
		emission.rateOverTimeMultiplier = DefaultRateOverTimeMultiplier * flakeMultiplier;
		emission = emberParticleSystem.emission;
		emission.rateOverTimeMultiplier = DefaultRateOverTimeMultiplier * flakeMultiplier;
		emission = smokeParticleSystem.emission;
		emission.rateOverTimeMultiplier = DefaultSmokeRateOverTimeMultiplier * flakeMultiplier;

		ParticleSystem.MainModule main = flakeParticleSystem.main;
		main.duration = duration;
		main = emberParticleSystem.main;
		main.duration = duration;
		main = smokeParticleSystem.main;
		main.duration = duration;
	}

	public void Play()
	{
		flakeParticleSystem.Play();
	}
}
