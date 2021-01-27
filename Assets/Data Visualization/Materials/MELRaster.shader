// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Shader created with Shader Forge v1.30 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.30;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:3,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False;n:type:ShaderForge.SFN_Final,id:3138,x:32106,y:33396,varname:node_3138,prsc:2|emission-1316-OUT,alpha-2337-OUT,clip-325-OUT;n:type:ShaderForge.SFN_Tex2d,id:5348,x:29951,y:33609,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:_node_5348,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Color,id:5054,x:30940,y:33031,ptovrint:False,ptlb:Low,ptin:_Low,varname:node_5054,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.04827571,c3:1,c4:1;n:type:ShaderForge.SFN_Color,id:6249,x:30944,y:34297,ptovrint:False,ptlb:High,ptin:_High,varname:node_6249,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.8344827,c3:1,c4:1;n:type:ShaderForge.SFN_Lerp,id:1623,x:31282,y:33285,varname:node_1623,prsc:2|A-5054-RGB,B-3293-RGB,T-5209-OUT;n:type:ShaderForge.SFN_Ceil,id:579,x:30996,y:34809,varname:node_579,prsc:2|IN-3458-OUT;n:type:ShaderForge.SFN_Slider,id:5624,x:30800,y:35017,ptovrint:False,ptlb:Opacity,ptin:_Opacity,varname:node_5624,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;n:type:ShaderForge.SFN_RemapRange,id:3458,x:30800,y:34809,varname:node_3458,prsc:2,frmn:0.05,frmx:1,tomn:0,tomx:1|IN-3724-OUT;n:type:ShaderForge.SFN_Color,id:8289,x:30940,y:33659,ptovrint:False,ptlb:Medium,ptin:_Medium,varname:_Max_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:0.9310346,c3:0,c4:1;n:type:ShaderForge.SFN_Lerp,id:8784,x:31282,y:34072,varname:node_8784,prsc:2|A-3319-RGB,B-6249-RGB,T-1743-OUT;n:type:ShaderForge.SFN_RemapRange,id:5209,x:30806,y:33278,varname:node_5209,prsc:2,frmn:0,frmx:0.2,tomn:0,tomx:1|IN-1724-OUT;n:type:ShaderForge.SFN_RemapRange,id:5018,x:30806,y:33605,varname:node_5018,prsc:2,frmn:0.2,frmx:0.4,tomn:0,tomx:1|IN-1724-OUT;n:type:ShaderForge.SFN_Add,id:1316,x:31921,y:33396,varname:node_1316,prsc:2|A-1764-OUT,B-8986-OUT,C-9854-OUT,D-2793-OUT,E-8063-OUT;n:type:ShaderForge.SFN_Color,id:3293,x:30940,y:33350,ptovrint:False,ptlb:Low - Medium,ptin:_LowMedium,varname:_Low_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:1,c3:0.006896734,c4:1;n:type:ShaderForge.SFN_Color,id:3319,x:30944,y:33943,ptovrint:False,ptlb:Medium - High,ptin:_MediumHigh,varname:_Medium_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.9034481,c2:0,c3:1,c4:1;n:type:ShaderForge.SFN_RemapRange,id:5916,x:30806,y:33834,varname:node_5916,prsc:2,frmn:0.4,frmx:0.6,tomn:0,tomx:1|IN-1724-OUT;n:type:ShaderForge.SFN_RemapRange,id:1743,x:30809,y:34153,varname:node_1743,prsc:2,frmn:0.6,frmx:0.8,tomn:0,tomx:1|IN-1724-OUT;n:type:ShaderForge.SFN_RemapRange,id:9,x:30798,y:34470,varname:node_9,prsc:2,frmn:0.8,frmx:1,tomn:0,tomx:1|IN-1724-OUT;n:type:ShaderForge.SFN_Lerp,id:6106,x:31282,y:33784,varname:node_6106,prsc:2|A-8289-RGB,B-3319-RGB,T-5916-OUT;n:type:ShaderForge.SFN_Lerp,id:2199,x:31282,y:33535,varname:node_2199,prsc:2|A-3293-RGB,B-8289-RGB,T-5018-OUT;n:type:ShaderForge.SFN_Color,id:9906,x:30935,y:34631,ptovrint:False,ptlb:Extreme,ptin:_Extreme,varname:_High_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Step,id:4836,x:31282,y:33161,varname:node_4836,prsc:2|A-9469-OUT,B-2106-OUT;n:type:ShaderForge.SFN_Vector1,id:2106,x:30940,y:33239,varname:node_2106,prsc:2,v1:0.2;n:type:ShaderForge.SFN_Set,id:6686,x:30181,y:33680,varname:Tex,prsc:2|IN-5348-RGB;n:type:ShaderForge.SFN_Get,id:9469,x:30940,y:33176,varname:node_9469,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Vector1,id:9131,x:30944,y:33561,varname:node_9131,prsc:2,v1:0.4;n:type:ShaderForge.SFN_Get,id:4606,x:30944,y:33497,varname:node_4606,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Step,id:2085,x:31282,y:33414,varname:node_2085,prsc:2|A-4606-OUT,B-9131-OUT;n:type:ShaderForge.SFN_Step,id:4815,x:31282,y:33661,varname:node_4815,prsc:2|A-7003-OUT,B-9495-OUT;n:type:ShaderForge.SFN_Vector1,id:9495,x:30944,y:33864,varname:node_9495,prsc:2,v1:0.6;n:type:ShaderForge.SFN_Get,id:7003,x:30944,y:33800,varname:node_7003,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Multiply,id:1764,x:31622,y:33174,varname:node_1764,prsc:2|A-1623-OUT,B-4836-OUT;n:type:ShaderForge.SFN_Multiply,id:1618,x:31622,y:33304,varname:node_1618,prsc:2|A-2199-OUT,B-2085-OUT;n:type:ShaderForge.SFN_Subtract,id:8986,x:31622,y:33451,varname:node_8986,prsc:2|A-1618-OUT,B-4836-OUT;n:type:ShaderForge.SFN_Multiply,id:5227,x:31622,y:33580,varname:node_5227,prsc:2|A-6106-OUT,B-4815-OUT;n:type:ShaderForge.SFN_OneMinus,id:9444,x:31452,y:33514,varname:node_9444,prsc:2|IN-2085-OUT;n:type:ShaderForge.SFN_Multiply,id:9854,x:31622,y:33725,varname:node_9854,prsc:2|A-5227-OUT,B-9444-OUT;n:type:ShaderForge.SFN_Get,id:5184,x:30944,y:34084,varname:node_5184,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Vector1,id:1559,x:30944,y:34205,varname:node_1559,prsc:2,v1:0.8;n:type:ShaderForge.SFN_Step,id:154,x:31282,y:33932,varname:node_154,prsc:2|A-5184-OUT,B-1559-OUT;n:type:ShaderForge.SFN_Multiply,id:3618,x:31622,y:33862,varname:node_3618,prsc:2|A-8784-OUT,B-154-OUT;n:type:ShaderForge.SFN_OneMinus,id:1786,x:31452,y:33759,varname:node_1786,prsc:2|IN-4815-OUT;n:type:ShaderForge.SFN_Multiply,id:2793,x:31622,y:34016,varname:node_2793,prsc:2|A-3618-OUT,B-1786-OUT;n:type:ShaderForge.SFN_Lerp,id:6518,x:31284,y:34370,varname:node_6518,prsc:2|A-6249-RGB,B-9906-RGB,T-9-OUT;n:type:ShaderForge.SFN_OneMinus,id:9508,x:31447,y:34016,varname:node_9508,prsc:2|IN-154-OUT;n:type:ShaderForge.SFN_Multiply,id:8063,x:31622,y:34158,varname:node_8063,prsc:2|A-6518-OUT,B-9508-OUT;n:type:ShaderForge.SFN_Get,id:1724,x:30473,y:33639,varname:node_1724,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Get,id:3724,x:30563,y:34809,varname:node_3724,prsc:2|IN-9992-OUT;n:type:ShaderForge.SFN_Set,id:9992,x:30181,y:33596,varname:Mask,prsc:2|IN-5348-RGB;n:type:ShaderForge.SFN_Tex2d,id:7349,x:31327,y:35108,ptovrint:False,ptlb:Dither,ptin:_Dither,varname:node_7349,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:ac587ddcc78d9f94b9ea159be0d72226,ntxv:0,isnm:False|UVIN-1125-OUT;n:type:ShaderForge.SFN_ComponentMask,id:325,x:31186,y:34809,varname:node_325,prsc:2,cc1:0,cc2:-1,cc3:-1,cc4:-1|IN-579-OUT;n:type:ShaderForge.SFN_Frac,id:1125,x:31155,y:35125,varname:node_1125,prsc:2|IN-7465-OUT;n:type:ShaderForge.SFN_ScreenPos,id:4296,x:30127,y:35069,varname:node_4296,prsc:2,sctp:0;n:type:ShaderForge.SFN_ScreenParameters,id:6006,x:29678,y:35218,varname:node_6006,prsc:2;n:type:ShaderForge.SFN_Multiply,id:7399,x:30335,y:35116,varname:node_7399,prsc:2|A-4296-UVOUT,B-8240-OUT;n:type:ShaderForge.SFN_Append,id:8240,x:30155,y:35218,varname:node_8240,prsc:2|A-6153-OUT,B-7787-OUT;n:type:ShaderForge.SFN_Vector1,id:5609,x:29678,y:35379,varname:node_5609,prsc:2,v1:32;n:type:ShaderForge.SFN_Divide,id:6153,x:29918,y:35218,varname:node_6153,prsc:2|A-6006-PXW,B-5609-OUT;n:type:ShaderForge.SFN_Divide,id:7787,x:29930,y:35345,varname:node_7787,prsc:2|A-6006-PXH,B-5609-OUT;n:type:ShaderForge.SFN_Multiply,id:2337,x:31351,y:34934,varname:node_2337,prsc:2|A-5624-OUT,B-7349-A;n:type:ShaderForge.SFN_Add,id:7465,x:30800,y:35123,varname:node_7465,prsc:2|A-2411-UVOUT,B-3710-OUT;n:type:ShaderForge.SFN_ValueProperty,id:7322,x:30434,y:35260,ptovrint:False,ptlb:OffsetX,ptin:_OffsetX,varname:node_7322,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:3710,x:30641,y:35260,varname:node_3710,prsc:2|A-7322-OUT,B-8129-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8129,x:30434,y:35348,ptovrint:False,ptlb:OffsetY,ptin:_OffsetY,varname:_node_7322_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Rotator,id:2411,x:30605,y:35077,varname:node_2411,prsc:2|UVIN-7399-OUT,ANG-3439-OUT;n:type:ShaderForge.SFN_Slider,id:5344,x:30049,y:34879,ptovrint:False,ptlb:Rotate,ptin:_Rotate,varname:node_5344,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:1;n:type:ShaderForge.SFN_Pi,id:6821,x:30208,y:34965,varname:node_6821,prsc:2;n:type:ShaderForge.SFN_Multiply,id:3439,x:30405,y:34976,varname:node_3439,prsc:2|A-5344-OUT,B-6821-OUT;proporder:5348-5054-3293-8289-3319-6249-9906-5624-7349-7322-8129-5344;pass:END;sub:END;*/

Shader "Shader Forge/HeightMap" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _Low ("Low", Color) = (0,0.04827571,1,1)
        _LowMedium ("Low - Medium", Color) = (0,1,0.006896734,1)
        _Medium ("Medium", Color) = (1,0.9310346,0,1)
        _MediumHigh ("Medium - High", Color) = (0.9034481,0,1,1)
        _High ("High", Color) = (0,0.8344827,1,1)
        _Extreme ("Extreme", Color) = (1,1,1,1)
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
            //#pragma exclude_renderers gles3 metal d3d11_9x xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _Low;
            uniform float4 _High;
            uniform float _Opacity;
            uniform float4 _Medium;
            uniform float4 _LowMedium;
            uniform float4 _MediumHigh;
            uniform float4 _Extreme;
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
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float3 Mask = _MainTex_var.rgb;
                clip(ceil((Mask*1.052632+-0.05263158)).r - 0.5);
////// Lighting:
////// Emissive:
                float3 Tex = _MainTex_var.rgb;
                float3 node_1724 = Tex;
                float3 node_4836 = step(Tex,0.2);
                float3 node_2085 = step(Tex,0.4);
                float3 node_4815 = step(Tex,0.6);
                float3 node_154 = step(Tex,0.8);
                float3 emissive = ((lerp(_Low.rgb,_LowMedium.rgb,(node_1724*5.0+0.0))*node_4836)+((lerp(_LowMedium.rgb,_Medium.rgb,(node_1724*5.0+-1.0))*node_2085)-node_4836)+((lerp(_Medium.rgb,_MediumHigh.rgb,(node_1724*5.0+-2.0))*node_4815)*(1.0 - node_2085))+((lerp(_MediumHigh.rgb,_High.rgb,(node_1724*5.0+-3.0))*node_154)*(1.0 - node_4815))+(lerp(_High.rgb,_Extreme.rgb,(node_1724*5.0+-4.0))*(1.0 - node_154)));
                float3 finalColor = emissive;
                float node_2411_ang = (_Rotate*3.141592654);
                float node_2411_spd = 1.0;
                float node_2411_cos = cos(node_2411_spd*node_2411_ang);
                float node_2411_sin = sin(node_2411_spd*node_2411_ang);
                float2 node_2411_piv = float2(0.5,0.5);
                float node_5609 = 32.0;
                float2 node_2411 = (mul((i.screenPos.rg*float2((_ScreenParams.r/node_5609),(_ScreenParams.g/node_5609)))-node_2411_piv,float2x2( node_2411_cos, -node_2411_sin, node_2411_sin, node_2411_cos))+node_2411_piv);
                float2 node_1125 = frac((node_2411+float2(_OffsetX,_OffsetY)));
                float4 _Dither_var = tex2D(_Dither,TRANSFORM_TEX(node_1125, _Dither));
                return fixed4(finalColor,(_Opacity*_Dither_var.a));
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
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex );
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float3 Mask = _MainTex_var.rgb;
                clip(ceil((Mask*1.052632+-0.05263158)).r - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
