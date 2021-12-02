
Shader "MSP2050/RasterShaderCustomizable" {
	Properties{
		_MainTex("MainTex", 2D) = "white" {}
		_ColorGradient("Color Gradient", 2D) = "white" {} 
		_Opacity("Opacity", Range(0, 1)) = 1
		_Dither("Dither", 2D) = "white" {}
		_PatternPixelOffsetX("Pattern Pixel Offset X", Float) = 0
		_PatternPixelOffsetY("Pattern Pixel Offset Y", Float) = 0
		_Rotate("Rotate", Range(0, 1)) = 0
		_ValueCutoff("Value Cutoff", Range(0,1)) = 0.05
	}
		SubShader{
			Tags {
				"IgnoreProjector" = "True"
				"Queue" = "Transparent"
				"RenderType" = "Transparent"
			}
			Pass {
				Name "FORWARD"
				Tags {
					"LightMode" = "ForwardBase"
				}
				Blend SrcAlpha OneMinusSrcAlpha
				ZWrite Off

				CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
				#pragma vertex vert
				#pragma fragment frag
#ifndef UNITY_PASS_FORWARDBASE
				#define UNITY_PASS_FORWARDBASE
#endif
				#include "UnityCG.cginc"
				#pragma multi_compile_fwdbase
				#pragma only_renderers d3d9 d3d11 glcore gles metal
				#pragma target 3.0
				uniform sampler2D _MainTex;
				uniform float4 _MainTex_ST;
				uniform sampler2D _ColorGradient;
				uniform float4 _ColorGradient_ST;
				uniform float _Opacity;
				uniform sampler2D _Dither;
				uniform float4 _Dither_ST;
				uniform float _PatternPixelOffsetX;
				uniform float _PatternPixelOffsetY;
				uniform float _Rotate;
				uniform float _ValueCutoff;

				struct VertexInput {
					float4 vertex : POSITION;
					float2 texcoord0 : TEXCOORD0;
				};
				struct VertexOutput {
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 projPos : TEXCOORD1;
				};

				VertexOutput vert(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord0;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.projPos = ComputeScreenPos(o.pos);
					COMPUTE_EYEDEPTH(o.projPos.z);
					return o;
				}

				float4 frag(VertexOutput i) : COLOR
				{
					float2 screenUV = (i.projPos.xy / i.projPos.w);
					float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
					float sampledValue = _MainTex_var.r;
					clip(sampledValue.r - _ValueCutoff);

					float4 finalColor = tex2D(_ColorGradient, TRANSFORM_TEX(float2(saturate(sampledValue), 0.5), _ColorGradient));

					//Dither pattern.
					float ditherPatternSize = 32.0f;
					float rotationAngle = (_Rotate*3.141592654);
					float rotationCos = cos(rotationAngle);
					float rotationSin = sin(rotationAngle);
					float2 rotationPivot = float2(0.5,0.5);
					float2x2 rotationMatrix = float2x2(rotationCos, -rotationSin, rotationSin, rotationCos);
					float2 ditherUV = round(((screenUV * 2 - 1).rg * float2(_ScreenParams.r, _ScreenParams.g))) / ditherPatternSize;
					float2 rotatedDitherUV = mul(ditherUV - rotationPivot, rotationMatrix) + rotationPivot;
					float4 _Dither_var = tex2D(_Dither, TRANSFORM_TEX(rotatedDitherUV + float2(_PatternPixelOffsetX, _PatternPixelOffsetY), _Dither));
					
					finalColor.a = finalColor.a * (_Opacity * _Dither_var.a);
					return finalColor;
				}
				ENDCG
			}
			Pass {
				Name "ShadowCaster"
				Tags {
					"LightMode" = "ShadowCaster"
				}
				Offset 1, 1
				Cull Back

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
#ifndef UNITY_PASS_SHADOWCASTER
				#define UNITY_PASS_SHADOWCASTER
#endif
				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_shadowcaster
				#pragma only_renderers d3d9 d3d11 glcore gles metal
				#pragma target 3.0
				uniform sampler2D _MainTex; 
				uniform float4 _MainTex_ST;
				uniform float _ValueCutoff;
				struct VertexInput {
					float4 vertex : POSITION;
					float2 texcoord0 : TEXCOORD0;
				};
				struct VertexOutput {
					V2F_SHADOW_CASTER;
					float2 uv0 : TEXCOORD1;
				};
				VertexOutput vert(VertexInput v) {
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord0;
					o.pos = UnityObjectToClipPos(v.vertex);
					TRANSFER_SHADOW_CASTER(o)
					return o;
				}
				float4 frag(VertexOutput i) : COLOR {
					float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
					float Tex = _MainTex_var.r;
					clip(Tex.r - _ValueCutoff);
					SHADOW_CASTER_FRAGMENT(i)
				}
				ENDCG
			}
		}
			FallBack "Diffuse"
}
