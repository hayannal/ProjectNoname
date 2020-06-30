using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeSound : MecanimEventBase {

	override public bool RangeSignal { get { return false; } }
	public AudioClip audio;
	public float volume = 1.0f;
	[Range(-3.0f, 3.0f)]
	public float pitch = 1.0f;

	public bool playAtPoint;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		//EditorGUI.BeginChangeCheck();
		audio = (AudioClip)EditorGUILayout.ObjectField("Sound :", audio, typeof(AudioClip), false);
		//if (EditorGUI.EndChangeCheck())
		//{
		//	AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(audio));
		//	assetBundleName = importer.assetBundleName;
		//	audioName = audio.name;
		//}

		volume = EditorGUILayout.Slider("Volume :", volume, 0.0f, 1.0f);
		pitch = EditorGUILayout.Slider("Pitch :", pitch, -3.0f, 3.0f);
		playAtPoint = EditorGUILayout.Toggle("Play At Point (3D) :", playAtPoint);
	}
#endif

	Transform _spawnTransform;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_spawnTransform == null)
			_spawnTransform = animator.transform;

		if (playAtPoint)
			SoundManager.instance.PlayClipAtPoint(audio, _spawnTransform.position, volume);
		else
			SoundManager.instance.PlaySFX(audio, volume, pitch);
	}
}