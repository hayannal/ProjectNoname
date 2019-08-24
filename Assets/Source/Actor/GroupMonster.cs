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
}
