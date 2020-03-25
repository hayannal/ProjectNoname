using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceGround : MonoBehaviour
{
	public static ExperienceGround instance;

	public float monsterSpawnLineZ = 72.0f;
	public GameObject monsterPrefab;
	public Canvas worldCanvas;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
    {
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		_spawnRemainTime = 0.0f;
	}

	// Update is called once per frame
	void Update()
    {
		UpdateSpawnMonster();
	}

	float _spawnRemainTime;
	int _monsterCount;
	void UpdateSpawnMonster()
	{
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

		float randomX = Random.Range(-2.5f, 2.5f);
		float randomZ = Random.Range(77.0f, 80.0f);
		Vector3 randomPosition = new Vector3(randomX, 0.0f, randomZ);
		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, randomPosition, Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 70.0f) - randomPosition));

		_spawnRemainTime = 3.0f;
	}
}
