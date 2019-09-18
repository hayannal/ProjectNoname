using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitObjectAnimator : MonoBehaviour
{
	MeHitObject _signal;
	Animator _animator;

	public void InitializeSignal(MeHitObject meHit, Actor parentActor, Animator animator)
	{
		_signal = meHit;
		_animator = animator;
	}
}
