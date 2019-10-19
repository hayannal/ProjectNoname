using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public bool passiveSkill;
		public string iconName;
		public float cooltime;
		public int actionNameHash;
		public string[] passiveAffectorValueId;
		public string nameId;
		public string descriptionId;
		public List<AffectorBase> listPassiveAffector;
	}

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	List<SkillInfo> _listSkillInfo;
	public void InitializeSkill(string actorId)
	{
		cooltimeProcessor = GetComponent<CooltimeProcessor>();
		if (cooltimeProcessor == null) cooltimeProcessor = gameObject.AddComponent<CooltimeProcessor>();

		affectorProcessor = GetComponent<AffectorProcessor>();
		if (affectorProcessor == null) affectorProcessor = gameObject.AddComponent<AffectorProcessor>();

		_listSkillInfo = new List<SkillInfo>();
		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			SkillTableData skillTableData = TableDataManager.instance.skillTable.dataArray[i];
			if (skillTableData.actorId != actorId) continue;

			SkillInfo info = new SkillInfo();
			int skillLevel = 1; // actor.GetSkillLevel(skillInfo.skillID);

			info.skillId = skillTableData.id;
			info.skillLevel = skillLevel;
			info.passiveSkill = skillTableData.passiveSkill;
			info.iconName = skillTableData.icon;
			info.cooltime = skillTableData.cooltime;
			info.actionNameHash = 0;
			info.passiveAffectorValueId = skillTableData.passiveAffectorValueId;
			info.nameId = skillTableData.nameId;
			info.descriptionId = skillTableData.descriptionId;

			if (skillTableData.useCooltimeOverriding || skillTableData.useMecanimNameOverriding || skillTableData.usePassiveAffectorValueIdOverriding || skillTableData.useNameIdOverriding || skillTableData.useDescriptionIdOverriding)
			{
				SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(info.skillId, skillLevel);
				if (skillLevelTableData != null)
				{
					if (skillTableData.useCooltimeOverriding)
						info.cooltime = skillLevelTableData.cooltime;
					if (skillTableData.useMecanimNameOverriding)
						info.actionNameHash = BattleInstanceManager.instance.GetActionNameHash(skillLevelTableData.mecanimName);
					if (skillTableData.usePassiveAffectorValueIdOverriding)
						info.passiveAffectorValueId = skillLevelTableData.passiveAffectorValueId;
					if (skillTableData.useNameIdOverriding)
						info.nameId = skillLevelTableData.nameId;
					if (skillTableData.useDescriptionIdOverriding)
						info.descriptionId = skillLevelTableData.descriptionId;
				}
			}

			#region Passive Skill
			if (info.passiveSkill)
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
		if (info.passiveAffectorValueId.Length == 0)
			return;

		if (info.listPassiveAffector == null)
			info.listPassiveAffector = new List<AffectorBase>();
		info.listPassiveAffector.Clear();

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = actor.actorStatus.statusBase;
		CopyEtcStatus(ref hitParameter.statusStructForHitObject, actor);
		hitParameter.statusStructForHitObject.skillLevel = info.skillLevel;

		for (int i = 0; i < info.passiveAffectorValueId.Length; ++i)
		{
			AffectorBase passiveAffector = affectorProcessor.ApplyAffectorValue(info.passiveAffectorValueId[i], hitParameter, true);
			if (passiveAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(passiveAffector.affectorType))
				info.listPassiveAffector.Add(passiveAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a passive skill! / SkillId = {0} / AffectorValueId = {1}", info.skillId, info.passiveAffectorValueId[i]);
		}
	}

	void LevelUpPassiveSkill(SkillInfo info)
	{
		for (int i = 0; i < info.listPassiveAffector.Count; ++i)
			info.listPassiveAffector[i].finalized = true;

		info.skillLevel += 1;

		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(info.skillId);
		if (skillTableData.usePassiveAffectorValueIdOverriding)
		{
			SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(info.skillId, info.skillLevel);
			if (skillLevelTableData != null)
			{
				if (skillTableData.usePassiveAffectorValueIdOverriding)
					info.passiveAffectorValueId = skillLevelTableData.passiveAffectorValueId;
			}
		}
		InitializePassiveSkill(info);
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
		public int level;
		public int maxLevel;
		public string iconName;
		public string[] affectorValueId;
		public string nameId;
		public string descriptionId;
		public string[] descriptionParameterList;
		public List<AffectorBase> listAffector;
	}

	// 형태가 패시브와 매우 유사하여 스킬 프로세서에 넣는다. 게임 구조가 다르다면 하단은 삭제.
	Dictionary<string, LevelPackInfo> _dicLevelPack = null;
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
		int nextlevel = 0;
		bool createAffector = false;
		if (_dicLevelPack.ContainsKey(levelPackId) == false)
		{
			info = new LevelPackInfo();
			nextlevel = 1;
			info.maxLevel = levelPackTableData.defaultMax;
			ActorLevelPackTableData actorLevelPackTableData = TableDataManager.instance.FindActorLevelPackTableData(actor.actorId, levelPackId);
			if (actorLevelPackTableData != null)
				info.maxLevel = actorLevelPackTableData.overridingMax;
			info.iconName = levelPackTableData.icon;
			if (levelPackTableData.useAffectorValueIdOverriding == false)
				info.affectorValueId = levelPackTableData.affectorValueId;
			info.nameId = levelPackTableData.nameId;
			info.descriptionId = levelPackTableData.descriptionId;
			createAffector = true;
			_dicLevelPack.Add(levelPackId, info);
		}
		else
		{
			info = _dicLevelPack[levelPackId];
			nextlevel = info.level + 1;
		}

		LevelPackLevelTableData levelPackLevelTableData = TableDataManager.instance.FindLevelPackLevelTableData(levelPackId, nextlevel);
		if (levelPackLevelTableData != null)
		{
			if (levelPackTableData.useAffectorValueIdOverriding)
			{
				info.affectorValueId = levelPackLevelTableData.affectorValueId;
				createAffector = true;
			}
			info.descriptionParameterList = levelPackLevelTableData.parameter;
		}

		if (createAffector)
		{
			CreateLevelPackAffector(levelPackId, info);
		}
	}

	void CreateLevelPackAffector(string levelPackId, LevelPackInfo info)
	{
		if (actor == null)
			return;
		if (info.affectorValueId.Length == 0)
			return;

		if (info.listAffector == null)
			info.listAffector = new List<AffectorBase>();

		for (int i = 0; i < info.listAffector.Count; ++i)
			info.listAffector[i].finalized = true;
		info.listAffector.Clear();

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = actor.actorStatus.statusBase;
		CopyEtcStatus(ref hitParameter.statusStructForHitObject, actor);
		hitParameter.statusStructForHitObject.skillLevel = info.level;

		for (int i = 0; i < info.affectorValueId.Length; ++i)
		{
			AffectorBase newAffector = affectorProcessor.ApplyAffectorValue(info.affectorValueId[i], hitParameter, true);
			if (newAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(newAffector.affectorType))
				info.listAffector.Add(newAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a levelPack! / LevelPackId = {0} / AffectorValueId = {1}", levelPackId, info.affectorValueId[i]);
		}
	}
	#endregion
}
