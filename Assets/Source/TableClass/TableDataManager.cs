﻿using System.Collections;
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
	public NotLegendCharTable notLegendCharTable;
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
	public ShopReturnScrollTable shopReturnScrollTable;
	public ResearchTable researchTable;
	public ExtraStatTable extraStatTable;
	public WingLookTable wingLookTable;
	public WingPowerTable wingPowerTable;
	public NodeWarTable nodeWarTable;
	public NodeWarSpawnTable nodeWarSpawnTable;
	public NodeWarTrapTable nodeWarTrapTable;
	public ChapterTrapTable chapterTrapTable;
	public SubQuestTable subQuestTable;
	public GuideQuestTable guideQuestTable;
	public BossBattleTable bossBattleTable;
	public BossExpTable bossExpTable;
	public BossRewardTable bossRewardTable;
	public InvasionTable invasionTable;
	public AnalysisTable analysisTable;
	public AnalysisKeyTable analysisKeyTable;

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

	public float FindNotLegendCharAdjustWeight(int count)
	{
		for (int i = notLegendCharTable.dataArray.Length - 1; i >= 0; --i)
		{
			if (notLegendCharTable.dataArray[i].accumulateMin <= count)
				return notLegendCharTable.dataArray[i].adjustWeight;
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

	public ExtraStatTableData FindExtraStatsTableData(CharacterInfoStatsCanvas.eStatsType statsType, int point)
	{
		for (int i = 0; i < extraStatTable.dataArray.Length; ++i)
		{
			if (extraStatTable.dataArray[i].extraStatId == (int)statsType && extraStatTable.dataArray[i].level == point)
				return extraStatTable.dataArray[i];
		}
		return null;
	}

	public WingLookTableData FindWingLookTableData(int lookId)
	{
		for (int i = 0; i < wingLookTable.dataArray.Length; ++i)
		{
			if (wingLookTable.dataArray[i].wingLookId == lookId)
				return wingLookTable.dataArray[i];
		}
		return null;
	}

	public WingPowerTableData FindWingPowerTableData(int wingType, int grade)
	{
		for (int i = 0; i < wingPowerTable.dataArray.Length; ++i)
		{
			if (wingPowerTable.dataArray[i].wingType == wingType && wingPowerTable.dataArray[i].grade == grade)
				return wingPowerTable.dataArray[i];
		}
		return null;
	}

	public NodeWarTableData FindNodeWarTableData(int level)
	{
		for (int i = 0; i < nodeWarTable.dataArray.Length; ++i)
		{
			if (nodeWarTable.dataArray[i].level == level)
				return nodeWarTable.dataArray[i];
		}
		return null;
	}

	public NodeWarSpawnTableData FindNodeWarSpawnTableData(string monsterId)
	{
		for (int i = 0; i < nodeWarSpawnTable.dataArray.Length; ++i)
		{
			if (nodeWarSpawnTable.dataArray[i].monsterId == monsterId)
				return nodeWarSpawnTable.dataArray[i];
		}
		return null;
	}

	public ChapterTrapTableData FindChapterTrapTableData(int chapter, bool lastStage)
	{
		for (int i = 0; i < chapterTrapTable.dataArray.Length; ++i)
		{
			if (chapterTrapTable.dataArray[i].chapter == chapter)
			{
				if ((lastStage && chapterTrapTable.dataArray[i].last == 1) || (lastStage == false && chapterTrapTable.dataArray[i].last == 0))
					return chapterTrapTable.dataArray[i];
			}
		}
		return null;
	}

	public SubQuestTableData FindSubQuestTableData(string type)
	{
		for (int i = 0; i < subQuestTable.dataArray.Length; ++i)
		{
			if (subQuestTable.dataArray[i].type == type)
				return subQuestTable.dataArray[i];
		}
		return null;
	}

	public GuideQuestTableData FindGuideQuestTableData(int index)
	{
		for (int i = 0; i < guideQuestTable.dataArray.Length; ++i)
		{
			if (guideQuestTable.dataArray[i].id == index)
				return guideQuestTable.dataArray[i];
		}
		return null;
	}

	public BossBattleTableData FindBossBattleData(int id)
	{
		for (int i = 0; i < bossBattleTable.dataArray.Length; ++i)
		{
			if (bossBattleTable.dataArray[i].num == id)
				return bossBattleTable.dataArray[i];
		}
		return null;
	}

	public BossRewardTableData FindBossRewardData(int id, int difficulty)
	{
		for (int i = 0; i < bossRewardTable.dataArray.Length; ++i)
		{
			if (bossRewardTable.dataArray[i].num == id && bossRewardTable.dataArray[i].difficulty == difficulty)
				return bossRewardTable.dataArray[i];
		}
		return null;
	}

	public InvasionTableData FindInvasionTableData(int dayOfWeek, int difficulty)
	{
		for (int i = 0; i < invasionTable.dataArray.Length; ++i)
		{
			if (invasionTable.dataArray[i].dayWeek == dayOfWeek && invasionTable.dataArray[i].hard == difficulty)
				return invasionTable.dataArray[i];
		}
		return null;
	}

	public AnalysisTableData FindAnalysisTableData(int level)
	{
		for (int i = 0; i < analysisTable.dataArray.Length; ++i)
		{
			if (analysisTable.dataArray[i].level == level)
				return analysisTable.dataArray[i];
		}
		return null;
	}

	public AnalysisKeyTableData FindAnalysisKeyTableData(int remain)
	{
		for (int i = 0; i < analysisKeyTable.dataArray.Length; ++i)
		{
			if (analysisKeyTable.dataArray[i].remainMin == remain)
				return analysisKeyTable.dataArray[i];
		}
		return null;
	}
}
