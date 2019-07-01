// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Bumped Specular shader. Differences from regular Bumped Specular one:
// - no Main Color nor Specular Color
// - specular lighting directions are approximated per vertex
// - writes zero to alpha channel
// - Normalmap uses Tiling/Offset of the Base texture
// - no Deferred Lighting support
// - no Lightmap support
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/Blend Specular" {
Properties {
    //[PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
	_Shininess("Main Shininess", Range(0.25, 15)) = 0.5
    _MainTex ("Main Texture (RGB) Gloss (A)", 2D) = "white" {}
	_MainTex2("Sub Texture (RGB) Gloss (A)", 2D) = "white" {}

	[Toggle(_SUBSPECULAR)] _UseSubSpecular("========== Use Sub Specular ==========", Float) = 0
	_Shininess2("Sub Shininess", Range(0.25, 15)) = 0.5
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 250

CGPROGRAM
#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview interpolateview
#pragma shader_feature _SUBSPECULAR

inline fixed4 LightingMobileBlinnPhong (SurfaceOutput s, fixed3 lightDir, float3 halfDir, fixed atten)
{
    fixed diff = max (0, dot (s.Normal, lightDir));
    fixed nh = max (0, dot (s.Normal, halfDir));
    fixed spec = pow (nh, s.Specular*128) * s.Gloss;

    fixed4 c;
    c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
    UNITY_OPAQUE_ALPHA(c.a);
    return c;
}

sampler2D _MainTex;
sampler2D _MainTex2;
half _Shininess;
#if _SUBSPECULAR
half _Shininess2;
#endif

struct Input {
    float2 uv_MainTex;
	float2 uv_MainTex2;
	fixed4 color : COLOR;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 col1 = tex2D(_MainTex, IN.uv_MainTex);
	fixed4 col2 = tex2D(_MainTex2, IN.uv_MainTex2);
    o.Albedo = IN.color.r * col1.rgb + IN.color.g * col2.rgb;
	o.Gloss = IN.color.r * col1.a;
#if _SUBSPECULAR
	o.Gloss += IN.color.g * col2.a;
#endif
	o.Alpha = 1;
    o.Specular = _Shininess;
}
ENDCG
}

FallBack "Mobile/VertexLit"
}
