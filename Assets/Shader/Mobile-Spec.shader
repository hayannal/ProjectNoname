// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Bumped Specular shader. Differences from regular Bumped Specular one:
// - no Main Color nor Specular Color
// - specular lighting directions are approximated per vertex
// - writes zero to alpha channel
// - Normalmap uses Tiling/Offset of the Base texture
// - no Deferred Lighting support
// - no Lightmap support
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/Specular" {
Properties {
    //[PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
	_Shininess("Shininess", Range(0.25, 15)) = 0.5
    _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}

	[Toggle(_GLOSSPOWER)] _UseGlossPower("========== Use Gloss Power ==========", Float) = 0
	_GlossPower("Gloss Power", Range(0.0, 50)) = 1.0

	[Toggle(_ADJUSTSHININESS)] _AdjustShininess("========== Adust Shininess ==========", Float) = 0
	_DotHalfDirAdjust("Adjust Shininess", Range(0.0, 0.05)) = 0.0
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 250

CGPROGRAM
#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview interpolateview
#pragma target 3.0
#pragma shader_feature _GLOSSPOWER
#pragma shader_feature _ADJUSTSHININESS

#if _ADJUSTSHININESS
fixed _DotHalfDirAdjust;
#endif

inline fixed4 LightingMobileBlinnPhong (SurfaceOutput s, fixed3 lightDir, float3 halfDir, fixed atten)
{
    fixed diff = max (0, dot (s.Normal, lightDir));
    fixed nh = max (0, dot (s.Normal, halfDir));
#if _ADJUSTSHININESS
	nh += _DotHalfDirAdjust;
	nh = saturate(nh);
#endif
    fixed spec = pow (nh, s.Specular*128) * s.Gloss;

    fixed4 c;
    c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
    UNITY_OPAQUE_ALPHA(c.a);
    return c;
}

sampler2D _MainTex;
half _Shininess;
#if _GLOSSPOWER
fixed _GlossPower;
#endif

struct Input {
    float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = tex.rgb;
    o.Gloss = tex.a;
#if _GLOSSPOWER
	o.Gloss *= _GlossPower;
#endif
	o.Alpha = tex.a;
    o.Specular = _Shininess;
}
ENDCG
}

FallBack "Mobile/VertexLit"
}
