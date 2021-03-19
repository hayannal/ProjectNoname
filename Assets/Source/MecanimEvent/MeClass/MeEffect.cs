using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeEffect : MecanimEventBase {

	override public bool RangeSignal { get { return false; } }
	public GameObject effectData;
	public Vector3 offset;
	public bool fixedWorldPositionY;
	public Vector3 direction = Vector3.forward;
	public bool useWorldSpaceDirection;
	public string parentName;
	public bool followPosition;
	public bool aliveOnlyOne;
	public bool immediateDisableAliveOnlyOne;
	public bool disableOnMapChanged;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		effectData = (GameObject)EditorGUILayout.ObjectField("Object :", effectData, typeof(GameObject), false);
		offset = EditorGUILayout.Vector3Field("Offset :", offset);
		fixedWorldPositionY = EditorGUILayout.Toggle("Fixed World Position Y :", fixedWorldPositionY);
		direction = EditorGUILayout.Vector3Field("Direction :", direction);
		useWorldSpaceDirection = EditorGUILayout.Toggle("Use World Space :", useWorldSpaceDirection);
		parentName = EditorGUILayout.TextField("Parent Transform Name :", parentName);
		followPosition = EditorGUILayout.Toggle("Follow Position :", followPosition);
	}
#endif

	Transform _spawnTransform;
	DummyFinder _dummyFinder = null;
	Transform _aliveOnlyOneEffectTransform;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		// aliveOnlyOne이면 동시에 하나만 존재해야한다.
		if (aliveOnlyOne && _aliveOnlyOneEffectTransform != null && _aliveOnlyOneEffectTransform.gameObject.activeSelf)
		{
			if (immediateDisableAliveOnlyOne)
				_aliveOnlyOneEffectTransform.gameObject.SetActive(false);
			else
				DisableParticleEmission.DisableEmission(_aliveOnlyOneEffectTransform);
		}

		GameObject effectObject = null;
		if (string.IsNullOrEmpty(parentName))
		{
			if (_spawnTransform == null)
			{
				if (animator.transform.parent != null)
					_spawnTransform = animator.transform.parent;
				if (_spawnTransform == null)
					_spawnTransform = animator.transform;
			}
			if (_spawnTransform != null)
			{
				//Vector3 result = offset * animator.transform.localScale.x;
				Vector3 resultFoward = Vector3.forward;
				if (useWorldSpaceDirection)
					resultFoward = direction;
				else
					resultFoward = _spawnTransform.TransformDirection(direction);
				if (fixedWorldPositionY)
				{
					Vector3 convertOffset = Vector3.zero;
					convertOffset.x = offset.x;
					convertOffset.z = offset.z;
					Vector3 spawnPosition = _spawnTransform.TransformPoint(convertOffset);
					spawnPosition.y = offset.y;
					effectObject = BattleInstanceManager.instance.GetCachedObject(effectData, spawnPosition, Quaternion.LookRotation(resultFoward));
				}
				else
					effectObject = BattleInstanceManager.instance.GetCachedObject(effectData, _spawnTransform.TransformPoint(offset), Quaternion.LookRotation(resultFoward));
			}
		}
		else
		{
			if (_dummyFinder == null) _dummyFinder = animator.GetComponent<DummyFinder>();
			if (_dummyFinder == null) _dummyFinder = animator.gameObject.AddComponent<DummyFinder>();

			Transform attachTransform = _dummyFinder.FindTransform(parentName);
			if (attachTransform != null)
				effectObject = BattleInstanceManager.instance.GetCachedObject(effectData, attachTransform.position, attachTransform.rotation, attachTransform);
		}
		if (effectObject != null)
		{
			Transform effectTransform = effectObject.transform;

			if (animator.updateMode == AnimatorUpdateMode.UnscaledTime)
				UnscaledTimeEffect.Unscaled(effectTransform);
			if (followPosition)
				FollowTransform.Follow(effectTransform, _spawnTransform, offset);
			if (aliveOnlyOne)
				_aliveOnlyOneEffectTransform = effectTransform;
			if (disableOnMapChanged)
				BattleInstanceManager.instance.OnInitializeManagedEffectObject(effectObject);
		}
	}
}