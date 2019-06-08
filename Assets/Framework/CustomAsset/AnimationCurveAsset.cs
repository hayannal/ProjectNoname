using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "NewAnimationCurve", menuName = "CustomAsset/Create Animation Curve", order = 1100)]
public class AnimationCurveAsset : ScriptableObject
{
	public AnimationCurve curve;
}