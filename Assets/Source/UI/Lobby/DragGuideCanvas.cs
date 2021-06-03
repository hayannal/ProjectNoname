using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragGuideCanvas : MonoBehaviour
{
	public static DragGuideCanvas instance;

	void Awake()
	{
		instance = this;
	}

	// 인풋 감지 하려다가 SubTrigger에 도착하는거로 끄기로 해서 우선 패스
	//void Update()
	//{
	//}
}