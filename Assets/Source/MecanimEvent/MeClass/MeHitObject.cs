using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public class MeHitObject : MecanimEventBase {

	override public bool RangeSignal { get { return false; } }

	public GameObject hitObjectPrefab;
	public float lifeTime;
	public bool movable;
	public HitObject.eTargetDetectType targetDetectType;
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

	public HitObjectMovement.eMovementType movementType;
	public HitObjectMovement.eStartDirectionType startDirectionType;
	public Vector3 startDirection = Vector3.forward;
	public bool useWorldSpaceDirection;
	public bool bothRandomAngle = true;
	public float leftRightRandomAngle;
	public float leftRandomAngle;
	public float rightRandomAngle;
	public float upDownRandomAngle;
	public float speed;
	public float curve;
	public float curveAdd;
	public bool curveLockY;

	public bool contactAll;
	public int monsterThroughCount;
	public bool wallThrough;
	public bool quadThrough;
	public int bounceWallQuadCount;
	public int ricochetCount;
	public bool useHitStay;
	public float hitStayInterval;
	public int hitStayGroupNumber = 0;
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
	public bool showHitBlink;
	public bool showHitRimBlink;


	#if UNITY_EDITOR
	SerializedObject so = null;
	ReorderableList reorderableList = null;
	Vector2 _propertyScrollPosition;
	override public void OnGUI_PropertyWindow()
	{
		_propertyScrollPosition = EditorGUILayout.BeginScrollView(_propertyScrollPosition);
		hitObjectPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", hitObjectPrefab, typeof(GameObject), false);
		lifeTime = EditorGUILayout.FloatField("LifeTime :", lifeTime);
		if (lifeTime > 0.0f) movable = EditorGUILayout.Toggle("Movable :", movable);
		else movable = false;
		targetDetectType = (HitObject.eTargetDetectType)EditorGUILayout.EnumPopup("Target Find Type :", targetDetectType);
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

		if (movable)
		{
			movementType = (HitObjectMovement.eMovementType)EditorGUILayout.EnumPopup("Movement Type :", movementType);
			if (movementType == HitObjectMovement.eMovementType.FollowTarget)
			{
				curve = EditorGUILayout.FloatField("Curve Power :", curve);
				curveAdd = EditorGUILayout.FloatField("Curve Power Add :", curveAdd);
				curveLockY = EditorGUILayout.Toggle("Curve Lock Y :", curveLockY);
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
			speed = EditorGUILayout.FloatField("Speed :", speed);
			EditorGUILayout.LabelField("-----------------------------------------------------------------");
		}

		if (targetDetectType == HitObject.eTargetDetectType.Collider)
		{
			contactAll = EditorGUILayout.Toggle("Contact All :", contactAll);
			monsterThroughCount = EditorGUILayout.IntField("Monster Through Count :", monsterThroughCount);
			wallThrough = EditorGUILayout.Toggle("Wall Through :", wallThrough);
			quadThrough = EditorGUILayout.Toggle("Quad Through :", quadThrough);
			bounceWallQuadCount = EditorGUILayout.IntField("Bounce Wall Quad Count :", bounceWallQuadCount);
			ricochetCount = EditorGUILayout.IntField("Ricochet Count :", ricochetCount);

			if (oneHitPerTarget == false)
				useHitStay = EditorGUILayout.Toggle("Use Hit Stay :", useHitStay);
			if (useHitStay)
			{
				hitStayInterval = EditorGUILayout.FloatField("Hit Stay Interval :", hitStayInterval);
				hitStayGroupNumber = EditorGUILayout.IntField("Hit Stay Group Number", hitStayGroupNumber);
			}
			if (useHitStay == false)
			{
				oneHitPerTarget = EditorGUILayout.Toggle("One Hit Per Target :", oneHitPerTarget);
			}
			useLineRenderer = EditorGUILayout.Toggle("Use LineRenderer :", useLineRenderer);
			EditorGUILayout.LabelField("-----------------------------------------------------------------");
		}

		if (reorderableList == null)
		{
			so = new SerializedObject(this);
			reorderableList = new ReorderableList(so, so.FindProperty("affectorValueIdList"), true, true, true, true);
			reorderableList.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Affector Value ID List");
			reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				rect.y += 2;
				rect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(rect, reorderableList.serializedProperty.GetArrayElementAtIndex(index));
			};
		}
		if (reorderableList != null)
		{
			so.Update();
			reorderableList.DoLayoutList();
			so.ApplyModifiedProperties();
		}

		showHitEffect = EditorGUILayout.Toggle("Show HitEffect :", showHitEffect);
		if (showHitEffect)
		{
			useWeaponHitEffect = EditorGUILayout.Toggle("Use Weapon HitEffect :", useWeaponHitEffect);
			if (useWeaponHitEffect)
				weaponDummyName = EditorGUILayout.TextField("Weapon Dummy Name :", weaponDummyName);
			else
				hitEffectObject = (GameObject)EditorGUILayout.ObjectField("HitEffect Object :", hitEffectObject, typeof(GameObject), false);
		}
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
		Vector3 direction = HitObjectMovement.GetStartDirection(this, offsetPosition, t, 0, HitObjectMovement.GetFallbackTargetPosition(t)) * 1.5f;

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
		HitObject.InitializeHit(spawnTransform, this, actor, parentTransform, hitSignalIndexInAction);
	}

}