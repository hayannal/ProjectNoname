// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI/DividedHealthBar"
{
	Properties
	{
		_MainTex("Main (RGB)", 2D) = "white" { }
		_CellTex("Cell (RGB)", 2D) = "white" { }
		_BurnTex("Cell (RGB)", 2D) = "white" { }
		_FullColour ("FullColour", Color) = (1.0, 1.0, 1.0, 1.0)
		_EmptyColour("EmptyColour", Color) = (1.0, 1.0, 1.0, 1.0)
		_GapColour("GapColour", Color) = (1.0, 1.0, 1.0, 1.0)
		_BigGapColour("BigGapColour", Color) = (1.0, 1.0, 1.0, 1.0)
		_DamageColour("DamageColour", Color) = (1.0, 1.0, 1.0, 1.0)

		_GapInterval("GapInterval", Float) = 0.1
		_GapSize("GapSize", Float) = 0.025
		_BigGapInterval("Big Gap Interval", Float) = 0.4
		_BigGapSize("Big Gap Size", Float) = 0.025

		_Value("Value", Float) = 1.0
		_MaxValue("MaxValue", Float) = 1.0
		_DamageValue("Damage Value", Float) = 0.0
		_Flip("Flip", Float) = 0.0

		_AAF("Anti Aliasing Factor", Float) = 0.0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType"="Transparent" }
		LOD 100

		ZWrite Off
		//ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float2 screenPos : TEXCOORD1;
			};

			fixed4 _FullColour;
			fixed4 _EmptyColour;
			fixed4 _GapColour;
			fixed4 _BigGapColour;
			fixed4 _DamageColour;

			float _GapInterval;
			float _GapSize;
			float _BigGapInterval;
			float _BigGapSize;

			float _Value;
			float _MaxValue;
			float _DamageValue;
			float _Flip;

			float _AAF;
			float _AAFX;
			float _AAFY;
			float cellSize;
			float patternSize;
			float smlPatternU;
			fixed4 cellColour;
			fixed4 burnColour;

			sampler2D _CellTex;
			sampler2D _BurnTex;
			float4 _CellTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _CellTex);
				if (_Flip > 0.5) o.uv.x = o.uv.x*-1.0 + 1.0;
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			inline fixed4 smallPattern(float2 uv)
			{
				return lerp(
					cellColour,
					_GapColour,
					smoothstep(cellSize - _AAFX, cellSize + _AAFX, uv.x));
			}

			inline fixed4 pattern(float2 uv)
			{
				fixed4 c = lerp(
					_GapColour,
					lerp(
						smallPattern(float2(smlPatternU, uv.y)),
						cellColour,
						smoothstep(cellSize + _GapSize - _AAFX, cellSize + _GapSize + _AAFX, smlPatternU)
					),
					smoothstep(-_AAFX, _AAFX, smlPatternU)
				);
				c = lerp(
					c,
					_BigGapColour,
					smoothstep(patternSize - _BigGapSize - _AAFX, patternSize - _BigGapSize + _AAFX, uv.x)
				);
				return c;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c;

				float2 dxduv = ddx(i.uv);
				float2 dyduv = ddy(i.uv);

				_AAFX = (abs(dxduv.x) + abs(dyduv.x))*1.0 * _AAF;
				_AAFY = (abs(dxduv.y) + abs(dyduv.y))*1.0 * _AAF;

				_GapSize *= 0.1+smoothstep(0, 0.02, _AAFX);
				_BigGapSize *= 0.1+smoothstep(0, 0.02, _AAFX);

				int nGaps = _MaxValue / _GapInterval;
				int nBigGaps = nGaps / _BigGapInterval;
				int nSmlGaps = nGaps - nBigGaps;
				int nCells = nGaps;
				float remainder = fmod(_MaxValue, _GapInterval);
				float accumGapSize = nBigGaps * _BigGapSize + nSmlGaps * _GapSize;
				//cellSize = (1.0 - ())/nCells;
				float accumCellsSize = 1 - accumGapSize;
				cellSize = accumCellsSize / (nCells + (remainder / _GapInterval));

				// Pattern position
				patternSize = _BigGapSize + (_BigGapInterval-1)*_GapSize + _BigGapInterval*cellSize;
				float patternX = fmod(i.uv.x, patternSize);
				smlPatternU = fmod(patternX, cellSize + _GapSize);
				float cellUV = float2(i.uv.y, clamp(smlPatternU, 0, cellSize) / cellSize);

				// Only calculate cell colours here once, avoid multiple texture lookups
				cellColour = _FullColour*tex2D(_CellTex, cellUV);
				burnColour = _DamageColour*tex2D(_BurnTex, cellUV);

				// Pattern colour
				c = lerp(
					_BigGapColour,
					lerp(
						pattern(float2(patternX, i.uv.y)),
						cellColour,
						smoothstep(patternSize - _AAFX, patternSize + _AAFX, patternX)
						),
					smoothstep(-_AAFX, _AAFX, patternX)
				);

				// Burn
				float fullLength = _Value / _MaxValue;
				c = lerp(
					c,
					burnColour,
					smoothstep(fullLength - _AAFX, fullLength + _AAFX, i.uv.x));

				// Empty
				float fullAndBurn = fullLength + _DamageValue / _MaxValue;
				c = lerp(
					c,
					_EmptyColour,
					smoothstep(fullAndBurn - _AAFX, fullAndBurn + _AAFX, i.uv.x));

				float a = smoothstep(0, _AAFY, i.uv.y) * (1-  smoothstep(1 - _AAFY, 1, i.uv.y));
				a *= smoothstep(0, _AAFX, i.uv.x) * (1 - smoothstep(1 - _AAFX, 1, i.uv.x));
				c.a *= a;

				return c;
			}
			ENDCG
		}
	}
}
