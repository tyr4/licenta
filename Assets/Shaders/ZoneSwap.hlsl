#ifndef ZONE_SWAP_INCLUDED
#define ZONE_SWAP_INCLUDED

#define MAX_ZONES 256
#define MAX_PALETTES 16

TEXTURE2D_ARRAY(_ZoneTex);          // slice = zone*3 + channel (0=R, 1=G, 2=B)
SAMPLER(sampler_ZoneTex);
float  _ZoneCount;
float _PaletteCount;
float4 _ZoneRect[MAX_ZONES];        // xy = min corner, zw = max corner (world)
float4 _ZoneStyle[MAX_ZONES];       // x = noise strength, y = noise scale, z = texture world size, w = softness
float4 _PaletteInfo[MAX_PALETTES];  // x = texture world size

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = Hash21(i), b = Hash21(i + float2(1, 0));
    float c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float Fbm(float2 p)
{
    float v = 0, a = 0.5;
    for (int i = 0; i < 3; i++) { v += a * ValueNoise(p); p *= 2.03; a *= 0.5; }
    return (v - 0.44) * 2.0;
}

void ZoneSwap_float(float2 World, float4 R, float4 G, float4 B,
                    out float4 OutR, out float4 OutG, out float4 OutB)
{
    OutR = R; OutG = G; OutB = B;

#ifndef SHADERGRAPH_PREVIEW
    float pm[MAX_PALETTES];
    [unroll] for (int k = 0; k < MAX_PALETTES; k++) pm[k] = 0;
    
    // 1. every rectangle adds to its palette's mark (union via mask)
    [loop] for (int i = 0; i < MAX_ZONES; i++)
    {
        if (i >= (int)_ZoneCount) break;

        float4 rect = _ZoneRect[i];
        float4 style = _ZoneStyle[i];
        int p = (int)style.z;

        // distance to the nearest edge: positive inside, negative outside
        float2 a = World - rect.xy;
        float2 b = rect.zw - World;
        float e = min(min(a.x, a.y), min(b.x, b.y));

        // noise wobbles the border
        e += Fbm(World / max(style.y, 0.01) + i * 17.0) * style.x;
        
        pm[p] = max(pm[p], smoothstep(0.0, max(style.w, 0.001), e));
    }

    // 2. each palette is sampled once, only wehre it has any mask
    [loop] for (int i = 0; i < MAX_PALETTES; i++) 
    {
        if (i >= (int)_PaletteCount) break;
        if (pm[i] <= 0.0) continue;

        float2 uv = World * max(_PaletteInfo[i].x, 0.01);
        float4 zr = SAMPLE_TEXTURE2D_ARRAY_LOD(_ZoneTex, sampler_ZoneTex, uv, i * 3 + 0, 0);
        float4 zg = SAMPLE_TEXTURE2D_ARRAY_LOD(_ZoneTex, sampler_ZoneTex, uv, i * 3 + 1, 0);
        float4 zb = SAMPLE_TEXTURE2D_ARRAY_LOD(_ZoneTex, sampler_ZoneTex, uv, i * 3 + 2, 0);

        OutR = lerp(OutR, zr, pm[i]);
        OutG = lerp(OutG, zg, pm[i]);
        OutB = lerp(OutB, zb, pm[i]);
    }
#endif
}
#endif