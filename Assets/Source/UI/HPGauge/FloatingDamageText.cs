using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FloatingDamageText : MonoBehaviour
{
	public enum eFloatingDamageType
	{
		Miss,
		Invincible,
		Headshot,
		Immortal,
		ReduceContinuousDamage,
		DefenseStrongDamage,
		PaybackSp,
		Critical,
	}

	public Text damageText;
	public Transform positionAnimationTransform;
	public DOTweenAnimation alphaTweenAnimation;

	public void InitializeText(eFloatingDamageType floatingDamageType, Actor actor, int index)
	{
		switch (floatingDamageType)
		{
			case eFloatingDamageType.Miss:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Miss"));
				break;
			case eFloatingDamageType.Invincible:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Invincible"));
				break;
			case eFloatingDamageType.Headshot:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Headshot"));
				break;
			case eFloatingDamageType.Immortal:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_ImmortalWill"));
				break;
			case eFloatingDamageType.ReduceContinuousDamage:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_ReduceContinuousDmg"));
				break;
			case eFloatingDamageType.DefenseStrongDamage:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_DefenseStrongDmg"));
				break;
			case eFloatingDamageType.PaybackSp:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_PaybackSp"));
				break;
			case eFloatingDamageType.Critical:
				damageText.SetLocalizedText(UIString.instance.GetString("GameUI_Critical"));
				break;
		}
		damageText.color = Color.white;

		_offsetY = actor.gaugeOffsetY;
		_targetTransform = actor.cachedTransform;
		_targetHeight = ColliderUtil.GetHeight(actor.GetCollider());
		UpdateGaugePosition();

		//float rotateY = cachedTransform.position.x * 2.0f;
		//cachedTransform.rotation = Quaternion.Euler(0.0f, rotateY, 0.0f);

		// position ani
		positionAnimationTransform.localPosition = Vector3.zero;
		_targetPosition = FloatingDamageTextRootCanvas.instance.positionAnimationTargetList[index];
		_firstPositionAniRemainTime = FirstPositionAniDuration;
	}

	// Update is called once per frame
	Vector3 _prevTargetPosition = -Vector3.up;
	void Update()
	{
		if (_targetTransform != null)
		{
			if (_targetTransform.position != _prevTargetPosition)
			{
				UpdateGaugePosition();
				_prevTargetPosition = _targetTransform.position;
			}
		}

		UpdateFirstPositionAni();
	}

	Transform _targetTransform;
	float _targetHeight;
	float _offsetY;
	void UpdateGaugePosition()
	{
		Vector3 desiredPosition = _targetTransform.position;
		desiredPosition.y += _targetHeight;
		desiredPosition.y += _offsetY;
		cachedTransform.position = desiredPosition;
	}

	Vector3 _targetPosition;
	const float FirstPositionAniDuration = 1.0f;
	float _firstPositionAniRemainTime = 0.0f;
	void UpdateFirstPositionAni()
	{
		positionAnimationTransform.localPosition = Vector3.Lerp(positionAnimationTransform.localPosition, _targetPosition, Time.deltaTime * 5.0f);

		if (_firstPositionAniRemainTime > 0.0f)
		{
			_firstPositionAniRemainTime -= Time.deltaTime;
			if (_firstPositionAniRemainTime <= 0.0f)
			{
				_firstPositionAniRemainTime = 0.0f;
				alphaTweenAnimation.DORestart();
			}
		}
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