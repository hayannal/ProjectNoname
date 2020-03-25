using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInfoGround : MonoBehaviour
{
	public static CharacterInfoGround instance;

	public GameObject experienceRootObject;
	public GameObject monsterPrefab;
	public float monsterSpawnLineZ = 72.0f;

	void Awake()
	{
		instance = this;
	}

	bool _experienceMode;
	public void EnableExperienceMode(bool enable)
	{
		_experienceMode = enable;
		_spawnRemainTime = 0.0f;
		experienceRootObject.SetActive(enable);
	}

	float _spawnRemainTime;
	int _monsterCount;
	void Update()
	{
		if (_experienceMode == false)
			return;

		if (_spawnRemainTime > 0.0f)
		{
			_spawnRemainTime -= Time.deltaTime;
			if (_spawnRemainTime <= 0.0f)
				_spawnRemainTime = 0.0f;
			return;
		}

		List<MonsterActor> listLiveMonster = BattleInstanceManager.instance.GetLiveMonsterList();
		if (listLiveMonster.Count > 0)
			return;

		if (CustomFollowCamera.instance.targetTransform.position.z > monsterSpawnLineZ)
			return;

		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, new Vector3(0.0f, 0.0f, 78.0f), Quaternion.identity);

		_spawnRemainTime = 3.0f;
	}
}