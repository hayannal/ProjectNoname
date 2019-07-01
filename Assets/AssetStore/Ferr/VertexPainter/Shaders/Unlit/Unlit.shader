Shader "Ferr/Blend/Unlit" {
	Properties {
		_BlendStrength("Blend Strength", Range(0.001,0.2)) = 0.1

		_MainTex ("Red   Texture (RGB) Height (A)", 2D   ) = "white" {}
		_MainTex2("Green Texture (RGB) Height (A)", 2D   ) = "white" {}
		_MainTex3("Blue  Texture (RGB) Height (A)", 2D   ) = "white" {}
		_MainTex4("Alpha Texture (RGB) Height (A)", 2D   ) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			CGPROGRAM
			#pragma vertex   UnlitVert
			#pragma fragment UnlitFrag
			#pragma multi_compile_fog

			#pragma shader_feature BLEND_TEX_2 BLEND_TEX_3 BLEND_TEX_4
			#pragma shader_feature BLEND_HEIGHT BLEND_HARD BLEND_SOFT
			#pragma shader_feature BLEND_WORLDUV_OFF BLEND_WORLDUV

			#define BLEND_SEPARATEOFFSETS
			
			#include "UnityCG.cginc" 
			#include "../BlendCommon.cginc"
			#include "UnlitCommon.cginc"
			
			ENDCG
		}
	} 
	CustomEditor "Ferr.BlendShaderGUI"
	FallBack     "Diffuse"
}
