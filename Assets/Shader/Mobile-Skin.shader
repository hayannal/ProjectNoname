// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/Skin" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
	_NormalLerp("Normal Lerp", Range(0, 1)) = 0
	_LightIntensity("Light Intensity", Range(0, 2)) = 1
	[Toggle(_NORMAL)] _UseNormal ("========== Use NormalMap ==========", Float) = 0
	_NormalMap ("Normal Map", 2D) = "bump" {}
	[Toggle(_CUTOFF)] _UseCutoff ("========== Use Cutoff ==========", Float) = 0
	_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
	[Toggle(_SHADOWSHIFT)] _UseShadowShift("========== Use Shadow Shift ==========", Float) = 0
	_ShadowColor ("Shadow Color", Color) = (0.8,0.8,0.8)
	_ShadeShift ("Shade Shift", Range(0, 1)) = 0.5
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
	Cull[_Cull]

CGPROGRAM
#pragma surface surf Skin exclude_path:prepass nolightmap noforwardadd
#pragma target 3.0
#pragma shader_feature _NORMAL
#pragma shader_feature _CUTOFF
#pragma shader_feature _SHADOWSHIFT

// 5.x 라이팅 함수로는 쉐이더 내에서 GI(Ambient Color)를 컨트롤 할 수 없어서 사용하지 않기로 한다.
inline half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, fixed atten)
{
	fixed diff = max(0, dot(s.Normal, lightDir));

	fixed4 c;
	c.rgb = s.Albedo * _LightColor0.rgb * diff * atten;
	// atten은 그림자를 담당. 새 쉐이더 라이팅 함수에선 light.color가 이 값을 가지고 있는다.
	// diff는 위 계산대로 lambert 결과
	// _LightColor0은 주광의 컬러
	// 이 rgb는 간접광 말고 직접광의 결과를 저장하는거다.
	// 이 값이 0이 되면 Ambient Color에 Albedo 곱한 결과만 화면에 나오게 된다. 즉 주광이 없는 상태.
	c.a = s.Alpha;
	return c;
}

/*
// Toon Shader 예제
fixed3 _ShadowColor;
fixed _ShadeShift;
fixed _IndirectLightIntensity;

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
*/

// 그래서 2019 쉐이더 기반 라이트를 사용하기로 한다.
// 앰비언트 처리 따로 안할거면 사실 예전꺼 써도 상관은 없다.
fixed _NormalLerp;
fixed _LightIntensity;
#if _SHADOWSHIFT
fixed3 _ShadowColor;
fixed _ShadeShift;
#endif
inline fixed4 SkinLight(SurfaceOutput s, UnityLight light)
{
	// 디폴트인 lambert 라이트. 이게 마네킹처럼 보이게 하는 원인이다.
	//fixed diff = max(0, dot(s.Normal, light.dir));
	//fixed4 c;
	//c.rgb = s.Albedo * light.color * diff;
	
	fixed4 c;
	half3 convertNormal = lerp(s.Normal, normalize(s.Normal + light.dir * 1.5f), _NormalLerp);
	fixed diff = max(0, dot(convertNormal, light.dir));
#if _SHADOWSHIFT
	half3 lightColor = lerp(_ShadowColor, 1, light.color * step(_ShadeShift, dot(s.Normal, light.dir) * 0.5f + 0.5f));
	c.rgb = s.Albedo * lightColor * diff * _LightIntensity;
#else
	// light.color가 동적 그림자를 담당
	c.rgb = s.Albedo * light.color * diff * _LightIntensity;
#endif
	c.a = s.Alpha;
	return c;
}

inline fixed4 LightingSkin(SurfaceOutput s, UnityGI gi)
{
	fixed4 c;
	c = SkinLight(s, gi.light);

#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
	// 여기서 앰비언트 처리
	c.rgb += s.Albedo * gi.indirect.diffuse;
#endif

	return c;
}

inline void LightingSkin_GI(SurfaceOutput o, UnityGIInput data, inout UnityGI gi)
{
	LightingLambert_GI(o, data, gi);
}

sampler2D _MainTex;
fixed3 _TintColor;
#if _NORMAL
sampler2D _NormalMap;
#endif
#if _CUTOFF
fixed _Cutoff;
#endif

struct Input {
    float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb * 2.0f * _TintColor;
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
