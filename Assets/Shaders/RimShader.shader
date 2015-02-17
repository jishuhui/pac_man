Shader "Custom/RimShader" {
	Properties {
		_MainColor ("Base (RGB)", Color) = (1,1,1,1)
		_RimColor ("Rim (RGB)", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		float4 _MainColor;
		float4 _RimColor;

		struct Input {
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			//half4 c = tex2D (_MainTex, IN.uv_MainTex);
			half4 c = _MainColor;
			o.Albedo = c.rgb;
			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
			o.Emission = _RimColor*pow(rim,2.5);
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
