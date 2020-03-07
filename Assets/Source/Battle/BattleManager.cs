using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
	public static BattleManager instance;

	public GameObject targetCircleObject;
	public GameObject monsterDieAshParticlePrefab;
	public AnimationCurveAsset monsterDieDissolveCurve;
	public AnimationCurveAsset bossMonsterDieDissolveCurve;
	public GameObject playerSpawnEffectPrefab;
	public GameObject rangeIndicatorPrefab;

	public GameObject monsterHPGaugeRootCanvasPrefab;
	public GameObject monsterHPGaugePrefab;
	public GameObject bossMonsterHPGaugePrefab;
	public GameObject playerHPGaugePrefab;
	public GameObject playerIgnoreEvadeCanvasPrefab;
	public GameObject skillSlotCanvasPrefab;
	public GameObject ultimateCirclePrefab;
	public GameObject battleToastCanvasPrefab;

	public GameObject playerIndicatorPrefab;
	public GameObject levelUpIndicatorPrefab;
	public GameObject playerLevelUpEffectPrefab;
	public GameObject levelPackGainEffectPrefab;
	public GameObject healEffectPrefab;

	public GameObject floatingDamageTextRootCanvasPrefab;
	public GameObject pauseCanvasPrefab;

	public GameObject[] dropObjectPrefabList;
	public GameObject portalPrefab;
	public GameObject portalMoveEffectPrefab;
	public GameObject portalGaugePrefab;

	// for level pack
	public GameObject diagonalNwayGeneratorPrefab;
	public GameObject leftRightNwayGeneratorPrefab;
	public GameObject backNwayGeneratorPrefab;

	void Awake()
	{
		instance = this;

		if (_currentBattleMode == null)
			Initialize(eBattleMode.DefaultMode);
	}

	public enum eBattleMode
	{
		DefaultMode,
	}

	BattleModeProcessorBase _currentBattleMode = null;

	public void Initialize(eBattleMode battleMode = eBattleMode.DefaultMode)
	{
		switch (battleMode)
		{
			case eBattleMode.DefaultMode:
				_currentBattleMode = new BattleModeProcessorBase();
				break;
		}
	}

	public void OnPreInstantiateMap()
	{
		_currentBattleMode.OnPreInstantiateMap();
	}

	public void OnLoadedMap()
	{
		_currentBattleMode.OnLoadedMap();
	}

	public void OnSpawnFlag()
	{
		_currentBattleMode.OnSpawnFlag();
	}

	public void OnSpawnMonster(MonsterActor monsterActor)
	{
		_currentBattleMode.OnSpawnMonster(monsterActor);
	}

	public void OnDiePlayer(PlayerActor playerActor)
	{
		_currentBattleMode.OnDiePlayer(playerActor);
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		_currentBattleMode.OnDieMonster(monsterActor);
	}

	public void OnClearStage()
	{
		_currentBattleMode.OnClearStage();
	}

	public bool IsAutoPlay()
	{
		return _currentBattleMode.IsAutoPlay();
	}

	public int GetSpawnedMonsterCount()
	{
		return _currentBattleMode.GetSpawnedMonsterCount();
	}

	public void AddDamageCountOnStage()
	{
		_currentBattleMode.AddDamageCountOnStage();
	}

	public int GetDamageCountOnStage()
	{
		return _currentBattleMode.GetDamageCountOnStage();
	}

	#region Common
	int _stackDropExp = 0;
	public void StackDropExp(int exp)
	{
		_stackDropExp += exp;
	}

	public void GetStackedDropExp()
	{
		// Stack된걸 적용하기 직전에 현재 맵의 보정치를 적용시킨다.
		_stackDropExp += StageManager.instance.addDropExp;
		_stackDropExp = (int)(_stackDropExp * StageManager.instance.currentStageTableData.DropExpAdjustment);

		//Debug.LogFormat("Drop Exp Add {0} / Get Exp : {1}", StageManager.instance.addDropExp, _stackDropExp);
		
		if (_stackDropExp < 0)
			Debug.LogError("Invalid Drop Exp : Negative Total Exp!");

		// 경험치 얻는 처리를 한다.
		// 이펙트가 먼저 나오고 곧바로 렙업창이 뜬다. 두번 이상 렙업 되는걸 처리하기 위해 업데이트 돌면서 스택에 쌓아둔채 꺼내쓰는 방법으로 해야할거다.
		StageManager.instance.AddExp(_stackDropExp);

		_stackDropExp = 0;
	}

	// 레벨팩이 드랍되면 체크해놨다가 먹어야 GatePillar가 나오게 해야한다.
	public int reservedLevelPackCount { get; set; }
	#endregion
}
