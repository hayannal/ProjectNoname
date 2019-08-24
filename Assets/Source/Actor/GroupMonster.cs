using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupMonster : MonoBehaviour
{
	List<MonsterActor> _listMonsterActor = new List<MonsterActor>();
	public List<MonsterActor> listMonsterActor { get { return _listMonsterActor; } }

	void Awake()
	{
		GetComponentsInChildren<MonsterActor>(_listMonsterActor);
	}

	bool _started = false;
	void Start()
	{
		InitializeGroup();
		_started = true;
	}

	#region ObjectPool
	void OnEnable()
	{
		if (_started)
			ReinitializeGroup();
	}
	#endregion

	void Update()
	{
		if (_reservedDisable)
		{
			gameObject.SetActive(false);
			_reservedDisable = false;
		}
	}

	List<Vector3> _listPosition = new List<Vector3>();
	List<Quaternion> _listRotation = new List<Quaternion>();
	void InitializeGroup()
	{
		for (int i = 0; i < _listMonsterActor.Count; ++i)
		{
			_listPosition.Add(_listMonsterActor[i].cachedTransform.localPosition);
			_listRotation.Add(_listMonsterActor[i].cachedTransform.localRotation);
		}
	}

	#region ObjectPool
	void ReinitializeGroup()
	{
		for (int i = 0; i < _listMonsterActor.Count; ++i)
		{
			_listMonsterActor[i].cachedTransform.localPosition = _listPosition[i];
			_listMonsterActor[i].cachedTransform.localRotation = _listRotation[i];
			_listMonsterActor[i].gameObject.SetActive(true);
		}
	}

	bool _reservedDisable = false;
	public void CheckAllDisable()
	{
		bool allDiable = true;
		for (int i = 0; i < _listMonsterActor.Count; ++i)
		{
			if (_listMonsterActor[i].gameObject.activeSelf)
			{
				allDiable = false;
				break;
			}
		}
		if (allDiable)
			_reservedDisable = true;
	}
	#endregion

	public bool IsLastAliveMonster(MonsterActor monsterActor)
	{
		bool allDie = true;
		for (int i = 0; i < _listMonsterActor.Count; ++i)
		{
			if (_listMonsterActor[i] == monsterActor)
				continue;

			// 보스와 함께 그룹이 묶였을때를 대비해서
			// 같은 그룹내 보스는 live체크를 하지 않는다.
			// 검사하는 대상이 보스인지는 검사하지 않아도 되는게
			// 보스일 경우는 별도로 보스들만 모아서 처리하기 때문에 여기서 할필요가 없다.
			if (monsterActor.bossMonster == false && _listMonsterActor[i].bossMonster)
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
