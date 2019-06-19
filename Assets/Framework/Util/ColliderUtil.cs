using UnityEngine;
using System.Collections;

public class ColliderUtil : MonoBehaviour {

	static public float GetRadius(Collider col)
	{
		float colliderRadius = -1.0f;
		if (col is CharacterController) colliderRadius = ((CharacterController)col).radius;
		else if (col is CapsuleCollider) colliderRadius = ((CapsuleCollider)col).radius;
		else if (col is SphereCollider) colliderRadius = ((SphereCollider)col).radius;
		else if (col is BoxCollider) colliderRadius = ((BoxCollider)col).size.y;
		return colliderRadius;
	}

	static public float GetHeight(Collider col)
	{
		float colliderHeight = -1.0f;
		if (col is CharacterController) colliderHeight = ((CharacterController)col).height;
		else if (col is CapsuleCollider) colliderHeight = ((CapsuleCollider)col).height;
		else if (col is SphereCollider) colliderHeight = ((SphereCollider)col).radius;
		else if (col is BoxCollider) colliderHeight = ((BoxCollider)col).size.y;
		return colliderHeight;
	}

	static public float GetCenterY(Collider col)
	{
		float colliderCenterY = -1.0f;
		if (col is CharacterController) colliderCenterY = ((CharacterController)col).center.y;
		else if (col is CapsuleCollider) colliderCenterY = ((CapsuleCollider)col).center.y;
		else if (col is SphereCollider) colliderCenterY = ((SphereCollider)col).center.y;
		else if (col is BoxCollider) colliderCenterY = ((BoxCollider)col).center.y;
		return colliderCenterY;
	}
}
