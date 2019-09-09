using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyGaugeCanvas : MonoBehaviour
{
	public static EnergyGaugeCanvas instance;

	public Slider energyRatioSlider;
	public Text energyText;
	public Text fillRemainTimeText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		cachedTransform.position = GatePillar.instance.cachedTransform.position;
		RefreshEnergy();
	}

	void Update()
	{
		UpdateFillRemainTime();
	}

	public void RefreshEnergy()
	{
		int current = 13;
		int max = 30;
		energyRatioSlider.value = (float)current / max;
		energyText.text = string.Format("{0}/{1}", current, max);
		if (current == max)
		{
			fillRemainTimeText.text = "";
			_needUpdate = false;
		}
		else
		{
			_nextFillDateTime = System.DateTime.Now + System.TimeSpan.FromSeconds(248);
			_needUpdate = true;
			_lastRemainTimeSecond = -1;
		}
	}

	bool _needUpdate = false;
	System.DateTime _nextFillDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateFillRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (System.DateTime.Now < _nextFillDateTime)
		{
			System.TimeSpan remainTime = _nextFillDateTime - System.DateTime.Now;
			if (_lastRemainTimeSecond != remainTime.Seconds)
				fillRemainTimeText.text = string.Format("{0}:{1:00}", remainTime.Minutes, remainTime.Seconds);
		}
		else
		{
			// 클라단에서 미리 올려놓고 서버에 보내봐야하나?
			// ++current;
			// 이쪽은 이제 플레이팹 연동할때 맞춰봐야할듯. 서버에서 어찌 보내주는지 보자.
			// 저거 할때 홈키 눌렀다가 오는것도 같이 봐야한다.
			RefreshEnergy();
		}
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
