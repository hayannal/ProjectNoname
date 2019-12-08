using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using ECM.Components;

public class MeMovePositionCurve : MecanimEventBase {

	override public bool RangeSignal { get { return true; } }

	public bool useLocalPositionX;
	public AnimationCurve curveX;
	public bool useLocalPositionY;
	public AnimationCurve curveY;
	public bool useLocalPositionZ;
	public AnimationCurve curveZ;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		useLocalPositionX = EditorGUILayout.Toggle("Use Local X :", useLocalPositionX);
		if (useLocalPositionX) curveX = EditorGUILayout.CurveField("Curve X :", curveX);
		useLocalPositionY = EditorGUILayout.Toggle("Use Local Y :", useLocalPositionY);
		if (useLocalPositionY) curveY = EditorGUILayout.CurveField("Curve Y :", curveY);
		useLocalPositionZ = EditorGUILayout.Toggle("Use Local Z :", useLocalPositionZ);
		if (useLocalPositionZ) curveZ = EditorGUILayout.CurveField("Curve Z :", curveZ);
	}
#endif


	// 아무래도 컨트롤러는 Rigidbody가지고 움직이니 여기서는 transform을 직접 움직이는게 나아보인다.
	//BaseCharacterController _baseCharacterController;
	Transform _transform;
	Rigidbody _rigidbody;
	float _prevX;
	float _prevZ;
	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (useLocalPositionX || useLocalPositionY || useLocalPositionZ)
		{
			if (_transform == null)
			{
				CharacterMovement characterMovement = animator.GetComponent<CharacterMovement>();
				if (characterMovement == null)
					characterMovement = animator.transform.parent.GetComponent<CharacterMovement>();
				if (characterMovement != null)
				{
					_transform = characterMovement.transform;
					_rigidbody = characterMovement.GetComponent<Rigidbody>();
				}
			}
		}

		Vector3 localTranslation = Vector3.zero;
		if (useLocalPositionY)
		{
			float targetY = curveY.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
			localTranslation.y = targetY - _transform.position.y;
		}

		if (useLocalPositionX)
		{
			float value = curveX.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
			float diff = value - _prevX;
			if (diff != 0.0f)
				localTranslation.x = diff;
			_prevX = value;
		}

		if (useLocalPositionZ)
		{
			float value = curveZ.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
			float diff = value - _prevZ;
			if (diff != 0.0f)
				localTranslation.z = diff;
			_prevZ = value;
		}

		if (localTranslation != Vector3.zero)
		{
			if (_rigidbody != null)
				_rigidbody.MovePosition(_rigidbody.position + _transform.TransformDirection(localTranslation));
			else if (_transform != null)
				_transform.Translate(localTranslation, Space.Self);
		}
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (_transform == null)
			return;

		if (useLocalPositionY)
		{
			float firstValue = curveY.keys[0].value;
			float lastValue = curveY.keys[curveY.length - 1].value;
			_transform.position = new Vector3(_transform.position.x, _basePositionY + (lastValue - firstValue), _transform.position.z);
		}
		if (useLocalPositionX)
			_prevX = 0.0f;
		if (useLocalPositionZ)
			_prevZ = 0.0f;
	}

	float _basePositionY;
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);

		_prevX = _prevZ = 0.0f;
		_basePositionY = animator.transform.position.y;
	}
}