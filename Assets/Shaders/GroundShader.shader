Shader "Custom/GroundShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LineTex ("Line", 2D) = "black" {}
		_NormalTex ("Normal", 2D) = "black" {}
		_NormalTex2 ("Normal 2", 2D) = "black" {}
		_LineColor ("Line Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _NormalTex;
		sampler2D _NormalTex2;
		sampler2D _LineTex;
		fixed4 _LineColor;

		struct Input {
			float2 uv_MainTex;
			float2 uv_NormalTex;
			float2 uv_NormalTex2;
			float2 uv_LineTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			half4 n = tex2D (_NormalTex, IN.uv_NormalTex);
			half4 n2 = tex2D (_NormalTex2, IN.uv_NormalTex2+half2(0, _Time.x*6));
			half2 wn = UnpackNormal (n).xy;
			half2 uv = IN.uv_LineTex+wn*0.01+n2.rb*0.08+half2(_Time.x*8,0);
			half4 l = tex2D (_LineTex, uv);
			o.Albedo = c.rgb+l*_LineColor*l.a;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
