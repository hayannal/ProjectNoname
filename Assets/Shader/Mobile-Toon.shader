// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/Toon" {
Properties {
	[NoScaleOffset] _MainTex("Base (RGB)", 2D) = "white" {}
	_Color("Main Color", Color) = (1,1,1,1)
	[MaterialToggle]_KeepW("Keep White", int) = 1

	_ShadowColor("Shadow Color", Color) = (0.7,0.7,0.7)
	_ShadeShift("Shade Shift", Range(-1, 1)) = 0
	_IndirectLightIntensity("GI Intensity", Range(0, 1)) = 0.5

	[Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull_Mode", int) = 2

	[Toggle(_NORMAL)] _UseNormal("========== Use NormalMap ==========", Float) = 0
	_NormalMap("Normal Map", 2D) = "bump" {}

	[Toggle(_CUTOFF)] _UseCutoff("========== Use Cutoff ==========", Float) = 0
	_Cutoff("Alpha cutoff", Range(0, 1)) = 0.5

	[Toggle(_HUECHANGE)] _UseHueChange("========== Use Hue Change ==========", Float) = 0
	_HueC("Hue Change",Range(0, 1)) = 0
	[HideInInspector] _HueCI("HueInstancing", Range(0,1)) = 0.0
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150
	Cull[_CullMode]

CGPROGRAM
#pragma surface surf Toon exclude_path:prepass nolightmap noforwardadd
#pragma target 3.0
#pragma shader_feature _NORMAL
#pragma shader_feature _CUTOFF
#pragma shader_feature _HUECHANGE
#pragma multi_compile_instancing

fixed3 _ShadowColor;
fixed _ShadeShift;
fixed _IndirectLightIntensity;

UNITY_INSTANCING_BUFFER_START(Props)
	//UNITY_DEFINE_INSTANCED_PROP(fixed3, _Color)
	UNITY_DEFINE_INSTANCED_PROP(fixed, _HueCI)
UNITY_INSTANCING_BUFFER_END(Props)

inline fixed4 LightingToon(SurfaceOutput o, UnityGI gi)
{
	fixed3 d = lerp(_ShadowColor, 1, gi.light.color * step(_ShadeShift * 0.5f + 0.5f, dot(o.Normal, gi.light.dir) * 0.5f + 0.5f));
	fixed4 c;
	c.rgb = o.Albedo*d + _IndirectLightIntensity * gi.indirect.diffuse;
	c.rgb += o.Emission;
	c.a = o.Alpha;
	return c;
}
inline void LightingToon_GI(SurfaceOutput o, UnityGIInput data, inout UnityGI gi)
{
	LightingLambert_GI(o, data, gi);
}
#if _HUECHANGE
inline fixed3 hueChange(fixed3 col, fixed hueC) {
	fixed3 rc = lerp(col, fixed3(col.g, col.g, col.b), smoothstep(0, 1, hueC));
	rc = lerp(rc, fixed3(col.g, col.b, col.b), smoothstep(1, 2, hueC));
	rc = lerp(rc, fixed3(col.g, col.b, col.r), smoothstep(2, 3, hueC));
	rc = lerp(rc, fixed3(col.b, col.b, col.r), smoothstep(3, 4, hueC));
	rc = lerp(rc, fixed3(col.b, col.r, col.r), smoothstep(4, 5, hueC));
	rc = lerp(rc, fixed3(col.b, col.r, col.g), smoothstep(5, 6, hueC));
	rc = lerp(rc, fixed3(col.r, col.r, col.g), smoothstep(6, 7, hueC));
	rc = lerp(rc, fixed3(col.r, col.g, col.g), smoothstep(7, 8, hueC));
	rc = lerp(rc, fixed3(col.r, col.g, col.b), smoothstep(8, 9, hueC));
	return rc;
}
#endif

sampler2D _MainTex;
fixed3 _Color;
fixed _KeepW;
#if _NORMAL
sampler2D _NormalMap;
#endif
#if _CUTOFF
fixed _Cutoff;
#endif
#if _HUECHANGE
fixed _HueC;
#endif

struct Input {
    float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb * lerp(_Color, lerp(_Color, 1, c.rgb), _KeepW);
#if _HUECHANGE
	o.Albedo = hueChange(o.Albedo, _HueC * 9);
	o.Albedo = hueChange(o.Albedo, UNITY_ACCESS_INSTANCED_PROP(Props, _HueCI) * 9); //GPU Instancing
#endif
    o.Alpha = c.a;
#if _NORMAL
	o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
#endif
#if _CUTOFF
	clip(o.Alpha - _Cutoff);
#endif
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
