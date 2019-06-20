Shader "FrameworkPV/DiffuseRimNormal" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0)
		_RimNormalTex ("Rim Mask (Normal)", 2D) = "Bump" {}    //  Normal을 저장하고 있는 텍스처
		_RimColor ("Rim Color", Color) = (0.1, 0.3, 0.2)	// 유니티 에디터에서 직접 수정할 수 있도록 합니다.
		_RimPower ("Rim Power", Range(-1.0, 2)) = 1
		_RimDirAdjust ("Rim Dir Adjust", Vector) = (0, 0, 0, 0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf Lambert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		// Properties와 동일한 변수명으로 설정합니다.
		sampler2D _MainTex;
		fixed4 _Color;
		sampler2D _RimNormalTex;
		float4 _RimColor;
      	float _RimPower;
      	float4 _RimDirAdjust;

		// Vertex를 연산할 때 필요한 데이터를 정합니다.
		// surf 함수로 호출될 때마다 해당 정점의 관련 데이터가 연산에 적용됩니다.
		struct Input {
			float2 uv_MainTex;	// uv는 해당 텍스처의 텍스처 좌표를 의미합니다.
			float3 viewDir;	// 관찰자의 위치를 향하는 방향 벡타입니다. Normal Vector와 내적하기 위해서 추가합니다.
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color;

			float3 rimNormal = UnpackNormal(tex2D(_RimNormalTex, IN.uv_MainTex));
			float3 vNormal = normalize(o.Normal * 2.0f + rimNormal);
			float3 viewNormal = normalize(IN.viewDir + _RimDirAdjust.xyz);
			half rim = 1.0 - dot(viewNormal, vNormal);
			o.Emission = _RimColor.rgb * saturate(rim - _RimPower) * 2.0f;
		}
		ENDCG
	}
	FallBack "Diffuse"
}