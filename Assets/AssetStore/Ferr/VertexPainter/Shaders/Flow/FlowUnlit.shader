Shader "Ferr/Flow/Unlit" {
	Properties {
		_MainTex  ("Base (RGB)",      2D   ) = "white" {}
		_CrossTime("Crossfade Time",  Float) = 0.2
		_Speed    ("Animation speed", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			CGPROGRAM
			#pragma vertex   UnlitVert
			#pragma fragment UnlitFrag
			#include "UnityCG.cginc"
			#include "FlowCommon.cginc"

			float _CrossTime;
			float _Speed;

			sampler2D _MainTex;
			float4    _MainTex_ST;

			struct appdata_ferr {
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color    : COLOR;
			};
			
			struct unlit_vert {
				float4 position : SV_POSITION;
				float2 uv       : TEXCOORD0;
				fixed4 color    : COLOR;
			};

			unlit_vert UnlitVert (appdata_ferr aInput) {
				unlit_vert result;
				result.position = UnityObjectToClipPos(aInput.vertex);
				result.uv       = TRANSFORM_TEX(aInput.texcoord, _MainTex);
				result.color    = aInput.color;

				return result;
			}

			fixed4 UnlitFrag (unlit_vert aInput) : COLOR {
				float  time  = _Time.x + sin(aInput.uv.x*3.14159 * 2)*.01 + cos(aInput.uv.y*3.14159 * 2)*.01;

				half4  uv         = CreateFlowUV(time, aInput.uv, aInput.color, _Speed, _CrossTime);
				float  crossAlpha = CrossAlpha  (time, _Speed, _CrossTime);
				fixed4 color      = FlowSample(_MainTex, uv, crossAlpha);

				return color;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
