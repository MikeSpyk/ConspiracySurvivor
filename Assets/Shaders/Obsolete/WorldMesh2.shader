Shader "Custom/WorldMesh2" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("_MainTex (RGB)", 2D) = "white" {}
		_SecoundTex ("_SecoundTex (RGB)", 2D) = "white" {}
		_SplashTex ("_SplashTex (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _SecoundTex;
		sampler2D _SplashTex;

		struct Input {
			float2 uv_MainTex;
			float2 uv_SplashTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 mainColor = tex2D (_MainTex, IN.uv_MainTex) ;
			fixed4 secoundColor = tex2D (_SecoundTex, IN.uv_MainTex) ;
			fixed4 splashColor = tex2D (_SplashTex, IN.uv_SplashTex) ;
			fixed4 c = splashColor ;
			o.Albedo = (mainColor.rgb * splashColor.rgb + (1-splashColor.rgb) * secoundColor) *_Color;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
