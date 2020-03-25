using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInfoGround : MonoBehaviour
{
	public static CharacterInfoGround instance;

	public Collider planeCollider;

	void Awake()
	{
		instance = this;
	}
}