﻿Shader "PostProcess/RadialBlur"
{
	Properties
	{
		_MainTex ("Input", RECT) = "white" {}
		_BlurStrength ("", Float) = 0.5
		_BlurWidth ("", Float) = 0.5
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
			uniform half _BlurStrength;
			uniform half _BlurWidth;
			
			fixed4 frag (v2f_img i) : COLOR
			{
				fixed4 color = tex2D(_MainTex, i.uv);
				
				// some sample positions
				half samples[10] = {-0.08,-0.05,-0.03,-0.02,-0.01,0.01,0.02,0.03,0.05,0.08};
				
				//vector to the middle of the screen
				half2 dir = 0.5 - i.uv;
				
				//distance to center
				half dist = sqrt(dir.x*dir.x + dir.y*dir.y);
				
				//normalize direction
				dir = dir/dist;
				
				//additional samples towards center of screen
				fixed4 sum = color;
				for(int n = 0; n < 10; n++)
				{
					sum += tex2D(_MainTex, i.uv + dir * samples[n] * _BlurWidth);
				}
				
				//eleven samples...
				sum *= 1.0/11.0;
				
				//weighten blur depending on distance to screen center
				half t = dist * _BlurStrength;
				t = clamp(t, 0.0, 1.0);
				
				//blend original with blur
				return lerp(color, sum, t);
			}
			ENDCG
		}
	}
}