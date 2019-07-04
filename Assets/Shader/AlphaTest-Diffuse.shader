// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "FrameworkPV/CutoutDiffuse" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
}

SubShader{
	Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
	LOD 200

CGPROGRAM
#pragma surface surf Lambert alphatest:_Cutoff exclude_path:prepass nolightmap noforwardadd

sampler2D _MainTex;

struct Input {
	float2 uv_MainTex;
};

void surf(Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb;
	o.Alpha = c.a;
}
ENDCG
}

Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}