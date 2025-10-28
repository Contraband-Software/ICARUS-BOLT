Shader "Unlit/Toon3"
{
	Properties
	{
		/* ────────── SurfaceVariables ────────── */
		_GroundDiffuse ("ground texture", 2D) = "white" {}
		_Albedo ("albedo", Color) = (1, 1, 1)
		_GroundNormal ("ground normal", 2D) = "bump" {}
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
		[Toggle(USE_ALPHACLIP)] _AlphaClip("Enable Alpha‐Clip", Float) = 0
		_Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

	    [Toggle(USE_ROUGHNESS_MAP)] _UseRoughnessMap("Use Roughness Map", Float) = 0
	    _RoughnessMap("Roughness Map", 2D) = "white" {}


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

			
			#define UNITY_PI 3.14159265359f
			#define UNITY_TWO_PI 6.28318530718f

			
			
			
			CBUFFER_START(UnityPerMaterial)
				sampler2D _GroundDiffuse;
				sampler2D _GroundNormal;
				float3 _Albedo;
				float4 _GroundDiffuse_ST;
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
				float _Cutoff;

				sampler2D _RoughnessMap;


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
				float3 nomalStrong;
				float3 view;
				float smoothness;
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
                o.uv = TRANSFORM_TEX(v.uv, _GroundDiffuse);

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
				// #pragma multi_compile _ DYNAMICLIGHTMAP_ON       // ★ realtime GI needed?
				// #pragma multi_compile _ _FORWARD_PLUS
				#pragma multi_compile_fog
				#pragma shader_feature_local USE_ALPHACLIP
				#pragma shader_feature_local USE_ROUGHNESS_MAP

				// #if defined(UNITY_DOTS_INSTANCING_ENABLED)
				// #define LIGHTMAP_NAME unity_Lightmaps
				// #define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
				// #define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
				// #define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV, unity_LightmapIndex.x
				// #else
				// #define LIGHTMAP_NAME unity_Lightmap
				// #define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
				// #define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
				// #define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV
				// #endif


				
				#pragma vertex vert
				#pragma fragment frag

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


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
					specular *= diffuse * s.smoothness;
					specular = s.smoothness * smoothstep(
						(1 - s.smoothness) * _EdgeSpecular + _EdgeSpecularOffset,
						_EdgeSpecular + _EdgeSpecularOffset,
						specular
					);

					float rim = 1 - dot(s.view, s.normal);
					rim *= pow(diffuse, _RimThreshold);
					rim = s.smoothness * _RimBrightness * smoothstep(
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

				
				float4 frag(GeomData i) : SV_Target
				{
					float3 diffuseColor = tex2Dbias(_GroundDiffuse, float4(i.uv, 0, 0)).rgb* _Albedo;
					#ifdef USE_ALPHACLIP
						float alpha = tex2Dbias(_GroundDiffuse, float4(i.uv, 0, 0)).a;
					    clip(alpha - _Cutoff);
					#endif

					float distanceFromCamera = LinearEyeDepth(i.pos.z / i.pos.w, _ZBufferParams)/8192 +0.3;
					float3 tangentNormal = UnpackNormal(tex2Dbias(_GroundNormal, float4(i.uv, 0, 0)));
					float3 tangentNormalStrong = normalize(tangentNormal*float3(5,5,1));
					float3x3 tbn = CreateTangentToWorld(normalize(i.normal), i.tangent,0);

					float3 normalWS      = normalize(TransformTangentToWorld(
                                     tangentNormal,      // TS normal
                                     tbn,           // interpolated WS normal
                                     false));        // interpolated WS tangent
					float3 normalWSStrong      = normalize(TransformTangentToWorld(
                                     tangentNormalStrong,      // TS normal
                                     tbn,           // interpolated WS normal
                                     false));      

					SurfaceVariables s;
					s.normal = normalWS;//i.normal;
					s.nomalStrong = normalWSStrong;
					s.view = GetWorldSpaceNormalizeViewDir(i.worldPos);
					s.smoothness = _SmoothnessToon;
					#ifdef USE_ROUGHNESS_MAP
					    s.smoothness = 1-tex2Dbias(_RoughnessMap, float4(i.uv, 0, 2)).r;
					#endif



				// Main directional light contribution
					VertexPositionInputs vertexInput = (VertexPositionInputs) 0;
					vertexInput.positionWS = i.worldPos;

					float4 shadowCoord = GetShadowCoord(vertexInput);
					Light light = GetMainLight(shadowCoord);
					float3 baseColor = diffuseColor;
					
					float3 gi = SAMPLE_GI(i.lightmapUV,float3(0,0,0),s.normal);
					// half4 cubemap = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0,  reflect(-s.view, s.normal), (1-s.smoothness)*4)*s.smoothness;
					float3 cubemap = CalculateIrradianceFromReflectionProbes(reflect(-s.view, s.normal),i.worldPos,1-s.smoothness);
					float3 lighting = lightContribution(light,s)+cubemap; //*SampleAmbientOcclusion()
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
				    float ao = SampleAmbientOcclusion(screenUV) *min(gi, 0.06)/0.06;
					litColor*=(ao);
					litColor+=gi*ao*baseColor;

					float3 finalColor = quantize(litColor)*litColor + _Ambient*baseColor*quantize((0.7+0.3*dot(normalize(s.view*0.25+float3(0,1,0)),s.nomalStrong))*ao);
					finalColor = finalColor * darkRim(s,GetMainLight(shadowCoord).direction,diffuseAccumulated);//*quantize(pow(gi,0.25));
					float4 output = float4(finalColor,1); //float4(color,min((1-i.t)*8,1));
					Applyfog(output,i.worldPos);
					return output;
				}
			ENDHLSL
		}


	    Pass // make a performance independent alpha clip version TODO
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
	        // #pragma multi_compile _DOTS_INSTANCING_ON // needed?

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
		    #pragma shader_feature_local USE_ALPHACLIP


		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#ifdef USE_ALPHACLIP
		    	struct Attributes   { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
				struct Varyings     { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
			#else
				struct Attributes   { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
				struct Varyings     { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
			#endif
			

		    Varyings vertDN (Attributes v)
		    {
		        Varyings o;
		        o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
		        o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
		    	#ifdef USE_ALPHACLIP
					o.uv = v.uv;
		    	#endif
		        return o;
		    }

		    float4 fragDN (Varyings i) : SV_Target
		    {
		    	#ifdef USE_ALPHACLIP
		    		float alpha = tex2Dbias(_GroundDiffuse, float4(i.uv, 0, 0)).a;
				    clip(alpha - _Cutoff);
				#endif
		        return float4(i.normalWS, 0);
		    }
		    ENDHLSL
		}

	}
}