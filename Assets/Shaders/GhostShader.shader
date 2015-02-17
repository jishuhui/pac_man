Shader "Custom/GhostShader" {
	Properties {
		_ColorTop ("COLOR TOP", Color) = (1,1,1,1)
		_ColorBot ("COLOR BOT", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200
		
		ZWrite On
		Blend One One
		
		CGPROGRAM
		#pragma surface surf Lambert alpha vertex:vert

		sampler2D _MainTex;
		float4 _ColorBot;
		float4 _ColorTop;
		
		void vert (inout appdata_full v) {
             v.vertex.xy *= 1.24;
             v.vertex.x += sin(_Time.z*4+v.vertex.y*6)*0.05;
             v.vertex.z -= 0.1;
             v.color = v.vertex.y*0.9+0.5;
        }
        
        struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float4 color: COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 top = _ColorTop;
			half4 bot = _ColorBot;
			o.Albedo = top*IN.color.x + bot*(1-IN.color.x);
			half rim = saturate(dot (normalize(IN.viewDir), o.Normal));
			o.Alpha = rim*0.4;
		}
		ENDCG
		/**/
		Blend off
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert

		sampler2D _MainTex;
		float4 _ColorTop;
		float4 _ColorBot;
		
		void vert (inout appdata_full v) {
             //v.vertex.xy *= 1.22;
             v.vertex.x += sin(_Time.z*4+v.vertex.y*6)*0.03;
             v.color = v.vertex.y*0.9+0.5;
        }

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 top = _ColorTop;
			half4 bot = _ColorBot;
			o.Albedo = top*IN.color.x + bot*(1-IN.color.x);
			//o.Albedo = IN.color;
			o.Alpha = 1;
		}
		ENDCG
		
	} 
	FallBack "Diffuse"
}
