// AntiBacteria palette sprite shader.
// Sprites are AUTHORED in three index colours and recoloured here from the active palette
// (see Docs/ArtDirection/StyleGuide.md):
//   opaque black -> background colour   (e.g. the player's nucleus "hole")
//   white        -> foreground colour    (player, shots, enemy bodies, walls)
//   red          -> accent colour        (danger and reward: enemy cores, enemy bullets, vitamins, hearts)
//   transparent  -> nothing
// The palette is global (set by PaletteManager), so every sprite changes palette at once.
Shader "AntiBacteria/Palette Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (index colours)", 2D) = "white" {}
        [Toggle] _Flash ("Hit flash (draw all as foreground)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Flash;
            CBUFFER_END

            // Global, written by PaletteManager.
            float4 _PaletteBackground;
            float4 _PaletteForeground;
            float4 _PaletteAccent;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // Classify the authoring colour: white has high green, red has high red but low green.
                half3 c = t.g > 0.5h ? _PaletteForeground.rgb
                        : (t.r > 0.5h ? _PaletteAccent.rgb : _PaletteBackground.rgb);
                c = _Flash > 0.5 ? _PaletteForeground.rgb : c;
                // SpriteRenderer.color only fades (alpha); the palette decides every colour.
                return half4(c, t.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
