// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkNG/Particle/Light" {
Properties {
	_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
    _MainTex ("Base (RGB)", 2D) = "white" {}

	[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend Mode", Float) = 1
	[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend Mode", Float) = 1
	//[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest", Float) = 4
	[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0
}
SubShader {
	Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
    LOD 150

	Blend[_SrcBlend][_DstBlend]
	Cull[_Cull]
	ZWrite Off
	Fog {Mode Off}

CGPROGRAM
#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd keepalpha
#pragma vertex vert

sampler2D _MainTex;
fixed4 _TintColor;

struct Input {
    float2 uv_MainTex;
	fixed4 color;
};

struct appdata_t
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	fixed4 color : COLOR;
	float2 texcoord : TEXCOORD0;
};

void vert(inout appdata_t v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
	o.color = v.color;
}

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = 2.0f * IN.color.rgb * _TintColor.rgb * c.rgb;
    o.Alpha = 2.0f * IN.color.a * _TintColor.a * c.a;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
