using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECM.Controllers;

public class Actor : MonoBehaviour {

	public static int ACTOR_LAYER;

	public string actorId;
	public ActionController actionController { get; private set; }
	public BaseCharacterController baseCharacterController { get; private set; }
	//public MovementController movementController { get; private set; }
	//public CastingProcessor castingProcessor { get; private set; }
	public CooltimeProcessor cooltimeProcessor { get; private set; }
	public ActorStatus actorStatus { get; protected set; }
	public AffectorProcessor affectorProcessor { get; private set; }
	public Team team { get; private set; }
	public TargetingProcessor targetingProcessor { get; private set; }

	protected Rigidbody _rigidbody { get; private set; }
	protected Collider _collider { get; private set; }

	void Awake()
	{
		InitializeComponent();
	}

	void Start()
	{
		InitializeActor();
	}

	protected virtual void InitializeComponent()
	{
		if (ACTOR_LAYER == 0) ACTOR_LAYER = LayerMask.NameToLayer("Actor");
		//ObjectUtil.ChangeLayer(gameObject, ACTOR_LAYER);

		if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
		if (_collider == null) _collider = GetComponent<Collider>();

		actionController = GetComponent<ActionController>();
		if (actionController == null) actionController = gameObject.AddComponent<ActionController>();

		baseCharacterController = GetComponent<BaseCharacterController>();
		//movementController = GetComponent<MovementController>();
		//if (movementController == null) movementController = gameObject.AddComponent<MovementController>();

		cooltimeProcessor = GetComponent<CooltimeProcessor>();
		if (cooltimeProcessor == null) cooltimeProcessor = gameObject.AddComponent<CooltimeProcessor>();

		affectorProcessor = GetComponent<AffectorProcessor>();
		if (affectorProcessor == null) affectorProcessor = gameObject.AddComponent<AffectorProcessor>();

		team = GetComponent<Team>();
		if (team == null) team = gameObject.AddComponent<Team>();

		targetingProcessor = GetComponent<TargetingProcessor>();
		if (targetingProcessor == null) targetingProcessor = gameObject.AddComponent<TargetingProcessor>();
	}

	protected virtual void InitializeActor()
	{
		actionController.InitializeActionPlayInfo(actorId);
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

	public virtual void OnChangedHP()
	{

	}

	public virtual void OnDie()
	{
		actionController.PlayActionByActionName("Die");
		actionController.idleAnimator.enabled = false;
		HitObject.EnableRigidbodyAndCollider(false, _rigidbody, _collider, false);
	}

	/*
	// Team
	public int TeamID
	{
		get { return m_Team.teamID; }
		set { m_Team.teamID = value; }
	}

	#region Weaon
	// Weapon
	struct sWeaponInfo
	{
		public int weaponID;
		public GameObject weaponObject;
	}
	Dictionary<string, sWeaponInfo> m_dicWeaponInfo = new Dictionary<string, sWeaponInfo>();
	public virtual int GetWeaponID(string weaponDummyName)
	{
		if (string.IsNullOrEmpty(weaponDummyName)) weaponDummyName = "Dummy_weapon_R";
		if (m_dicWeaponInfo.ContainsKey(weaponDummyName))
			return m_dicWeaponInfo[weaponDummyName].weaponID;
		return 0;
	}
	public virtual void AttachWeapon(int weaponID, string weaponDummyName)
	{
		DetachWeapon(weaponDummyName);

		string weaponKey = string.Format("id{0}", weaponID);
		Google2u.WeaponRow weaponRow = Google2u.Weapon.Instance.GetRow(weaponKey);
		if (weaponRow == null) return;
		GameObject orig = AssetBundleManager.LoadAsset<GameObject>("character.unity3d", weaponRow._Prefab);
		if (orig == null) return;

		GameObject weaponObject = ObjectUtil.InstantiateFromBundle(orig);
		AttachedObject attached = weaponObject.GetComponent<AttachedObject>();
		if (attached == null) attached = weaponObject.AddComponent<AttachedObject>();
		attached.parentTransformName = weaponDummyName;
		attached.targetTransform = transform;
		attached.Attach();

		sWeaponInfo weaponInfo = new sWeaponInfo();
		weaponInfo.weaponID = weaponID;
		weaponInfo.weaponObject = weaponObject;
		m_dicWeaponInfo.Add(weaponDummyName, weaponInfo);
	}
	public virtual void DetachWeapon(string weaponDummyName)
	{
		if (!m_dicWeaponInfo.ContainsKey(weaponDummyName))
			return;

		Destroy(m_dicWeaponInfo[weaponDummyName].weaponObject);
		m_dicWeaponInfo.Remove(weaponDummyName);
	}
	#endregion
	*/
}
