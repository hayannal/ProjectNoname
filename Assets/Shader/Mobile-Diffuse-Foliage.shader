// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/DiffuseFoliage" {
Properties {
	_MainTex("MainTex", 2D) = "white" {}
	[Normal]_NormalMap("Normal Map", 2D) = "bump" {}
	_Cutoff("Mask Clip Value", Float) = 0.75
	_NormalStrength("Normal Strength", Float) = 1
	_WindFoliageAmplitude("Wind Foliage Amplitude", Range(0 , 1)) = 0
	_WindFoliageSpeed("Wind Foliage Speed", Range(0 , 1)) = 0
	_WindTrunkAmplitude("Wind Trunk Amplitude", Range(0 , 1)) = 0
	_WindTrunkSpeed("Wind Trunk Speed", Range(0 , 1)) = 0
	[HideInInspector] _texcoord("", 2D) = "white" {}
	[HideInInspector] __dirty("", Int) = 1
}
SubShader {
	Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
	LOD 200
		
	CGPROGRAM
	#include "VS_indirect.cginc"
	#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd vertex:vertexDataFunc addshadow
		
	struct Input
	{
		float3 worldPos;
		float2 uv_texcoord;
	};

	uniform float _WindTrunkSpeed;
	uniform float _WindTrunkAmplitude;
	uniform float _WindFoliageSpeed;
	uniform float _WindFoliageAmplitude;
	uniform float _NormalStrength;
	uniform sampler2D _NormalMap;
	uniform float4 _NormalMap_ST;
	uniform sampler2D _MainTex;
	uniform float4 _MainTex_ST;
	uniform float _Cutoff = 0.75;

	float3 mod2D289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

	float2 mod2D289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

	float3 permute(float3 x) { return mod2D289(((x * 34.0) + 1.0) * x); }

	float snoise(float2 v)
	{
		const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
		float2 i = floor(v + dot(v, C.yy));
		float2 x0 = v - i + dot(i, C.xx);
		float2 i1;
		i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
		float4 x12 = x0.xyxy + C.xxzz;
		x12.xy -= i1;
		i = mod2D289(i);
		float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
		float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
		m = m * m;
		m = m * m;
		float3 x = 2.0 * frac(p * C.www) - 1.0;
		float3 h = abs(x) - 0.5;
		float3 ox = floor(x + 0.5);
		float3 a0 = x - ox;
		m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
		float3 g;
		g.x = a0.x * x0.x + h.x * x0.y;
		g.yz = a0.yz * x12.xz + h.yz * x12.yw;
		return 130.0 * dot(m, g);
	}

	void vertexDataFunc(inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input, o);
		float temp_output_130_0 = (_Time.y * (2.0 * _WindTrunkSpeed));
		float4 appendResult141 = (float4(((sin(temp_output_130_0) * _WindTrunkAmplitude) * v.color.b), 0.0, (v.color.b * ((_WindTrunkAmplitude * 0.5) * cos(temp_output_130_0))), 0.0));
		float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex);
		float4 appendResult149 = (float4(ase_worldPos.x, ase_worldPos.y, ase_worldPos.z, 0.0));
		float2 panner93 = ((_Time.y * _WindFoliageSpeed) * float2(2, 2) + appendResult149.xy);
		float simplePerlin2D101 = snoise(panner93);
		float3 ase_vertexNormal = v.normal.xyz;
		v.vertex.xyz += (appendResult141 + float4((simplePerlin2D101 * _WindFoliageAmplitude * ase_vertexNormal * v.color.r), 0.0)).rgb;
	}

	void surf(Input i, inout SurfaceOutput o)
	{
		float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
		o.Normal = UnpackScaleNormal(tex2D(_NormalMap, uv_NormalMap), _NormalStrength);
		float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
		float4 tex2DNode36 = tex2D(_MainTex, uv_MainTex);
		o.Albedo = tex2DNode36.rgb;
		o.Alpha = 1;
		clip(tex2DNode36.a - _Cutoff);
	}
	ENDCG
}

Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}
