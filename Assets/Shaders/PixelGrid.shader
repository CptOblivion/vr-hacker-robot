Shader "Hidden/PixelGrid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Zoom ("Zoom", range(1,30)) = 1
		_Barrel ("Barrel Distortion", Range(-2,2)) = 0
		_PhosphorBleed ("Phosphor Bleed", Range(0,10)) = .5
		_PhosphorBleed2 ("Phosphor Bleed Alt", Range(0, 2)) = .5
		_Intensity ("Grid Intensity", Range(0,1)) = 1
		_gridToggle ("Grid algorithm toggle (REMEMBER TO REMOVE)", Range(0,1)) = 1
		_gridScale ("Pixel Grid Scale", float) = 1
		_borderFade("Border Fade", Range(0,100)) = 4
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
			half _PhosphorBleed;
			half _PhosphorBleed2;
			float _Zoom;
			half _Intensity;
			half _Barrel;
			int _gridToggle;
			half _gridScale;
			half _borderFade;

            fixed4 frag (v2f i) : SV_Target
            {
				float bleed = 1/(3*_PhosphorBleed+1);
				float2 pixelSize = _MainTex_TexelSize.xy*_gridScale;
                fixed4 col = 1;
				
				//barrel distortion
				i.uv = i.uv*2-1; //centered coordinates
				
				i.uv /= _Zoom + pow(min(_Barrel, 0) * -.5, .81);

				half dist = length(i.uv);
				i.uv /= (1+ _Barrel * dist * dist);

				//screen borders
				half clip = smoothstep(1, 1-pixelSize.x * _borderFade, abs(i.uv[0]));
				clip *= smoothstep(1, 1-pixelSize.y * _borderFade, abs(i.uv[1]));

				i.uv = i.uv/2+.5; //back to corner coordinates

				fixed3 imageRaw = tex2D(_MainTex, i.uv); //the unmodified image (post-barrel-distortion, that is)

				//chromatic abberation, sort of, so the pixel bleed matches the image pixels
				float2 uvShift = i.uv;
				fixed3 mainTexShifted = imageRaw;

				uvShift.x += _MainTex_TexelSize/3*_gridScale;
				mainTexShifted.r = tex2D(_MainTex, uvShift).r;
				uvShift.x -= _MainTex_TexelSize/1.5*_gridScale;
				mainTexShifted.b = tex2D(_MainTex, uvShift).b;

				//pixel grid
				i.uv /= pixelSize;
				
				fixed3 pixelRGB = 0;
				if (_gridToggle < 1){
					pixelRGB = fixed3(-.5, .25, 1);
					pixelRGB = sin(i.uv.x * 6.28 - pixelRGB * 3.14) * _PhosphorBleed2 + 1-_PhosphorBleed2;
				}

				else{
					pixelRGB = fixed3(i.uv.x*6. - 1.,i.uv.x*6. - 3.,i.uv.x*6. - 5.);
					pixelRGB = abs(pixelRGB % 6. - 3.);
					pixelRGB *= 1.+_PhosphorBleed;
					pixelRGB -=2.;
					pixelRGB *= bleed;
					pixelRGB = pow(pixelRGB, .4545);
				}

				//horizontal lines
				pixelRGB *= 1-abs(frac(i.uv[1])*2-1);
				pixelRGB = saturate(pixelRGB-.0001);

				//pixel grid strength
				mainTexShifted = pixelRGB * mainTexShifted;
				col.rgb = lerp(imageRaw, mainTexShifted*2, pow(_Intensity, .4545));
				//pixelRGB.g = 0;
				//pixelRGB.r = 0;

				//apply borders
				col.rgb *= clip;
				col.a = clip;
                return col;
            }
            ENDCG
        }
    }
}
