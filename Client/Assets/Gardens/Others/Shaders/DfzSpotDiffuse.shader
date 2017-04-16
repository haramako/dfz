// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

// MEMO: Mobile/Diffuse を元に作成

Shader "Dfz/SpotDiffuse" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	//_SpotTex ("SpotTex", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 150

CGPROGRAM
#pragma surface surf Lambert noforwardadd vertex:vert

sampler2D _MainTex; // add

sampler2D _SpotTex;
float4x4 _SpotTransform;

struct Input {
	float2 uv_MainTex;
	float3 objPos;
};

// add start
void vert (inout appdata_full v, out Input o) {
    UNITY_INITIALIZE_OUTPUT(Input,o);
    o.objPos = mul(_SpotTransform, mul(unity_ObjectToWorld, v.vertex)).xyz;
}
// add end

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb;
	o.Alpha = c.a;

	// add start
	fixed4 spot = tex2D(_SpotTex, IN.objPos.xz);
	//c.rgb *= spot.r;
	c *= spot.a;
	fixed3 gray = (c.r + c.g + c.b) / 3;
	fixed alpha = clamp((spot.a - 0.4 ) * 3, 0, 1);
	o.Albedo = lerp(gray, c.rgb, alpha);
	// add end
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
