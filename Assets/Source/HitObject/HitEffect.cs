using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour {

	#region staticFunction
	public static void ShowHitEffect(AffectorProcessor affectorProcessor, MeHitObject meHit, Vector3 contactPoint, int weaponIDAtCreation)
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
				Instantiate(meHit.hitEffectObject, contactPoint, Quaternion.identity);
		}
	}
	#endregion
}
