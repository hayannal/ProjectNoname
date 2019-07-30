using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapObject
{
	public class Plane : MonoBehaviour
	{
		public GameObject quadRootObject;

		// Start is called before the first frame update
		void Start()
		{
			if (CustomFollowCamera.instance != null)
				CustomFollowCamera.instance.OnLoadPlaneObject(quadRootObject);
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}
