Shader "UI/Unlit/InteractiveColor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _HoverColor ("Hover Color", Color) = (1,1,1,1)
        _CurrentColor ("Current Color", Color) = (1,1,1,1)
        _TargetColor ("Target Color", Color) = (1,1,1,1)
        _LerpFactor ("Lerp Factor", Range(0,1)) = 0
        
        // UI-specific properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "UI_MAIN"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _MainColor;
            fixed4 _HoverColor;
            fixed4 _CurrentColor;
            fixed4 _TargetColor;
            float _LerpFactor;
            float4 _ClipRect;
            float4 _TextureSampleAdd;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample texture
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd);
                
                // Blend between current and target colors
                fixed4 blendedColor = lerp(_CurrentColor, _TargetColor, saturate(_LerpFactor));
                
                // Apply color blending
                color *= blendedColor * i.color;
                
                #ifdef UNITY_UI_CLIP_RECT
                // Apply clipping rectangle (for ScrollRect, etc.)
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                // Alpha clipping for hard edges
                clip(color.a - 0.001);
                #endif
                
                // CRITICAL: Premultiply alpha for proper transparency blending
                // This is what makes edges smooth with "Blend One OneMinusSrcAlpha"
                color.rgb *= color.a;
                
                return color;
            }
            ENDCG
        }
    }
    
    FallBack "UI/Default"
}