Shader "PostProcess/LUT3D"
{
	Properties
	{
		_MainTex ("Texture", RECT) = "white" {}
		_LUTTex ("_LUTTex", 3D) = "" {}
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
			sampler3D _LUTTex;
			
			fixed4 frag (v2f_img i) : COLOR
			{
				fixed4 color = tex2D(_MainTex, i.uv);
				return tex3D(_LUTTex, color.rgb);
			}
			ENDCG
		}
	}
}