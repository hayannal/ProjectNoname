using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMInfo : MonoBehaviour
{
	public string addressForVerify;
	public AudioClip audioClip;

	[Range(0.0f, 2.0f)]
	public float volume = 1.0f;
}