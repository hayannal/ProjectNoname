﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BossMonsterGaugeCanvas : MonoBehaviour
{
	static BossMonsterGaugeCanvas instance
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

	public static bool IsShow()
	{
		if (_instance != null && _instance.gameObject.activeSelf)
			return true;
		return false;
	}

	public static void InitializeGauge(MonsterActor monsterActor)
	{
		instance.InternalInitializeGauge(monsterActor);
	}

	public static void InitializeSequentialGauge(SequentialMonster sequentialMonster)
	{
		instance.InternalInitializeSequentialGauge(sequentialMonster);
	}

	public static void OnChangedHP(MonsterActor monsterActor)
	{
		if (IsShow() == false)
			return;
		_instance.InternalOnChangedHP(monsterActor);
	}

	public static void OnDie(MonsterActor monsterActor)
	{
		if (IsShow() == false)
			return;
		_instance.InternalOnDie(monsterActor);
	}

	public static bool IsLastAliveMonster(MonsterActor monsterActor)
	{
		if (IsShow() == false)
			return false;
		return _instance.InternalIsLastAliveMonster(monsterActor);
	}

	public Slider hpRatio1Slider;
	public RectTransform hpFill1RectTransform;
	public RectTransform lateFill1RectTransform;
	public Slider hpRatio2Slider;
	public RectTransform hpFill2RectTransform;
	public RectTransform lateFill2RectTransform;
	public Slider hpRatio3Slider;
	public RectTransform hpFill3RectTransform;
	public RectTransform lateFill3RectTransform;
	public Text questionText;
	public DOTweenAnimation shakeTween;
	public DOTweenAnimation textShakeTween;

	void OnDisable()
	{
		_listMonsterActor.Clear();
		_dieCount = 0;
		_lateFillDelayRemainTime = 0.0f;
		_lateFillLerpStarted = false;
		_initializedSequentialGauge = false;
	}

	List<MonsterActor> _listMonsterActor = new List<MonsterActor>();
	public void InternalInitializeGauge(MonsterActor monsterActor)
	{
		if (BattleInstanceManager.instance.bossGaugeSequentialMonster != null)
			return;

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

	bool _initializedSequentialGauge = false;
	public void InternalInitializeSequentialGauge(SequentialMonster sequentialMonster)
	{
		if (sequentialMonster != BattleInstanceManager.instance.bossGaugeSequentialMonster)
			return;
		if (_initializedSequentialGauge)
			return;

		if (StageManager.instance.currentBossHpPer1Line == 0.0f)
		{
			Debug.LogError("Invalid Data! BossHpPer1Line is 0.");
			return;
		}

		float sumHp = sequentialMonster.GetSumBossCurrentHp();
		float hpLineRatio = sumHp / StageManager.instance.currentBossHpPer1Line;
		_lastHpLineRatio = hpLineRatio;
		RefreshBossHpGauge(hpLineRatio, true);
		_initializedSequentialGauge = true;
		gameObject.SetActive(true);
	}

	void RefreshBossHpGauge(float hpLineRatio, bool immediatelyUpdateLateFill)
	{
		if (hpLineRatio > 3.0f)
		{
			hpRatio1Slider.gameObject.SetActive(false);
			hpRatio2Slider.gameObject.SetActive(false);
			hpRatio3Slider.gameObject.SetActive(false);
			questionText.gameObject.SetActive(true);
			textShakeTween.DORestart();
		}
		else
		{
			hpRatio1Slider.gameObject.SetActive(true);
			hpRatio2Slider.gameObject.SetActive(true);
			hpRatio3Slider.gameObject.SetActive(true);
			questionText.gameObject.SetActive(false);

			if (hpLineRatio > 2.0f)
			{
				hpRatio1Slider.value = 1.0f;
				hpRatio2Slider.value = 1.0f;
				float line3Ratio = hpLineRatio - 2.0f;
				hpRatio3Slider.value = line3Ratio;
			}
			else if (hpLineRatio > 1.0f)
			{
				hpRatio1Slider.value = 1.0f;
				float line2Ratio = hpLineRatio - 1.0f;
				hpRatio2Slider.value = line2Ratio;
				hpRatio3Slider.value = 0.0f;
			}
			else
			{
				hpRatio1Slider.value = hpLineRatio;
				float line2Ratio = 0.0f;
				hpRatio2Slider.value = line2Ratio;
				hpRatio3Slider.value = line2Ratio;
			}

			lateFill1RectTransform.anchorMin = new Vector2(hpFill1RectTransform.anchorMax.x, 0.0f);
			lateFill2RectTransform.anchorMin = new Vector2(hpFill2RectTransform.anchorMax.x, 0.0f);
			lateFill3RectTransform.anchorMin = new Vector2(hpFill3RectTransform.anchorMax.x, 0.0f);
			if (immediatelyUpdateLateFill)
			{
				lateFill1RectTransform.anchorMax = hpFill1RectTransform.anchorMax;
				lateFill2RectTransform.anchorMax = hpFill2RectTransform.anchorMax;
				lateFill3RectTransform.anchorMax = hpFill3RectTransform.anchorMax;
			}
			else
			{
				shakeTween.DORestart();
			}
		}
	}



	// Update is called once per frame
	void Update()
	{
		UpdateLateFill();
	}

	float _lastHpLineRatio = 1.0f;
	public void InternalOnChangedHP(MonsterActor monsterActor)
	{
		float sumHp = 0.0f;
		if (monsterActor.sequentialMonster != null)
		{
			if (monsterActor.sequentialMonster == BattleInstanceManager.instance.bossGaugeSequentialMonster)
				sumHp = monsterActor.sequentialMonster.GetSumBossCurrentHp();
		}
		else
		{
			if (_listMonsterActor.Contains(monsterActor) == false)
				return;

			for (int i = 0; i < _listMonsterActor.Count; ++i)
				sumHp += _listMonsterActor[i].actorStatus.GetHP();
		}

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
				lateFill3RectTransform.anchorMax = new Vector2(1.0f, lateFill1RectTransform.anchorMax.y);
			}

			if (hpRatio1Slider.gameObject.activeSelf && _lateFillLerpStarted == false && _lateFillDelayRemainTime == 0.0f)
			{
				_lateFillDelayRemainTime = LateFillDelay;
				_lastLateHpLineRatio = _lastHpLineRatio;
			}
		}

		_lastHpLineRatio = hpLineRatio;
	}

	// 보스는 세줄을 한번에 처리해야 자연스럽게 된다.
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
		float value3 = 0.0f;
		if (_lastLateHpLineRatio > 2.0f)
		{
			value1 = 1.0f;
			value2 = 1.0f;
			value3 = _lastLateHpLineRatio - 2.0f;

			// 4줄 넘게 있다가 한번에 3줄 이하로 진입할때를 대비해서 맥스 처리
			if (value3 > 1.0f) value3 = 1.0f;
		}
		else if (_lastLateHpLineRatio > 1.0f)
		{
			value1 = 1.0f;
			value2 = _lastLateHpLineRatio - 1.0f;
			value3 = 0.0f;
		}
		else
		{
			value1 = _lastLateHpLineRatio;
			value2 = 0.0f;
			value3 = 0.0f;
		}
		lateFill1RectTransform.anchorMax = new Vector2(value1, lateFill1RectTransform.anchorMax.y);
		lateFill2RectTransform.anchorMax = new Vector2(value2, lateFill1RectTransform.anchorMax.y);
		lateFill3RectTransform.anchorMax = new Vector2(value3, lateFill1RectTransform.anchorMax.y);
	}

	int _dieCount = 0;
	public void InternalOnDie(MonsterActor monsterActor)
	{
		bool allDie = false;
		if (monsterActor.sequentialMonster != null)
		{
			if (monsterActor.sequentialMonster == BattleInstanceManager.instance.bossGaugeSequentialMonster)
			{
				allDie = (monsterActor.sequentialMonster.IsLastAliveMonster(monsterActor) && monsterActor.actorStatus.IsDie());
				if (allDie)
					BattleInstanceManager.instance.bossGaugeSequentialMonster = null;
			}
		}
		else
		{
			++_dieCount;
			if (_dieCount >= _listMonsterActor.Count)
				allDie = true;
		}

		if (allDie)
			gameObject.SetActive(false);
	}

	public bool InternalIsLastAliveMonster(MonsterActor monsterActor)
	{
		if (monsterActor.sequentialMonster != null && monsterActor.sequentialMonster == BattleInstanceManager.instance.bossGaugeSequentialMonster)
			return monsterActor.sequentialMonster.IsLastAliveMonster(monsterActor);

		bool allDie = true;
		for (int i = 0; i < _listMonsterActor.Count; ++i)
		{
			if (_listMonsterActor[i] == monsterActor)
				continue;

			if (_listMonsterActor[i].actorStatus.IsDie() == false)
			{
				allDie = false;
				break;
			}
		}
		return allDie;
	}
}
