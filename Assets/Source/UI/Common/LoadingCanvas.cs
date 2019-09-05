using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingCanvas : MonoBehaviour
{
	public static LoadingCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(Resources.Load<GameObject>("UI/LoadingCanvas")).GetComponent<LoadingCanvas>();
			}
			return _instance;
		}
	}
	static LoadingCanvas _instance = null;

	float _enableTime;
	void OnEnable()
	{
		_enableTime = Time.realtimeSinceStartup;
	}

	void OnDisable()
	{
		float lifeTime = Time.realtimeSinceStartup - _enableTime;
		Debug.LogFormat("Loading Time : {0:0.###}", lifeTime);
	}
}
