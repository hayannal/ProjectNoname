using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ResearchObjects : MonoBehaviour
{
	public static ResearchObjects instance;

	public DOTweenAnimation objectTweenAnimation;
	public Transform effectRootTransform;

	void Awake()
	{
		instance = this;
	}
}