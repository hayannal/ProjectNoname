Shader "PostProcess/RGBSplit"
{
	Properties
	{
		_MainTex ("Input", RECT) = "white" {}
		_SplitPower ("", Float) = 0.5
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
			uniform half _SplitPower;
			uniform half4 _SplitValue;
			
			half4 frag (v2f_img i) : COLOR
			{
				half4 color = tex2D(_MainTex, i.uv);
				
				// some sample positions
				half samples[4] = {-0.06,-0.03,0.03,0.06};
				
				//vector to the middle of the screen
				half2 dir = 0.5 - i.uv;
				
				//distance to center
				half dist = sqrt(dir.x*dir.x + dir.y*dir.y);
				
				//normalize direction
				dir = dir/dist;
				
				//additional samples towards center of screen
				half4 sum = color;
				half3 rgbSplit = 0;
				for(int n = 0; n < 4; n++)
				{
					rgbSplit.r = tex2D(_MainTex, i.uv + dir * samples[n] * _SplitValue.x).r;
					rgbSplit.g = tex2D(_MainTex, i.uv + dir * samples[n] * _SplitValue.y).g;
					rgbSplit.b = tex2D(_MainTex, i.uv + dir * samples[n] * _SplitValue.z).b;
					sum.rgb += rgbSplit.rgb;
				}
				
				//eleven samples...
				sum *= 1.0/5.0;
				
				//weighten blur depending on distance to screen center
				half t = dist * _SplitPower;
				t = clamp(t, 0.0, 1.0);
				
				//blend original with blur
				return lerp(color, sum, t);
			}
			ENDCG
		}

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
			uniform half _SplitPower;
			uniform half4 _SplitValue;
			
			half4 frag (v2f_img i) : COLOR
			{
				half4 color = tex2D(_MainTex, i.uv);
				
				// some sample positions
				half samples[2] = {0.03,0.06};
				
				//vector to the middle of the screen
				half2 dirG = half2(0.0f, -1.0f);
				half2 dirR = half2(0.707f, 0.707f);
				half2 dirB = half2(-0.707f, 0.707f);
				
				//distance to center
				half2 dir = 0.5 - i.uv;
				half dist = sqrt(dir.x*dir.x + dir.y*dir.y);
				
				//additional samples towards center of screen
				half4 sum = color;
				half3 rgbSplit = 0;
				for(int n = 0; n < 2; n++)
				{
					rgbSplit.r = tex2D(_MainTex, i.uv + dirR * samples[n] * _SplitValue.x).r;
					rgbSplit.g = tex2D(_MainTex, i.uv + dirG * samples[n] * _SplitValue.y).g;
					rgbSplit.b = tex2D(_MainTex, i.uv + dirB * samples[n] * _SplitValue.z).b;
					sum.rgb += rgbSplit.rgb;
				}
				
				//eleven samples...
				sum *= 1.0/3.0;
				
				//weighten blur depending on distance to screen center
				half t = dist * _SplitPower;
				t = clamp(t, 0.0, 1.0);
				
				//blend original with blur
				return lerp(color, sum, t);
			}
			ENDCG
		}
	}
}