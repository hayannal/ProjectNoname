using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropObjectGroup : MonoBehaviour
{
	public static DropObjectGroup instance = null;

	public GameObject[] dropObjectPrefabList;
	public GameObject[] lootEffectPrefabList;
	public GameObject[] lootEndEffectPrefabList;

	void Awake()
	{
		instance = this;
	}
}