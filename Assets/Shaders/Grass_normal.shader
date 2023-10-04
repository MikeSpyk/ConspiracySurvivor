// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Grass_normal" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_ColorLerp ("_ColorLerp", Range(0,1)) = 0.5
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="AlphaTest"   }
		LOD 200
		Cull off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf SimpleLambert addshadow fullforwardshadows halfasview 		//v1
		//#pragma surface surf Standard addshadow fullforwardshadows       v1

		//#pragma surface surf addshadow fullforwardshadows halfasview

		// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0

		half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
              half NdotL = dot (s.Normal, lightDir);
              half4 c;
              c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
              c.a = s.Alpha;
              return c;
          }

		sampler2D _MainTex;

		struct Input 
		{
			float2 uv_MainTex;
		};

		fixed4 _Color;
		//fixed3 worldNormal;
		float _ColorLerp;


		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			//UNITY_DEFINE_INSTANCED_PROP(fixed4, meshNormals)
		UNITY_INSTANCING_BUFFER_END(Props)


		void surf (Input IN, inout SurfaceOutput   o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
			o.Albedo = lerp( c.rgb, _Color,_ColorLerp);
			//fixed3 distanceVec = worldNormal - fixed3(0,1,0);
			//o.Normal = normalize (- distanceVec);//fixed3(0,1,0);
			o.Normal = fixed3(0,1,0);
			//o.Normal = UNITY_ACCESS_INSTANCED_PROP(Props, meshNormals).rgb;
			//o.Normal = mul(UNITY_MATRIX_IT_MV , UNITY_ACCESS_INSTANCED_PROP(Props, meshNormals));
			//o.Normal = mul(UNITY_MATRIX_TEXTURE0  , UNITY_ACCESS_INSTANCED_PROP(Props, meshNormals));
			//o.Normal = mul(unity_WorldToObject  , UNITY_ACCESS_INSTANCED_PROP(Props, meshNormals));
			//o.pos = mul(UNITY_MATRIX_VP, worldPos);
			//o.Occlusion = half(0.0);
			clip(c.a - 0.5);
			//o.Metallic = half(0);
			//o.Smoothness = 0.9;
			o.Alpha = c.a - 0.5;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
