#ifndef OUTLINE_FUNCTIONS_INCLUDED
#define OUTLINE_FUNCTIONS_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float InvEyeDepth(float2 uv)
{
    return 1.0 / LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
}
#endif

void DepthEdge_float(float2 UV, float Thickness, float Threshold, float MaxDistance, out float Edge)
{
#ifdef SHADERGRAPH_PREVIEW
    Edge = 0;
#else
    float2 texel = Thickness * (_ScreenParams.zw - 1.0);

    float c = InvEyeDepth(UV);
    float l = InvEyeDepth(UV + float2(-texel.x, 0));
    float r = InvEyeDepth(UV + float2(texel.x, 0));
    float u = InvEyeDepth(UV + float2(0, texel.y));
    float d = InvEyeDepth(UV + float2(0, -texel.y));

    float diff = abs(l + r - 2.0 * c) + abs(u + d - 2.0 * c);
    float distance = 1.0 / c;
    Edge = step(Threshold, diff / c) * step(distance, MaxDistance);
#endif
}

#endif