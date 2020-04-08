using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		return (float)(System.Math.Truncate(result * 1000.0) / 1000.0);
	}
}