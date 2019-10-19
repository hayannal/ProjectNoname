using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using MEC;

public class MeHitObject : MecanimEventBase {

	override public bool RangeSignal { get { return false; } }

	public HitObject.eTargetDetectType targetDetectType;
	public GameObject hitObjectPrefab;
	public float lifeTime;
	public bool movable;
	public float maxDistance;
	public float defaultSphereCastDistance;
	public float sphereCastRadius;
	public Team.eTeamCheckFilter teamCheckType;

	public HitObject.eCreatePositionType createPositionType;
	public string boneName;
	public Vector3 offset;
	public bool useBoneRotation;
	public float areaRotationY;
	public float areaDistanceMin;
	public float areaDistanceMax;
	public float areaHeightMin;
	public float areaHeightMax;
	public float areaAngle;

	public int repeatCount;
	public float repeatInterval;

	public HitObjectMovement.eMovementType movementType;
	public HitObjectMovement.eStartDirectionType startDirectionType;
	public Vector3 startDirection = Vector3.forward;
	public HitObjectMovement.eHowitzerType howitzerType;
	public bool useWorldSpaceDirection;
	public bool bothRandomAngle = true;
	public float leftRightRandomAngle;
	public float leftRandomAngle;
	public float rightRandomAngle;
	public float upDownRandomAngle;
	public float speed;
	public float curve;
	public float curveAdd;
	public bool curveLockY = true;
	public float accelTurn;
	public float gravity = -9.81f;

	public int parallelCount;
	public float parallelDistance;
	public bool ignoreMainHitObjectByParallel;

	#region CircularSector Preset
	public int circularSectorCount;
	public float circularSectorBetweenAngle;
	public bool circularSectorUseWorldSpace;
	public float circularSectorWorldSpaceCenterAngleY;
	public bool ignoreMainHitObjectByCircularSector;
	#endregion

	public List<ContinuousHitObjectGeneratorBase> continuousHitObjectGeneratorBaseList;

	public bool contactAll;
	public int monsterThroughCount;
	public bool wallThrough;
	public bool quadThrough;
	public int bounceWallQuadCount;
	public int ricochetCount;
	public bool ricochetOneHitPerTarget;
	public bool useHitStay;
	public float hitStayInterval;
	public bool hitStayIgnoreDuplicate;
	// HitStay의 중첩을 관리하기 위해 쓰는 아이디. 해당 액터안에서만 독립적이면 된다. 그러니 하나만 쓸거라면 0으로 둬도 무방하다.
	public int hitStayIdForIgnoreDuplicate;
	public bool hitStayLineRendererTrigger;
	public bool oneHitPerTarget = false;
	public bool useLineRenderer;
	

	// 부채꼴을 쓸때 저 위가 되냐 - 안쓸거다
	// 폭발형 문제가 편집창이 하나라 폭발시 어떻게 될지에 대해 적을 공간이 없다. - 고민중
	// 레이저형은 어떤식으로 셋팅할거지 - hitStay에서 파생 형태로 할듯

	public List<string> affectorValueIdList;
	public bool showHitEffect;
	public GameObject hitEffectObject;  //Google2u.HitEffectRow
	public bool hitEffectLookAtNormal;
	public bool useWeaponHitEffect;
	public string weaponDummyName;
	public HitEffect.eLineRendererType hitEffectLineRendererType;
	public GameObject hitEffectLineRendererObject;
	public bool showHitBlink;
	public bool showHitRimBlink;


	#if UNITY_EDITOR
	SerializedObject serializedObjectForAffector = null;
	ReorderableList reorderableListForAffector = null;
	SerializedObject serializedObjectForGenerator = null;
	ReorderableList reorderableListForGenerator = null;
	Vector2 _propertyScrollPosition;
	override public void OnGUI_PropertyWindow()
	{
		_propertyScrollPosition = EditorGUILayout.BeginScrollView(_propertyScrollPosition);
		targetDetectType = (HitObject.eTargetDetectType)EditorGUILayout.EnumPopup("Target Find Type :", targetDetectType);
		if (targetDetectType == HitObject.eTargetDetectType.Preset)
		{
			// Preset Count
		}
		else if (targetDetectType == HitObject.eTargetDetectType.SphereCast)
		{
			hitObjectPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", hitObjectPrefab, typeof(GameObject), false);
			defaultSphereCastDistance = EditorGUILayout.FloatField("Default Distance :", defaultSphereCastDistance);
			sphereCastRadius = EditorGUILayout.FloatField("Radius :", sphereCastRadius);
			if (RangeSignal == false)
			{
				lifeTime = 0.0f;
				EditorGUILayout.LabelField("[Only available LifeTime zero]", EditorStyles.label);
				lifeTime = EditorGUILayout.FloatField("LifeTime :", lifeTime);
			}
		}
		else
		{
			hitObjectPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", hitObjectPrefab, typeof(GameObject), false);
			lifeTime = EditorGUILayout.FloatField("LifeTime :", lifeTime);
			if (lifeTime > 0.0f) movable = EditorGUILayout.Toggle("Movable :", movable);
			else movable = false;
			if (movable)
				maxDistance = EditorGUILayout.FloatField("Max Distance :", maxDistance);
		}
		teamCheckType = (Team.eTeamCheckFilter)EditorGUILayout.EnumPopup("Team Check Type :", teamCheckType);

		EditorGUILayout.LabelField("-----------------------------------------------------------------");

		createPositionType = (HitObject.eCreatePositionType)EditorGUILayout.EnumPopup("Create Position :", createPositionType);
		if (createPositionType == HitObject.eCreatePositionType.Bone)
		{
			boneName = EditorGUILayout.TextField("Bone Name :", boneName);
			useBoneRotation = EditorGUILayout.Toggle("Apply Bone Rotation :", useBoneRotation);
		}
		offset = EditorGUILayout.Vector3Field("Offset :", offset);

		if (targetDetectType == HitObject.eTargetDetectType.Area)
		{
			areaRotationY = EditorGUILayout.FloatField("Area RotationY :", areaRotationY);
			areaDistanceMin = EditorGUILayout.FloatField("Area DistanceMin :", areaDistanceMin);
			areaDistanceMax = EditorGUILayout.FloatField("Area DistanceMax :", areaDistanceMax);
			areaHeightMin = EditorGUILayout.FloatField("Area HeightMin :", areaHeightMin);
			areaHeightMax = EditorGUILayout.FloatField("Area HeightMax :", areaHeightMax);
			areaAngle = EditorGUILayout.FloatField("Area Angle :", areaAngle);
		}

		EditorGUILayout.LabelField("-----------------------------------------------------------------");

		repeatCount = EditorGUILayout.IntField("Repeat Count :", repeatCount);
		if (repeatCount > 0)
			repeatInterval = EditorGUILayout.FloatField("Repeat Interval :", repeatInterval);

		EditorGUILayout.LabelField("-----------------------------------------------------------------");

		if (movable)
		{
			movementType = (HitObjectMovement.eMovementType)EditorGUILayout.EnumPopup("Movement Type :", movementType);
			switch (movementType)
			{
				case HitObjectMovement.eMovementType.FollowTarget:
					curve = EditorGUILayout.FloatField("Curve Power :", curve);
					curveAdd = EditorGUILayout.FloatField("Curve Power Add :", curveAdd);
					curveLockY = EditorGUILayout.Toggle("Curve Lock Y :", curveLockY);
					break;
				case HitObjectMovement.eMovementType.Turn:
					accelTurn = EditorGUILayout.FloatField("Turn Power :", accelTurn);
					break;
				case HitObjectMovement.eMovementType.Howitzer:
					howitzerType = (HitObjectMovement.eHowitzerType)EditorGUILayout.EnumPopup("Howitzer Type :", howitzerType);
					gravity = EditorGUILayout.FloatField("Gravity :", gravity);
					break;
			}
			startDirectionType = (HitObjectMovement.eStartDirectionType)EditorGUILayout.EnumPopup("Start Direction Type :", startDirectionType);
			if (startDirectionType == HitObjectMovement.eStartDirectionType.Direction)
			{
				useWorldSpaceDirection = EditorGUILayout.Toggle("Use World Space :", useWorldSpaceDirection);
				startDirection = EditorGUILayout.Vector3Field("Direction :", startDirection);
			}
			bothRandomAngle = EditorGUILayout.Toggle("Both Random Angle :", bothRandomAngle);
			if (bothRandomAngle)
				leftRightRandomAngle = EditorGUILayout.FloatField("LeftRight Random Angle :", leftRightRandomAngle);
			else
			{
				leftRandomAngle = EditorGUILayout.FloatField("Left Random Angle :", leftRandomAngle);
				rightRandomAngle = EditorGUILayout.FloatField("Right Random Angle :", rightRandomAngle);
			}
			upDownRandomAngle = EditorGUILayout.FloatField("UpDown Random Angle :", upDownRandomAngle);
			if (targetDetectType != HitObject.eTargetDetectType.SphereCast)
				speed = EditorGUILayout.FloatField("Speed :", speed);
			EditorGUILayout.LabelField("-----------------------------------------------------------------");
		}

		if (targetDetectType == HitObject.eTargetDetectType.Collider)
		{
			parallelCount = EditorGUILayout.IntField("Parallel Count", parallelCount);
			if (parallelCount > 0)
			{
				parallelDistance = EditorGUILayout.FloatField("Parallel Distance :", parallelDistance);
				ignoreMainHitObjectByParallel = EditorGUILayout.Toggle("Ignore Main HitObject :", ignoreMainHitObjectByParallel);
			}

			circularSectorCount = EditorGUILayout.IntField("Circular Sector Count", circularSectorCount);
			if (circularSectorCount > 0)
			{
				circularSectorBetweenAngle = EditorGUILayout.FloatField("Between Angle :", circularSectorBetweenAngle);
				circularSectorUseWorldSpace = EditorGUILayout.Toggle("Use World Space :", circularSectorUseWorldSpace);
				if (circularSectorUseWorldSpace)
					circularSectorWorldSpaceCenterAngleY = EditorGUILayout.FloatField("World Space Angle Y:", circularSectorWorldSpaceCenterAngleY);
				ignoreMainHitObjectByCircularSector = EditorGUILayout.Toggle("Ignore Main HitObject :", ignoreMainHitObjectByCircularSector);
			}

			if (reorderableListForGenerator == null)
			{
				serializedObjectForGenerator = new SerializedObject(this);
				reorderableListForGenerator = new ReorderableList(serializedObjectForGenerator, serializedObjectForGenerator.FindProperty("continuousHitObjectGeneratorBaseList"), true, true, true, true);
				reorderableListForGenerator.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Continuous Generator List");
				reorderableListForGenerator.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					var element = reorderableListForGenerator.serializedProperty.GetArrayElementAtIndex(index);
					rect.y += 2;
					rect.height = EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(rect, reorderableListForGenerator.serializedProperty.GetArrayElementAtIndex(index));
				};
			}
			if (reorderableListForGenerator != null)
			{
				serializedObjectForGenerator.Update();
				reorderableListForGenerator.DoLayoutList();
				serializedObjectForGenerator.ApplyModifiedProperties();
			}

			EditorGUILayout.LabelField("-----------------------------------------------------------------");

			contactAll = EditorGUILayout.Toggle("Contact All :", contactAll);
			monsterThroughCount = EditorGUILayout.IntField("Monster Through Count :", monsterThroughCount);
			wallThrough = EditorGUILayout.Toggle("Wall Through :", wallThrough);
			quadThrough = EditorGUILayout.Toggle("Quad Through :", quadThrough);
			bounceWallQuadCount = EditorGUILayout.IntField("Bounce Wall Quad Count :", bounceWallQuadCount);
			ricochetCount = EditorGUILayout.IntField("Ricochet Count :", ricochetCount);
			if (ricochetCount > 0)
				ricochetOneHitPerTarget = EditorGUILayout.Toggle("Ricochet One Hit Per Target :", ricochetOneHitPerTarget);

			if (oneHitPerTarget == false)
				useHitStay = EditorGUILayout.Toggle("Use Hit Stay :", useHitStay);
			if (useHitStay)
			{
				hitStayInterval = EditorGUILayout.FloatField("Hit Stay Interval :", hitStayInterval);
				hitStayIgnoreDuplicate = EditorGUILayout.Toggle("Hit Stay No Duplicate", hitStayIgnoreDuplicate);
				if (hitStayIgnoreDuplicate)
					hitStayIdForIgnoreDuplicate = EditorGUILayout.IntField("Hit Stay Id in Actor :", hitStayIdForIgnoreDuplicate);
			}
			if (useHitStay == false)
			{
				oneHitPerTarget = EditorGUILayout.Toggle("One Hit Per Target :", oneHitPerTarget);
			}
			useLineRenderer = EditorGUILayout.Toggle("Use LineRenderer :", useLineRenderer);
			EditorGUILayout.LabelField("-----------------------------------------------------------------");
		}
		else if (targetDetectType == HitObject.eTargetDetectType.Area || targetDetectType == HitObject.eTargetDetectType.SphereCast)
		{
			// 잘만 하면 Area SphereCast 둘다 hitStay 적용할 수 있을듯. 그럼 위의 else if와 합쳐야한다.
			if (oneHitPerTarget == false)
				useHitStay = EditorGUILayout.Toggle("Use Hit Stay :", useHitStay);
			if (useHitStay)
			{
				hitStayInterval = EditorGUILayout.FloatField("Hit Stay Interval :", hitStayInterval);
				hitStayIgnoreDuplicate = EditorGUILayout.Toggle("Hit Stay No Duplicate", hitStayIgnoreDuplicate);
				if (hitStayIgnoreDuplicate)
					hitStayIdForIgnoreDuplicate = EditorGUILayout.IntField("Hit Stay Id in Actor :", hitStayIdForIgnoreDuplicate);
			}
			if (useHitStay == false)
			{
				oneHitPerTarget = EditorGUILayout.Toggle("One Hit Per Target :", oneHitPerTarget);
			}
			EditorGUILayout.LabelField("-----------------------------------------------------------------");
		}

		if (reorderableListForAffector == null)
		{
			serializedObjectForAffector = new SerializedObject(this);
			reorderableListForAffector = new ReorderableList(serializedObjectForAffector, serializedObjectForAffector.FindProperty("affectorValueIdList"), true, true, true, true);
			reorderableListForAffector.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Affector Value ID List");
			reorderableListForAffector.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var element = reorderableListForAffector.serializedProperty.GetArrayElementAtIndex(index);
				rect.y += 2;
				rect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(rect, reorderableListForAffector.serializedProperty.GetArrayElementAtIndex(index));
			};
		}
		if (reorderableListForAffector != null)
		{
			serializedObjectForAffector.Update();
			reorderableListForAffector.DoLayoutList();
			serializedObjectForAffector.ApplyModifiedProperties();
		}

		showHitEffect = EditorGUILayout.Toggle("Show HitEffect :", showHitEffect);
		if (showHitEffect)
		{
			useWeaponHitEffect = EditorGUILayout.Toggle("Use Weapon HitEffect :", useWeaponHitEffect);
			if (useWeaponHitEffect)
				weaponDummyName = EditorGUILayout.TextField("Weapon Dummy Name :", weaponDummyName);
			else
				hitEffectObject = (GameObject)EditorGUILayout.ObjectField("HitEffect Object :", hitEffectObject, typeof(GameObject), false);
			hitEffectLookAtNormal = EditorGUILayout.Toggle("LookAt ContactNormal :", hitEffectLookAtNormal);
		}
		hitEffectLineRendererType = (HitEffect.eLineRendererType)EditorGUILayout.EnumPopup("HitEffect LineRenderer Type :", hitEffectLineRendererType);
		if (hitEffectLineRendererType != HitEffect.eLineRendererType.None)
			hitEffectLineRendererObject = (GameObject)EditorGUILayout.ObjectField("HitEffect LineRenderer Object :", hitEffectLineRendererObject, typeof(GameObject), false);
		showHitBlink = EditorGUILayout.Toggle("Show HitBlink :", showHitBlink);
		showHitRimBlink = EditorGUILayout.Toggle("Show HitRimBlink :", showHitRimBlink);
		EditorGUILayout.EndScrollView();
	}

	override public void OnDrawGizmo(Transform t)
	{
		OnDrawGizmoArea(t);
		OnDrawGizmoDirection(t);
	}

	void OnDrawGizmoArea(Transform t)
	{
		if (targetDetectType != HitObject.eTargetDetectType.Area)
			return;

		float nearDistanceScaled = areaDistanceMin; // * t.localScale.x;
		float farDistanceScaled = areaDistanceMax; // * t.localScale.x;
		float heightMinScaled = areaHeightMin; // * t.localScale.x;
		float heightMaxScaled = areaHeightMax; // * t.localScale.x;
		Vector3 areaPosition = t.TransformPoint(offset); // offset * t.localScale

		Vector3 A = Vector3.zero; Vector3 prevA = Vector3.zero;
		Vector3 B = Vector3.zero; Vector3 prevB = Vector3.zero;
		Vector3 C = Vector3.zero; Vector3 prevC = Vector3.zero;
		Vector3 D = Vector3.zero; Vector3 prevD = Vector3.zero;
		Vector3 vTemp = Vector3.zero;

		int sideCount = (int)areaAngle;
		for (int i = 0; i <= sideCount; ++i)
		{
			vTemp = Vector3.forward;
			float currentAngle = t.rotation.eulerAngles.y + areaRotationY + areaAngle * 0.5f - (float)i;
			Quaternion q = Quaternion.AngleAxis(currentAngle, Vector3.up);
			vTemp = q * vTemp;
			vTemp.Normalize();

			A = B = areaPosition + vTemp * nearDistanceScaled;
			A.y += heightMaxScaled;
			B.y += heightMinScaled;

			C = D = areaPosition + vTemp * farDistanceScaled;
			C.y += heightMaxScaled;
			D.y += heightMinScaled;

			if (i == 0 || i == sideCount)
			{
				Debug.DrawLine(A, B, Color.red);
				Debug.DrawLine(C, D, Color.red);
				Debug.DrawLine(A, C, Color.red);
				Debug.DrawLine(B, D, Color.red);
			}
			if ((i != 0 || i == sideCount) && (0 < sideCount))
			{
				Debug.DrawLine(prevA, A, Color.red);
				Debug.DrawLine(prevB, B, Color.red);
				Debug.DrawLine(prevC, C, Color.red);
				Debug.DrawLine(prevD, D, Color.red);
			}
			prevA = A;
			prevB = B;
			prevC = C;
			prevD = D;
		}
	}

	void OnDrawGizmoDirection(Transform t)
	{
		if (targetDetectType == HitObject.eTargetDetectType.Area)
			return;
		if (movable == false)
			return;

		Transform spawnTransform = t;
		if (createPositionType == HitObject.eCreatePositionType.Bone && !string.IsNullOrEmpty(boneName))
		{
			Animator animator = t.gameObject.GetComponentInChildren<Animator>();
			if (_dummyFinder == null) _dummyFinder = animator.GetComponent<DummyFinder>();
			if (_dummyFinder == null) _dummyFinder = animator.gameObject.AddComponent<DummyFinder>();

			Transform attachTransform = _dummyFinder.FindTransform(boneName);
			if (attachTransform != null)
				spawnTransform = attachTransform;
		}

		Vector3 offsetPosition = HitObject.GetSpawnPosition(spawnTransform, this, t);
		Vector3 direction = HitObject.GetSpawnDirection(offsetPosition, this, t, HitObject.GetFallbackTargetPosition(t)) * 1.5f;

		Color defaultColor = Gizmos.color;
		Gizmos.color = new Color(1.0f, 0.1f, 0.0f, 0.9f);
		Gizmos.DrawSphere(offsetPosition, 0.1f);
		Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.9f);
		Gizmos.DrawRay(offsetPosition, direction);

		float arrowHeadAngle = 20.0f;
		float arrowHeadLength = 0.25f;
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 down = Quaternion.LookRotation(direction) * Quaternion.Euler(180 + arrowHeadAngle, 0, 0) * new Vector3(0, 0, 1);
		Vector3 up = Quaternion.LookRotation(direction) * Quaternion.Euler(180 - arrowHeadAngle, 0, 0) * new Vector3(0, 0, 1);
		Gizmos.DrawRay(offsetPosition + direction, right * arrowHeadLength);
		Gizmos.DrawRay(offsetPosition + direction, left * arrowHeadLength);
		Gizmos.DrawRay(offsetPosition + direction, down * arrowHeadLength);
		Gizmos.DrawRay(offsetPosition + direction, up * arrowHeadLength);

		Gizmos.DrawLine(offsetPosition + direction + right * arrowHeadLength, offsetPosition + direction + left * arrowHeadLength);
		Gizmos.DrawLine(offsetPosition + direction + down * arrowHeadLength, offsetPosition + direction + up * arrowHeadLength);
		Gizmos.DrawLine(offsetPosition + direction + right * arrowHeadLength, offsetPosition + direction + up * arrowHeadLength);
		Gizmos.DrawLine(offsetPosition + direction + up * arrowHeadLength, offsetPosition + direction + left * arrowHeadLength);
		Gizmos.DrawLine(offsetPosition + direction + left * arrowHeadLength, offsetPosition + direction + down * arrowHeadLength);
		Gizmos.DrawLine(offsetPosition + direction + down * arrowHeadLength, offsetPosition + direction + right * arrowHeadLength);
		Gizmos.color = defaultColor;
	}
	#endif

	Actor actor;
	HitObjectAnimator hitObjectAnimator;
	ActionController actionController;
	DummyFinder _dummyFinder = null;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		//f (MecanimEventBase.s_bForceCallUpdate) return;

		if (actor == null)
		{
			if (animator.transform.parent != null)
				actor = animator.transform.parent.GetComponent<Actor>();
			if (actor == null)
				actor = animator.GetComponent<Actor>();
			if (actor == null)
			{
				hitObjectAnimator = animator.GetComponent<HitObjectAnimator>();
				if (hitObjectAnimator != null)
					actor = hitObjectAnimator.parentActor;
			}
			if (actor == null)
			{
				Debug.LogError("HitObject not created. Not Found Actor!");
				return;
			}
		}

		Transform spawnTransform = null;
		Transform parentTransform = null;
		if (hitObjectAnimator != null)
		{
			spawnTransform = hitObjectAnimator.cachedTransform;
			parentTransform = hitObjectAnimator.cachedTransform;
		}
		else
		{
			spawnTransform = actor.cachedTransform;
			parentTransform = actor.cachedTransform;
		}

		actionController = actor.actionController;
		int hitSignalIndexInAction = 0;
		if (actionController != null)
			hitSignalIndexInAction = actionController.OnHitObjectSignal(stateInfo.fullPathHash);
		if (createPositionType == HitObject.eCreatePositionType.Bone && !string.IsNullOrEmpty(boneName))
		{
			if (_dummyFinder == null) _dummyFinder = animator.GetComponent<DummyFinder>();
			if (_dummyFinder == null) _dummyFinder = animator.gameObject.AddComponent<DummyFinder>();

			Transform attachTransform = _dummyFinder.FindTransform(boneName);
			if (attachTransform != null)
				spawnTransform = attachTransform;
		}

		InitializeHitObject(spawnTransform, this, actor, parentTransform, hitSignalIndexInAction);
	}

	protected virtual void InitializeHitObject(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, int hitSignalIndexInAction)
	{
		// Repeat처리가 가장 먼저다.
		// 그런데 상황에 따라 메인 발사체를 스폰하지 않을 수 있다.
		HitObject.InitializeHit(spawnTransform, meHit, parentActor, parentTransform, hitSignalIndexInAction, 0);

		if (repeatCount > 0)
		{
			Timing.RunCoroutine(RepeatProcess(spawnTransform, meHit, parentActor, parentTransform, hitSignalIndexInAction));
		}
	}

	IEnumerator<float> RepeatProcess(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, int hitSignalIndexInAction)
	{
		// Repeat 하기전 트랜스폼들을 복제해서 캐싱해야한다. 이래야 본 포지션 및 캐릭터 방향까지 기억할 수 있다.
		Transform duplicatedSpawnTransform = BattleInstanceManager.instance.GetEmptyTransform(spawnTransform.position, spawnTransform.rotation);
		Transform duplicatedParentTransform = BattleInstanceManager.instance.GetEmptyTransform(parentTransform.position, parentTransform.rotation);
		for (int i = 1; i <= meHit.repeatCount; ++i)
		{
			yield return Timing.WaitForSeconds(meHit.repeatInterval);

			// avoid gc
			if (this == null)
				yield break;

			HitObject.InitializeHit(duplicatedSpawnTransform, meHit, parentActor, duplicatedParentTransform, hitSignalIndexInAction, i);
		}
		duplicatedSpawnTransform.gameObject.SetActive(false);
		duplicatedParentTransform.gameObject.SetActive(false);
	}
}