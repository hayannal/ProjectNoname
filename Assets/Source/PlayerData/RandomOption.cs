using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public static class RandomOption
{
	public enum eRandomCalculateType
	{
		UnityRandom,
		Curve,
		Exponential,
		Linear,
		Distribution,
	}

	public static int RandomOptionCountMax = 3;

	public static float GetRandomRange(float min, float max, eRandomCalculateType randType, float f1, RandomFromDistribution.Direction_e direction)
	{
		switch (randType)
		{
			case eRandomCalculateType.UnityRandom:
				return Random.Range(min, max);
			case eRandomCalculateType.Curve:
				return RandomFromDistribution.RandomRangeSlope(min, max, f1, direction);
			case eRandomCalculateType.Exponential:
				return RandomFromDistribution.RandomRangeExponential(min, max, f1, direction);
			case eRandomCalculateType.Linear:
				return RandomFromDistribution.RandomRangeLinear(min, max, f1);
			case eRandomCalculateType.Distribution:
				return RandomFromDistribution.RandomRangeNormalDistribution(min, max, RandomFromDistribution.ConfidenceLevel_e._999);
		}
		return 0.0f;
	}

	public static float GetRandomEquipMainOption(EquipTableData equipTableData)
	{
		float result = GetRandomRange(equipTableData.min, equipTableData.max, (eRandomCalculateType)equipTableData.randType, equipTableData.f1,
			(equipTableData.leftRight == 1) ? RandomFromDistribution.Direction_e.Left : RandomFromDistribution.Direction_e.Right);
		return (float)(System.Math.Truncate(result * 10000.0) / 10000.0);
	}

	public static float GetRandomEquipSubOption(OptionTableData optionTableData)
	{
		float result = GetRandomRange(optionTableData.min, optionTableData.max, (eRandomCalculateType)optionTableData.randType, optionTableData.f1,
			(optionTableData.leftRight == 1) ? RandomFromDistribution.Direction_e.Left : RandomFromDistribution.Direction_e.Right);
		return (float)(System.Math.Truncate(result * 10000.0) / 10000.0);
	}

	static List<float> _listSumRandomOptionWeight;
	public static int GetRandomOptionCount(int innerGrade)
	{
		if (_listSumRandomOptionWeight == null)
			_listSumRandomOptionWeight = new List<float>();
		_listSumRandomOptionWeight.Clear();

		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(innerGrade);
		if (innerGradeTableData == null)
			return 0;

		float sumWeight = innerGradeTableData.zeroOptionWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.oneOptionWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.twoOptionWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.threeOptionWeight;
		_listSumRandomOptionWeight.Add(sumWeight);

		int index = -1;
		float result = Random.Range(0.0f, sumWeight);
		for (int i = 0; i < _listSumRandomOptionWeight.Count; ++i)
		{
			if (result <= _listSumRandomOptionWeight[i])
			{
				index = i;
				break;
			}
		}
		return (index == -1) ? 0 : index;
	}

	public static int GetTransmuteRemainCount(int innerGrade)
	{
		if (_listSumRandomOptionWeight == null)
			_listSumRandomOptionWeight = new List<float>();
		_listSumRandomOptionWeight.Clear();

		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(innerGrade);
		if (innerGradeTableData == null)
			return 0;

		float sumWeight = innerGradeTableData.zeroTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.oneTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.twoTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.threeTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.fourTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.fiveTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.sixTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);
		sumWeight += innerGradeTableData.sevenTransmuteWeight;
		_listSumRandomOptionWeight.Add(sumWeight);

		int index = -1;
		float result = Random.Range(0.0f, sumWeight);
		for (int i = 0; i < _listSumRandomOptionWeight.Count; ++i)
		{
			if (result <= _listSumRandomOptionWeight[i])
			{
				index = i;
				break;
			}
		}
		return (index == -1) ? 0 : index;
	}

	class RandomOptionData
	{
		public OptionTableData optionTableData;
		public float sumWeight;
	}
	static List<RandomOptionData> _listRandomOptionInfo;
	public static void GenerateRandomOption(int optionType, int innerGrade, ref eActorStatus eType, ref float value)
	{
		if (_listRandomOptionInfo == null)
			_listRandomOptionInfo = new List<RandomOptionData>();
		_listRandomOptionInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.optionTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.optionTable.dataArray[i].optionType != optionType)
				continue;
			if (TableDataManager.instance.optionTable.dataArray[i].innerGrade != innerGrade)
				continue;

			sumWeight += TableDataManager.instance.optionTable.dataArray[i].createWeight;
			RandomOptionData newInfo = new RandomOptionData();
			newInfo.optionTableData = TableDataManager.instance.optionTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listRandomOptionInfo.Add(newInfo);
		}

		int index = -1;
		float result = Random.Range(0.0f, sumWeight);
		for (int i = 0; i < _listRandomOptionInfo.Count; ++i)
		{
			if (result <= _listRandomOptionInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index != -1)
		{
			OptionTableData optionTableData = _listRandomOptionInfo[index].optionTableData;
			System.Enum.TryParse<eActorStatus>(optionTableData.option, out eType);
			value = GetRandomEquipSubOption(optionTableData);
		}
	}
}