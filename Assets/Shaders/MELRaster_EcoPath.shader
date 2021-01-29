// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Shader created with Shader Forge v1.30 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.30;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:3,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False;n:type:ShaderForge.SFN_Final,id:3138,x:31929,y:34055,varname:node_3138,prsc:2|emission-8212-OUT,alpha-325-OUT,clip-2337-OUT;n:type:ShaderForge.SFN_Tex2d,id:5348,x:29904,y:34632,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:_node_5348,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Ceil,id:579,x:30996,y:34809,varname:node_579,prsc:2|IN-3458-OUT;n:type:ShaderForge.SFN_Slider,id:5624,x:30800,y:35017,ptovrint:False,ptlb:Opacity,ptin:_Opacity,varname:node_5624,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;n:type:ShaderForge.SFN_RemapRange,id:3458,x:30800,y:34809,varname:node_3458,prsc:2,frmn:0.05,frmx:1,tomn:0,tomx:1|IN-5348-RGB;n:type:ShaderForge.SFN_Tex2d,id:7349,x:31327,y:35108,ptovrint:False,ptlb:Dither,ptin:_Dither,varname:node_7349,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:ac587ddcc78d9f94b9ea159be0d72226,ntxv:0,isnm:False|UVIN-1125-OUT;n:type:ShaderForge.SFN_ComponentMask,id:325,x:31186,y:34809,varname:node_325,prsc:2,cc1:0,cc2:-1,cc3:-1,cc4:-1|IN-579-OUT;n:type:ShaderForge.SFN_Frac,id:1125,x:31155,y:35125,varname:node_1125,prsc:2|IN-7465-OUT;n:type:ShaderForge.SFN_ScreenPos,id:4296,x:30127,y:35069,varname:node_4296,prsc:2,sctp:0;n:type:ShaderForge.SFN_ScreenParameters,id:6006,x:29678,y:35218,varname:node_6006,prsc:2;n:type:ShaderForge.SFN_Multiply,id:7399,x:30335,y:35116,varname:node_7399,prsc:2|A-4296-UVOUT,B-8240-OUT;n:type:ShaderForge.SFN_Append,id:8240,x:30155,y:35218,varname:node_8240,prsc:2|A-6153-OUT,B-7787-OUT;n:type:ShaderForge.SFN_Vector1,id:5609,x:29678,y:35379,varname:node_5609,prsc:2,v1:32;n:type:ShaderForge.SFN_Divide,id:6153,x:29918,y:35218,varname:node_6153,prsc:2|A-6006-PXW,B-5609-OUT;n:type:ShaderForge.SFN_Divide,id:7787,x:29930,y:35345,varname:node_7787,prsc:2|A-6006-PXH,B-5609-OUT;n:type:ShaderForge.SFN_Multiply,id:2337,x:31351,y:34934,varname:node_2337,prsc:2|A-5624-OUT,B-7349-A;n:type:ShaderForge.SFN_Frac,id:8349,x:30996,y:35141,varname:node_8349,prsc:2|IN-7465-OUT;n:type:ShaderForge.SFN_Add,id:7465,x:30800,y:35123,varname:node_7465,prsc:2|A-2411-UVOUT,B-3710-OUT;n:type:ShaderForge.SFN_ValueProperty,id:7322,x:30434,y:35260,ptovrint:False,ptlb:OffsetX,ptin:_OffsetX,varname:node_7322,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:3710,x:30641,y:35260,varname:node_3710,prsc:2|A-7322-OUT,B-8129-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8129,x:30434,y:35348,ptovrint:False,ptlb:OffsetY,ptin:_OffsetY,varname:_node_7322_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Rotator,id:2411,x:30605,y:35077,varname:node_2411,prsc:2|UVIN-7399-OUT,ANG-3439-OUT;n:type:ShaderForge.SFN_Slider,id:5344,x:30049,y:34879,ptovrint:False,ptlb:Rotate,ptin:_Rotate,varname:node_5344,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:1;n:type:ShaderForge.SFN_Pi,id:6821,x:30208,y:34965,varname:node_6821,prsc:2;n:type:ShaderForge.SFN_Multiply,id:3439,x:30405,y:34976,varname:node_3439,prsc:2|A-5344-OUT,B-6821-OUT;n:type:ShaderForge.SFN_Append,id:8212,x:31444,y:33805,varname:node_8212,prsc:2|A-9511-OUT,B-540-OUT,C-2053-OUT;n:type:ShaderForge.SFN_If,id:9511,x:30770,y:33252,cmnt:Red,varname:node_9511,prsc:2|A-3182-OUT,B-161-OUT,GT-4562-OUT,EQ-4562-OUT,LT-9011-OUT;n:type:ShaderForge.SFN_Vector1,id:3182,x:30491,y:33198,varname:node_3182,prsc:2,v1:0.66;n:type:ShaderForge.SFN_Set,id:8873,x:30076,y:34685,varname:Value,prsc:2|IN-5348-R;n:type:ShaderForge.SFN_Get,id:161,x:30470,y:33265,varname:node_161,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Vector1,id:2035,x:30055,y:33569,varname:node_2035,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Vector1,id:7647,x:30295,y:33336,varname:node_7647,prsc:2,v1:0.83333;n:type:ShaderForge.SFN_Subtract,id:4562,x:30491,y:33362,varname:node_4562,prsc:2|A-7647-OUT,B-4929-OUT;n:type:ShaderForge.SFN_Get,id:4428,x:30034,y:33512,varname:node_4428,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Multiply,id:9011,x:30491,y:33512,varname:node_9011,prsc:2|A-4476-OUT,B-2973-OUT;n:type:ShaderForge.SFN_Vector1,id:2973,x:30295,y:33642,varname:node_2973,prsc:2,v1:2;n:type:ShaderForge.SFN_Subtract,id:4476,x:30295,y:33512,varname:node_4476,prsc:2|A-4428-OUT,B-2035-OUT;n:type:ShaderForge.SFN_Get,id:4929,x:30274,y:33398,varname:node_4929,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_If,id:540,x:30781,y:33763,cmnt:Green,varname:node_540,prsc:2|A-3544-OUT,B-9752-OUT,GT-4928-OUT,EQ-4928-OUT,LT-9358-OUT;n:type:ShaderForge.SFN_Vector1,id:3544,x:30502,y:33709,varname:node_3544,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Get,id:9752,x:30481,y:33776,varname:node_9752,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Vector1,id:3931,x:30327,y:33847,varname:node_3931,prsc:2,v1:2.35;n:type:ShaderForge.SFN_Vector1,id:9358,x:30502,y:33984,varname:node_9358,prsc:2,v1:1;n:type:ShaderForge.SFN_Get,id:8188,x:30306,y:33909,varname:node_8188,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Multiply,id:4928,x:30502,y:33847,varname:node_4928,prsc:2|A-3931-OUT,B-8188-OUT;n:type:ShaderForge.SFN_If,id:2053,x:30780,y:34162,cmnt:Blue,varname:node_2053,prsc:2|A-2678-OUT,B-3652-OUT,GT-443-OUT,EQ-443-OUT,LT-5082-OUT;n:type:ShaderForge.SFN_Vector1,id:2678,x:30501,y:34108,varname:node_2678,prsc:2,v1:0.333;n:type:ShaderForge.SFN_Get,id:3652,x:30480,y:34175,varname:node_3652,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Vector1,id:9930,x:30326,y:34246,varname:node_9930,prsc:2,v1:2.5;n:type:ShaderForge.SFN_Get,id:2395,x:30305,y:34308,varname:node_2395,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Multiply,id:443,x:30501,y:34246,varname:node_443,prsc:2|A-9930-OUT,B-2395-OUT;n:type:ShaderForge.SFN_Vector1,id:9379,x:29018,y:34293,varname:node_9379,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Get,id:4070,x:28997,y:34236,varname:node_4070,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Multiply,id:723,x:29454,y:34236,varname:node_723,prsc:2|A-5173-OUT,B-177-OUT;n:type:ShaderForge.SFN_Vector1,id:177,x:29258,y:34366,varname:node_177,prsc:2,v1:2;n:type:ShaderForge.SFN_Subtract,id:5173,x:29258,y:34236,varname:node_5173,prsc:2|A-4070-OUT,B-9379-OUT;n:type:ShaderForge.SFN_Multiply,id:8400,x:29639,y:34237,varname:node_8400,prsc:2|A-723-OUT,B-3393-OUT;n:type:ShaderForge.SFN_Add,id:8858,x:29828,y:34237,varname:node_8858,prsc:2|A-8400-OUT,B-8509-OUT;n:type:ShaderForge.SFN_Vector1,id:3393,x:29454,y:34366,varname:node_3393,prsc:2,v1:1.78;n:type:ShaderForge.SFN_Vector1,id:8509,x:29639,y:34366,varname:node_8509,prsc:2,v1:0.215;n:type:ShaderForge.SFN_If,id:5082,x:30107,y:34157,cmnt:Green,varname:node_5082,prsc:2|A-9577-OUT,B-8192-OUT,GT-9182-OUT,EQ-9182-OUT,LT-8858-OUT;n:type:ShaderForge.SFN_Vector1,id:9577,x:29828,y:34103,varname:node_9577,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Get,id:8192,x:29807,y:34170,varname:node_8192,prsc:2|IN-8873-OUT;n:type:ShaderForge.SFN_Vector1,id:9182,x:29828,y:34388,varname:node_9182,prsc:2,v1:0;proporder:5348-5624-7349-7322-8129-5344;pass:END;sub:END;*/

Shader "Shader Forge/HeightMap" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Dither ("Dither", 2D) = "white" {}
        _OffsetX ("OffsetX", Float ) = 0
        _OffsetY ("OffsetY", Float ) = 0
        _Rotate ("Rotate", Range(0, 1)) = 0
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers gles3 d3d11_9x xbox360 xboxone ps3 ps4 psp2 //metal
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _Opacity;
            uniform sampler2D _Dither; uniform float4 _Dither_ST;
            uniform float _OffsetX;
            uniform float _OffsetY;
            uniform float _Rotate;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex );
                o.screenPos = o.pos;
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float node_2411_ang = (_Rotate*3.141592654);
                float node_2411_spd = 1.0;
                float node_2411_cos = cos(node_2411_spd*node_2411_ang);
                float node_2411_sin = sin(node_2411_spd*node_2411_ang);
                float2 node_2411_piv = float2(0.5,0.5);
                float node_5609 = 32.0;
                float2 node_2411 = (mul((i.screenPos.rg*float2((_ScreenParams.r/node_5609),(_ScreenParams.g/node_5609)))-node_2411_piv,float2x2( node_2411_cos, -node_2411_sin, node_2411_sin, node_2411_cos))+node_2411_piv);
                float2 node_7465 = (node_2411+float2(_OffsetX,_OffsetY));
                float2 node_1125 = frac(node_7465);
                float4 _Dither_var = tex2D(_Dither,TRANSFORM_TEX(node_1125, _Dither));
                clip((_Opacity*_Dither_var.a) - 0.5);
////// Lighting:
////// Emissive:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float Value = _MainTex_var.r;
                float node_9511_if_leA = step(0.66,Value);
                float node_9511_if_leB = step(Value,0.66);
                float node_4562 = (0.83333-Value);
                float node_540_if_leA = step(0.5,Value);
                float node_540_if_leB = step(Value,0.5);
                float node_4928 = (2.35*Value);
                float node_2053_if_leA = step(0.333,Value);
                float node_2053_if_leB = step(Value,0.333);
                float node_5082_if_leA = step(0.5,Value);
                float node_5082_if_leB = step(Value,0.5);
                float node_9182 = 0.0;
                float node_443 = (2.5*Value);
                float3 emissive = float3(lerp((node_9511_if_leA*((Value-0.5)*2.0))+(node_9511_if_leB*node_4562),node_4562,node_9511_if_leA*node_9511_if_leB),lerp((node_540_if_leA*1.0)+(node_540_if_leB*node_4928),node_4928,node_540_if_leA*node_540_if_leB),lerp((node_2053_if_leA*lerp((node_5082_if_leA*((((Value-0.5)*2.0)*1.78)+0.215))+(node_5082_if_leB*node_9182),node_9182,node_5082_if_leA*node_5082_if_leB))+(node_2053_if_leB*node_443),node_443,node_2053_if_leA*node_2053_if_leB));
                float3 finalColor = emissive;
                return fixed4(finalColor,ceil((_MainTex_var.rgb*1.052632+-0.05263158)).r);
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma exclude_renderers gles3 d3d11_9x xbox360 xboxone ps3 ps4 psp2 //metal
            #pragma target 3.0
            uniform float _Opacity;
            uniform sampler2D _Dither; uniform float4 _Dither_ST;
            uniform float _OffsetX;
            uniform float _OffsetY;
            uniform float _Rotate;
            struct VertexInput {
                float4 vertex : POSITION;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float4 screenPos : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex );
                o.screenPos = o.pos;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
                float node_2411_ang = (_Rotate*3.141592654);
                float node_2411_spd = 1.0;
                float node_2411_cos = cos(node_2411_spd*node_2411_ang);
                float node_2411_sin = sin(node_2411_spd*node_2411_ang);
                float2 node_2411_piv = float2(0.5,0.5);
                float node_5609 = 32.0;
                float2 node_2411 = (mul((i.screenPos.rg*float2((_ScreenParams.r/node_5609),(_ScreenParams.g/node_5609)))-node_2411_piv,float2x2( node_2411_cos, -node_2411_sin, node_2411_sin, node_2411_cos))+node_2411_piv);
                float2 node_7465 = (node_2411+float2(_OffsetX,_OffsetY));
                float2 node_1125 = frac(node_7465);
                float4 _Dither_var = tex2D(_Dither,TRANSFORM_TEX(node_1125, _Dither));
                clip((_Opacity*_Dither_var.a) - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
