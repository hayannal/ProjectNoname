Shader "PolygonArsenal/PolyRimAlphaBlend"
{
	Properties
	{
		_RimColor ("Rim Color", Color) = (0.26,0.19,0.16,1.0)
		_RimWidth ("Rim Width", Range(0.0,20.0)) = 3.0
		_RimGlow ("Rim Glow Multiplier", Range(0.0,9.0)) = 1.0

		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest Mode", Float) = 4
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0
	}
		
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		ZTest [_ZTest]
		Cull [_Cull]
		Lighting Off
		ZWrite Off
		Fog {Mode Off}
		
		CGPROGRAM
		#pragma surface surf NoLighting alpha

		//Custom lightning function to prevent any light from affecting the globe
		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
		{
			fixed4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}
	
		struct Input
		{
			float3 viewDir;
		};

		float4 _RimColor;
		float _RimWidth;
		float _RimGlow;

		void surf(Input IN, inout SurfaceOutput o)
		{
			half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
			o.Albedo = _RimColor.rgb * _RimGlow * pow(rim, _RimWidth);
			o.Alpha = _RimColor.a * rim;
		}
		ENDCG
	}
	Fallback "Diffuse"
}