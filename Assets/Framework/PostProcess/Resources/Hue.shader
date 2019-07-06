Shader "PostProcess/Hue"
{
	Properties
	{
		_MainTex ("Texture", RECT) = "white" {}
		_Hue ("Hue (0 - 360)", float) = 0
	}
		
	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			
			CGPROGRAM
			// Upgrade NOTE: excluded shader from Xbox360 and OpenGL ES 2.0 because it uses unsized arrays
			#pragma exclude_renderers xbox360
			
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			
			#include "UnityCG.cginc"
			
			uniform sampler2D _MainTex;
			uniform float _Hue;

			inline float3 applyHue(float3 aColor)
			{
				float angle = radians(_Hue);
				float3 k = float3(0.57735, 0.57735, 0.57735);
				float cosAngle = cos(angle);

				return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
			}
			
			fixed4 frag (v2f_img i) : COLOR
			{
				fixed4 color = tex2D(_MainTex, i.uv);
				color.rgb = applyHue(color.rgb);
				return color;
			}
			ENDCG
		}
	}
}