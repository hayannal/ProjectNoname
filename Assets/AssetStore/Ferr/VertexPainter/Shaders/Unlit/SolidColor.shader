Shader "Ferr/Blend/Solid Color" {
	Properties {
		_BlendStrength("Blend Strength", Range(0.001,0.2)) = 0.1

		_Color ("Red   Color (RGB)", Color ) = (1,0,0,1)
		_Color2("Green Color (RGB)", Color ) = (0,1,0,1)
		_Color3("Blue  Color (RGB)", Color ) = (0,0,1,1)
		_Color4("Alpha Color (RGB)", Color ) = (0,0,0,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			CGPROGRAM
			#pragma vertex   UnlitVert
			#pragma fragment UnlitFrag
			
			#pragma shader_feature BLEND_TEX_2 BLEND_TEX_3 BLEND_TEX_4
			#pragma shader_feature BLEND_HARD BLEND_SOFT

			#define BLEND_SOLIDCOLOR
			#define BLEND_FIXCHANNELS
			
			#include "UnityCG.cginc" 
			#include "../BlendCommon.cginc"
			#include "UnlitCommon.cginc"
			
			ENDCG
		}
	}
	CustomEditor "Ferr.BlendShaderGUI"
	FallBack "Diffuse"
}
