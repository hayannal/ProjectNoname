using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class TimeSpacePortal : MonoBehaviour
{
	Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, -75.0f);

	void OnTriggerEnter(Collider other)
	{
		if (_processing)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		Timing.RunCoroutine(MoveProcess());
	}

	GameObject _timeSpaceGroundPrefab = null;
	bool _processing = false;
	IEnumerator<float> MoveProcess()
	{
		if (_processing)
			yield break;

		_processing = true;
		if (_timeSpaceGroundPrefab == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("TimeSpaceGround", "Map", (prefab) =>
			{
				_timeSpaceGroundPrefab = prefab;
			});
		}

		yield return Timing.WaitForSeconds(0.2f);
		//changeEffectParticleRootObject.SetActive(true);

		FadeCanvas.instance.FadeOut(0.2f, 0.7f);
		yield return Timing.WaitForSeconds(0.2f);

		if (MainSceneBuilder.instance.lobby)
		{
			while (MainSceneBuilder.instance.IsDoneLateInitialized() == false)
				yield return Timing.WaitForOneFrame;
			if (TitleCanvas.instance != null)
				TitleCanvas.instance.gameObject.SetActive(false);
		}
		if (TimeSpaceGround.instance == null)
		{
			while (_timeSpaceGroundPrefab == null)
				yield return Timing.WaitForOneFrame;
			Instantiate<GameObject>(_timeSpaceGroundPrefab, _rootOffsetPosition, Quaternion.identity);
			TimeSpaceGround.instance.SetOrigTimeSpacePortal(this);
		}
		else
		{
			TimeSpaceGround.instance.gameObject.SetActive(!TimeSpaceGround.instance.gameObject.activeSelf);
		}

		FadeCanvas.instance.FadeIn(0.4f);

		_processing = false;
	}
}