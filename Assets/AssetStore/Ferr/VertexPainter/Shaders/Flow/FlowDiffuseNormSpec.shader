Shader "Ferr/Flow/Legacy Specular" {
	Properties {

		_Shininess("Specular Shine", Range(0.03, 2)) = 0.078125
		_SpecColor("Specular Color", Color         ) = (0.5,0.5,0.5,1)
		
		_CrossTime("Crossfade Time",  Float) = 0.2
		_Speed    ("Animation speed", Float) = 1

		_MainTex ("Base (RGB)",   2D) = "white" {}
		_BumpMap ("Normal Map",   2D) = "bump"  {}
		_SpecTex ("Specular Map", 2D) = "white" {}

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf BlinnPhong
		#include "FlowCommon.cginc" 

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _SpecTex;

		float     _CrossTime;
		float     _Speed;
		float     _Shininess;

		struct Input {
			float2 uv_MainTex;
			fixed4 color : COLOR;
			INTERNAL_DATA
		};

		void surf (Input IN, inout SurfaceOutput o) {
			float time = _Time.x + sin(IN.uv_MainTex.x*3.14159 * 2)*.01 + cos(IN.uv_MainTex.y*3.14159 * 2)*.01;

			half4  uv         = CreateFlowUV(time, IN.uv_MainTex,  IN.color, _Speed, _CrossTime);
			float  crossAlpha = CrossAlpha  (time, _Speed, _CrossTime);
			o.Albedo          = FlowSample  (_MainTex,       uv, crossAlpha).rgb;
			fixed4 specData   = FlowSample  (_SpecTex,       uv, crossAlpha);
			float3 normal     = UnpackNormal(FlowSample  (_BumpMap,       uv, crossAlpha));

			o.Specular = _Shininess;
			o.Gloss    = specData.r;
			o.Alpha    = 1;
			o.Normal   = normal;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}