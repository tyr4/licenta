using UnityEngine;
using UnityEngine.Tilemaps;

public class StageOneGenerator : MonoBehaviour
{
    [SerializeField] private WorldGenerationSettings settings;
    [SerializeField] private RuleTile pathTile;
    [SerializeField] private TileBase baseTile;
    
    private BaseGenerator _generator;
    private Tilemap _tilemap;
    private TileType[,] _values;
    
    private void Start()
    {
        _generator = new BaseGenerator(settings);
        _tilemap = GetComponent<Tilemap>();
        _values = new TileType[settings.chunkSize, settings.chunkSize];
        
        GenerateMatrix();
    }

    private void GenerateMatrix()
    {
        var size = settings.chunkSize;
        var half = size / 2;

        for (int y = -half; y < half; y++)
        {
            for (int x = -half; x < half; x++)
            {
                var value = _generator.GetBaseTile(x, y);
                UpdateTileData(x, y, value, true);
            }
        }
        
        RemoveStrayOnes();
    }

    private TileBase GetTileByValue(TileType value)
    {
        return value switch
        {
            TileType.Grass => baseTile,
            TileType.Dirt => pathTile,
            _ => baseTile,
        };
    }

    private void RemoveStrayOnes()
    {
        var size = settings.chunkSize;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var xMinus = Mathf.Max(0, x - 1);
                var xPlus = Mathf.Min(size - 1, x + 1);
                
                if (_values[y, x] == TileType.Dirt)
                {
                    if (_values[y, xMinus] == TileType.Grass && _values[y, xPlus] == TileType.Grass)
                    {
                        Debug.Log($"Found stray 0 horizontally at {y}, {x}");
                        UpdateTileData(x, y, TileType.Grass);
                    }
                }

                if (_values[x, y] == TileType.Dirt)
                {
                    if (_values[xMinus, y] == TileType.Grass && _values[xPlus, y] == TileType.Grass)
                    {
                        Debug.Log($"Found stray 0 vertically at {y}, {x}");
                        UpdateTileData(y, x, TileType.Grass);
                    }
                }
                
                if (_values[y, x] == TileType.Grass)
                {
                    if (_values[y, xMinus] == TileType.Dirt && _values[y, xPlus] == TileType.Dirt)
                    {
                        Debug.Log($"Found stray 0 horizontally at {y}, {x}");
                        UpdateTileData(x, y, TileType.Dirt);
                    }
                }

                if (_values[x, y] == TileType.Grass)
                {
                    if (_values[xMinus, y] == TileType.Dirt && _values[xPlus, y] == TileType.Dirt)
                    {
                        Debug.Log($"Found stray 0 vertically at {y}, {x}");
                        UpdateTileData(y, x, TileType.Dirt);
                    }
                }
            }
        }
    }

    private void UpdateTileData(int x, int y, TileType value, bool usingHalf = false)
    {
        var half = settings.chunkSize / 2;
        Vector3Int tilePos;
        
        if (usingHalf)
        {
            tilePos = new Vector3Int(x, y, 0);
            _values[y + half, x + half] = value;
        }
        else
        {
            tilePos = new Vector3Int(x - half, y - half, 0);
            _values[y, x] = value;
        }

        _tilemap.SetTile(tilePos, GetTileByValue(value));
    }
}
