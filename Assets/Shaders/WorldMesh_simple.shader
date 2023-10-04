Shader "Custom/WorldMesh_simple" {
   Properties 
   {
   		  _ScaleTex ("_ScaleTex", 2D) = "white" {}
	      _GrassTex ("_GrassTexture", 2D) = "white" {}
	      _RockTex ("_RockTexture", 2D) = "gray" {}
	      _SnowTex ("_SnowTex", 2D) = "gray" {}
	      _BeachSandTex ("_BeachSandTex", 2D) = "gray" {}
	      _UnderwaterTex ("_UnderwaterTex", 2D) = "gray" {}
	      _DeadGrassTex ("_DeadGrassTex", 2D) = "gray" {}
	      _DeadGrassColor ("_DeadGrassColor", Color) = (1,1,1,1)
	      _DirtTex ("_DirtTex", 2D) = "gray" {}
	      _ForestTex ("_ForestTex", 2D) = "gray" {}
	      // _VertexTextureMap-Values:
	      // 0: grass, 1:Rock, 2: Snow, 3:beachSand
    }
    SubShader 
    {
	      Tags { "RenderType" = "Opaque" }
	      CGPROGRAM
	      #pragma surface surf Lambert

	      struct Input 
	      {
	          float2 uv_ScaleTex;
	      };
	      sampler2D _GrassTex;
	      sampler2D _RockTex;
	      sampler2D _SnowTex;
	      sampler2D _BeachSandTex;
	      sampler2D _UnderwaterTex;
	      sampler2D _DeadGrassTex;
	      sampler2D _DirtTex;
	      sampler2D _ForestTex;

	      uniform float4 _GrassTex_ST; 
	      uniform float4 _RockTex_ST; 
	      uniform float4 _SnowTex_ST; 
	      uniform float4 _BeachSandTex_ST; 
	      uniform float4 _UnderwaterTex_ST; 
	      uniform float4 _DeadGrassTex_ST; 
	      uniform float4 _DirtTex_ST; 
	      uniform float4 _ForestTex_ST; 

	      half4 _DeadGrassColor;

	      float _VertexTextureMap[169]; // _VertexCountEdge * _VertexCountEdge

	      void surf (Input IN, inout SurfaceOutput o) 
	      {
	         int indexX_A_FloatArray = IN.uv_ScaleTex.x * 13 ;
	         int indexY_A_FloatArray = (int)(IN.uv_ScaleTex.y *13) *13 ;

	         //int indexX_B_FloatArray = indexX_A_FloatArray;
	         //int indexY_B_FloatArray = (int)((IN.uv_RockTex.y * 13) +1) *13;

	         //int indexX_C_FloatArray = indexX_A_FloatArray +1;
	         //int indexY_C_FloatArray = (int)((IN.uv_RockTex.y * 13) +1) *13;

	         //int indexX_D_FloatArray = indexX_A_FloatArray +1;
	         //int indexY_D_FloatArray = indexY_A_FloatArray;

	         float A_Texture_Index = _VertexTextureMap[indexX_A_FloatArray + indexY_A_FloatArray];
	         float B_Texture_Index = _VertexTextureMap[indexX_A_FloatArray + (int)((IN.uv_ScaleTex.y * 13) +1) *13];
	         float C_Texture_Index = _VertexTextureMap[indexX_A_FloatArray +1+ (int)((IN.uv_ScaleTex.y * 13) +1) *13];
	         float D_Texture_Index = _VertexTextureMap[indexX_A_FloatArray +1+ indexY_A_FloatArray];

	         half3 A_Color;
	         half3 B_Color;
	         half3 C_Color;
	         half3 D_Color;

	         if(A_Texture_Index == 0)
	         {
	         	A_Color = tex2D (_GrassTex, (IN.uv_ScaleTex * _GrassTex_ST.xy + _GrassTex_ST.zw) ).rgb;
	         }
	         else if (A_Texture_Index == 1)
	         {
	         	A_Color = tex2D (_RockTex, (IN.uv_ScaleTex * _RockTex_ST.xy + _RockTex_ST.zw)).rgb ;
	         }
	         else if (A_Texture_Index == 2)
	         {
	         	A_Color = tex2D (_SnowTex, (IN.uv_ScaleTex * _SnowTex_ST.xy + _SnowTex_ST.zw) ).rgb;
	         }
	         else if(A_Texture_Index == 3)
	         {
	         	A_Color = tex2D (_UnderwaterTex, (IN.uv_ScaleTex * _UnderwaterTex_ST.xy + _UnderwaterTex_ST.zw) ).rgb;
	         }
	         else if(A_Texture_Index == 4)
	         {
	         	A_Color = tex2D (_BeachSandTex, (IN.uv_ScaleTex * _BeachSandTex_ST.xy + _BeachSandTex_ST.zw) ).rgb;
	         }
	         else if(A_Texture_Index == 5)
	         {
	         	A_Color = tex2D (_DeadGrassTex, (IN.uv_ScaleTex * _DeadGrassTex_ST.xy + _DeadGrassTex_ST.zw) ).rgb * _DeadGrassColor.rgb;
	         }
	         else if(A_Texture_Index == 6)
	         {
	         	A_Color = tex2D (_DirtTex, (IN.uv_ScaleTex * _DirtTex_ST.xy + _DirtTex_ST.zw) ).rgb;
	         }
	         else if(A_Texture_Index == 7)
	         {
	         	A_Color = tex2D (_ForestTex, (IN.uv_ScaleTex * _ForestTex_ST.xy + _ForestTex_ST.zw) ).rgb;
	         }

	         if(B_Texture_Index == 0)
	         {
	         	B_Color = tex2D (_GrassTex, (IN.uv_ScaleTex * _GrassTex_ST.xy + _GrassTex_ST.zw) ).rgb;
	         }
	         else if (B_Texture_Index == 1)
	         {
	         	B_Color = tex2D (_RockTex, (IN.uv_ScaleTex * _RockTex_ST.xy + _RockTex_ST.zw)).rgb ;
	         }
	         else if (B_Texture_Index == 2)
	         {
	         	B_Color = tex2D (_SnowTex, (IN.uv_ScaleTex * _SnowTex_ST.xy + _SnowTex_ST.zw) ).rgb;
	         }
	         else if(B_Texture_Index == 3)
	         {
	         	B_Color = tex2D (_UnderwaterTex, (IN.uv_ScaleTex * _UnderwaterTex_ST.xy + _UnderwaterTex_ST.zw) ).rgb;
	         }
	         else if(B_Texture_Index == 4)
	         {
	         	B_Color = tex2D (_BeachSandTex, (IN.uv_ScaleTex * _BeachSandTex_ST.xy + _BeachSandTex_ST.zw) ).rgb;
	         }
	         else if(B_Texture_Index == 5)
	         {
	         	B_Color = tex2D (_DeadGrassTex, (IN.uv_ScaleTex * _DeadGrassTex_ST.xy + _DeadGrassTex_ST.zw) ).rgb * _DeadGrassColor.rgb;
	         }
	         else if(B_Texture_Index == 6)
	         {
	         	B_Color = tex2D (_DirtTex, (IN.uv_ScaleTex * _DirtTex_ST.xy + _DirtTex_ST.zw) ).rgb;
	         }
	         else if(B_Texture_Index == 7)
	         {
	         	B_Color = tex2D (_ForestTex, (IN.uv_ScaleTex * _ForestTex_ST.xy + _ForestTex_ST.zw) ).rgb;
	         }

	         if(C_Texture_Index == 0)
	         {
	         	C_Color = tex2D (_GrassTex, (IN.uv_ScaleTex * _GrassTex_ST.xy + _GrassTex_ST.zw) ).rgb;
	         }
	         else if (C_Texture_Index == 1)
	         {
	         	C_Color = tex2D (_RockTex, (IN.uv_ScaleTex * _RockTex_ST.xy + _RockTex_ST.zw)).rgb ;
	         }
	         else if (C_Texture_Index == 2)
	         {
	         	C_Color = tex2D (_SnowTex, (IN.uv_ScaleTex * _SnowTex_ST.xy + _SnowTex_ST.zw) ).rgb;
	         }
	         else if(C_Texture_Index == 3)
	         {
	         	C_Color = tex2D (_UnderwaterTex, (IN.uv_ScaleTex * _UnderwaterTex_ST.xy + _UnderwaterTex_ST.zw) ).rgb;
	         }
	         else if(C_Texture_Index == 4)
	         {
	         	C_Color = tex2D (_BeachSandTex, (IN.uv_ScaleTex * _BeachSandTex_ST.xy + _BeachSandTex_ST.zw) ).rgb;
	         }
	         else if(C_Texture_Index == 5)
	         {
	         	C_Color = tex2D (_DeadGrassTex, (IN.uv_ScaleTex * _DeadGrassTex_ST.xy + _DeadGrassTex_ST.zw) ).rgb * _DeadGrassColor.rgb;
	         }
	         else if(C_Texture_Index == 6)
	         {
	         	C_Color = tex2D (_DirtTex, (IN.uv_ScaleTex * _DirtTex_ST.xy + _DirtTex_ST.zw) ).rgb;
	         }
	         else if(C_Texture_Index == 7)
	         {
	         	C_Color = tex2D (_ForestTex, (IN.uv_ScaleTex * _ForestTex_ST.xy + _ForestTex_ST.zw) ).rgb;
	         }

	         if(D_Texture_Index == 0)
	         {
	         	D_Color = tex2D (_GrassTex, (IN.uv_ScaleTex * _GrassTex_ST.xy + _GrassTex_ST.zw) ).rgb;
	         }
	         else if (D_Texture_Index == 1)
	         {
	         	D_Color = tex2D (_RockTex, (IN.uv_ScaleTex * _RockTex_ST.xy + _RockTex_ST.zw)).rgb ;
	         }
	         else if (D_Texture_Index == 2)
	         {
	         	D_Color = tex2D (_SnowTex, (IN.uv_ScaleTex * _SnowTex_ST.xy + _SnowTex_ST.zw) ).rgb;
	         }
	         else if(D_Texture_Index == 3)
	         {
	         	D_Color = tex2D (_UnderwaterTex, (IN.uv_ScaleTex * _UnderwaterTex_ST.xy + _UnderwaterTex_ST.zw) ).rgb;
	         }
	         else if(D_Texture_Index == 4)
	         {
	         	D_Color = tex2D (_BeachSandTex, (IN.uv_ScaleTex * _BeachSandTex_ST.xy + _BeachSandTex_ST.zw) ).rgb;
	         }
	         else if(D_Texture_Index == 5)
	         {
	         	D_Color = tex2D (_DeadGrassTex, (IN.uv_ScaleTex * _DeadGrassTex_ST.xy + _DeadGrassTex_ST.zw) ).rgb * _DeadGrassColor.rgb;
	         }
	         else if(D_Texture_Index == 6)
	         {
	         	D_Color = tex2D (_DirtTex, (IN.uv_ScaleTex * _DirtTex_ST.xy + _DirtTex_ST.zw) ).rgb;
	         }
	         else if(D_Texture_Index == 7)
	         {
	         	D_Color = tex2D (_ForestTex, (IN.uv_ScaleTex * _ForestTex_ST.xy + _ForestTex_ST.zw) ).rgb;
	         }
	
	         // texture size is 1024
	         // vertexCount on edge is 13
	         // pixel-count between two vertices is 78.77

	         float UV_2Vertex_x = (IN.uv_ScaleTex.x * 1024 - indexX_A_FloatArray * 78.77) / 78.77;
	         float UV_2Vertex_y = (IN.uv_ScaleTex.y * 1024 - (indexY_A_FloatArray/13.0) * 78.77) / 78.77;

	         //float3 AB_Color = A_Color * (1-UV_2Vertex_y) + B_Color * UV_2Vertex_y;
	         half3 BC_Color = B_Color * (1-UV_2Vertex_x) + C_Color * UV_2Vertex_x;
	         //float3 CD_Color = D_Color * (1-UV_2Vertex_y) + C_Color * UV_2Vertex_y;
	         half3 DA_Color = A_Color * (1-UV_2Vertex_x) + D_Color * UV_2Vertex_x;

	         //float3 XDim_Color = DA_Color * (1-UV_2Vertex_y) + BC_Color * UV_2Vertex_y;
	         //float3 YDim_Color = AB_Color * (1-UV_2Vertex_x )  + CD_Color * UV_2Vertex_x;

	         o.Albedo = DA_Color * (1-UV_2Vertex_y) + BC_Color * UV_2Vertex_y; // = XDim_Color

	      }
	      ENDCG
    } 
    Fallback "Diffuse"
}
