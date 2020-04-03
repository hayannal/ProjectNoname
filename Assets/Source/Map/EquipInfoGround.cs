using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipInfoGround : MonoBehaviour
{
	public static EquipInfoGround instance;

	public GameObject altarObject;
	public GameObject emptyAltarObject;

	void Awake()
	{
		instance = this;
	}
}