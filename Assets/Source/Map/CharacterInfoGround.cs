using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInfoGround : MonoBehaviour
{
	public static CharacterInfoGround instance;

	public Collider planeCollider;
	public GameObject stoneObject;

	void Awake()
	{
		instance = this;
	}
}