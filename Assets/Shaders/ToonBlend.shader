Shader "Unlit/ToonBlend"
{
	Properties
	{
		/* ────────── SurfaceVariables ────────── */
		_Albedo ("Albedo", Color) = (1, 1, 1)
		_baseColor("Base Color", Color) = (1, 1, 1)
		_Texture0   ("Layer 0 Albedo", 2D) = "white" {}
		_Texture1   ("Layer 1 Albedo", 2D) = "white" {}
		_Texture2   ("Layer 2 Albedo", 2D) = "white" {}
		_Texture3   ("Layer 3 Albedo", 2D) = "white" {}

		_Texture0_N ("Layer 0 Normal", 2D) = "bump" {}
		_Texture1_N ("Layer 0 Normal", 2D) = "bump" {}
		_Texture2_N ("Layer 0 Normal", 2D) = "bump" {}
		_Texture3_N ("Layer 0 Normal", 2D) = "bump" {}
        _SmoothnessToon          ("Smoothness"                , Range(0, 1))   = 0.5
        _Shininess           ("Shininess (Phong power)"   , Float)         = 32
        _RimThreshold        ("Rim Threshold"             , Range(0, 1))   = 0.5
        _RimBrightness       ("Rim Brightness"            , Range(0, 5))   = 1
        _ShadeLevels         ("Shade Levels"              , Range(1, 10))  = 4
        _DarkRimThreshold    ("Dark Rim Threshold"        , Float)   = 0.2
        _DarkRimDarkness     ("Dark Rim Darkness"         , Range(0, 1))   = 0.5
		_DarkRimPower		("Dark Rim Power"         , Float)   = 0.5
		_DarkRimCutoff		("Dark Rim Cutoff"         , Float)   = 0.5
		_Ambient                ("Ambient Colour"               , Color)         = (1,1,1,1)

        /* ────────── EdgeConstants ────────── */
        _EdgeDiffuse             ("Edge Diffuse"              , Range(0, 1)) = 0.1
        _DiffuseOffset           ("Diffuse Offset"            , Range(0, 1)) = 0.1
        _EdgeSpecular            ("Edge Specular"             , Range(0, 1)) = 0.5
        _EdgeSpecularOffset      ("Edge Specular Offset"      , Range(0, 1)) = 0.0
        _EdgeDistanceAttenuation ("Edge Distance Attenuation" , Float)       = 1
        _EdgeShadowAttenuation   ("Edge Shadow Attenuation"   , Float)       = 1
        _EdgeRim                 ("Edge Rim"                  , Range(0, 1)) = 0.5
        _EdgeRimOffset           ("Edge Rim Offset"           , Range(0, 1)) = 0.1
		

	}
SubShader
	{
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

		LOD 100

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
			sampler2D _Texture0;           float4 _Texture0_ST;
		    sampler2D _Texture1;           float4 _Texture1_ST;
		    sampler2D _Texture2;           float4 _Texture2_ST;
		    sampler2D _Texture3;           float4 _Texture3_ST;

		    sampler2D _Texture0_N;
		    sampler2D _Texture1_N;
		    sampler2D _Texture2_N;
		    sampler2D _Texture3_N;

			
			#define UNITY_PI 3.14159265359f
			#define UNITY_TWO_PI 6.28318530718f

			
			
			
			CBUFFER_START(UnityPerMaterial)
				float3 _Albedo;
				float3 _baseColor;
                float  _SmoothnessToon;
                float  _Shininess;
                float  _RimThreshold;
                float  _RimBrightness;
                float  _ShadeLevels;
                float  _DarkRimThreshold;
                float  _DarkRimDarkness;
				float  _DarkRimPower;
				float _DarkRimCutoff;
				float3 _Ambient;

                /* EdgeConstants */
                float  _EdgeDiffuse;
                float  _DiffuseOffset;
                float  _EdgeSpecular;
                float  _EdgeSpecularOffset;
                float  _EdgeDistanceAttenuation;
                float  _EdgeShadowAttenuation;
                float  _EdgeRim;
                float  _EdgeRimOffset;
            CBUFFER_END


			struct VertexInput
			{
				float4 vertex  : POSITION;
				float3 normal  : NORMAL;
				float4 tangent : TANGENT;
				float2 uv	  : TEXCOORD0;
            	float2 uvLM     : TEXCOORD1;
				float4 color : COLOR;
			};

            struct GeomData
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 worldPos  : TEXCOORD1;
                float3 normal    : TEXCOORD2;   // world-space normal
                float4 tangent   : TEXCOORD3;   // world-space tangent (w = handedness)
                float  t         : TEXCOORD4;   // your custom scalar
			    float2 lightmapUV : TEXCOORD5;               // ★ passed to pixel stage
			    float3 vertexSH   : TEXCOORD6;               // ★ SH for probe lighting
			    float4 color      : COLOR;
            };


			struct SurfaceVariables{
				float3 normal;
				float3 view;
			};


			GeomData vert (VertexInput v)
            {
                GeomData o;

                // clip-space position
                o.pos = TransformObjectToHClip(v.vertex.xyz);

                // world-space data
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.normal   = TransformObjectToWorldNormal(v.normal);
                o.tangent  = float4( TransformObjectToWorldDir(v.tangent.xyz),
                                     v.tangent.w );      // preserve handedness

                // texture coordinates
                o.uv = TRANSFORM_TEX(v.uv, _Texture0);

                // custom values
                o.t     = 0;
                o.color = v.color;

			    o.lightmapUV = v.uvLM * unity_LightmapST.xy + unity_LightmapST.zw; // ★
			    o.vertexSH   = SampleSHVertex(TransformObjectToWorldNormal(v.normal)); // ★
				return o;
            }
			
		ENDHLSL
	
		Pass
		{
			Fog {}
			Name "GrassPass"
			Tags { "LightMode" = "UniversalForward" }
			ZWrite On //?
			Cull Back
			
			
			HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"


				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHTS
				#pragma multi_compile _ _SHADOWS_SOFT
				#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				#pragma multi_compile _ LIGHTMAP_ON              // ★ static light-maps
				#pragma multi_compile _ DIRLIGHTMAP_COMBINED     // ★ directional light-map
				#pragma multi_compile _ DYNAMICLIGHTMAP_ON       // ★ realtime GI needed?
				#pragma multi_compile _ _FORWARD_PLUS
				#pragma multi_compile_fog 

				
				#pragma vertex vert
				#pragma fragment frag

				void Applyfog(inout float4 color, float3 positionWS)
				{
				    float4 inColor = color;
				  
				    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
				    float viewZ = -TransformWorldToView(positionWS).z;
				    float nearZ0ToFarZ = max(viewZ - _ProjectionParams.y, 0);
				    float density = 1.0f - ComputeFogIntensity(ComputeFogFactorZ0ToFar(nearZ0ToFarZ));

				    color = lerp(color, unity_FogColor,  density);

				    #else
				    color = color;
				    #endif
				}

				float lightDiffuse(Light l,SurfaceVariables s)
				{
					float diffuse = pow(max(0.0, dot(s.normal, l.direction)),1);
					diffuse = smoothstep(_DiffuseOffset, _DiffuseOffset + _EdgeDiffuse, diffuse);
					float diffuseNormalMapped = max(0.0, dot(s.normal, l.direction));
					return diffuse;
				}
				float lightDiffuseAttenuated(Light l,SurfaceVariables s)
				{
					float diffuse = lightDiffuse(l,s);
					return diffuse*l.shadowAttenuation*l.distanceAttenuation;
				}
				
				float3 lightContribution(Light l,SurfaceVariables s){

				
					float diffuse = lightDiffuse(l,s);

					float3 h = SafeNormalize(l.direction + s.view);
					float specular = saturate(dot(s.normal, h));
					specular = pow(specular, _Shininess);
					specular *= diffuse * _SmoothnessToon;
					specular = _SmoothnessToon * smoothstep(
						(1 - _SmoothnessToon) * _EdgeSpecular + _EdgeSpecularOffset,
						_EdgeSpecular + _EdgeSpecularOffset,
						specular
					);

					float rim = 1 - dot(s.view, s.normal);
					rim *= pow(diffuse, _RimThreshold);
					rim = _SmoothnessToon * _RimBrightness * smoothstep(
						_EdgeRim - 0.5f * _EdgeRimOffset,
						_EdgeRim + 0.5f * _EdgeRimOffset,
						rim
					);
								
					float attenuation = l.distanceAttenuation * (l.shadowAttenuation > 0.7 ? 1 : l.shadowAttenuation);
					float3 shading = (l.color.rgb * (diffuse+ max(specular, rim)));
					return shading*attenuation;
				}

				float fresnel(float3 viewDir, float3 normal, float power)
				{
				    float cosTheta = saturate(dot(viewDir, normal));
				    return 0.04 + (1.0 - 0.04) *pow(1.0 - cosTheta, power);
				}


				float darkRim(SurfaceVariables s, float3 mainLightDir, float diffuse)
				{
					float fresnelass = fresnel(s.normal,s.view,_DarkRimPower);
					float mainDot = dot(s.normal,mainLightDir);

					float rim = saturate(1-saturate(step(fresnelass*mainDot,_DarkRimCutoff))*_DarkRimDarkness);
					float mask = saturate(step(step(1,(1-diffuse)*fresnel(s.view,s.normal,_DarkRimThreshold)),0.001));

					return saturate(rim+mask);
				}
				inline float quantize(float x)
				{
					return floor(x*_ShadeLevels)/_ShadeLevels;
				}
				inline float quantize(float3 x)
				{
					return floor(length(x)*_ShadeLevels)/_ShadeLevels;
				}
				float blueNoise2D(float2 uv)
				{
				    //  hash based on pixel coords (cheap for full-screen or terrain)
				    uv = fmod(uv, 256);                 // keep numbers small for precision
				    return frac(52.9829189 * frac(dot(uv, float2(0.06711056, 0.00583715))));
				}
				float noise2D(float2 uv)
				{
				    //  hash based on pixel coords (cheap for full-screen or terrain)
				    uv = fmod(uv, 256);                 // keep numbers small for precision
				    return frac(52.9829189 * frac(sin(dot(uv, float2(0.06711056, 0.00583715)))));
				}

				float3 quantizeDither(float3 v, float2 screenUV)
				{
				    float dither  = (blueNoise2D(screenUV/2) - 0.5) / _ShadeLevels;
				    return floor(saturate(length(v) + dither/2) * _ShadeLevels) * rcp(_ShadeLevels);
				}

				
				float4 frag(GeomData i) : SV_Target
				{
					float distanceFromCamera = LinearEyeDepth(i.pos.z / i.pos.w, _ZBufferParams)/8192 +0.3;
					
					float4 w      = i.color;                     // vertex-paint weights
					float  sumW   = w.r + w.g + w.b + w.a;
					w            /= max(sumW, 1e-4);             // normalise just in case

					float2 uv0    = TRANSFORM_TEX(i.uv, _Texture0);
					float2 uv1    = TRANSFORM_TEX(i.uv, _Texture1);
					float2 uv2    = TRANSFORM_TEX(i.uv, _Texture2);
					float2 uv3    = TRANSFORM_TEX(i.uv, _Texture3);

					float3 col0   = tex2D(_Texture0, uv0).rgb;
					float3 col1   = tex2D(_Texture1, uv1).rgb;
					float3 col2   = tex2D(_Texture2, uv2).rgb;
					float3 col3   = tex2D(_Texture3, uv3).rgb;

					float3 diffuseColor =     w.r * _baseColor
					                        + w.g * col1
					                        + w.b * col2
					                        + w.a * col3;

					float3 n0 = UnpackNormal(tex2D(_Texture0_N, uv0));
					float3 n1 = UnpackNormal(tex2D(_Texture1_N, uv1));
					float3 n2 = UnpackNormal(tex2D(_Texture2_N, uv2));
					float3 n3 = UnpackNormal(tex2D(_Texture3_N, uv3));
					float3 tangentNormal = normalize(
							w.r * float3(0,0,1) +
							w.g * n1 +
					        w.b * n2 +
					        w.a * n3);
					
					SurfaceVariables s;
					s.view = GetWorldSpaceNormalizeViewDir(i.worldPos);
					float3x3 tbn = CreateTangentToWorld(normalize(i.normal), i.tangent,0);
					float3 normalWS      = normalize(TransformTangentToWorld(
						tangentNormal,      // TS normal
						tbn,           // interpolated WS normal
						false));        // interpolated WS tangent
					s.normal = normalWS;


				// Main directional light contribution
					VertexPositionInputs vertexInput = (VertexPositionInputs) 0;
					vertexInput.positionWS = i.worldPos;

					float4 shadowCoord = GetShadowCoord(vertexInput);
					Light light = GetMainLight(shadowCoord);
					float3 baseColor = diffuseColor*_Albedo;
					
					float3 gi = SAMPLE_GI(i.lightmapUV,float3(0,0,0),s.normal);       
					float3 lighting = lightContribution(light,s); //*SampleAmbientOcclusion()
					float diffuseAccumulated = lightDiffuseAttenuated(light,s);
					// float3 litColor = baseColor*gi*32;
					float3 litColor = baseColor*lighting;
					
					 int pixelLightCount = GetAdditionalLightsCount();
					 for (int lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
					 {
					 	light = GetAdditionalLight(lightIndex, i.worldPos, 1);
					 	lighting = lightContribution(light,s);
					 	diffuseAccumulated += lightDiffuseAttenuated(light,s);
					 	litColor += baseColor*lighting;
					 }

					float2 screenUV = GetNormalizedScreenSpaceUV(i.pos.xy);
				    float ao = SampleAmbientOcclusion(screenUV);
					litColor*=(ao);
					// litColor+=gi;

					float3 finalColor = quantize(litColor)*litColor + _Ambient*baseColor*quantize((0.7+0.3*dot(normalize(s.view*0.25+float3(0,1,0)),s.normal))*ao);
					finalColor = finalColor * darkRim(s,GetMainLight(shadowCoord).direction,diffuseAccumulated)*pow(gi,0.25);
					float4 output = float4(finalColor,1); //float4(color,min((1-i.t)*8,1));
					Applyfog(output,i.worldPos);
					return output;
				}
			ENDHLSL
		}
		Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite on
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitGBufferPassVertex
            #pragma fragment LitGBufferPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            //#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"
            ENDHLSL
        }

	    Pass
	    {
	        Name "ShadowCaster"
	        Tags { "LightMode" = "ShadowCaster" }

	        ZWrite On
	        ZTest  LEqual
	        // Cull Back is the default; use Cull Off for two-sided grass if required
	        // Cull Off

	        HLSLPROGRAM
	        #pragma vertex   ShadowPassVertex      // <- use the helper’s vertex
	        #pragma fragment ShadowPassFragment

	        // Optional: remove _ALPHATEST_ON keyword if you do not alpha-clip in this shader
	        #pragma shader_feature _ALPHATEST_ON
	        #pragma multi_compile_instancing
	        #pragma multi_compile _ DOTS_INSTANCING_ON

	        #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
	        ENDHLSL
	    }
		Pass                           // depth-normals pass for SSAO
		{
		    Name "DepthNormals"
		    Tags { "LightMode" = "DepthNormals" }

		    HLSLPROGRAM
		    #pragma vertex vertDN
		    #pragma fragment fragDN

		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		    struct Attributes   { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
		    struct Varyings     { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

		    Varyings vertDN (Attributes v)
		    {
		        Varyings o;
		        o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
		        o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
		        return o;
		    }

		    float4 fragDN (Varyings i) : SV_Target
		    {
		        return float4(i.normalWS, 0);
		    }
		    ENDHLSL
		}

	}
}