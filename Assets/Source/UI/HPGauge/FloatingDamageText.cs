using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingDamageText : MonoBehaviour
{
	public enum eFloatingDamageType
	{
		Miss,
		Invincible,
		Evade,
		Headshot,
	}

	public Text damageText;

	public void InitializeText(eFloatingDamageType floatingDamageType, Actor actor)
	{
		switch (floatingDamageType)
		{
			case eFloatingDamageType.Miss:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Miss"));
				break;
			case eFloatingDamageType.Invincible:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Invincible"));
				break;
			case eFloatingDamageType.Evade:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Evade"));
				break;
			case eFloatingDamageType.Headshot:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Headshot"));
				break;
		}

		Vector3 desiredPosition = actor.cachedTransform.position;
		desiredPosition.y += ColliderUtil.GetHeight(actor.GetCollider());
		desiredPosition.y += actor.gaugeOffsetY;
		cachedTransform.position = desiredPosition;

		float rotateY = cachedTransform.position.x * 2.0f;
		cachedTransform.rotation = Quaternion.Euler(0.0f, rotateY, 0.0f);

		// position ani
		int index = FloatingDamageTextRootCanvas.instance.GetPositionAnimationIndex(actor);
		Debug.Log(index);
	}


	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}