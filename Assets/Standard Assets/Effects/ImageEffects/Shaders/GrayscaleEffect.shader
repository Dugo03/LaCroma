Shader "Hidden/Grayscale Effect" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_RampTex ("Base (RGB)", 2D) = "grayscaleRamp" {}
	_Color ("Color", Float) = 1
	_Gray ("Gray", Float) = 0
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
				
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"

uniform sampler2D _MainTex;
uniform sampler2D _RampTex;
uniform half _RampOffset;
half4 _MainTex_ST;
float _Color;
float _Gray;

fixed4 frag (v2f_img i) : SV_Target
{
	fixed4 original = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
	fixed grayscale = Luminance(original.rgb);
	half2 remap = half2 (grayscale + _RampOffset, .5);
	fixed4 output = tex2D(_RampTex, remap);
	output.a = original.a;
	return _Gray*output + _Color*original;
}
ENDCG

	}
}

Fallback off

}
