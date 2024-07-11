Shader "Hidden/ImageCropping/Unlit"
{
	Properties
	{
		_Mode("Mode", int) = 0
		
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB), Alpha (A)", 2D) = "white" {}
		_ScaleAndOffset("Scale And Offset", Vector) = (1, 1, 0, 0)
		
		_CornerMode("Corner Mode", int) = 0
		_RoundRadiusX("Round Radius X", Range(0, 0.5)) = 0
		_RoundRadiusY("Round Radius Y", Range(0, 0.5)) = 0
		_HyperEllipticPower("Hyper Rlliptic Power", Range(0, 10)) = 3
		_Softness ("Softness", Range(0, 1)) = 0
		_EdgeMove ("Edge Move", Range(-1, 1)) = 0
	}
	SubShader
	{
		LOD 200

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Offset -1, -1
			Fog { Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			int _Mode;
			sampler2D _MainTex;
			float4 _ScaleAndOffset;
			fixed4 _Color;

			int _CornerMode;
			float _RoundRadiusX;
			float _RoundRadiusY;
			float _HyperEllipticPower;
			float _Softness;
			float _EdgeMove;
			
			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				half4 col;
				if (_Mode == 0) {
					col = _Color;
					if (_CornerMode == 0) {
						col.a = 0;
					}
				} else {
					float2 uv = i.uv * _ScaleAndOffset.xy + _ScaleAndOffset.zw;
					col = tex2D(_MainTex, uv);
					if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) {
						col.a = 0;
					}
				}
				if (_CornerMode == 1) {
					if (_RoundRadiusX > 0 && _RoundRadiusY > 0) {
						float4 range = float4(_RoundRadiusX, _RoundRadiusY, 1 - _RoundRadiusX, 1 - _RoundRadiusY);
						float2 delta = abs(i.uv - clamp(i.uv, range.xy, range.zw)) / float2(_RoundRadiusX, _RoundRadiusY);
						col.a *= clamp((1 - (delta.x * delta.x + delta.y * delta.y)) / (_Softness + 0.0001) + _EdgeMove, 0, 1);
					}
				} else if (_CornerMode == 2) {
					float2 pos = i.uv * 2 - 1;
					float distance = pow(abs(pos.x), _HyperEllipticPower) + pow(abs(pos.y), _HyperEllipticPower);
					col.a *= clamp((1 - distance) / (_Softness + 0.0001) + _EdgeMove, 0, 1);
				}
				return col;
			}
			ENDCG
		}
	}
	FallBack "Unlit/Transparent"
}