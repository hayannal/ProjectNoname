Shader "Ferr/Flow/Standard" {
	Properties {
		_CrossTime("Crossfade Time",  Float) = 0.2
		_Speed    ("Animation speed", Float) = 1

		_Metallic  ("Metallic",   Range(0.03, 1)) = 0
		_Glossiness("Smoothness", Range(0.03, 1)) = 0
		
		_MainTex ("Albedo (RGB)",   2D) = "white" {}
		_BumpMap ("Normal",   2D) = "bump"  {}
		_MetallicGlossMap("Metallic(R) Glossiness(A)", 2D) = "white" {}

		_EmissionMap  ("Emissive", 2D) = "black" {}
		_EmissionColor("Color", Color) = (0,0,0)
	}
	SubShader {
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma shader_feature _EMISSION
		#include "FlowCommon.cginc" 

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;
		sampler2D _EmissionMap;

		float     _CrossTime;
		float     _Speed;
		float     _Metallic;
		float     _Glossiness;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			fixed4 color : COLOR;
			INTERNAL_DATA
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float time = _Time.x +sin(IN.uv_MainTex.x*3.14159*2)*.01 + cos(IN.uv_MainTex.y*3.14159*2)*.01;

			half4  uv         = CreateFlowUV(time, IN.uv_MainTex,  IN.color, _Speed, _CrossTime);
			float  crossAlpha = CrossAlpha  (time, _Speed, _CrossTime);
			o.Albedo          = FlowSample  (_MainTex,          uv, crossAlpha).rgb;
			#ifdef _EMISSION
				o.Emission = FlowSample  (_EmissionMap,      uv, crossAlpha).rgb;
			#endif
			fixed4 specData   = FlowSample  (_MetallicGlossMap, uv, crossAlpha);
			float3 normal     = UnpackNormal(FlowSample  (_BumpMap,       uv, crossAlpha));

			o.Metallic   = specData.r * _Metallic;
			o.Smoothness = specData.a * _Glossiness;
			o.Alpha    = 1;
			o.Normal   = normal;
		}
		ENDCG
	}
	CustomEditor "Ferr.FlowShaderGUI"
	FallBack "Diffuse"
}
