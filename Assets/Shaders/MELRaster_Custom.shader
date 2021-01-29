// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:32106,y:33396,varname:node_3138,prsc:2|emission-1316-OUT,alpha-2337-OUT,clip-325-OUT;n:type:ShaderForge.SFN_Tex2d,id:5348,x:29973,y:33661,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:_node_5348,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Color,id:5054,x:29120,y:33426,ptovrint:False,ptlb:Low,ptin:_Low,varname:node_5054,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.04827571,c3:1,c4:1;n:type:ShaderForge.SFN_Color,id:6249,x:29105,y:34165,ptovrint:False,ptlb:High,ptin:_High,varname:node_6249,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.8344827,c3:1,c4:1;n:type:ShaderForge.SFN_Ceil,id:579,x:31106,y:32420,varname:node_579,prsc:2|IN-3458-OUT;n:type:ShaderForge.SFN_Slider,id:5624,x:30917,y:34703,ptovrint:False,ptlb:Opacity,ptin:_Opacity,varname:node_5624,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;n:type:ShaderForge.SFN_RemapRange,id:3458,x:30910,y:32420,varname:node_3458,prsc:2,frmn:0.05,frmx:1,tomn:0,tomx:1|IN-3724-OUT;n:type:ShaderForge.SFN_Color,id:8289,x:29105,y:33799,ptovrint:False,ptlb:Medium,ptin:_Medium,varname:_Max_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:0.9310346,c3:0,c4:1;n:type:ShaderForge.SFN_Add,id:1316,x:31921,y:33396,varname:node_1316,prsc:2|A-2232-OUT,B-859-OUT,C-4780-OUT,D-5909-OUT;n:type:ShaderForge.SFN_Color,id:3293,x:29109,y:33620,ptovrint:False,ptlb:Low - Medium,ptin:_LowMedium,varname:_Low_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:1,c3:0.006896734,c4:1;n:type:ShaderForge.SFN_Color,id:3319,x:29105,y:33977,ptovrint:False,ptlb:Medium - High,ptin:_MediumHigh,varname:_Medium_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.9034481,c2:0,c3:1,c4:1;n:type:ShaderForge.SFN_Set,id:6686,x:30181,y:33680,varname:Tex,prsc:2|IN-5348-R;n:type:ShaderForge.SFN_Get,id:1724,x:30743,y:33330,varname:node_1724,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Get,id:3724,x:30673,y:32420,varname:node_3724,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Tex2d,id:7349,x:31310,y:34862,ptovrint:False,ptlb:Dither,ptin:_Dither,varname:node_7349,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:ac587ddcc78d9f94b9ea159be0d72226,ntxv:0,isnm:False|UVIN-7465-OUT;n:type:ShaderForge.SFN_ComponentMask,id:325,x:31320,y:32373,varname:node_325,prsc:2,cc1:0,cc2:-1,cc3:-1,cc4:-1|IN-579-OUT;n:type:ShaderForge.SFN_ScreenPos,id:4296,x:29374,y:34915,varname:node_4296,prsc:2,sctp:0;n:type:ShaderForge.SFN_ScreenParameters,id:6006,x:29374,y:35064,varname:node_6006,prsc:2;n:type:ShaderForge.SFN_Multiply,id:7399,x:29791,y:34962,varname:node_7399,prsc:2|A-4296-UVOUT,B-8240-OUT;n:type:ShaderForge.SFN_Append,id:8240,x:29596,y:35064,varname:node_8240,prsc:2|A-6006-PXW,B-6006-PXH;n:type:ShaderForge.SFN_Vector1,id:5609,x:29776,y:35192,varname:node_5609,prsc:2,v1:32;n:type:ShaderForge.SFN_Multiply,id:2337,x:31334,y:34688,varname:node_2337,prsc:2|A-5624-OUT,B-7349-A;n:type:ShaderForge.SFN_Add,id:7465,x:30783,y:34877,varname:node_7465,prsc:2|A-2411-UVOUT,B-3710-OUT;n:type:ShaderForge.SFN_ValueProperty,id:7322,x:30417,y:35014,ptovrint:False,ptlb:OffsetX,ptin:_OffsetX,varname:node_7322,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:3710,x:30624,y:35014,varname:node_3710,prsc:2|A-7322-OUT,B-8129-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8129,x:30417,y:35102,ptovrint:False,ptlb:OffsetY,ptin:_OffsetY,varname:_node_7322_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Rotator,id:2411,x:30588,y:34831,varname:node_2411,prsc:2|UVIN-4101-OUT,ANG-3439-OUT;n:type:ShaderForge.SFN_Slider,id:5344,x:30032,y:34633,ptovrint:False,ptlb:Rotate,ptin:_Rotate,varname:node_5344,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:1;n:type:ShaderForge.SFN_Pi,id:6821,x:30191,y:34719,varname:node_6821,prsc:2;n:type:ShaderForge.SFN_Multiply,id:3439,x:30388,y:34730,varname:node_3439,prsc:2|A-5344-OUT,B-6821-OUT;n:type:ShaderForge.SFN_Slider,id:8789,x:29060,y:33256,ptovrint:False,ptlb:Max,ptin:_Max,varname:node_8789,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;n:type:ShaderForge.SFN_Slider,id:9023,x:29060,y:33140,ptovrint:False,ptlb:Min,ptin:_Min,varname:node_9023,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:1;n:type:ShaderForge.SFN_Subtract,id:2560,x:29481,y:33192,varname:node_2560,prsc:2|A-8789-OUT,B-9023-OUT;n:type:ShaderForge.SFN_Multiply,id:9704,x:29816,y:32907,varname:node_9704,prsc:2|A-5812-OUT,B-2560-OUT;n:type:ShaderForge.SFN_Vector1,id:5812,x:29617,y:32907,varname:node_5812,prsc:2,v1:0.35;n:type:ShaderForge.SFN_Set,id:7382,x:30192,y:32907,varname:Step1,prsc:2|IN-6867-OUT;n:type:ShaderForge.SFN_Multiply,id:7437,x:29816,y:33047,varname:node_7437,prsc:2|A-7388-OUT,B-2560-OUT;n:type:ShaderForge.SFN_Vector1,id:7388,x:29617,y:33047,varname:node_7388,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Set,id:7668,x:30192,y:33047,varname:Step2,prsc:2|IN-9720-OUT;n:type:ShaderForge.SFN_Multiply,id:8400,x:29816,y:33228,varname:node_8400,prsc:2|A-2440-OUT,B-2560-OUT;n:type:ShaderForge.SFN_Vector1,id:2440,x:29617,y:33229,varname:node_2440,prsc:2,v1:0.65;n:type:ShaderForge.SFN_Set,id:4282,x:30203,y:33228,varname:Step3,prsc:2|IN-9279-OUT;n:type:ShaderForge.SFN_Add,id:6867,x:30001,y:32907,varname:node_6867,prsc:2|A-9704-OUT,B-9023-OUT;n:type:ShaderForge.SFN_Add,id:9720,x:30001,y:33047,varname:node_9720,prsc:2|A-7437-OUT,B-9023-OUT;n:type:ShaderForge.SFN_Add,id:9279,x:30001,y:33228,varname:node_9279,prsc:2|A-8400-OUT,B-9023-OUT;n:type:ShaderForge.SFN_Get,id:3132,x:30743,y:33452,varname:node_3132,prsc:2|IN-7668-OUT;n:type:ShaderForge.SFN_Step,id:8668,x:30967,y:33274,varname:node_8668,prsc:2|A-1724-OUT,B-3132-OUT;n:type:ShaderForge.SFN_Step,id:5872,x:30967,y:33403,varname:node_5872,prsc:2|A-3566-OUT,B-1724-OUT;n:type:ShaderForge.SFN_Get,id:3566,x:30743,y:33403,varname:node_3566,prsc:2|IN-7382-OUT;n:type:ShaderForge.SFN_Multiply,id:859,x:31352,y:33494,varname:node_859,prsc:2|A-8668-OUT,B-5872-OUT,C-2865-OUT;n:type:ShaderForge.SFN_Lerp,id:2865,x:31177,y:33577,varname:node_2865,prsc:2|A-968-OUT,B-6551-OUT,T-5987-OUT;n:type:ShaderForge.SFN_RemapRangeAdvanced,id:5987,x:30967,y:33526,varname:node_5987,prsc:2|IN-1724-OUT,IMIN-3566-OUT,IMAX-3132-OUT,OMIN-5371-OUT,OMAX-343-OUT;n:type:ShaderForge.SFN_Vector1,id:5371,x:30743,y:33567,varname:node_5371,prsc:2,v1:0;n:type:ShaderForge.SFN_Vector1,id:343,x:30743,y:33624,varname:node_343,prsc:2,v1:1;n:type:ShaderForge.SFN_Set,id:4451,x:29311,y:33426,varname:Color1,prsc:2|IN-5054-RGB;n:type:ShaderForge.SFN_Set,id:3239,x:29294,y:33620,varname:Color2,prsc:2|IN-3293-RGB;n:type:ShaderForge.SFN_Set,id:5019,x:29294,y:33799,varname:Color3,prsc:2|IN-8289-RGB;n:type:ShaderForge.SFN_Set,id:995,x:29279,y:33977,varname:Color4,prsc:2|IN-3319-RGB;n:type:ShaderForge.SFN_Set,id:4807,x:29279,y:34165,varname:Color5,prsc:2|IN-6249-RGB;n:type:ShaderForge.SFN_Get,id:968,x:30931,y:33655,varname:node_968,prsc:2|IN-3239-OUT;n:type:ShaderForge.SFN_Get,id:6551,x:30931,y:33711,varname:node_6551,prsc:2|IN-5019-OUT;n:type:ShaderForge.SFN_Get,id:4267,x:30739,y:33824,varname:node_4267,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Get,id:4095,x:30739,y:33946,varname:node_4095,prsc:2|IN-4282-OUT;n:type:ShaderForge.SFN_Step,id:7898,x:30963,y:33768,varname:node_7898,prsc:2|A-4267-OUT,B-4095-OUT;n:type:ShaderForge.SFN_Step,id:8626,x:30963,y:33897,varname:node_8626,prsc:2|A-6407-OUT,B-4267-OUT;n:type:ShaderForge.SFN_Get,id:6407,x:30739,y:33897,varname:node_6407,prsc:2|IN-7668-OUT;n:type:ShaderForge.SFN_Multiply,id:4780,x:31348,y:33988,varname:node_4780,prsc:2|A-7898-OUT,B-8626-OUT,C-2980-OUT;n:type:ShaderForge.SFN_Lerp,id:2980,x:31173,y:34071,varname:node_2980,prsc:2|A-4476-OUT,B-7466-OUT,T-7077-OUT;n:type:ShaderForge.SFN_RemapRangeAdvanced,id:7077,x:30963,y:34020,varname:node_7077,prsc:2|IN-4267-OUT,IMIN-6407-OUT,IMAX-4095-OUT,OMIN-6898-OUT,OMAX-6872-OUT;n:type:ShaderForge.SFN_Vector1,id:6898,x:30739,y:34061,varname:node_6898,prsc:2,v1:0;n:type:ShaderForge.SFN_Vector1,id:6872,x:30739,y:34118,varname:node_6872,prsc:2,v1:1;n:type:ShaderForge.SFN_Get,id:4476,x:30927,y:34149,varname:node_4476,prsc:2|IN-5019-OUT;n:type:ShaderForge.SFN_Get,id:7466,x:30927,y:34200,varname:node_7466,prsc:2|IN-995-OUT;n:type:ShaderForge.SFN_Get,id:255,x:30729,y:34267,varname:node_255,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Step,id:7364,x:30953,y:34267,varname:node_7364,prsc:2|A-5968-OUT,B-255-OUT;n:type:ShaderForge.SFN_Get,id:5968,x:30729,y:34334,varname:node_5968,prsc:2|IN-4282-OUT;n:type:ShaderForge.SFN_Multiply,id:5909,x:31338,y:34358,varname:node_5909,prsc:2|A-7364-OUT,B-9878-OUT;n:type:ShaderForge.SFN_Lerp,id:9878,x:31163,y:34441,varname:node_9878,prsc:2|A-6212-OUT,B-1777-OUT,T-1677-OUT;n:type:ShaderForge.SFN_RemapRangeAdvanced,id:1677,x:30953,y:34390,varname:node_1677,prsc:2|IN-255-OUT,IMIN-5968-OUT,IMAX-5372-OUT,OMIN-2247-OUT,OMAX-5372-OUT;n:type:ShaderForge.SFN_Vector1,id:2247,x:30729,y:34431,varname:node_2247,prsc:2,v1:0;n:type:ShaderForge.SFN_Vector1,id:5372,x:30729,y:34488,varname:node_5372,prsc:2,v1:1;n:type:ShaderForge.SFN_Get,id:6212,x:30917,y:34519,varname:node_6212,prsc:2|IN-995-OUT;n:type:ShaderForge.SFN_Get,id:1777,x:30917,y:34581,varname:node_1777,prsc:2|IN-4807-OUT;n:type:ShaderForge.SFN_Get,id:3524,x:30766,y:32831,varname:node_3524,prsc:2|IN-6686-OUT;n:type:ShaderForge.SFN_Step,id:9587,x:30990,y:32904,varname:node_9587,prsc:2|A-3524-OUT,B-2623-OUT;n:type:ShaderForge.SFN_Get,id:2623,x:30766,y:32940,varname:node_2623,prsc:2|IN-7382-OUT;n:type:ShaderForge.SFN_Multiply,id:2232,x:31375,y:32995,varname:node_2232,prsc:2|A-9587-OUT,B-315-OUT;n:type:ShaderForge.SFN_Lerp,id:315,x:31200,y:33078,varname:node_315,prsc:2|A-6842-OUT,B-3828-OUT,T-7671-OUT;n:type:ShaderForge.SFN_RemapRangeAdvanced,id:7671,x:30990,y:33027,varname:node_7671,prsc:2|IN-3524-OUT,IMIN-2484-OUT,IMAX-2623-OUT,OMIN-9589-OUT,OMAX-9346-OUT;n:type:ShaderForge.SFN_Vector1,id:9589,x:30766,y:33093,varname:node_9589,prsc:2,v1:0;n:type:ShaderForge.SFN_Vector1,id:9346,x:30766,y:33156,varname:node_9346,prsc:2,v1:1;n:type:ShaderForge.SFN_Get,id:6842,x:30954,y:33156,varname:node_6842,prsc:2|IN-4451-OUT;n:type:ShaderForge.SFN_Get,id:3828,x:30954,y:33207,varname:node_3828,prsc:2|IN-3239-OUT;n:type:ShaderForge.SFN_Round,id:2538,x:29979,y:34962,varname:node_2538,prsc:2|IN-7399-OUT;n:type:ShaderForge.SFN_Divide,id:4101,x:30174,y:34962,varname:node_4101,prsc:2|A-2538-OUT,B-5609-OUT;n:type:ShaderForge.SFN_Vector1,id:2484,x:30766,y:33027,varname:node_2484,prsc:2,v1:0.05;proporder:5348-5054-3293-8289-3319-6249-5624-7349-7322-8129-5344-8789-9023;pass:END;sub:END;*/

Shader "Shader Forge/RasterShader" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _Low ("Low", Color) = (0,0.04827571,1,1)
        _LowMedium ("Low - Medium", Color) = (0,1,0.006896734,1)
        _Medium ("Medium", Color) = (1,0.9310346,0,1)
        _MediumHigh ("Medium - High", Color) = (0.9034481,0,1,1)
        _High ("High", Color) = (0,0.8344827,1,1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Dither ("Dither", 2D) = "white" {}
        _OffsetX ("OffsetX", Float ) = 0
        _OffsetY ("OffsetY", Float ) = 0
        _Rotate ("Rotate", Range(0, 1)) = 0
        _Max ("Max", Range(0, 1)) = 1
        _Min ("Min", Range(0, 1)) = 0
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
#ifndef UNITY_PASS_FORWARDBASE
            #define UNITY_PASS_FORWARDBASE
#endif
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma only_renderers d3d9 d3d11 glcore gles metal
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _Low;
            uniform float4 _High;
            uniform float _Opacity;
            uniform float4 _Medium;
            uniform float4 _LowMedium;
            uniform float4 _MediumHigh;
            uniform sampler2D _Dither; uniform float4 _Dither_ST;
            uniform float _OffsetX;
            uniform float _OffsetY;
            uniform float _Rotate;
            uniform float _Max;
            uniform float _Min;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 projPos : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float Tex = _MainTex_var.r;
                clip(ceil((Tex*1.052632+-0.05263158)).r - 0.5);
////// Lighting:
////// Emissive:
                float node_3524 = Tex;
                float node_2560 = (_Max-_Min);
                float Step1 = ((0.35*node_2560)+_Min);
                float node_2623 = Step1;
                float3 Color1 = _Low.rgb;
                float3 Color2 = _LowMedium.rgb;
                float node_2484 = 0.05;
                float node_9589 = 0.0;
                float node_1724 = Tex;
                float Step2 = ((0.5*node_2560)+_Min);
                float node_3132 = Step2;
                float node_3566 = Step1;
                float3 Color3 = _Medium.rgb;
                float node_5371 = 0.0;
                float node_4267 = Tex;
                float Step3 = ((0.65*node_2560)+_Min);
                float node_4095 = Step3;
                float node_6407 = Step2;
                float3 Color4 = _MediumHigh.rgb;
                float node_6898 = 0.0;
                float node_5968 = Step3;
                float node_255 = Tex;
                float3 Color5 = _High.rgb;
                float node_5372 = 1.0;
                float node_2247 = 0.0;
                float3 emissive = ((step(node_3524,node_2623)*lerp(Color1,Color2,(node_9589 + ( (node_3524 - node_2484) * (1.0 - node_9589) ) / (node_2623 - node_2484))))+(step(node_1724,node_3132)*step(node_3566,node_1724)*lerp(Color2,Color3,(node_5371 + ( (node_1724 - node_3566) * (1.0 - node_5371) ) / (node_3132 - node_3566))))+(step(node_4267,node_4095)*step(node_6407,node_4267)*lerp(Color3,Color4,(node_6898 + ( (node_4267 - node_6407) * (1.0 - node_6898) ) / (node_4095 - node_6407))))+(step(node_5968,node_255)*lerp(Color4,Color5,(node_2247 + ( (node_255 - node_5968) * (node_5372 - node_2247) ) / (node_5372 - node_5968)))));
                float3 finalColor = emissive;
                float node_2411_ang = (_Rotate*3.141592654);
                float node_2411_spd = 1.0;
                float node_2411_cos = cos(node_2411_spd*node_2411_ang);
                float node_2411_sin = sin(node_2411_spd*node_2411_ang);
                float2 node_2411_piv = float2(0.5,0.5);
                float2 node_2411 = (mul((round(((sceneUVs * 2 - 1).rg*float2(_ScreenParams.r,_ScreenParams.g)))/32.0)-node_2411_piv,float2x2( node_2411_cos, -node_2411_sin, node_2411_sin, node_2411_cos))+node_2411_piv);
                float2 node_7465 = (node_2411+float2(_OffsetX,_OffsetY));
                float4 _Dither_var = tex2D(_Dither,TRANSFORM_TEX(node_7465, _Dither));
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
                o.pos = UnityObjectToClipPos( v.vertex );
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float Tex = _MainTex_var.r;
                clip(ceil((Tex*1.052632+-0.05263158)).r - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
