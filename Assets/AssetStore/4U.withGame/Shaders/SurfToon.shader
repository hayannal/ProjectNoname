Shader "4U.withGame/SurfToon" {

	Properties {
		[Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull_Mode", int) = 2
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		[Space]
		[NoScaleOffset] _MainTex("Base (RGB)", 2D) = "white" {}
		_Color("Main Color", COLOR) = (1,1,1,1)
		[Space]
		[MaterialToggle]_KeepW("Keep White",int) = 1
		_HueC("Hue Change",Range(0, 1)) = 0
		[Space]
		_ShadowColor("Shadow Color", Color) = (0.7,0.7,0.7)
		_ShadeShift("Shade Shift", Range(-1, 1)) = 0
		[Space]
		[HDR]_EmissionColor("Emission Color", Color) = (0,0,0)
		[NoScaleOffset] _EmissionMap("EmissionMap", 2D) = "white" {}
		[Space]
		_IndirectLightIntensity("GI Intensity", Range(0, 1)) = 0.5
		[HideInInspector] _HueCI("HueInstancing", Range(0,1)) = 0.0
	}
	SubShader {
		Tags {  "RenderType"="Opaque" "IgnoreProjector"="True"  "PassFlags" = "OnlyDirectional"}//"Queue" = "AlphaTest" "RenderType"="TransparentCutout" "ForceNoShadowCasting" = "True"  "PassFlags" = "OnlyDirectional"
		LOD 150
		Cull [_CullMode]
		CGPROGRAM
		#pragma surface surf Toon noforwardadd //exclude_path:prepass exclude_path:deferred //noshadow nodynlightmap nodirlightmap nolightmap alphatest:_Cutoff
		#pragma target 3.0
		#pragma multi_compile_instancing
		//#pragma multi_compile_fwdbase
		//#pragma multi_compile_fog
		//#define UNITY_PASS_FORWARDBASE
		#define UNITY_NO_FULL_STANDARD_SHADER
		#define USING_DIRECTIONAL_LIGHT
		#include "UnityPBSLighting.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};
		fixed _Cutoff;
		fixed3 _Color;
		fixed _KeepW;
		fixed _HueC;
		fixed3 _ShadowColor;
		fixed _ShadeShift;
		sampler2D _EmissionMap;
		fixed3 _EmissionColor;
		fixed _IndirectLightIntensity;

		UNITY_INSTANCING_BUFFER_START(Props)
			//UNITY_DEFINE_INSTANCED_PROP(fixed3, _Color)
			UNITY_DEFINE_INSTANCED_PROP(fixed, _HueCI)
		UNITY_INSTANCING_BUFFER_END(Props)

		inline fixed4 LightingToon(SurfaceOutput o, UnityGI gi) {
			
			fixed3 d = lerp(_ShadowColor, 1,gi.light.color*step(_ShadeShift*0.5 + 0.5, dot(o.Normal, gi.light.dir)*0.5 + 0.5));
			fixed4 c;
			c.rgb = o.Albedo*d + _IndirectLightIntensity * gi.indirect.diffuse;
			c.rgb += o.Emission;
			c.a = o.Alpha;
			return  c;
		}
		inline void LightingToon_GI(SurfaceOutput o, UnityGIInput data, inout UnityGI gi) {
			LightingLambert_GI(o, data, gi);
		}
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

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb*lerp(_Color, lerp(_Color, 1, c.rgb), _KeepW);
			o.Albedo = hueChange(o.Albedo, _HueC *9);
			o.Albedo = hueChange(o.Albedo, UNITY_ACCESS_INSTANCED_PROP(Props, _HueCI) * 9); //GPU Instancing
			o.Alpha = c.a;
			clip(o.Alpha - _Cutoff);
			fixed3 e = tex2D(_EmissionMap, IN.uv_MainTex).rgb*_EmissionColor;
			o.Emission = e.rgb; 		
		}
	ENDCG
	}
Fallback "Standard"
}
