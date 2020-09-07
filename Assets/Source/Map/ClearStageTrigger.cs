using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearStageTrigger : MonoBehaviour
{
	void OnEnable()
	{
		_spawnedGatePillar = false;

		// ClientSaveData에 새로운 타입을 추가해서 저장할까 하다가 게이트필라가 보여진 상태라면 동작하지 않으면 되는거라 게이트필라 여부를 가져다쓰기로 한다.
		if (ClientSaveData.instance.IsLoadingInProgressGame())
		{
			if (ClientSaveData.instance.GetCachedGatePillar())
				_spawnedGatePillar = true;
		}
	}

	bool _spawnedGatePillar;
	void OnTriggerEnter(Collider other)
	{
		if (_spawnedGatePillar)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		// MultipleLineIndicatorCanvas에 적혀있는 문구를 바꿔준다.
		if (MultipleLineIndicatorCanvas.instance != null && MultipleLineIndicatorCanvas.instance.gameObject.activeSelf)
		{
			MultipleLineIndicatorCanvas.instance.gameObject.SetActive(false);
			MultipleLineIndicatorCanvas.instance.contextText.SetLocalizedText(UIString.instance.GetString("Tutorial_GatePillarMapClear"));
			MultipleLineIndicatorCanvas.instance.gameObject.SetActive(true);
		}

		BattleManager.instance.OnClearStage();
		_spawnedGatePillar = true;
	}
}