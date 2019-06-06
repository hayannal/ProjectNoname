// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FrameworkNG/PostProcess/DisplacementTrail" {
Properties {
	_MainTex ("Main", 2D) = "white" {}
	_Color ("Color", Color) = (1, 1, 1, 1)
	_DispMap ("Displacement Map (RG)", 2D) = "white" {}
	_StrengthX  ("Displacement Strength X", Float) = 1
	_StrengthY  ("Displacement Strength Y", Float) = -1
}

Category {
	Tags { "Queue"="Geometry+999" "RenderType"="Opaque" }
	//AlphaTest Greater .01
	Blend One Zero
	Cull Off Lighting Off ZWrite Off ZTest LEqual
	// 우선 잘 보여야해서 ZTest Always
	
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}

	SubShader {
		//GrabPass {							
		//	Name "BASE"
		//	Tags { "LightMode" = "Always" }
 		//}

		Pass {
			Name "BASE"
			Tags { "LightMode" = "Always" }
			
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#include "UnityCG.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	float2 texcoord: TEXCOORD0;
};

struct v2f {
	float4 vertex : POSITION;
	float2 uvmain : TEXCOORD0;
	float4 uvgrab : TEXCOORD1;
};

half _StrengthX;
half _StrengthY;

sampler2D _MainTex;
fixed4 _Color;
float4 _DispMap_ST;
sampler2D _DispMap;

v2f vert (appdata_t v)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y) + o.vertex.w) * 0.5;
	o.uvgrab.zw = o.vertex.zw;
	o.uvmain = TRANSFORM_TEX(v.texcoord, _DispMap);
	return o;
}

//sampler2D _GrabTexture;
uniform sampler2D _FrameBufferTexture;

half4 frag (v2f i) : COLOR
{
	// get main
	fixed4 mainTex = tex2D(_MainTex, i.uvmain);
	
	// get displacement color
	half4 offsetColor = tex2D(_DispMap, i.uvmain);

	// get offset
	half oftX = offsetColor.r * _StrengthX * mainTex.r;
	half oftY = offsetColor.g * _StrengthY * mainTex.r;

	i.uvgrab.x += oftX;
	i.uvgrab.y += oftY;

	half4 col = tex2Dproj(_FrameBufferTexture, UNITY_PROJ_COORD(i.uvgrab));
	col.rgb += (_Color.rgb * mainTex.r);
	return col;
}
ENDCG
		}
}

	// ------------------------------------------------------------------
	// Fallback for older cards and Unity non-Pro
	
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {
			Name "BASE"
			SetTexture [_MainTex] {	combine texture * primary double, texture * primary }
		}
	}
}
}
