using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class DieAshParticle : MonoBehaviour
{
	public static void ShowParticle(Transform rootTransform, bool bossMonster, float flakeMultiplier)
	{
		DieAshParticle dieAshParticle = rootTransform.GetComponent<DieAshParticle>();
		if (dieAshParticle == null) dieAshParticle = rootTransform.gameObject.AddComponent<DieAshParticle>();
		dieAshParticle.ShowParticle(bossMonster, flakeMultiplier);
	}

	List<AshParticle> _listAshParticle;
	public void ShowParticle(bool bossMonster, float flakeMultiplier)
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
				ashParticle.SetParticleInfo(skinnedMeshRendererList[i], duration, flakeMultiplier);
				_listAshParticle.Add(ashParticle);
			}

			if (skinnedMeshRendererList.Length == 0)	
			{
				MeshRenderer[] meshRendererList = GetComponentsInChildren<MeshRenderer>();
				if (meshRendererList.Length > 0)
				{
					AnimationCurveAsset curveAsset = bossMonster ? BattleManager.instance.bossMonsterDieDissolveCurve : BattleManager.instance.monsterDieDissolveCurve;
					duration = curveAsset.curve.keys[curveAsset.curve.length - 1].time;

					for (int i = 0; i < meshRendererList.Length; ++i)
					{
						GameObject newObject = BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.monsterDieAshParticlePrefab, meshRendererList[i].transform);
						AshParticle ashParticle = newObject.GetComponent<AshParticle>();
						ashParticle.SetParticleInfo(meshRendererList[i], duration, flakeMultiplier);
						_listAshParticle.Add(ashParticle);
					}
				}
			}
		}

		for (int i = 0; i < _listAshParticle.Count; ++i)
			_listAshParticle[i].Play();
	}
}
