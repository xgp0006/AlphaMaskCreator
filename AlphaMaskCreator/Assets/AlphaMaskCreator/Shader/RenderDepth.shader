 Shader "AlphaMaskCreator/RenderDepth"
 {
     Properties
     {
         _MainTex ("Base (RGB)", 2D) = "white" {}
     }
     SubShader
     {
         Pass
         {
             CGPROGRAM
 
             #pragma vertex vert
             #pragma fragment frag
             #include "UnityCG.cginc"
             
             uniform sampler2D _MainTex;
             uniform sampler2D _CameraDepthTexture;
             uniform half4 _MainTex_TexelSize;
 
             struct appdata
             {
                 float4 pos : POSITION;
                 half2 uv : TEXCOORD0;
             };
 
             struct v2f
             {
                 float4 pos : SV_POSITION;
                 half2 uv : TEXCOORD0;
             };
 
 
             v2f vert(appdata v)
             {
				 v2f o;

                 o.pos = UnityObjectToClipPos(v.pos);
                 o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.uv);
 
                 return o;
             }
             
             fixed4 frag(v2f i) : COLOR
             {
                 float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
                 return depth;
             }
             
             ENDCG
         }
     } 
 }