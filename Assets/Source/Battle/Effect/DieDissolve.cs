using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class DieDissolve : MonoBehaviour
{
	static int s_dissolvePropertyID;
	static int s_cutoffPropertyID;

	public static void ShowDieDissolve(Transform rootTransform, bool bossMonster)
	{
		if (s_dissolvePropertyID == 0) s_dissolvePropertyID = Shader.PropertyToID("_UseDissolve");
		if (s_cutoffPropertyID == 0) s_cutoffPropertyID = Shader.PropertyToID("_EdgeCutoff");

		DieDissolve dieDissolve = rootTransform.GetComponent<DieDissolve>();
		if (dieDissolve == null) dieDissolve = rootTransform.gameObject.AddComponent<DieDissolve>();
		dieDissolve.Dissolve(bossMonster);
	}

	List<Material> _listCachedMaterial;
	float _dissolveStartTime;
	bool _bossMonster;
	public void Dissolve(bool bossMonster)
	{
		enabled = true;

		if (_listCachedMaterial == null)
		{
			_listCachedMaterial = new List<Material>();
			Renderer[] renderers = GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderers.Length; ++i)
			{
				for (int j = 0; j < renderers[i].materials.Length; ++j)
				{
					if (renderers[i].materials[j].HasProperty(s_dissolvePropertyID))
						_listCachedMaterial.Add(renderers[i].materials[j]);
				}
			}
		}

		if (_listCachedMaterial.Count == 0)
		{
			//Debug.Log("")
			return;
		}

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].EnableKeyword("_DISSOLVE");
		}

		_bossMonster = bossMonster;
		_dissolveStartTime = Time.time;
		_updateDissolveCutoff = true;
	}

	bool _updateDissolveCutoff = false;
	void Update()
	{
		if (!_updateDissolveCutoff)
			return;

		float fTime = Time.time - _dissolveStartTime;

		AnimationCurveAsset curveAsset = _bossMonster ? BattleManager.instance.bossMonsterDieDissolveCurve : BattleManager.instance.monsterDieDissolveCurve;
		if (fTime > curveAsset.curve.keys[curveAsset.curve.length - 1].time)
		{
			ResetDissolve();
			enabled = false;
			gameObject.SetActive(false);
			return;
		}
		float result = curveAsset.curve.Evaluate(fTime);

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].SetFloat(s_cutoffPropertyID, result);
		}
	}

	void ResetDissolve()
	{
		if (_listCachedMaterial == null)
			return;
		if (_listCachedMaterial.Count == 0)
			return;

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].DisableKeyword("_DISSOLVE");
			_listCachedMaterial[i].SetFloat(s_cutoffPropertyID, 0.0f);
		}
		_updateDissolveCutoff = false;
	}
}
