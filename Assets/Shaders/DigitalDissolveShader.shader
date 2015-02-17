Shader "Custom/DigitalDissolveShader" {
	Properties {
		_Color ("Base Color(RGB)", Color) = (1,1,1,1)
		_Dissolve("Dissolve amount", Range(0,1)) = 0
		_DissolveMask("Mask (RGB)", 2D) = "white"{}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert alpha vertex:vert

		sampler2D _DissolveMask;
		float4 _Color;
		float _Dissolve;
		
		void vert (inout appdata_full v) {
			//Get position in a world coordinate system
			float4 world_v = mul(_Object2World, v.vertex);
			//World position as coordinates
			v.texcoord = world_v;
		}

		struct Input {
			float2 uv_DissolveMask;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = _Color;
			half4 mask = tex2D(_DissolveMask, IN.uv_DissolveMask);
			o.Albedo = c.rgb;
			float alp = 1;
			if (_Dissolve>mask.r) {
				//float diff = _Dissolve-mask.r;
				//alp = lerp(1,0,diff);
				alp = 0;
			} else {
				float diff = mask.r - _Dissolve;
				alp = lerp(0,1,diff*12);
			}
			o.Alpha = alp;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
