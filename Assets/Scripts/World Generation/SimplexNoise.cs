using System;

public sealed class SimplexNoise
{
    private readonly byte[] _perm = new byte[512];

    private static readonly int[][] Grad3 =
    {
        new[] { 1, 1, 0 }, new[] { -1, 1, 0 }, new[] { 1, -1, 0 }, new[] { -1, -1, 0 },
        new[] { 1, 0, 1 }, new[] { -1, 0, 1 }, new[] { 1, 0, -1 }, new[] { -1, 0, -1 },
        new[] { 0, 1, 1 }, new[] { 0, -1, 1 }, new[] { 0, 1, -1 }, new[] { 0, -1, -1 }
    };

    private const float F2 = 0.3660254037844386f;  // 0.5 * (sqrt(3) - 1)
    private const float G2 = 0.21132486540518713f; // (3 - sqrt(3)) / 6
    private const float F3 = 1f / 3f;
    private const float G3 = 1f / 6f;

    public SimplexNoise(int seed = 0)
    {
        var p = new byte[256];
        for (int i = 0; i < 256; i++) p[i] = (byte)i;

        // Fisher-Yates shuffle with a seeded RNG
        var rng = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
    }

    private static int FastFloor(float x) => x > 0 ? (int)x : (int)x - 1;

    private static float Dot(int[] g, float x, float y) => g[0] * x + g[1] * y;
    private static float Dot(int[] g, float x, float y, float z) => g[0] * x + g[1] * y + g[2] * z;

    /// <summary>
    /// 2D simplex noise, range ~[-1, 1].
    /// </summary>
    public float Noise(float x, float y)
    {
        // Skew input space to find which simplex cell we're in
        float s = (x + y) * F2;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);

        // Unskew cell origin back to (x, y) space
        float t = (i + j) * G2;
        float x0 = x - (i - t);
        float y0 = y - (j - t);

        // Middle corner offset: lower or upper triangle
        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; }
        else         { i1 = 0; j1 = 1; }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1f + 2f * G2;
        float y2 = y0 - 1f + 2f * G2;

        int ii = i & 255;
        int jj = j & 255;
        int gi0 = _perm[ii + _perm[jj]] % 12;
        int gi1 = _perm[ii + i1 + _perm[jj + j1]] % 12;
        int gi2 = _perm[ii + 1 + _perm[jj + 1]] % 12;

        float n0 = 0, n1 = 0, n2 = 0;

        float t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 >= 0) { t0 *= t0; n0 = t0 * t0 * Dot(Grad3[gi0], x0, y0); }

        float t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 >= 0) { t1 *= t1; n1 = t1 * t1 * Dot(Grad3[gi1], x1, y1); }

        float t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 >= 0) { t2 *= t2; n2 = t2 * t2 * Dot(Grad3[gi2], x2, y2); }

        return 70f * (n0 + n1 + n2);
    }

    /// <summary>3D simplex noise, range ~[-1, 1].</summary>
    public float Noise(float x, float y, float z)
    {
        float s = (x + y + z) * F3;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);
        int k = FastFloor(z + s);

        float t = (i + j + k) * G3;
        float x0 = x - (i - t);
        float y0 = y - (j - t);
        float z0 = z - (k - t);

        // Determine which simplex (of 6) we're in
        int i1, j1, k1, i2, j2, k2;
        if (x0 >= y0)
        {
            if (y0 >= z0)      { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
            else               { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
        }
        else
        {
            if (y0 < z0)       { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
            else if (x0 < z0)  { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
            else               { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
        }

        float x1 = x0 - i1 + G3,        y1 = y0 - j1 + G3,        z1 = z0 - k1 + G3;
        float x2 = x0 - i2 + 2f * G3,   y2 = y0 - j2 + 2f * G3,   z2 = z0 - k2 + 2f * G3;
        float x3 = x0 - 1f + 3f * G3,   y3 = y0 - 1f + 3f * G3,   z3 = z0 - 1f + 3f * G3;

        int ii = i & 255, jj = j & 255, kk = k & 255;
        int gi0 = _perm[ii + _perm[jj + _perm[kk]]] % 12;
        int gi1 = _perm[ii + i1 + _perm[jj + j1 + _perm[kk + k1]]] % 12;
        int gi2 = _perm[ii + i2 + _perm[jj + j2 + _perm[kk + k2]]] % 12;
        int gi3 = _perm[ii + 1 + _perm[jj + 1 + _perm[kk + 1]]] % 12;

        float n0 = 0, n1 = 0, n2 = 0, n3 = 0;

        float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
        if (t0 >= 0) { t0 *= t0; n0 = t0 * t0 * Dot(Grad3[gi0], x0, y0, z0); }

        float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
        if (t1 >= 0) { t1 *= t1; n1 = t1 * t1 * Dot(Grad3[gi1], x1, y1, z1); }

        float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
        if (t2 >= 0) { t2 *= t2; n2 = t2 * t2 * Dot(Grad3[gi2], x2, y2, z2); }

        float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
        if (t3 >= 0) { t3 *= t3; n3 = t3 * t3 * Dot(Grad3[gi3], x3, y3, z3); }

        return 32f * (n0 + n1 + n2 + n3);
    }

    /// <summary>Fractal Brownian motion (layered 2D noise), normalized to ~[-1, 1].</summary>
    public float Fbm(float x, float y, int octaves = 4, float lacunarity = 2f, float gain = 0.5f)
    {
        float sum = 0, amp = 1, freq = 1, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += Noise(x * freq, y * freq) * amp;
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return sum / norm;
    }
    
    /// <summary>Maps a raw noise value from [-1, 1] to [0, 1] (clamped).</summary>
    public static float Normalize(float v)
    {
        float n = (v + 1f) * 0.5f;
        return n < 0f ? 0f : (n > 1f ? 1f : n);
    }

    /// <summary>
    /// 2D simplex noise, normalized to [0, 1].
    /// frequency: higher = more detail (e.g. 0.05f).
    /// </summary>
    public float Noise2D(float x, float y, float frequency = 0.05f)
    {
        return Normalize(Noise(x * frequency, y * frequency));
    }

    /// <summary>
    /// Fractal (fBm) 2D noise, normalized to [0, 1].
    /// frequency: base frequency (e.g. 0.01f); octaves/lacunarity/gain control the layering.
    /// </summary>
    public float Fbm2D(float x, float y, float frequency = 0.01f,
        int octaves = 4, float lacunarity = 2f, float gain = 0.5f)
    {
        return Normalize(Fbm(x * frequency, y * frequency, octaves, lacunarity, gain));
    }
}

// Example usage:
//
// var noise = new SimplexNoise(seed: 1234);
// float v2 = noise.Noise(x * 0.05f, y * 0.05f);          // 2D
// float v3 = noise.Noise(x * 0.05f, y * 0.05f, time);    // 3D (e.g. animated)
// float f  = noise.Fbm(x * 0.01f, y * 0.01f, octaves: 5); // terrain-style
// float normalized = (v2 + 1f) * 0.5f;                    // map to [0, 1]