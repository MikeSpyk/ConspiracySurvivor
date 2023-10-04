// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Terrain_Rock" {
	Properties {
		_RockTex ("Albedo (RGB)", 2D) = "white" {}
		_RockTexBumpMap ("Bumpmap", 2D) = "bump" {}
		_GroundTex ("Albedo (RGB)", 2D) = "white" {}
		_GroundTexBumpMap ("Bumpmap", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Occlusion ("Occlusion", Range(0,1)) = 0.0
		_TexFadingStart("TexFadingStart", Range(0,4)) = 0.0
		_TexFadingEnd("TexFadingEnd", Range(0,4)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _RockTex;
		sampler2D _RockTexBumpMap;
		sampler2D _GroundTex;
		sampler2D _GroundTexBumpMap;

		struct Input 
		{
			float2 uv_RockTex;
			float2 uv_RockTexBumpMap;
			float2 uv_GroundTex;
			float2 uv_GroundTexBumpMap;
			float3 normalW;
			INTERNAL_DATA
		};

		half _Glossiness;
		half _Metallic;
		float _TexFadingStart;
		float _TexFadingEnd;
		half _Occlusion;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		 void vert(inout appdata_full v, out Input data)
		 {
            UNITY_INITIALIZE_OUTPUT(Input,data);
            data.normalW = mul((float3x3)unity_ObjectToWorld, v.normal);
        }

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Occlusion = _Occlusion;

			float3 vectorUp = float3(0,1,0);
			float angle = abs( dot(IN.normalW, vectorUp));

			if(angle < _TexFadingStart)
			{
				o.Albedo =  tex2D (_RockTex, IN.uv_RockTex);
				//o.Alpha = c.a;
				o.Normal = UnpackNormal (tex2D (_RockTexBumpMap, IN.uv_RockTexBumpMap));
				//o.Albedo = float3(1,0,0);
			}
			else if(angle < _TexFadingEnd)
			{
				o.Albedo = lerp(tex2D (_RockTex, IN.uv_RockTex),tex2D (_GroundTex, IN.uv_GroundTex), (angle-_TexFadingStart)/(_TexFadingEnd-_TexFadingStart) );
				o.Normal = lerp(UnpackNormal (tex2D (_RockTexBumpMap, IN.uv_RockTexBumpMap)),UnpackNormal (tex2D (_GroundTexBumpMap, IN.uv_GroundTexBumpMap)), (angle-_TexFadingStart)/(_TexFadingEnd-_TexFadingStart) );
				//o.Albedo = float3(0,0,1);
			}
			else
			{
				o.Albedo = tex2D (_GroundTex, IN.uv_GroundTex);
				//o.Alpha = c.a;
				o.Normal = UnpackNormal (tex2D (_GroundTexBumpMap, IN.uv_GroundTexBumpMap));
				//o.Albedo = float3(0,1,0);
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}