using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class DamageCanvas : MonoBehaviour
{
	public static DamageCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.damageCanvasPrefab).GetComponent<DamageCanvas>();
			}
			return _instance;
		}
	}
	static DamageCanvas _instance = null;

	public Image damageImage;

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	TweenerCore<Color, Color, ColorOptions> _tweenReferenceForFade;
	public void ShowDamageScreen(float duration = 1.2f)
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		if (_tweenReferenceForFade != null)
			_tweenReferenceForFade.Kill();

		damageImage.color = new Color(damageImage.color.r, damageImage.color.g, damageImage.color.b, 0.15f);
		_tweenReferenceForFade = damageImage.DOFade(0.0f, duration);
		_duration = duration;
	}

	float _duration;
	void Update()
	{
		if (_duration > 0.0f)
		{
			_duration -= Time.deltaTime;
			if (_duration <= 0.0f)
			{
				_duration = 0.0f;
				_tweenReferenceForFade = null;
				gameObject.SetActive(false);
			}
		}
	}
}