Shader "FrameworkPV/Flow" {
	Properties {
		_MainTex  ("Base (RGB)",      2D   ) = "white" {}
		_CrossTime("Crossfade Time",  Float) = 0.2
		_Speed    ("Animation speed", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd
		#include "FlowCommon.cginc"

		sampler2D _MainTex;
		float     _CrossTime;
		float     _Speed;

		struct Input {
			float2 uv_MainTex;
			fixed4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			float time = _Time.x + sin(IN.uv_MainTex.x*3.14159 * 2)*.01 + cos(IN.uv_MainTex.y*3.14159 * 2)*.01;

			half4  uv         = CreateFlowUV(time, IN.uv_MainTex,  IN.color, _Speed, _CrossTime);
			float  crossAlpha = CrossAlpha(time, _Speed, _CrossTime);
			fixed4 c  = FlowSample(_MainTex, uv, crossAlpha);
			o.Albedo = c.rgb;
			o.Alpha  = 1;
		}
		ENDCG
	} 
	Fallback "Mobile/VertexLit"
}
