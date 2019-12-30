using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public enum eSkillType
{
	Active = 1,
	Passive = 2,
	NonAni = 3,
}

// 이 플젝에선 몬스터의 경우 레벨팩을 들고있지도 않을거고
// 액티브 스킬 혹은 패시브스킬도 가지고 있더라도 스킬레벨이 들어있지 않을거라
// 액션툴 혹은 AI툴에다가 직접 어펙터를 설정하게 될 것이다.
// 심지어 스킬 쓸때 연출이나 스킬 이름 같은게 나올거도 아니라서 스킬을 들고있을 필요가 전혀 없으므로
// 플레이어 액터만 이걸 들고있게 해서 처리하기로 한다.
// 다음 게임에서도 위와 같은 조건이라면 PlayerActor만 가지게 될 것이고 그렇지 않다면 Actor가 가지도록 설계하면 될 것이다.
public class SkillProcessor : MonoBehaviour
{
	public Actor actor { get; private set; }
	public CooltimeProcessor cooltimeProcessor { get; private set; }
	public AffectorProcessor affectorProcessor { get; private set; }

	public class SkillInfo
	{
		public string skillId;
		public int skillLevel;
		public eSkillType skillType;
		public string iconName;
		public float cooltime;
		public int actionNameHash;
		public string[] tableAffectorValueIdList;
		public string nameId;
		public string descriptionId;
		public List<AffectorBase> listPassiveAffector;
	}

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	List<SkillInfo> _listSkillInfo;
	public void InitializeSkill()
	{
		cooltimeProcessor = GetComponent<CooltimeProcessor>();
		if (cooltimeProcessor == null) cooltimeProcessor = gameObject.AddComponent<CooltimeProcessor>();

		affectorProcessor = GetComponent<AffectorProcessor>();
		if (affectorProcessor == null) affectorProcessor = gameObject.AddComponent<AffectorProcessor>();

		_listSkillInfo = new List<SkillInfo>();
		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			SkillTableData skillTableData = TableDataManager.instance.skillTable.dataArray[i];
			if (skillTableData.actorId != actor.actorId) continue;

			SkillInfo info = new SkillInfo();
			int skillLevel = 1; // actor.GetSkillLevel(skillInfo.skillID);

			info.skillId = skillTableData.id;
			info.skillLevel = skillLevel;
			info.skillType = (eSkillType)skillTableData.skillType;
			info.iconName = skillTableData.icon;
			info.cooltime = skillTableData.cooltime;
			info.actionNameHash = 0;
			info.tableAffectorValueIdList = skillTableData.tableAffectorValueId;
			info.nameId = skillTableData.nameId;
			info.descriptionId = skillTableData.descriptionId;

			if (skillTableData.useCooltimeOverriding || skillTableData.useMecanimNameOverriding || skillTableData.useTableAffectorValueIdOverriding || skillTableData.useNameIdOverriding || skillTableData.useDescriptionIdOverriding)
			{
				SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(info.skillId, skillLevel);
				if (skillLevelTableData != null)
				{
					if (skillTableData.useCooltimeOverriding)
						info.cooltime = skillLevelTableData.cooltime;
					if (skillTableData.useMecanimNameOverriding)
						info.actionNameHash = BattleInstanceManager.instance.GetActionNameHash(skillLevelTableData.mecanimName);
					if (skillTableData.useTableAffectorValueIdOverriding)
						info.tableAffectorValueIdList = skillLevelTableData.tableAffectorValueId;
					if (skillTableData.useNameIdOverriding)
						info.nameId = skillLevelTableData.nameId;
					if (skillTableData.useDescriptionIdOverriding)
						info.descriptionId = skillLevelTableData.descriptionId;
				}
			}

			#region Passive Skill
			if (info.skillType == eSkillType.Passive)
				InitializePassiveSkill(info);
			#endregion

			_listSkillInfo.Add(info);
		}
	}

	public SkillInfo GetSkillInfo(string skillId)
	{
		for (int i = 0; i < _listSkillInfo.Count; ++i)
		{
			if (_listSkillInfo[i].skillId == skillId)
				return _listSkillInfo[i];
		}
		return null;
	}

	#region Passive Skill
	void InitializePassiveSkill(SkillInfo info)
	{
		if (actor == null)
			return;
		if (info.tableAffectorValueIdList.Length == 0)
			return;

		if (info.listPassiveAffector == null)
			info.listPassiveAffector = new List<AffectorBase>();
		info.listPassiveAffector.Clear();

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = new StatusBase();
		actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
		CopyEtcStatus(ref hitParameter.statusStructForHitObject, actor);
		hitParameter.statusStructForHitObject.skillLevel = info.skillLevel;

		for (int i = 0; i < info.tableAffectorValueIdList.Length; ++i)
		{
			AffectorBase passiveAffector = affectorProcessor.ApplyAffectorValue(info.tableAffectorValueIdList[i], hitParameter, true);
			if (passiveAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(passiveAffector.affectorType))
				info.listPassiveAffector.Add(passiveAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a passive skill! / SkillId = {0} / AffectorValueId = {1}", info.skillId, info.tableAffectorValueIdList[i]);
		}
	}

	void LevelUpPassiveSkill(SkillInfo info)
	{
		for (int i = 0; i < info.listPassiveAffector.Count; ++i)
			info.listPassiveAffector[i].finalized = true;

		info.skillLevel += 1;

		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(info.skillId);
		if (skillTableData.useTableAffectorValueIdOverriding)
		{
			SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(info.skillId, info.skillLevel);
			if (skillLevelTableData != null)
			{
				if (skillTableData.useTableAffectorValueIdOverriding)
					info.tableAffectorValueIdList = skillLevelTableData.tableAffectorValueId;
			}
		}
		InitializePassiveSkill(info);
	}
	#endregion

	#region NonAni Skill
	public void ApplyNonAniSkill(SkillInfo info)
	{
		if (actor == null)
			return;
		if (info.tableAffectorValueIdList.Length == 0)
			return;

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = new StatusBase();
		actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
		CopyEtcStatus(ref hitParameter.statusStructForHitObject, actor);
		hitParameter.statusStructForHitObject.skillLevel = info.skillLevel;

		// 애니만 없을뿐 active스킬처럼 쓰는거라서 managed는 false로 호출한다.
		for (int i = 0; i < info.tableAffectorValueIdList.Length; ++i)
			affectorProcessor.ApplyAffectorValue(info.tableAffectorValueIdList[i], hitParameter, false);
	}
	#endregion

	public static void CopyEtcStatus(ref StatusStructForHitObject statusStructForHitObject, Actor actor)
	{
		statusStructForHitObject.actorInstanceId = actor.GetInstanceID();
		statusStructForHitObject.teamId = actor.team.teamId;
		statusStructForHitObject.skillLevel = 0;

		//statusStructForHitObject.weaponIDAtCreation = 0;
		//if (meHit.useWeaponHitEffect)
		//	statusStructForHitObject.weaponIDAtCreation = actor.GetWeaponID(meHit.weaponDummyName);
		statusStructForHitObject.hitSignalIndexInAction = 0;
	}







	#region Level Pack

	public class LevelPackInfo
	{
		public int stackCount;
		public bool exclusive;
		public string iconAddress;
		public string[] affectorValueIdList;
		public string nameId;
		public string descriptionId;
		public string[] descriptionParameterList;
		public List<AffectorBase> listAffector;
	}

	// 형태가 패시브와 매우 유사하여 스킬 프로세서에 넣는다. 게임 구조가 다르다면 하단은 삭제.
	Dictionary<string, LevelPackInfo> _dicLevelPack = null;
	public Dictionary<string, LevelPackInfo> dicLevelPack { get { return _dicLevelPack; } }
	public void AddLevelPack(string levelPackId)
	{
		if (actor == null)
			return;

		if (_dicLevelPack == null)
			_dicLevelPack = new Dictionary<string, LevelPackInfo>();

		LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(levelPackId);
		if (levelPackTableData == null)
			return;

		LevelPackInfo info = null;
		if (_dicLevelPack.ContainsKey(levelPackId) == false)
		{
			info = new LevelPackInfo();
			info.stackCount = 1;
			info.exclusive = levelPackTableData.exclusive;
			info.iconAddress = levelPackTableData.iconAddress;
			if (levelPackTableData.useAffectorValueIdOverriding == false)
				info.affectorValueIdList = levelPackTableData.affectorValueId;
			info.nameId = levelPackTableData.nameId;
			info.descriptionId = levelPackTableData.descriptionId;
			_dicLevelPack.Add(levelPackId, info);

			// 공용으로 쓰는 디버프나 버프 이펙트들은 특정 액터가 들고있기 애매하다.
			// 그래서 레벨팩이나 뭔가 다른 시스템에 의해 필요한 순간이 오면 어드레스로 접근해서 로드한 후 commonPool에 넣기로 한다.
			// 이래야 초기로딩이 길어지지도, 쓰지 않는 이펙트때문에 메모리를 낭비하는 일도 안생긴다.
			for (int i = 0; i < levelPackTableData.effectAddress.Length; ++i)
			{
				AddressableAssetLoadManager.GetAddressableGameObject(levelPackTableData.effectAddress[i], "CommonEffect", (prefab) =>
				{
					BattleInstanceManager.instance.AddCommonPoolPreloadObjectList(prefab);
				});
			}
		}
		else
		{
			info = _dicLevelPack[levelPackId];
			++info.stackCount;
		}

		LevelPackLevelTableData levelPackLevelTableData = TableDataManager.instance.FindLevelPackLevelTableData(levelPackId, info.stackCount);
		if (levelPackLevelTableData != null)
		{
			if (levelPackTableData.useAffectorValueIdOverriding)
				info.affectorValueIdList = levelPackLevelTableData.affectorValueId;
			info.descriptionParameterList = levelPackLevelTableData.parameter;
		}

		CreateLevelPackAffector(levelPackId, info);
	}

	void CreateLevelPackAffector(string levelPackId, LevelPackInfo info)
	{
		if (actor == null)
			return;
		if (info.affectorValueIdList.Length == 0)
			return;

		if (info.listAffector == null)
			info.listAffector = new List<AffectorBase>();

		for (int i = 0; i < info.listAffector.Count; ++i)
			info.listAffector[i].finalized = true;
		info.listAffector.Clear();

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = new StatusBase();
		actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
		CopyEtcStatus(ref hitParameter.statusStructForHitObject, actor);
		hitParameter.statusStructForHitObject.skillLevel = info.stackCount;

		for (int i = 0; i < info.affectorValueIdList.Length; ++i)
		{
			AffectorBase newAffector = affectorProcessor.ApplyAffectorValue(info.affectorValueIdList[i], hitParameter, true);
			if (newAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(newAffector.affectorType))
				info.listAffector.Add(newAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a levelPack! / LevelPackId = {0} / AffectorValueId = {1}", levelPackId, info.affectorValueIdList[i]);
		}
	}

	public int GetLevelPackStackCount(string levelPackId)
	{
		if (actor == null || _dicLevelPack == null)
			return 0;

		if (_dicLevelPack.ContainsKey(levelPackId) == false)
			return 0;

		return _dicLevelPack[levelPackId].stackCount;
	}
	#endregion
}
