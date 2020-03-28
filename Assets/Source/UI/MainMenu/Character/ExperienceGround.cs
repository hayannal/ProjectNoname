using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceGround : MonoBehaviour
{
	public static ExperienceGround instance;

	public Ground ground;
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

	Collider _prevPlaneCollider;
	Ground _prevGround;
	void OnEnable()
	{
		_spawnRemainTime = 0.0f;

		_prevPlaneCollider = BattleInstanceManager.instance.planeCollider;
		_prevGround = BattleInstanceManager.instance.currentGround;
		BattleInstanceManager.instance.planeCollider = CharacterInfoGround.instance.planeCollider;
		BattleInstanceManager.instance.currentGround = ground;
	}

	void OnDisable()
	{
		BattleInstanceManager.instance.planeCollider = _prevPlaneCollider;
		BattleInstanceManager.instance.currentGround = _prevGround;

		List<MonsterActor> listLiveMonster = BattleInstanceManager.instance.GetLiveMonsterList();
		if (listLiveMonster.Count > 0)
			Debug.LogError("Invalid Call. Monsters remain.");

		BattleInstanceManager.instance.FinalizeAllPositionBuffAffector(true);
		BattleInstanceManager.instance.FinalizeAllHitObject();
	}

	// Update is called once per frame
	void Update()
    {
		UpdateSpawnMonster();
	}

	float _spawnRemainTime;
	int _monsterCount;
	bool _createFrame = false;
	void UpdateSpawnMonster()
	{
		List<MonsterActor> listLiveMonster = BattleInstanceManager.instance.GetLiveMonsterList();
		if (listLiveMonster.Count > 0)
		{
			if (_createFrame)
			{
				for (int i = 0; i < listLiveMonster.Count; ++i)
					listLiveMonster[i].actorStatus.ChangeExperienceMode(CharacterListCanvas.instance.selectedPlayerActor);
				_createFrame = false;
			}
		}

		if (ExperienceCanvas.instance.backButton.interactable == false && _createFrame == false && listLiveMonster.Count == 0)
			ExperienceCanvas.instance.backButton.interactable = true;

		if (_spawnRemainTime > 0.0f)
		{
			_spawnRemainTime -= Time.deltaTime;
			if (_spawnRemainTime <= 0.0f)
				_spawnRemainTime = 0.0f;
			return;
		}

		if (listLiveMonster.Count > 0)
			return;

		if (CustomFollowCamera.instance.targetTransform.position.z > monsterSpawnLineZ)
			return;

		float randomX = Random.Range(-2.5f, 2.5f);
		float randomZ = Random.Range(77.0f, 80.0f);
		Vector3 randomPosition = new Vector3(randomX, 0.0f, randomZ);

		for (int i = 0; i < 3; ++i)
		{
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, randomPosition, Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 70.0f) - randomPosition));
			MonsterActor newMonsterActor = newObject.GetComponent<MonsterActor>();
			switch (i)
			{
				case 0: newMonsterActor.cachedTransform.Translate(0.0f, 0.0f, 0.5f, Space.Self); break;
				case 1: newMonsterActor.cachedTransform.Translate(-0.5f, 0.0f, -0.5f, Space.Self); break;
				case 2: newMonsterActor.cachedTransform.Translate(0.5f, 0.0f, -0.5f, Space.Self); break;
			}
		}

		// 생성하고나서 즉시 스탯을 수정할 수 없는게 Start 함수를 지나야 수정이 된다.
		// 그래서 차라리 _created 걸어두고 한프레임 뒤에 몬스터 리스트 받아와서 셋팅하는거로 한다.
		_createFrame = true;
		ExperienceCanvas.instance.backButton.interactable = false;

		_spawnRemainTime = 3.0f;
	}
}
