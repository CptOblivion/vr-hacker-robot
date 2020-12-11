Shader "Hidden/BarrelDistort"
{
    Properties
    {
		_Zoom("Zoom", range(1,30)) = 1
		_Barrel("Barrel Distortion", Range(-2,2)) = 0
		[PerRendererData]_MainTex ("Texture", 2D) = "white" {}
		
		// required for UI.Mask
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
    }
    SubShader
    {
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}
		Stencil
		 {
			 Ref[_Stencil]
			 Comp[_StencilComp]
			 Pass[_StencilOp]
			 ReadMask[_StencilReadMask]
			 WriteMask[_StencilWriteMask]
		 }

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _MainTex_ST;
			float _Zoom;
			half _Barrel;

			fixed4 frag(v2f i) : SV_Target
			{
				float2 pixelSize = _MainTex_TexelSize.xy;
				
				//barrel distortion
				i.uv = i.uv*2-1; //centered coordinates
				
				i.uv /= _Zoom + pow(min(_Barrel, 0) * -.5, .81);

				half dist = length(i.uv);
				i.uv /= (1+ _Barrel * dist * dist);
				half border = 4;

				//screen borders
				half tempMask = smoothstep(1, 1-pixelSize.x * border, abs(i.uv[0]));
				tempMask *= smoothstep(1, 1-pixelSize.y * border, abs(i.uv[1]));

				i.uv = i.uv/2+.5; //back to corner coordinates



				fixed4 col = tex2D(_MainTex, i.uv);
				//apply borders
				col.rgb *= tempMask;
				col.a = tempMask;
#ifdef UNITY_UI_ALPHACLIP
				clip(col.a - 0.001);
#endif
                return col;
            }
            ENDCG
        }
    }
}
