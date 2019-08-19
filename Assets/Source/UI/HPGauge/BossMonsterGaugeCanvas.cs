using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossMonsterGaugeCanvas : MonoBehaviour
{
	public static BossMonsterGaugeCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.bossMonsterHPGaugePrefab).GetComponent<BossMonsterGaugeCanvas>();
			}
			return _instance;
		}
	}
	static BossMonsterGaugeCanvas _instance = null;

	public Slider hpRatio1Slider;
	public RectTransform hpFill1RectTransform;
	public RectTransform lateFill1RectTransform;
	public Slider hpRatio2Slider;
	public RectTransform hpFill2RectTransform;
	public RectTransform lateFill2RectTransform;
	public Text questionText;

	void OnDisable()
	{
		_listMonsterActor.Clear();
		_dieCount = 0;
		_lateFillDelayRemainTime = 0.0f;
		_lateFillLerpStarted = false;
	}

	List<MonsterActor> _listMonsterActor = new List<MonsterActor>();
	public void InitializeGauge(MonsterActor monsterActor)
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		_listMonsterActor.Add(monsterActor);

		if (StageManager.instance.currentBossHpPer1Line == 0.0f)
		{
			Debug.LogError("Invalid Data! BossHpPer1Line is 0.");
			return;
		}

		float sumHp = 0.0f;
		for (int i = 0; i < _listMonsterActor.Count; ++i)
			sumHp += _listMonsterActor[i].actorStatus.GetHP();
		float hpLineRatio = sumHp / StageManager.instance.currentBossHpPer1Line;
		_lastHpLineRatio = hpLineRatio;
		RefreshBossHpGauge(hpLineRatio, true);
	}

	void RefreshBossHpGauge(float hpLineRatio, bool immediatelyUpdateLateFill)
	{
		if (hpLineRatio > 2.0f)
		{
			hpRatio1Slider.gameObject.SetActive(false);
			hpRatio2Slider.gameObject.SetActive(false);
			questionText.gameObject.SetActive(true);
		}
		else
		{
			hpRatio1Slider.gameObject.SetActive(true);
			hpRatio2Slider.gameObject.SetActive(true);
			questionText.gameObject.SetActive(false);

			if (hpLineRatio > 1.0f)
			{
				hpRatio1Slider.value = 1.0f;
				float line2Ratio = hpLineRatio - 1.0f;
				hpRatio2Slider.value = line2Ratio;
			}
			else
			{
				hpRatio1Slider.value = hpLineRatio;
				float line2Ratio = 0.0f;
				hpRatio2Slider.value = line2Ratio;
			}

			lateFill1RectTransform.anchorMin = new Vector2(hpFill1RectTransform.anchorMax.x, 0.0f);
			lateFill2RectTransform.anchorMin = new Vector2(hpFill2RectTransform.anchorMax.x, 0.0f);
			if (immediatelyUpdateLateFill)
			{
				lateFill1RectTransform.anchorMax = hpFill1RectTransform.anchorMax;
				lateFill2RectTransform.anchorMax = hpFill2RectTransform.anchorMax;
			}
		}
	}



	// Update is called once per frame
	void Update()
	{
		UpdateLateFill();
	}

	float _lastHpLineRatio = 1.0f;
	public void OnChangedHP(MonsterActor monsterActor)
	{
		if (_listMonsterActor.Contains(monsterActor) == false)
			return;

		float sumHp = 0.0f;
		for (int i = 0; i < _listMonsterActor.Count; ++i)
			sumHp += _listMonsterActor[i].actorStatus.GetHP();
		float hpLineRatio = sumHp / StageManager.instance.currentBossHpPer1Line;
		if (_lastHpLineRatio < hpLineRatio)
		{
			RefreshBossHpGauge(hpLineRatio, true);
		}
		else
		{
			bool prevQuestionState = questionText.gameObject.activeSelf;
			RefreshBossHpGauge(hpLineRatio, false);
			if (prevQuestionState && questionText.gameObject.activeSelf == false)
			{
				// 물음표 상태였다가 진입할땐 맥스치로 고정시켜준다.
				lateFill1RectTransform.anchorMax = new Vector2(1.0f, lateFill1RectTransform.anchorMax.y);
				lateFill2RectTransform.anchorMax = new Vector2(1.0f, lateFill1RectTransform.anchorMax.y);
			}

			if (hpRatio1Slider.gameObject.activeSelf && _lateFillLerpStarted == false && _lateFillDelayRemainTime == 0.0f)
			{
				_lateFillDelayRemainTime = LateFillDelay;
				_lastLateHpLineRatio = _lastHpLineRatio;
			}
		}

		_lastHpLineRatio = hpLineRatio;
	}

	// 보스는 두줄을 한번에 처리해야 자연스럽게 된다.
	float _lastLateHpLineRatio;

	const float LateFillDelay = 0.9f;
	float _lateFillDelayRemainTime = 0.0f;
	bool _lateFillLerpStarted = false;
	void UpdateLateFill()
	{
		if (_lateFillDelayRemainTime > 0.0f)
		{
			_lateFillDelayRemainTime -= Time.deltaTime;
			if (_lateFillDelayRemainTime <= 0.0f)
			{
				_lateFillDelayRemainTime = 0.0f;
				_lateFillLerpStarted = true;
			}
		}

		if (_lateFillLerpStarted == false)
			return;

		_lastLateHpLineRatio = Mathf.Lerp(_lastLateHpLineRatio, _lastHpLineRatio, Time.deltaTime * 4.0f);

		if (Mathf.Abs(_lastLateHpLineRatio - _lastHpLineRatio) < 0.005f * 2.0f)
		{
			_lastLateHpLineRatio = _lastHpLineRatio;
			_lateFillLerpStarted = false;
		}

		float value1 = 0.0f;
		float value2 = 0.0f;
		if (_lastLateHpLineRatio > 1.0f)
		{
			value1 = 1.0f;
			value2 = _lastLateHpLineRatio - 1.0f;

			// 3줄 넘게 있다가 한번에 2줄 이하로 진입할때를 대비해서 맥스 처리
			if (value2 > 1.0f) value2 = 1.0f;
		}
		else
		{
			value1 = _lastLateHpLineRatio;
			value2 = 0.0f;
		}
		lateFill1RectTransform.anchorMax = new Vector2(value1, lateFill1RectTransform.anchorMax.y);
		lateFill2RectTransform.anchorMax = new Vector2(value2, lateFill1RectTransform.anchorMax.y);
	}

	int _dieCount = 0;
	public void OnDie()
	{
		++_dieCount;
		if (_dieCount >= _listMonsterActor.Count)
			gameObject.SetActive(false);
	}
}
