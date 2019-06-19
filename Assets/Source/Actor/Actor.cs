using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour {

	public static int ACTOR_LAYER;

	public int actorID;
	public ActionController actionController { get; set; }
	//public MovementController movementController { get; set; }
	//public CastingProcessor castingProcessor { get; set; }
	public CooltimeProcessor cooltimeProcessor { get; set; }
	public ActorStatus actorStatus { get; set; }
	public AffectorProcessor affectorProcessor { get; set; }
	public Team team { get; set; }
	public TargetingProcessor targetingSystem { get; set; }

	void Awake()
	{
		InitializeComponent();
	}

	void Start()
	{
		InitializeActor();
	}

	protected void InitializeComponent()
	{
		if (ACTOR_LAYER == 0) ACTOR_LAYER = LayerMask.NameToLayer("Actor");
		//ObjectUtil.ChangeLayer(gameObject, ACTOR_LAYER);

		actionController = GetComponent<ActionController>();
		if (actionController == null) actionController = gameObject.AddComponent<ActionController>();

		//movementController = GetComponent<MovementController>();
		//if (movementController == null) movementController = gameObject.AddComponent<MovementController>();

		//castingProcessor = GetComponent<CastingProcessor>();
		//if (castingProcessor == null) castingProcessor = gameObject.AddComponent<CastingProcessor>();

		cooltimeProcessor = GetComponent<CooltimeProcessor>();
		if (cooltimeProcessor == null) cooltimeProcessor = gameObject.AddComponent<CooltimeProcessor>();

		actorStatus = GetComponent<ActorStatus>();
		if (actorStatus == null) actorStatus = gameObject.AddComponent<ActorStatus>();

		affectorProcessor = GetComponent<AffectorProcessor>();
		if (affectorProcessor == null) affectorProcessor = gameObject.AddComponent<AffectorProcessor>();

		team = GetComponent<Team>();
		if (team == null) team = gameObject.AddComponent<Team>();

		targetingSystem = GetComponent<TargetingProcessor>();
		if (targetingSystem == null) targetingSystem = gameObject.AddComponent<TargetingProcessor>();
	}

	//public void InitializeActor(DBData)
	protected void InitializeActor()
	{
		actionController.InitializeActionPlayInfo(actorID);
		actorStatus.InitializeActorStatus(actorID);
	}

	/*
	public virtual void OnDie()
	{
		m_Animator.CrossFade(GetDieAnimationHash(), Default_Transition_Duration);

		actionController.idleAnimator.enabled = false;
		BehaviorDesigner.Runtime.BehaviorTree bt = GetComponent<BehaviorDesigner.Runtime.BehaviorTree>();
		if (bt != null) bt.enabled = false;
		CharacterController cc = GetComponent<CharacterController>();
		if (cc != null) cc.enabled = false;
	}

	protected virtual int GetDieAnimationHash()
	{
		return Hash_Die;
	}

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
