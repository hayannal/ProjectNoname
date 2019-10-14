using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingDamageTextRootCanvas : MonoBehaviour
{
	public static FloatingDamageTextRootCanvas instance;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	const float ContinuousDelay = 0.5f;
	Dictionary<Actor, float> _dicLastTime = new Dictionary<Actor, float>();
	Dictionary<Actor, int> _dicLastIndex = new Dictionary<Actor, int>();
	public int GetPositionAnimationIndex(Actor actor)
	{
		if (_dicLastTime.ContainsKey(actor) == false)
		{
			_dicLastTime.Add(actor, Time.time);
			_dicLastIndex.Add(actor, 0);
			return 0;
		}

		float lastTime = _dicLastTime[actor];
		int lastIndex = _dicLastIndex[actor];
		if (Time.time > lastTime + ContinuousDelay)
			lastIndex = 0;
		else
			++lastIndex;
		_dicLastTime[actor] = Time.time;
		_dicLastIndex[actor] = lastIndex;
		return lastIndex;
	}



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}