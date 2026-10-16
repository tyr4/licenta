#ifndef SAMPLE_LIGHT_INCLUDED
#define SAMPLE_LIGHT_INCLUDED

TEXTURE2D(_ShapeLightTexture0);
SAMPLER(sampler_ShapeLightTexture0);

// Light: the 2D light color at this pixel.
// Mask:  1 where any light reaches the pixel, 0 where it is unlit.
void SampleLight_float(float2 ScreenUV, out float3 Light, out float Mask)
{
#ifdef SHADERGRAPH_PREVIEW
    Light = float3(1, 1, 1);
    Mask = 1;
#else
    Light = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, ScreenUV).rgb;
    Mask = saturate(max(max(Light.r, Light.g), Light.b));
#endif
}

#endif
