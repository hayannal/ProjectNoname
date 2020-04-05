Shader "FrameworkPV/Outline"
{
    Properties
    {
		_OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
		_OutlineWidth("Outline Width", Range(0, 3)) = 2

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend Mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend Mode", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+51" "RenderType" = "Transparent" "DisableBatching" = "True" }
        LOD 200

        Pass
        {
			Blend [_SrcBlend][_DstBlend]
			ColorMask RGB
			Cull Back
			Lighting Off
			ZWrite Off
			Fog {Mode Off}

			Stencil
			{
				Ref 1
				Comp NotEqual
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
				float3 smoothNormal : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
            };

			uniform fixed4 _OutlineColor;
			uniform float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 normal = any(v.smoothNormal) ? v.smoothNormal : v.normal;
				float3 viewPosition = UnityObjectToViewPos(v.vertex);
				float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

				o.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth / 1000.0);
				o.color = _OutlineColor;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				return i.color;
            }
            ENDCG
        }
    }
}
