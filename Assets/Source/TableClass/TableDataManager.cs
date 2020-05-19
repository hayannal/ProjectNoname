using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableDataManager : MonoBehaviour
{
	public static TableDataManager instance;

	// temp 
	public ActionTable actionTable;
	public ControlTable controlTable;
	public AffectorValueTable affectorValueTable;
	public AffectorValueLevelTable affectorValueLevelTable;
	public ActorStateTable actorStateTable;
	public StageTable stageTable;
	public MapTable mapTable;
	public ChapterTable chapterTable;
	public MapSetTable mapSetTable;
	public StagePenaltyTable stagePenaltyTable;
	public MonsterTable monsterTable;
	public ActorTable actorTable;
	public PowerLevelTable powerLevelTable;
	public SkillTable skillTable;
	public SkillLevelTable skillLevelTable;
	public ConditionValueTable conditionValueTable;
	public LevelPackTable levelPackTable;
	public LevelPackLevelTable levelPackLevelTable;
	public GlobalConstantFloatTable globalConstantFloatTable;
	public GlobalConstantIntTable globalConstantIntTable;
	public GlobalConstantStringTable globalConstantStringTable;
	public DropTable dropTable;
	public EquipTable equipTable;
	public InnerGradeTable innerGradeTable;
	public OptionTable optionTable;
	public EnhanceTable enhanceTable;
	public TransferTable transferTable;
	public RemainTable remainTable;
	public NotStreakTable notStreakTable;
	public NotCharTable notCharTable;
	public FixedCharTable fixedCharTable;
	public StageExpTable stageExpTable;
	public DamageRateTable damageRateTable;
	public ActorInfoTable actorInfoTable;
	public ActorLevelPackTable actorLevelPackTable;
	public ShopGoldTable shopGoldTable;
	public ShopDiamondTable shopDiamondTable;
	public ShopBoxTable shopBoxTable;
	public ShopLevelPackageTable shopLevelPackageTable;
	public ShopDailyDiamondTable shopDailyDiamondTable;
	public ResearchTable researchTable;

	void Awake()
	{
		instance = this;
	}

	public ActionTableData FindActionTableData(string actorId, string actionName)
	{
		for (int i = 0; i < actionTable.dataArray.Length; ++i)
		{
			if (actionTable.dataArray[i].actorId == actorId && actionTable.dataArray[i].actionName == actionName)
				return actionTable.dataArray[i];
		}
		return null;
	}

	public ControlTableData FindControlTableData(string controlId)
	{
		for (int i = 0; i < controlTable.dataArray.Length; ++i)
		{
			if (controlTable.dataArray[i].id == controlId)
				return controlTable.dataArray[i];
		}
		return null;
	}

	public AffectorValueTableData FindAffectorValueTableData(string affectorValueId)
	{
		for (int i = 0; i < affectorValueTable.dataArray.Length; ++i)
		{
			if (affectorValueTable.dataArray[i].id == affectorValueId)
				return affectorValueTable.dataArray[i];
		}
		return null;
	}

	public AffectorValueLevelTableData FindAffectorValueLevelTableData(string affectorValueId, int level)
	{
		for (int i = 0; i < affectorValueLevelTable.dataArray.Length; ++i)
		{
			if (affectorValueLevelTable.dataArray[i].affectorValueId == affectorValueId && affectorValueLevelTable.dataArray[i].level == level)
				return affectorValueLevelTable.dataArray[i];
		}
		return null;
	}

	public ActorStateTableData FindActorStateTableData(string actorStateId)
	{
		for (int i = 0; i < actorStateTable.dataArray.Length; ++i)
		{
			if (actorStateTable.dataArray[i].actorStateId == actorStateId)
				return actorStateTable.dataArray[i];
		}
		return null;
	}

	public StageTableData FindStageTableData(int chapter, int stage, bool chaos)
	{
		for (int i = 0; i < stageTable.dataArray.Length; ++i)
		{
			if (stageTable.dataArray[i].chapter == chapter && stageTable.dataArray[i].stage == stage && stageTable.dataArray[i].chaos == chaos)
				return stageTable.dataArray[i];
		}
		return null;
	}

	public MapTableData FindMapTableData(string mapId)
	{
		for (int i = 0; i < mapTable.dataArray.Length; ++i)
		{
			if (mapTable.dataArray[i].mapId == mapId)
				return mapTable.dataArray[i];
		}
		return null;
	}

	public ChapterTableData FindChapterTableData(int chapter)
	{
		for (int i = 0; i < chapterTable.dataArray.Length; ++i)
		{
			if (chapterTable.dataArray[i].chapter == chapter)
				return chapterTable.dataArray[i];
		}
		return null;
	}

	public MapSetTableData FindMapSetTableData(string mapSetId)
	{
		for (int i = 0; i < mapSetTable.dataArray.Length; ++i)
		{
			if (mapSetTable.dataArray[i].mapSetId == mapSetId)
				return mapSetTable.dataArray[i];
		}
		return null;
	}

	public StagePenaltyTableData FindStagePenaltyTableData(string stagePenaltyId)
	{
		for (int i = 0; i < stagePenaltyTable.dataArray.Length; ++i)
		{
			if (stagePenaltyTable.dataArray[i].stagePenaltyId == stagePenaltyId)
				return stagePenaltyTable.dataArray[i];
		}
		return null;
	}

	public MonsterTableData FindMonsterTableData(string monsterId)
	{
		for (int i = 0; i < monsterTable.dataArray.Length; ++i)
		{
			if (monsterTable.dataArray[i].monsterId == monsterId)
				return monsterTable.dataArray[i];
		}
		return null;
	}

	public ActorTableData FindActorTableData(string actorId)
	{
		for (int i = 0; i < actorTable.dataArray.Length; ++i)
		{
			if (actorTable.dataArray[i].actorId == actorId)
				return actorTable.dataArray[i];
		}
		return null;
	}

	public PowerLevelTableData FindPowerLevelTableData(int powerLevel)
	{
		for (int i = 0; i < powerLevelTable.dataArray.Length; ++i)
		{
			if (powerLevelTable.dataArray[i].powerLevel == powerLevel)
				return powerLevelTable.dataArray[i];
		}
		return null;
	}

	public SkillTableData FindSkillTableData(string skillId)
	{
		for (int i = 0; i < skillTable.dataArray.Length; ++i)
		{
			if (skillTable.dataArray[i].id == skillId)
				return skillTable.dataArray[i];
		}
		return null;
	}

	public SkillLevelTableData FindSkillLevelTableData(string skillId, int level)
	{
		for (int i = 0; i < skillLevelTable.dataArray.Length; ++i)
		{
			if (skillLevelTable.dataArray[i].skillId == skillId && skillLevelTable.dataArray[i].level == level)
				return skillLevelTable.dataArray[i];
		}
		return null;
	}

	public ConditionValueTableData FindConditionValueTableData(string id)
	{
		for (int i = 0; i < conditionValueTable.dataArray.Length; ++i)
		{
			if (conditionValueTable.dataArray[i].id == id)
				return conditionValueTable.dataArray[i];
		}
		return null;
	}

	public LevelPackTableData FindLevelPackTableData(string levelPackId)
	{
		for (int i = 0; i < levelPackTable.dataArray.Length; ++i)
		{
			if (levelPackTable.dataArray[i].levelPackId == levelPackId)
				return levelPackTable.dataArray[i];
		}
		return null;
	}

	public LevelPackLevelTableData FindLevelPackLevelTableData(string levelPackId, int level)
	{
		for (int i = 0; i < levelPackLevelTable.dataArray.Length; ++i)
		{
			if (levelPackLevelTable.dataArray[i].levelPackId == levelPackId && levelPackLevelTable.dataArray[i].level == level)
				return levelPackLevelTable.dataArray[i];
		}
		return null;
	}

	public float GetGlobalConstantFloat(string id)
	{
		for (int i = 0; i < globalConstantFloatTable.dataArray.Length; ++i)
		{
			if (globalConstantFloatTable.dataArray[i].id == id)
				return globalConstantFloatTable.dataArray[i].value;
		}
		return 0.0f;
	}

	public int GetGlobalConstantInt(string id)
	{
		for (int i = 0; i < globalConstantIntTable.dataArray.Length; ++i)
		{
			if (globalConstantIntTable.dataArray[i].id == id)
				return globalConstantIntTable.dataArray[i].value;
		}
		return 0;
	}

	public string GetGlobalConstantString(string id)
	{
		for (int i = 0; i < globalConstantStringTable.dataArray.Length; ++i)
		{
			if (globalConstantStringTable.dataArray[i].id == id)
				return globalConstantStringTable.dataArray[i].value;
		}
		return "";
	}

	public DropTableData FindDropTableData(string dropId)
	{
		for (int i = 0; i < dropTable.dataArray.Length; ++i)
		{
			if (dropTable.dataArray[i].dropId == dropId)
				return dropTable.dataArray[i];
		}
		return null;
	}

	public EquipTableData FindEquipTableData(string equipId)
	{
		for (int i = 0; i < equipTable.dataArray.Length; ++i)
		{
			if (equipTable.dataArray[i].equipId == equipId)
				return equipTable.dataArray[i];
		}
		return null;
	}

	public InnerGradeTableData FindInnerGradeTableData(int innerGrade)
	{
		for (int i = 0; i < innerGradeTable.dataArray.Length; ++i)
		{
			if (innerGradeTable.dataArray[i].innerGrade == innerGrade)
				return innerGradeTable.dataArray[i];
		}
		return null;
	}

	public EnhanceTableData FindEnhanceTableData(int innerGrade, int enhance)
	{
		for (int i = 0; i < enhanceTable.dataArray.Length; ++i)
		{
			if (enhanceTable.dataArray[i].innerGrade == innerGrade && enhanceTable.dataArray[i].enhance == enhance)
				return enhanceTable.dataArray[i];
		}
		return null;
	}

	public OptionTableData FindOptionTableData(string optionString, int innerGrade)
	{
		for (int i = 0; i < optionTable.dataArray.Length; ++i)
		{
			if (optionTable.dataArray[i].innerGrade == innerGrade && optionTable.dataArray[i].option == optionString)
				return optionTable.dataArray[i];
		}
		return null;
	}

	public TransferTableData FindTransferTableData(int innerGrade, int enhance)
	{
		for (int i = 0; i < transferTable.dataArray.Length; ++i)
		{
			if (transferTable.dataArray[i].innerGrade == innerGrade && transferTable.dataArray[i].enhance == enhance)
				return transferTable.dataArray[i];
		}
		return null;
	}

	public RemainTableData FindRemainTableData(int remainCount)
	{
		for (int i = 0; i < remainTable.dataArray.Length; ++i)
		{
			if (remainTable.dataArray[i].remainMin <= remainCount)
				return remainTable.dataArray[i];
		}
		return null;
	}

	public float FindNotStreakAdjustWeight(int count)
	{
		for (int i = notStreakTable.dataArray.Length - 1; i >= 0; --i)
		{
			if (notStreakTable.dataArray[i].accumulateMin <= count)
				return notStreakTable.dataArray[i].adjustWeight;
		}
		return 0.0f;
	}

	public float FindNotCharAdjustProb(int count)
	{
		for (int i = notCharTable.dataArray.Length - 1; i >= 0; --i)
		{
			if (notCharTable.dataArray[i].accumulateMin <= count)
				return notCharTable.dataArray[i].adjustProb;
		}
		return 0.0f;
	}

	public DamageRateTableData FindDamageTableData(string type, int addCount, string actorId)
	{
		for (int i = 0; i < damageRateTable.dataArray.Length; ++i)
		{
			if (damageRateTable.dataArray[i].overrideActorId == actorId && damageRateTable.dataArray[i].number == addCount && damageRateTable.dataArray[i].id == type)
				return damageRateTable.dataArray[i];
		}
		for (int i = 0; i < damageRateTable.dataArray.Length; ++i)
		{
			if (damageRateTable.dataArray[i].number == addCount && damageRateTable.dataArray[i].id == type)
				return damageRateTable.dataArray[i];
		}
		return null;
	}

	public ActorInfoTableData FindActorInfoTableData(string actorId)
	{
		for (int i = 0; i < actorInfoTable.dataArray.Length; ++i)
		{
			if (actorInfoTable.dataArray[i].actorId == actorId)
				return actorInfoTable.dataArray[i];
		}
		return null;
	}

	public string FindActorLevelPackByLevel(string actorId, int level)
	{
		for (int i = 0; i < actorLevelPackTable.dataArray.Length; ++i)
		{
			if (actorLevelPackTable.dataArray[i].level == level && actorLevelPackTable.dataArray[i].actorId == actorId)
				return actorLevelPackTable.dataArray[i].levelPack;
		}
		return null;
	}

	public ShopBoxTableData FindShopBoxTableData(string boxId)
	{
		for (int i = 0; i < shopBoxTable.dataArray.Length; ++i)
		{
			if (shopBoxTable.dataArray[i].boxPack == boxId)
				return shopBoxTable.dataArray[i];
		}
		return null;
	}

	public ShopLevelPackageTableData FindShopLevelPackageTableData(int level)
	{
		for (int i = 0; i < shopLevelPackageTable.dataArray.Length; ++i)
		{
			if (shopLevelPackageTable.dataArray[i].level == level)
				return shopLevelPackageTable.dataArray[i];
		}
		return null;
	}

	public ResearchTableData FindResearchTableData(int level)
	{
		for (int i = 0; i < researchTable.dataArray.Length; ++i)
		{
			if (researchTable.dataArray[i].level == level)
				return researchTable.dataArray[i];
		}
		return null;
	}
}
