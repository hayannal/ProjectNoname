using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitObjectLineRenderer : MonoBehaviour
{
	MeHitObject _signal;
	List<LineRenderer> _listLineRenderer = new List<LineRenderer>();
	List<float> _listStartWidthMultiplier = new List<float>();
	List<float> _listFadeOutWidthMultiplier = new List<float>();

	public void InitializeSignal(MeHitObject meHit)
	{
		_signal = meHit;

		if (_listLineRenderer.Count == 0)
		{
			GetComponentsInChildren<LineRenderer>(_listLineRenderer);
			for (int i = 0; i < _listLineRenderer.Count; ++i)
			{
				_listStartWidthMultiplier.Add(_listLineRenderer[i].widthMultiplier);
				_listFadeOutWidthMultiplier.Add(_listLineRenderer[i].widthMultiplier);
			}
		}

		for (int i = 0; i < _listLineRenderer.Count; ++i)
		{
			_listLineRenderer[i].useWorldSpace = true;
			_listLineRenderer[i].widthMultiplier = _listFadeOutWidthMultiplier[i] = _listStartWidthMultiplier[i];
			_listLineRenderer[i].SetPosition(0, cachedTransform.position);
			_listLineRenderer[i].SetPosition(1, cachedTransform.position);
		}

		_disabled = false;
		_fadeOutStarted = false;
	}

	void Update()
	{
		for (int i = 0; i < _listLineRenderer.Count; ++i)
		{
			_listLineRenderer[i].SetPosition(1, cachedTransform.position);
		}

		if (_disabled)
			return;

		if (_fadeOutStarted)
		{
			for (int i = 0; i < _listFadeOutWidthMultiplier.Count; ++i)
			{
				_listFadeOutWidthMultiplier[i] = Mathf.Lerp(_listFadeOutWidthMultiplier[i], 0.0f, Time.deltaTime * 5.0f);
				if (_listFadeOutWidthMultiplier[i] < 0.05f)
					_listFadeOutWidthMultiplier[i] = 0.0f;
			}
			for (int i = 0; i < _listLineRenderer.Count; ++i)
				_listLineRenderer[i].widthMultiplier = _listFadeOutWidthMultiplier[i];
			bool allZero = true;
			for (int i = 0; i < _listFadeOutWidthMultiplier.Count; ++i)
			{
				if (_listFadeOutWidthMultiplier[i] != 0.0f)
				{
					allZero = false;
					break;
				}
			}
			if (allZero)
			{
				_fadeOutStarted = false;
				_disabled = true;
			}
			return;
		}
	}

	bool _disabled = false;
	bool _fadeOutStarted = false;
	public void DisableLineRenderer(bool immediate)
	{
		if (immediate)
		{
			for (int i = 0; i < _listLineRenderer.Count; ++i)
				_listLineRenderer[i].widthMultiplier = 0.0f;
			_disabled = true;
			return;
		}

		_fadeOutStarted = true;
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
