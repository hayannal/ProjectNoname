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
	public string parentName;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		effectData = (GameObject)EditorGUILayout.ObjectField("Object :", effectData, typeof(GameObject), false);
		offset = EditorGUILayout.Vector3Field("Offset :", offset);
		fixedWorldPositionY = EditorGUILayout.Toggle("Fixed World Position Y :", fixedWorldPositionY);
		direction = EditorGUILayout.Vector3Field("Direction :", direction);
		parentName = EditorGUILayout.TextField("Parent Transform Name :", parentName);
	}
#endif

	Transform _spawnTransform;
	DummyFinder _dummyFinder = null;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
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
				Vector3 rotation = _spawnTransform.TransformDirection(direction);
				if (fixedWorldPositionY)
				{
					Vector3 convertOffset = Vector3.zero;
					convertOffset.x = offset.x;
					convertOffset.z = offset.z;
					Vector3 spawnPosition = _spawnTransform.TransformPoint(convertOffset);
					spawnPosition.y = offset.y;
					BattleInstanceManager.instance.GetCachedObject(effectData, spawnPosition, Quaternion.LookRotation(rotation));
				}
				else
					BattleInstanceManager.instance.GetCachedObject(effectData, _spawnTransform.TransformPoint(offset), Quaternion.LookRotation(rotation));
			}
		}
		else
		{
			if (_dummyFinder == null) _dummyFinder = animator.GetComponent<DummyFinder>();
			if (_dummyFinder == null) _dummyFinder = animator.gameObject.AddComponent<DummyFinder>();

			Transform attachTransform = _dummyFinder.FindTransform(parentName);
			if (attachTransform != null)
				BattleInstanceManager.instance.GetCachedObject(effectData, attachTransform.position, attachTransform.rotation, attachTransform);
		}
	}
}