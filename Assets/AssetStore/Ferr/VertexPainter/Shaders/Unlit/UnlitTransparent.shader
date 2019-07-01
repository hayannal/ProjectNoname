Shader "Ferr/Blend/Unlit Transparent" {
	Properties {
		_BlendStrength("Blend Strength", Range(0.001,0.2)) = 0.1

		_MainTex ("Red   Texture (RGB) Alpha (A)", 2D   ) = "white" {}
		_MainTex2("Green Texture (RGB) Alpha (A)", 2D   ) = "white" {}
		_MainTex3("Blue  Texture (RGB) Alpha (A)", 2D   ) = "white" {}
		_MainTex4("Alpha Texture (RGB) Alpha (A)", 2D   ) = "white" {}

		_HeightTex ("Red   Height (R)", 2D) = "white" {}
		_HeightTex2("Green Height (R)", 2D) = "white" {}
		_HeightTex3("Blue  Height (R)", 2D) = "white" {}
		_HeightTex4("Alpha Height (R)", 2D) = "white" {}
	}
	SubShader {
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend  SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull   Off
		LOD    200
		
		Pass {
			CGPROGRAM
			#pragma vertex   UnlitVert
			#pragma fragment UnlitFrag
			#pragma multi_compile_fog

			#pragma shader_feature BLEND_TEX_2 BLEND_TEX_3 BLEND_TEX_4
			#pragma shader_feature BLEND_HEIGHT BLEND_HARD BLEND_SOFT
			#pragma shader_feature BLEND_WORLDUV_OFF BLEND_WORLDUV

			#define BLEND_SEPARATEOFFSETS
			#define BLEND_SEPARATEHEIGHT
			
			#include "UnityCG.cginc" 
			#include "../BlendCommon.cginc"
			#include "UnlitCommon.cginc"
			
			ENDCG
		}
	} 
	CustomEditor "Ferr.BlendShaderGUI"
	FallBack     "Diffuse"
}
