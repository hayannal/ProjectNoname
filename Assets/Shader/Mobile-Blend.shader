// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/Blend" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
	_MainTex2("Sub Texture (RGB)", 2D) = "white" {}
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150

CGPROGRAM
#pragma surface surf Lambert noforwardadd

sampler2D _MainTex;
sampler2D _MainTex2;

struct Input {
    float2 uv_MainTex;
	float2 uv_MainTex2;
	fixed4 color : COLOR;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 col1 = tex2D(_MainTex, IN.uv_MainTex);
	fixed4 col2 = tex2D(_MainTex2, IN.uv_MainTex2);
	o.Albedo = IN.color.r * col1.rgb + IN.color.g * col2.rgb;
    o.Alpha = 1;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
