using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SubjectNerd.Utilities;

public class RailMonster : MonoBehaviour
{
	[Reorderable] public Vector3[] railNodePositionList;
	public float speed;

	MonsterActor _monsterActor;
	public MonsterActor monsterActor { get { return _monsterActor; } }

	void Awake()
	{
		_monsterActor = GetComponentInChildren<MonsterActor>();
	}

	void OnDestroy()
	{
		OnFinalized(this);
	}

	bool _started = false;
	void Start()
	{
		InitializeRail();
		_started = true;
	}

	// 레일몹은 몹이 죽을때 풀에 들어가는게 아니라 맵이동할때 풀에 들어가서 다음판에 재활용되는 구조다.
	#region ObjectPool
	void OnEnable()
	{
		if (_started)
			ReinitializeRail();
	}
	#endregion

	Vector3 _startPosition = Vector3.zero;
	Quaternion _startRotation = Quaternion.identity;
	void InitializeRail()
	{
		if (railNodePositionList == null || railNodePositionList.Length == 0)
			return;
		_startPosition = cachedTransform.position + railNodePositionList[0];
		_startRotation = _monsterActor.cachedTransform.rotation;
		_monsterActor.GetRigidbody().isKinematic = true;

		ReinitializeRail();
	}

	#region ObjectPool
	void ReinitializeRail()
	{
		_monsterActor.cachedTransform.position = _startPosition;
		_monsterActor.cachedTransform.rotation = _startRotation;
		_monsterActor.gameObject.SetActive(true);
		_currentNodeIndex = 0;

		OnInitialized(this);
	}
	#endregion

	int _currentNodeIndex = 0;
	void Update()
	{
		if (railNodePositionList == null || railNodePositionList.Length <= 1)
			return;
		if (_monsterActor.actorStatus.IsDie())
			return;

		int nextIndex = _currentNodeIndex + 1;
		if (nextIndex == railNodePositionList.Length)
			nextIndex = 0;
		Vector3 diff = cachedTransform.position + railNodePositionList[nextIndex] - _monsterActor.cachedTransform.position;
		float deltaDistanceMax = Time.deltaTime * speed;
		if (diff.magnitude < deltaDistanceMax)
			_monsterActor.cachedTransform.Translate(diff, Space.World);
		else
			_monsterActor.cachedTransform.Translate(diff.normalized * Time.deltaTime * speed, Space.World);

		diff = cachedTransform.position + railNodePositionList[nextIndex] - _monsterActor.cachedTransform.position;
		if (diff.sqrMagnitude < 0.05f * 0.05f)
		{
			++_currentNodeIndex;
			if (_currentNodeIndex == railNodePositionList.Length)
				_currentNodeIndex = 0;
			_monsterActor.cachedTransform.position = cachedTransform.position + railNodePositionList[_currentNodeIndex];
		}

		// kinematic 켜놨더니 rigidbody의 rotation으로 제어가 안된다. 그래서 transform꺼로 처리한다.
		if (_monsterActor.monsterAI.targetActor != null)
			_monsterActor.cachedTransform.rotation = Quaternion.LookRotation(_monsterActor.monsterAI.targetActor.cachedTransform.position - _monsterActor.cachedTransform.position);
	}

	void OnDrawGizmosSelected()
	{
		if (!enabled)
			return;
		if (railNodePositionList == null || railNodePositionList.Length == 0)
			return;

		Color defaultGizmoColor = Gizmos.color;
		Gizmos.color = Color.red;
		for (int i = 0; i < railNodePositionList.Length; ++i)
		{
			Gizmos.DrawSphere(cachedTransform.position + railNodePositionList[i], 0.25f);

			if (i == 0)
				continue;

			Gizmos.DrawLine(cachedTransform.position + railNodePositionList[i - 1], cachedTransform.position + railNodePositionList[i]);
		}
		Gizmos.color = defaultGizmoColor;
	}

	static List<RailMonster> s_listInitializedRail;
	static void OnInitialized(RailMonster railMonster)
	{
		if (s_listInitializedRail == null)
			s_listInitializedRail = new List<RailMonster>();

		if (s_listInitializedRail.Contains(railMonster) == false)
			s_listInitializedRail.Add(railMonster);
	}

	static void OnFinalized(RailMonster railMonster)
	{
		if (s_listInitializedRail == null)
			return;

		s_listInitializedRail.Remove(railMonster);
	}

	public static void OnPreInstantiateMap()
	{
		if (s_listInitializedRail == null)
			return;

		for (int i = 0; i < s_listInitializedRail.Count; ++i)
			s_listInitializedRail[i].gameObject.SetActive(false);
		s_listInitializedRail.Clear();
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