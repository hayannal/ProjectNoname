using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GiveAffectorValueAffector : AffectorBase
{
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.IsMonsterActor() == false)
			return;

		MonsterActor monsterActor = _actor as MonsterActor;
		if (monsterActor == null)
			return;

		int liveMonsterCount = 0;
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			if (listMonsterActor[i].actorStatus.IsDie())
				continue;
			if (listMonsterActor[i].team.teamId != (int)Team.eTeamID.DefaultMonster || listMonsterActor[i].excludeMonsterCount)
				continue;
			if (listMonsterActor[i].actorId == monsterActor.actorId)
				continue;

			++liveMonsterCount;
		}

		// 정상으로 깰때는 아무 메세지도 보여주지 않는다.
		if (liveMonsterCount == 0)
			return;



		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			if (listMonsterActor[i].actorStatus.IsDie())
				continue;
			if (listMonsterActor[i].team.teamId != (int)Team.eTeamID.DefaultMonster || listMonsterActor[i].excludeMonsterCount)
				continue;
			if (listMonsterActor[i].actorId == monsterActor.actorId)
				continue;

			if (affectorValueLevelTableData.sValue2.Contains(","))
			{
				string[] affectorValueIdList = BattleInstanceManager.instance.GetCachedString2StringList(affectorValueLevelTableData.sValue2);
				for (int j = 0; j < affectorValueIdList.Length; ++j)
				{
					if (affectorValueLevelTableData.iValue1 == 0)
						listMonsterActor[i].affectorProcessor.ApplyAffectorValue(affectorValueIdList[j], hitParameter, false);
					else if (affectorValueLevelTableData.iValue1 == 1)
						listMonsterActor[i].affectorProcessor.AddActorState(affectorValueIdList[j], hitParameter);
				}
			}
			else
			{
				if (affectorValueLevelTableData.iValue1 == 0)
					listMonsterActor[i].affectorProcessor.ApplyAffectorValue(affectorValueLevelTableData.sValue2, hitParameter, false);
				else if (affectorValueLevelTableData.iValue1 == 1)
					listMonsterActor[i].affectorProcessor.AddActorState(affectorValueLevelTableData.sValue2, hitParameter);
			}
		}

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue1) == false)
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString(affectorValueLevelTableData.sValue1), 2.5f);

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
		{
			GameObject effectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
			if (effectPrefab != null)
				BattleInstanceManager.instance.GetCachedObject(effectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation);
		}
	}
}