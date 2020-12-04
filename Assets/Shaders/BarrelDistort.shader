Shader "Hidden/BarrelDistort"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Zoom ("Zoom", range(1,30)) = 1
		_Barrel ("Barrel Distortion", Range(-2,2)) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

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
			float _Zoom;
			half _Barrel;

            fixed4 frag (v2f i) : SV_Target
            {
				float2 pixelSize = _MainTex_TexelSize.xy;
				
				//barrel distortion
				i.uv = i.uv*2-1; //centered coordinates
				
				i.uv /= _Zoom + pow(min(_Barrel, 0) * -.5, .81);

				half dist = length(i.uv);
				i.uv /= (1+ _Barrel * dist * dist);
				half border = 4;

				//screen borders
				half clip = smoothstep(1, 1-pixelSize.x * border, abs(i.uv[0]));
				clip *= smoothstep(1, 1-pixelSize.y * border, abs(i.uv[1]));

				i.uv = i.uv/2+.5; //back to corner coordinates
				fixed4 col = tex2D(_MainTex, i.uv);
				//apply borders
				col.rgb *= clip;
				col.a = clip;
                return col;
            }
            ENDCG
        }
    }
}
