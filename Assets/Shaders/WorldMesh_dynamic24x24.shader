Shader "Custom/WorldMesh_dynamic24x24" {
   Properties {
   		  _ScaleTex ("_ScaleTex", 2D) = "white" {}

		  _Metallic("DEBUG_Metallic", Range(0,1)) = 0.0
		  _Smoothness("DEBUG_Smoothness", Range(0,1)) = 0.0
		  _Occlusion("Occlusion", Range(0,1)) = 0.0
		  _TexScaleFactor("_TexScaleFactor", float) = 1
    }
    SubShader 
    {
	      Tags { "RenderType" = "Opaque" }
	      CGPROGRAM
	      #pragma surface surf Standard

	      #pragma target 3.0
	      //#include "UnityCG.cginc"

	      UNITY_DECLARE_TEX2DARRAY(_AlbedoTextures512);
	      UNITY_DECLARE_TEX2DARRAY(_NormalTextures512);
	      UNITY_DECLARE_TEX2DARRAY(_AlbedoTextures1024);
	      UNITY_DECLARE_TEX2DARRAY(_NormalTextures1024);
	      UNITY_DECLARE_TEX2DARRAY(_AlbedoTextures2048);
	      UNITY_DECLARE_TEX2DARRAY(_NormalTextures2048);

	      struct Input 
	      {
	          float2 uv_ScaleTex;
	      };

	      //uniform float4 _Tex1_ST; 

	      half _Metallic;
	      half _Smoothness;
	      half _Occlusion;
	      float _TexScaleFactor;
		  int _UVWorldOffsetX;
		  int _UVWorldOffsetZ;

	      float _VertexTextureMap[576]; // _VertexCountEdge * _VertexCountEdge
	      float _WorldIndexToTexArray[11]; //  texture-count in worldmanager
	      float _WorldIndexToTexArrayIndex[11]; //  texture-count in worldmanager
	      float _TexturesScale[11]; //  texture-count in worldmanager
	      float _TexturesSmoothness[11]; //  texture-count in worldmanager
	      float _TexturesMetallic[11]; //  texture-count in worldmanager

	      void surf (Input IN, inout SurfaceOutputStandard o) 
	      {
	         int indexX_A_FloatArray = IN.uv_ScaleTex.x * 24 ;
	         int indexY_A_FloatArray = (int)(IN.uv_ScaleTex.y *24) *24 ;

	         float A_Texture_Index = _VertexTextureMap[indexX_A_FloatArray + indexY_A_FloatArray];
	         float B_Texture_Index = _VertexTextureMap[indexX_A_FloatArray + (int)((IN.uv_ScaleTex.y * 24) +1) *24];
	         float C_Texture_Index = _VertexTextureMap[indexX_A_FloatArray +1+ (int)((IN.uv_ScaleTex.y * 24) +1) *24];
	         float D_Texture_Index = _VertexTextureMap[indexX_A_FloatArray +1+ indexY_A_FloatArray];

	         half3 A_Color;
	         half3 B_Color;
	         half3 C_Color;
	         half3 D_Color;

	         half3 A_Normal;
	         half3 B_Normal;
	         half3 C_Normal;
	         half3 D_Normal;

	         half3 A_Additional_Fading = half3(_TexturesSmoothness[A_Texture_Index], _TexturesMetallic[A_Texture_Index],0);
	         half3 B_Additional_Fading = half3(_TexturesSmoothness[B_Texture_Index], _TexturesMetallic[B_Texture_Index],0);
	         half3 C_Additional_Fading = half3(_TexturesSmoothness[C_Texture_Index], _TexturesMetallic[C_Texture_Index],0);
	         half3 D_Additional_Fading = half3(_TexturesSmoothness[D_Texture_Index], _TexturesMetallic[D_Texture_Index],0);

	         //float3 A_Normal = normalize( float3(1,1,1));
	         //float3 B_Normal = normalize( float3(1,1,1));
	         //float3 C_Normal = normalize( float3(1,1,1));
	         //float3 D_Normal = normalize( float3(1,1,1));

			 float2 uv_Scaled = IN.uv_ScaleTex *_TexScaleFactor *24 + int2(_UVWorldOffsetX, _UVWorldOffsetZ);


	         if(_WorldIndexToTexArray[A_Texture_Index] == 0) // resolution 512 x 512
	         {
	         	A_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures512, 							half3(uv_Scaled * _TexturesScale[A_Texture_Index], _WorldIndexToTexArrayIndex[A_Texture_Index]));
	         	A_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures512, 	half3(uv_Scaled * _TexturesScale[A_Texture_Index], _WorldIndexToTexArrayIndex[A_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[A_Texture_Index] == 1) // resolution 1024 x 1024
	         {
	         	A_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures1024, half3(uv_Scaled * _TexturesScale[A_Texture_Index], _WorldIndexToTexArrayIndex[A_Texture_Index]));
	         	A_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures1024, half3(uv_Scaled* _TexturesScale[A_Texture_Index], _WorldIndexToTexArrayIndex[A_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[A_Texture_Index] == 2) // resolution 2048 x 2048
	         {
	         	A_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures2048, half3(uv_Scaled * _TexturesScale[A_Texture_Index], _WorldIndexToTexArrayIndex[A_Texture_Index]));
	         	A_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures2048, half3(uv_Scaled * _TexturesScale[A_Texture_Index], _WorldIndexToTexArrayIndex[A_Texture_Index])));
	         }

	          if(_WorldIndexToTexArray[B_Texture_Index] == 0) // resolution 512 x 512
	         {
	         	B_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures512, half3(uv_Scaled * _TexturesScale[B_Texture_Index], _WorldIndexToTexArrayIndex[B_Texture_Index]));
	         	B_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures512, half3(uv_Scaled * _TexturesScale[B_Texture_Index], _WorldIndexToTexArrayIndex[B_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[B_Texture_Index] == 1) // resolution 1024 x 1024
	         {
	         	B_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures1024, half3(uv_Scaled * _TexturesScale[B_Texture_Index] , _WorldIndexToTexArrayIndex[B_Texture_Index]));
	         	B_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures1024, half3(uv_Scaled * _TexturesScale[B_Texture_Index], _WorldIndexToTexArrayIndex[B_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[B_Texture_Index] == 2) // resolution 2048 x 2048
	         {
	         	B_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures2048, half3(uv_Scaled * _TexturesScale[B_Texture_Index] , _WorldIndexToTexArrayIndex[B_Texture_Index]));
	         	B_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures2048, half3(uv_Scaled * _TexturesScale[B_Texture_Index], _WorldIndexToTexArrayIndex[B_Texture_Index])));
	         }

	          if(_WorldIndexToTexArray[C_Texture_Index] == 0) // resolution 512 x 512
	         {
	         	C_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures512, half3(uv_Scaled * _TexturesScale[C_Texture_Index], _WorldIndexToTexArrayIndex[C_Texture_Index]));
	         	C_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures512, half3(uv_Scaled * _TexturesScale[C_Texture_Index], _WorldIndexToTexArrayIndex[C_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[C_Texture_Index] == 1) // resolution 1024 x 1024
	         {
	         	C_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures1024, half3(uv_Scaled * _TexturesScale[C_Texture_Index], _WorldIndexToTexArrayIndex[C_Texture_Index]));
	         	C_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures1024, half3(uv_Scaled * _TexturesScale[C_Texture_Index], _WorldIndexToTexArrayIndex[C_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[C_Texture_Index] == 2) // resolution 2048 x 2048
	         {
	         	C_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures2048, half3(uv_Scaled * _TexturesScale[C_Texture_Index], _WorldIndexToTexArrayIndex[C_Texture_Index]));
	         	C_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures2048, half3(uv_Scaled * _TexturesScale[C_Texture_Index], _WorldIndexToTexArrayIndex[C_Texture_Index])));
	         }

	          if(_WorldIndexToTexArray[D_Texture_Index] == 0) // resolution 512 x 512
	         {
	         	D_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures512, half3(uv_Scaled * _TexturesScale[D_Texture_Index], _WorldIndexToTexArrayIndex[D_Texture_Index]));
	         	D_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures512, half3(uv_Scaled * _TexturesScale[D_Texture_Index], _WorldIndexToTexArrayIndex[D_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[D_Texture_Index] == 1) // resolution 1024 x 1024
	         {
	         	D_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures1024, half3(uv_Scaled * _TexturesScale[D_Texture_Index], _WorldIndexToTexArrayIndex[D_Texture_Index]));
	         	D_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures1024, half3(uv_Scaled * _TexturesScale[D_Texture_Index], _WorldIndexToTexArrayIndex[D_Texture_Index])));
	         }
	         else if(_WorldIndexToTexArray[D_Texture_Index] == 2) // resolution 2048 x 2048
	         {
	         	D_Color = 		UNITY_SAMPLE_TEX2DARRAY(_AlbedoTextures2048, half3(uv_Scaled * _TexturesScale[D_Texture_Index], _WorldIndexToTexArrayIndex[D_Texture_Index]));
	         	D_Normal = 	UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalTextures2048, half3(uv_Scaled * _TexturesScale[D_Texture_Index], _WorldIndexToTexArrayIndex[D_Texture_Index])));
	         }
	
	         // texture size is 2048
	         // vertexCount on edge is 24
	         // pixel-count between two vertices is 85.3333333333333

	         half3x3 A_combined = half3x3(A_Color.x, A_Color.y, A_Color.z, 
	         									A_Normal.x, A_Normal.y, A_Normal.z, 
	         									A_Additional_Fading.x , A_Additional_Fading.y, A_Additional_Fading.z		);

	         half3x3 B_combined = half3x3(B_Color.x, B_Color.y, B_Color.z, 
	         									B_Normal.x, B_Normal.y, B_Normal.z, 
	         									B_Additional_Fading.x , B_Additional_Fading.y, B_Additional_Fading.z		);

	         half3x3 C_combined = half3x3(C_Color.x, C_Color.y, C_Color.z, 
	         									C_Normal.x, C_Normal.y, C_Normal.z, 
	         									C_Additional_Fading.x , C_Additional_Fading.y, C_Additional_Fading.z		);

	         half3x3 D_combined = half3x3(D_Color.x, D_Color.y, D_Color.z, 
	         									D_Normal.x, D_Normal.y, D_Normal.z, 
	         									D_Additional_Fading.x , D_Additional_Fading.y, D_Additional_Fading.z		);



	         float UV_2Vertex_x = (IN.uv_ScaleTex.x * 2048 - indexX_A_FloatArray * 85.333) / 85.333; // 1024 / 24 = 42.666
	         float UV_2Vertex_y = (IN.uv_ScaleTex.y * 2048 - (indexY_A_FloatArray/24.0) * 85.333) / 85.333; // 1024 / 13 = 42.666

	         //float3 AB_Color = A_Color * (1-UV_2Vertex_y) + B_Color * UV_2Vertex_y;
	         //half3 BC_Color = B_Color * (1-UV_2Vertex_x) + C_Color * UV_2Vertex_x;
	         //float3 CD_Color = D_Color * (1-UV_2Vertex_y) + C_Color * UV_2Vertex_y;
	         //half3 DA_Color = A_Color * (1-UV_2Vertex_x) + D_Color * UV_2Vertex_x;

	         //float3 BC_Normal = B_Normal * (1-UV_2Vertex_x) + C_Normal * UV_2Vertex_x;
	         //float3 DA_Normal = A_Normal * (1-UV_2Vertex_x) + D_Normal * UV_2Vertex_x;

	         half3x3 BC_Result = B_combined * (1-UV_2Vertex_x) + C_combined * UV_2Vertex_x;
	         half3x3 DA_Result = A_combined * (1-UV_2Vertex_x) + D_combined * UV_2Vertex_x;

	         half3x3 Final_Result = DA_Result * (1-UV_2Vertex_y) + BC_Result * UV_2Vertex_y;

	         //float3 XDim_Color = DA_Color * (1-UV_2Vertex_y) + BC_Color * UV_2Vertex_y;
	         //float3 YDim_Color = AB_Color * (1-UV_2Vertex_x )  + CD_Color * UV_2Vertex_x;

	         //o.Albedo = DA_Color * (1-UV_2Vertex_y) + BC_Color * UV_2Vertex_y; // = XDim_Color

	         o.Albedo = Final_Result[0];

	         //o.Albedo = float3(.5,.5,.5);

	         //o.Normal = DA_Normal * (1-UV_2Vertex_y) + BC_Normal * UV_2Vertex_y;

	         o.Normal = Final_Result[1];

	         o.Metallic = Final_Result[2].y;
	         o.Smoothness = Final_Result[2].x;

	         //o.Metallic = _Metallic;
	         //o.Smoothness = _Smoothness;

	         o.Occlusion = _Occlusion;
	      }
	      ENDCG
    } 
    Fallback "Diffuse"
}