using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class DieAshParticle : MonoBehaviour
{
	public static void ShowParticle(Transform rootTransform, bool bossMonster)
	{
		DieAshParticle dieAshParticle = rootTransform.GetComponent<DieAshParticle>();
		if (dieAshParticle == null) dieAshParticle = rootTransform.gameObject.AddComponent<DieAshParticle>();
		dieAshParticle.ShowParticle(bossMonster);
	}

	List<AshParticle> _listAshParticle;
	public void ShowParticle(bool bossMonster)
	{
		if (_listAshParticle == null)
		{
			_listAshParticle = new List<AshParticle>();
			SkinnedMeshRenderer[] skinnedMeshRendererList = GetComponentsInChildren<SkinnedMeshRenderer>();

			float duration = 0.0f;
			if (skinnedMeshRendererList.Length > 0)
			{
				AnimationCurveAsset curveAsset = bossMonster ? BattleManager.instance.bossMonsterDieDissolveCurve : BattleManager.instance.monsterDieDissolveCurve;
				duration = curveAsset.curve.keys[curveAsset.curve.length - 1].time;
			}
			for (int i = 0; i < skinnedMeshRendererList.Length; ++i)
			{
				GameObject newObject = BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.monsterDieAshParticlePrefab, skinnedMeshRendererList[i].transform);
				AshParticle ashParticle = newObject.GetComponent<AshParticle>();
				ashParticle.SetParticleInfo(skinnedMeshRendererList[i], duration);
				_listAshParticle.Add(ashParticle);
			}
		}

		for (int i = 0; i < _listAshParticle.Count; ++i)
			_listAshParticle[i].Play();
	}
}
