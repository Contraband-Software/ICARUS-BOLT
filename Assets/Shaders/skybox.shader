Shader "Custom/URP Skybox Gradient"
{
    Properties
    {
        _HDRI        ("HDRI Cubemap" , CUBE) = "" {}   // NEW
        _Exposure    ("Exposure"     , Range(0, 8)) = 1 // optional
        _TopColor    ("Zenith Colour", Color) = (0.1, 0.3, 0.6, 1)
        _BottomColor ("Horizon"      , Color) = (0.8, 0.9, 1.0, 1)
        _Contrast    ("Contrast"     , Range(0, 1)) = 0.5
        _ShadeLevels    ("Shade Levels"     , float) = 5
    }


    SubShader
    {
        Tags
        {
            "RenderType"    = "Background"   // important – let URP recognise it as a skybox
            "Queue"         = "Background"
            "RenderPipeline"= "UniversalPipeline"
            // not strictly required but nice to have:
            "PreviewType"   = "Skybox"
        }

        Cull Off          // the mesh Unity uses for the skybox is inside-out
        ZWrite Off
//        ZTest  Always

        Pass
        {
            Name "Skybox"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ------------------------------------------------------------------
            //  Uniforms set from the Properties block
            // ------------------------------------------------------------------
            float4 _TopColor;
            float4 _BottomColor;
            float  _Contrast;
            float _ShadeLevels;
            
            TEXTURECUBE(_HDRI);
            SAMPLER(sampler_HDRI);

            float _Exposure;


            // ------------------------------------------------------------------
            //  Vertex -> fragment structs
            // ------------------------------------------------------------------
            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            // ------------------------------------------------------------------
            //  Vertex shader
            // ------------------------------------------------------------------
            Varyings vert (Attributes v)
            {
                Varyings o;

                // World-space position of the current vertex
                float3 posWS = TransformObjectToWorld(v.positionOS);
                o.positionWS = posWS;

                // Clip-space position for rasterisation
                o.positionCS = TransformWorldToHClip(posWS);
                return o;
            }

            // ------------------------------------------------------------------
            //  Fragment shader
            // ------------------------------------------------------------------
            float4 frag (Varyings i) : SV_Target
            {
                // world-space view direction
                float3 dir = normalize(i.positionWS - _WorldSpaceCameraPos.xyz);

                // sample the cubemap (mip level 0 – highest resolution)
                float3 hdr = SAMPLE_TEXTURECUBE_LOD(
                                 _HDRI, sampler_HDRI,
                                 dir, 0).rgb;

                hdr *= _Exposure;                 // optional manual exposure

                return float4(((1-_Contrast)+_Contrast*floor(length(hdr)*_ShadeLevels)/_ShadeLevels)*normalize(hdr)*pow(length(hdr)/4,0.5), 1); //normalize(hdr)*(0.5+0.5*length(hdr))
            }

            ENDHLSL
        }
    }
}
