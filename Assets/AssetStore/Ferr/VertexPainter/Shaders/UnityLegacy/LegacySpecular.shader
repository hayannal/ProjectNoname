Shader "Ferr/Blend/Legacy Specular" {
	Properties {
		_BlendStrength("Blend Strength", Range(0.001,0.2)) = 0.1

		_SpecColor("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Shininess("Shininess", Range(0.01,1)) = 0.3

		_MainTex ("Red   Texture (RGB) Height (A)", 2D   ) = "white" {}
		_BumpMap ("Red   Normal Map",               2D   ) = "bump"  {}
		_SpecTex ("Red   Specular",                 2D   ) = "white" {}
		
		_MainTex2("Green Texture (RGB) Height (A)", 2D   ) = "white" {}
		_BumpMap2("Green Normal Map",               2D   ) = "bump"  {}
		_SpecTex2("Green Specular",                 2D   ) = "white" {}
		
		_MainTex3("Blue  Texture (RGB) Height (A)", 2D   ) = "white" {}
		_BumpMap3("Blue  Normal Map",               2D   ) = "bump"  {}
		_SpecTex3("Blue  Specular",                 2D   ) = "white" {}

		_MainTex4("Alpha Texture (RGB) Height (A)", 2D) = "white" {}
		_BumpMap4("Alpha Normal Map",               2D) = "bump"  {}
		_SpecTex4("Alpha Specular",                 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface litSurface BlinnPhong vertex:litVertex

		#pragma shader_feature BLEND_TEX_2 BLEND_TEX_3 BLEND_TEX_4
		#pragma shader_feature BLEND_HEIGHT BLEND_HARD BLEND_SOFT
		#pragma shader_feature BLEND_WORLDUV_OFF BLEND_WORLDUV
		
		#define BLEND_SURFACE
		#define BLEND_NORMAL
		#define BLEND_SPECULAR
		#define BLEND_SEPARATEOFFSETS

		#include "UnityCG.cginc"
		#include "../BlendCommon.cginc"
		#include "LegacyCommon.cginc"
		
		ENDCG
	}
	CustomEditor "Ferr.BlendShaderSpecularGUI"
	FallBack "Diffuse"
}
