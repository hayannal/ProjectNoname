using UnityEngine;
using System.Collections;

public class WindUpdater : MonoBehaviour
{
	[Header("Parameter : Axis (XYZ) Frequency (W)")]
	public Vector4 windParameter = new Vector4(0.15f, 0.0f, 0.15f, 1.75f);

	public static int WIND_PARAMETER;

	void Start()
	{
		if (WIND_PARAMETER == 0) WIND_PARAMETER = Shader.PropertyToID("_WindParameterUpdater");
	}

	void Update()
	{
		Vector4 resultParameter;
		float wave = Mathf.Cos(Time.time * windParameter.w);
		resultParameter.x = windParameter.x * wave;
		resultParameter.y = windParameter.y * wave;
		resultParameter.z = windParameter.z * wave;
		resultParameter.w = 1.0f;

		Shader.SetGlobalVector(WIND_PARAMETER, resultParameter);
	}
}
