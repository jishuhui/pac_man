Shader "Custom/BackgroundNoise" {
	Properties {
		_Noise ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _Noise;

		struct Input {
			float2 uv_Noise;
		};


		void surf (Input IN, inout SurfaceOutput o) {
			half2 uv1 = IN.uv_Noise*half2(0.25, 0.36)+half2(_Time.x*0.6, _Time.x*1.2);
			half4 c1 = tex2D (_Noise, uv1);
			half2 uv2 = IN.uv_Noise*half2(0.5+sin(_Time.x)*0.2, 0.3)-half2(_Time.x*1.5, _Time.x*0.4-0.2f);
			half4 c2 = tex2D (_Noise, uv2);
			half2 uv3 = IN.uv_Noise*half2(1.2, 1.2)+half2(_Time.x*2, -_Time.x*0.8-0.1f)+c2.rg*0.2;
			half4 c3 = tex2D (_Noise, uv3);
			half4 final = (c1+c2)/2+pow(c3, 3)/4;
			//half4 final = c3;
			//half4 final = tex2D (_Noise, IN.uv_Noise);
			//float r = rand(IN.uv_MainTex*0.02+half2(_Time.x*0.1, sin(_Time.y*0.1))*0.02);
			o.Albedo = pow(final*1.0,2)*half4(0.6,0,1,1);
			//o.Emission = final;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
