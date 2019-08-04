using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TweenAutoRestart : MonoBehaviour {

    DOTweenAnimation _tweenAnimation;

	// Use this for initialization
	void Start () {
        _tweenAnimation = GetComponent<DOTweenAnimation>();
	}
	
    void OnEnable()
    {
		if (_tweenAnimation == null)
			return;

		if (_tweenAnimation.autoPlay)
			_tweenAnimation.DORestart();
	}
}
