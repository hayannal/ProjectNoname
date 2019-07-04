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
	public MonsterTable monsterTable;

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
}
