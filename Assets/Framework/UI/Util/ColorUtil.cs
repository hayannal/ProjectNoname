using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorUtil : MonoBehaviour
{
	public static Color halfGray
	{
		get
		{
			return (Color.white + Color.gray + Color.gray) * 0.3333f;
		}
	}
}