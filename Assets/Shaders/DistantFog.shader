// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Unlit/DistantFog"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		//_FadeHeightStart("_FadeHeightStart" , Range (0, 1)) = 0.3
		//_FadeHeightEnd("_FadeHeightEnd" , Range (0, 1)) = 0.4

		_FadeHeightStart("_FadeHeightStart" , float) = 0.3
		_FadeHeightEnd("_FadeHeightEnd" , float) = 0.4
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100

		ZWrite Off
     	Blend SrcAlpha OneMinusSrcAlpha 

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"


			#pragma multi_compile_instancing

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 pos : COLOR0;
			};

			float4 _Color;
			fixed _FadeHeightStart;
			fixed _FadeHeightEnd;

			UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.pos = v.vertex;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				if(i.pos[1] > _FadeHeightEnd)
				{
					return fixed4(0,0,0,0); // invisible
				}
				else if(i.pos[1] > _FadeHeightStart)
				{
					// fade
					fixed deltaFade = _FadeHeightEnd - _FadeHeightStart;
					half alphaTemp = _Color.a;
					alphaTemp = lerp(alphaTemp, 0, (i.pos[1]-_FadeHeightStart)/deltaFade);

					return half4(_Color.x,_Color.y,_Color.z,alphaTemp);
				}
				else
				{
					half4 col = _Color;
					return col;
				}
			}
			ENDCG
		}
	}
}
