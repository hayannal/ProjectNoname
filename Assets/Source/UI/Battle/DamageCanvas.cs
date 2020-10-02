using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

	public void ShowDamageScreen(float duration = 0.3f)
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		damageImage.color = new Color(damageImage.color.r, damageImage.color.g, damageImage.color.b, 0.3f);
		damageImage.DOFade(0.0f, duration);
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
				gameObject.SetActive(false);
			}
		}
	}
}