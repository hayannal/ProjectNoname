using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearStageSubTrigger : MonoBehaviour
{
	void OnEnable()
	{
		_checkedTrigger = false;

		// ClearStageTrigger 구조에서 변형해서 처리한다.
		if (ClientSaveData.instance.IsLoadingInProgressGame())
		{
			if (ClientSaveData.instance.GetCachedGatePillar())
				_checkedTrigger = true;
		}

		if (_checkedTrigger)
			return;

		AddressableAssetLoadManager.GetAddressableGameObject("DragGuideCanvas", "Canvas", (prefab) =>
		{
			_dragGuideCanvasObject = Instantiate<GameObject>(prefab);
		});
	}

	GameObject _dragGuideCanvasObject;
	bool _checkedTrigger;
	void OnTriggerEnter(Collider other)
	{
		if (_checkedTrigger)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		if (_dragGuideCanvasObject != null)
			_dragGuideCanvasObject.SetActive(false);

		_checkedTrigger = true;
	}
}