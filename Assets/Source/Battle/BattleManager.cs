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

	public GameObject monsterHPGaugeRootCanvasPrefab;
	public GameObject monsterHPGaugePrefab;
	public GameObject bossMonsterHPGaugePrefab;
	public GameObject playerHPGaugePrefab;
	public GameObject skillSlotCanvasPrefab;

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

	public void OnSpawnPlayer(PlayerActor playerActor)
	{
		_currentBattleMode.OnSpawnPlayer(playerActor);
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

	public int GetSpawnedMonsterCount()
	{
		return _currentBattleMode.GetSpawnedMonsterCount();
	}

	#region Common
	int _stackDropExp = 0;
	public void StackDropExp(int exp)
	{
		_stackDropExp += exp;
	}

	public void GetStackedDropExp()
	{
		Debug.LogFormat("Get Exp : {0}", _stackDropExp);

		// 경험치 얻는 처리를 한다.
		// 이펙트가 먼저 나오고 곧바로 렙업창이 뜬다. 두번 이상 렙업 되는걸 처리하기 위해 업데이트 돌면서 스택에 쌓아둔채 꺼내쓰는 방법으로 해야할거다.

		_stackDropExp = 0;
	}
	#endregion
}
