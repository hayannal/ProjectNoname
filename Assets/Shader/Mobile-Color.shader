// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "FrameworkPV/Color" {
Properties {
	_Color ("Tint Color", Color) = (1,1,1,1)
	_Emission ("Emission Color", Color) = (1,1,1,1)
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150

CGPROGRAM
#pragma surface surf Lambert exclude_path:prepass nolightmap noforwardadd
#pragma target 3.0

float4 _Color;
float4 _Emission;

struct Input {
	half value;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = _Color;
    o.Albedo = c.rgb;
    o.Alpha = c.a;
	o.Emission = _Emission.rgb;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
