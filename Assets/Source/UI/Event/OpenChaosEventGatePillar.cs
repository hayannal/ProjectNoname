using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class OpenChaosEventGatePillar : MonoBehaviour
{
	public static OpenChaosEventGatePillar instance;

	public DOTweenAnimation shakeTweenAnimation;
	public GameObject baseParticleRootObject;
	public GameObject touchParticleRootObject;

	void Awake()
	{
		instance = this;
	}

	public void OnTouch()
	{
		baseParticleRootObject.SetActive(false);
		touchParticleRootObject.SetActive(true);
		shakeTweenAnimation.DOPlay();
	}

	public void OnCompleteAllAnimation()
	{
		EventManager.instance.OnCompleteAnimation();
	}
}