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
	public StageTable stageTable;
	public MapTable mapTable;
	public MonsterTable monsterTable;
	public ActorPowerLevelTable actorPowerLevelTable;

	void Awake()
	{
		instance = this;
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

	public StageTableData FindStageTableData(int chapter, int stage)
	{
		for (int i = 0; i < stageTable.dataArray.Length; ++i)
		{
			if (stageTable.dataArray[i].chapter == chapter && stageTable.dataArray[i].stage == stage)
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

	public MonsterTableData FindMonsterTableData(string monsterId)
	{
		for (int i = 0; i < monsterTable.dataArray.Length; ++i)
		{
			if (monsterTable.dataArray[i].monsterId == monsterId)
				return monsterTable.dataArray[i];
		}
		return null;
	}

	public ActorPowerLevelTableData FindActorPowerLevelTableData(string actorId, int powerLevel)
	{
		for (int i = 0; i < actorPowerLevelTable.dataArray.Length; ++i)
		{
			if (actorPowerLevelTable.dataArray[i].actorId == actorId && actorPowerLevelTable.dataArray[i].powerLevel == powerLevel)
				return actorPowerLevelTable.dataArray[i];
		}
		return null;
	}
}
