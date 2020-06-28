using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SFXContainer : MonoBehaviour
{
	[System.Serializable]
	public class SoundData
	{
		public string name;
		public AudioClip audioClip;

		[Range(0.0f, 1.0f)]
		public float volume = 1.0f;
		[Range(-3.0f, 3.0f)]
		public float pitch = 1.0f;
	}

	[ReorderableList]
	public List<SoundData> sfxData;
}