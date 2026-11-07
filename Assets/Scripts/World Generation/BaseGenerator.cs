public class BaseGenerator
{
    private readonly WorldGenerationSettings _settings;
    private readonly SimplexNoise _simplexNoise;
    
    public BaseGenerator(WorldGenerationSettings settings)
    {
        _settings = settings;
        _simplexNoise = new SimplexNoise(_settings.seed);
    }
    
    public TileType GetBaseTile(int worldX, int worldY)
    {
        // float e = Mathf.PerlinNoise(_offsetX + worldX, _offsetY + worldY);
        float e = _simplexNoise.Fbm2D(
            worldX, worldY,
            _settings.frequency,
            _settings.octaves,
            _settings.lacunarity,
            _settings.gain
            );

        return e < _settings.tileThreshold ? TileType.Dirt : TileType.Grass;
    }
}