using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AshParticle : MonoBehaviour
{
	public ParticleSystem flakeParticleSystem;
	public ParticleSystem emberParticleSystem;
	public ParticleSystem smokeParticleSystem;

	public void SetParticleInfo(SkinnedMeshRenderer skinnedMeshRenderer, float duration)
	{
		ParticleSystem.ShapeModule shape = flakeParticleSystem.shape;
		shape.skinnedMeshRenderer = skinnedMeshRenderer;
		shape = emberParticleSystem.shape;
		shape.skinnedMeshRenderer = skinnedMeshRenderer;
		shape = smokeParticleSystem.shape;
		shape.skinnedMeshRenderer = skinnedMeshRenderer;

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
