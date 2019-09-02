// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/DiffuseTree" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
	[Normal]_Normal("Normal", 2D) = "bump" {}
	_NormalStrength("Normal Strength", Float) = 1
	_TrunkBrightness("Trunk Brightness", Range(0 , 1)) = 0.5
	_TrunkColorVariation("Trunk Color Variation", Color) = (0,0,0,0)
	_HeightGradient("Height Gradient", Float) = 1.5
	_HeightStartGradient("Height Start Gradient", Float) = 1.25
	_HeightBrightness("Height Brightness", Range(0 , 8)) = 0
	_WindTrunkAmplitude("Wind Trunk Amplitude", Range(0 , 1)) = 0
	_WindTrunkSpeed("Wind Trunk Speed", Range(0 , 1)) = 0
	[HideInInspector] _texcoord("", 2D) = "white" {}
	[HideInInspector] __dirty("", Int) = 1
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
		
	CGPROGRAM
	#include "VS_indirect.cginc"
	#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd vertex:vertexDataFunc

	struct Input
	{
		float2 uv_texcoord;
		float3 worldPos;
	};

	uniform float _WindTrunkSpeed;
	uniform float _WindTrunkAmplitude;
	uniform float _NormalStrength;
	uniform sampler2D _Normal;
	uniform float4 _Normal_ST;
	uniform float _HeightBrightness;
	uniform float _HeightStartGradient;
	uniform float _HeightGradient;
	uniform float4 _TrunkColorVariation;
	uniform float _TrunkBrightness;
	uniform sampler2D _MainTex;
	uniform float4 _MainTex_ST;

	void vertexDataFunc(inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input, o);
		float temp_output_48_0 = (_Time.y * (2.0 * _WindTrunkSpeed));
		float4 appendResult58 = (float4(((sin(temp_output_48_0) * _WindTrunkAmplitude) * v.color.b), 0.0, (v.color.b * ((_WindTrunkAmplitude * 0.5) * cos(temp_output_48_0))), 0.0));
		v.vertex.xyz += appendResult58.rgb;
	}

	void surf(Input i, inout SurfaceOutput o)
	{
		float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
		o.Normal = UnpackScaleNormal(tex2D(_Normal, uv_Normal), _NormalStrength);
		float3 ase_vertex3Pos = mul(unity_WorldToObject, float4(i.worldPos, 1));
		float4 clampResult38 = clamp(_TrunkColorVariation, float4(0, 0, 0, 0), float4(0.3867925, 0.3867925, 0.3867925, 0));
		float clampResult16 = clamp((_TrunkBrightness * 2.0), 0.5, 5.0);
		float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
		float4 tex2DNode2 = tex2D(_MainTex, uv_MainTex);
		float4 temp_output_19_0 = (clampResult38 + (clampResult16 * tex2DNode2));
		o.Albedo = ((_HeightBrightness * (saturate(((ase_vertex3Pos.y - _HeightStartGradient) / _HeightGradient)) * temp_output_19_0)) + temp_output_19_0).rgb;
		o.Alpha = 1;
	}
	ENDCG
}

Fallback "Mobile/VertexLit"
}
