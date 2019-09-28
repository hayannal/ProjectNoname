using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour {

	public enum eLineRendererType
	{
		None,
		LineRenderer,
		RayDesigner,
	}

	#region staticFunction
	public static void ShowHitEffect(MeHitObject meHit, Vector3 contactPoint, Vector3 contactNormal, int weaponIDAtCreation)
	{
		if (meHit.useWeaponHitEffect && weaponIDAtCreation != 0)
		{
			/*
			int weaponID = weaponIDAtCreation;
			string key = string.Format("id{0}", weaponID);
			Google2u.WeaponRow weaponRow = Google2u.Weapon.Instance.GetRow(key);
			if (weaponRow != null)
			{
				key = string.Format("id{0}", weaponRow._HitEffectID);
				Google2u.HitEffectRow hitEffectRow = Google2u.HitEffect.Instance.GetRow(key);
				if (hitEffectRow != null)
				{
					GameObject orig = null;
					if (Random.value < 0.6f) orig = AssetBundleManager.LoadAsset<GameObject>("effect.unity3d", hitEffectRow._Normal_Prefab);
					else orig = AssetBundleManager.LoadAsset<GameObject>("effect.unity3d", hitEffectRow._Critical_Prefab);
					if (orig != null)
						Instantiate(orig, contactPoint, Quaternion.identity);
				}
			}
			*/
		}
		else
		{
			if (meHit.hitEffectObject != null)
			{
				var instance = BattleInstanceManager.instance.GetCachedObject(meHit.hitEffectObject, contactPoint, Quaternion.identity);
				if (meHit.hitEffectLookAtNormal)
					instance.transform.LookAt(contactPoint + contactNormal);
			}
		}
	}

	public static void ShowHitEffectLineRenderer(MeHitObject meHit, Vector3 startPoint, Vector3 contactPoint)
	{
		switch (meHit.hitEffectLineRendererType)
		{
			case eLineRendererType.LineRenderer:
				LineRenderer lineRenderer = BattleInstanceManager.instance.GetCachedLineRenderer(meHit.hitEffectLineRendererObject, startPoint, Quaternion.identity);
				lineRenderer.SetPosition(0, startPoint);
				lineRenderer.SetPosition(1, contactPoint);
				break;
			case eLineRendererType.RayDesigner:
				RayDesigner rayDesigner = BattleInstanceManager.instance.GetCachedRayDesigner(meHit.hitEffectLineRendererObject, startPoint, Quaternion.identity);
				rayDesigner.IsDynamic = false;
				rayDesigner.UpdateStartPosition(startPoint, startPoint + Vector3.up);
				rayDesigner.UpdateTargetPosition(contactPoint, contactPoint + Vector3.up);
				rayDesigner.UpdateMesh();
				break;
		}
	}
	#endregion
}
