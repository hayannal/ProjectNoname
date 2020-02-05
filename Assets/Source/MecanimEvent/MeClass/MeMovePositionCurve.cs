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


	// 아무래도 컨트롤러는 Rigidbody가지고 움직이니 여기서는 transform을 직접 움직이는게 나아보였으나 속도가 빨라지면 벽을 뚫는 현상이 있어서 무조건 리지드바디를 써야할거 같다.
	//BaseCharacterController _baseCharacterController;
	//float _prevX;
	//float _prevZ;
	Actor _actor;
	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (useLocalPositionX || useLocalPositionY || useLocalPositionZ)
		{
			if (_actor == null)
			{
				if (animator.transform.parent != null)
					_actor = animator.transform.parent.GetComponent<Actor>();
				if (_actor == null)
					_actor = animator.GetComponent<Actor>();
			}
		}

		
		Vector3 localTranslation = Vector3.zero;
		if (useLocalPositionY)
		{
			float targetY = curveY.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
			localTranslation.y = targetY - _actor.cachedTransform.position.y;
		}

		// 예전에 포지션을 직접 옮길때 쓰던 코드인데 이제 필요없어서 주석처리.
		//if (useLocalPositionX)
		//{
		//	float value = curveX.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
		//	float diff = value - _prevX;
		//	if (diff != 0.0f)
		//		localTranslation.x = diff;
		//	_prevX = value;
		//}
		//if (useLocalPositionZ)
		//{
		//	float value = curveZ.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
		//	float diff = value - _prevZ;
		//	if (diff != 0.0f)
		//		localTranslation.z = diff;
		//	_prevZ = value;
		//}

		Vector3 velocity = Vector3.zero;
		if (useLocalPositionX)
		{
			float value = curveX.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
			velocity.x = value / ((stateInfo.normalizedTime - StartTime) * stateInfo.length);
		}
		if (useLocalPositionZ)
		{
			float value = curveZ.Evaluate((stateInfo.normalizedTime - StartTime) / (EndTime - StartTime));
			velocity.z = value / ((stateInfo.normalizedTime - StartTime) * stateInfo.length);
			//Debug.Log(Time.frameCount + " : " + Time.deltaTime + " : " + value + " / " + velocity.z + " / " + _transform.position.z);
		}

		if (_actor != null)
		{
			if (velocity.x != 0.0f || velocity.z != 0.0f)
			{
				//_rigidbody.MovePosition(_rigidbody.position + _transform.TransformDirection(localTranslation));
				// MovePosition 함수로는 무슨 수를 써도 - FixedUpdate에서 호출하더라도 컬리더를 뚫어서 velocity를 올리는 형태로 구현한다.
				// 확인해보니 rigidbody의 MovePosition은 kinematic true인 오브젝트들을 물리 적용하면서 이동시킬때 사용하는 함수였다.
				// 그러니 kinematic false인 일반적인 오브젝트는 velocity나 AddForce밖에 방법이 없는데
				// 한프레임 딱 적용하고 마찰에 의해 줄일 것도 아니고 매프레임 특정 속도를 입력해야하고 그러면서 질량도 무시한다면
				// AddForce - velocityChange 인자 주는거랑 다를 바 없으니 그냥 velocity를 쓰기로 한다.
				// 그런데 AddForce나 velocity나 둘다 정확하게 계산하려면 FixedUpdate에서 해야하나 시그널에서는 FixedUpdate를 호출할 방법이 없었다.
				// FixedUpdate에서 velocity대입하는거랑 일반 Update에서 대입하는거랑 완전히 결과가 다르다.(일반 Update에선 fixedDeltaTime을 쓰든 뭘 하든 틀어진다.)
				// 
				// 이거때문에 fixedDelta 대신 그냥 delta를 사용하다보니 커브에 넣은 숫자만큼 이동하지 않는데..
				// 그래서 아예 프로그램에서 제어하는 VelocityAffector를 만들어서 FixedUpdate를 호출하기로 했다.
				//_rigidbody.velocity = _transform.TransformDirection(new Vector3(localTranslation.x, 0.0f, velocity.z));
				AffectorValueLevelTableData velocityAffectorValue = new AffectorValueLevelTableData();
				velocityAffectorValue.fValue1 = (EndTime - stateInfo.normalizedTime) * stateInfo.length;
				velocityAffectorValue.fValue2 = velocity.x;
				velocityAffectorValue.fValue3 = velocity.z;
				_actor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Velocity, velocityAffectorValue, _actor, false);
			}

			if (localTranslation.y != 0.0f)
			{
				// y는 어차피 투과할 일 없으니 예전함수로 해서 높이를 커브만큼 맞춰준다.
				_actor.GetRigidbody().MovePosition(_actor.GetRigidbody().position + new Vector3(0.0f, localTranslation.y, 0.0f));
			}
		}
		//else if (_transform != null)
		//{
		//	if (localTranslation != Vector3.zero)
		//		_transform.Translate(localTranslation, Space.Self);
		//}
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (_actor == null)
			return;

		if (useLocalPositionY)
		{
			float firstValue = curveY.keys[0].value;
			float lastValue = curveY.keys[curveY.length - 1].value;
			_actor.GetRigidbody().position = new Vector3(_actor.GetRigidbody().position.x, _basePositionY + (lastValue - firstValue), _actor.GetRigidbody().position.z);
		}
		if (useLocalPositionX || useLocalPositionZ)
		{
			//if (useLocalPositionX)
			//	_prevX = 0.0f;
			//if (useLocalPositionZ)
			//	_prevZ = 0.0f;
			_actor.GetRigidbody().velocity = Vector3.zero;
		}
	}

	float _basePositionY;
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);

		//_prevX = _prevZ = 0.0f;
		_basePositionY = animator.transform.position.y;
	}
}