using System.Text;
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

    private const int FixPassesLimit = 50;
    private const int GlobalPassesLimit = 100;

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
                _values[y + half, x + half] = value;
            }
        }

        Debug.Log("Matrix before operations");
        PrintMatrix();

        int globalPasses = 0;
        bool overallChanged = true;

        while (overallChanged && globalPasses < GlobalPassesLimit)
        {
            overallChanged = false;
            
            var localPasses = 0;
            while (localPasses < FixPassesLimit)
            {
                localPasses++;

                if (FixScarceSpots())
                {
                    overallChanged = true;
                }
                else break;
            }
            Debug.Log($"Did {localPasses} scarce passes");
            
            localPasses = 0;
            while (localPasses < FixPassesLimit)
            {
                localPasses++;

                if (FixScarceSpotsWithSubMatrix())
                {
                    overallChanged = true;
                }
                else break;
            }
            Debug.Log($"Did {localPasses} diagonal passes");

            globalPasses++;
        }
        Debug.Log($"Did {globalPasses} global passes");

        ApplyToTilemap();
        PadTilemap();
    }

    private void PrintMatrix()
    {
        StringBuilder sb = new();
        var size =  settings.chunkSize;

        for (int i = size - 1; i >= 0; i--)
        {
            for (int j = 0; j < size; j++)
            {
                sb.Append($"{(_values[i, j] == TileType.Dirt? 1 : 0)} ");
            }

            sb.Append("\n");
        }

        Debug.Log(sb.ToString());
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

    private bool FixScarceSpots()
    {
        var size = settings.chunkSize;
        bool changed = false;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {

                var current = _values[y, x];
                var opposite = current == TileType.Grass ? TileType.Dirt : TileType.Grass;
                
                bool horizontalAxis = x > 0 && x < size - 1 
                                      && _values[y, x - 1] == opposite
                                      && _values[y, x + 1] == opposite;

                bool verticalAxis = y > 0 && y < size - 1
                                    && _values[y - 1, x] == opposite
                                    && _values[y + 1, x] == opposite;

                if (!horizontalAxis && !verticalAxis) continue;
                
                bool wouldBreakHorizontalAxis = horizontalAxis && y > 0 && y < size - 1
                                                && _values[y - 1, x] == current
                                                && _values[y + 1, x] == current;

                bool wouldBreakVerticalAxis = verticalAxis && x > 0 && x < size - 1
                                              && _values[y, x - 1] == current
                                              && _values[y, x + 1] == current;

                if (wouldBreakVerticalAxis || wouldBreakHorizontalAxis) continue;

                _values[y, x] = opposite;
                changed = true;
            }
        }

        return changed;
    }

    private bool FixScarceSpotsWithSubMatrix()
    {
        var size = settings.chunkSize;
        bool changed = false;

        for (int y = 1; y < size - 1; y++)
        {
            for (int x = 1; x < size - 1; x++)
            {
                var current = _values[y, x];
                var opposite = current == TileType.Grass ? TileType.Dirt : TileType.Grass;

                bool diagonal = _values[y - 1, x - 1] == opposite 
                                && _values[y + 1, x + 1] == opposite
                                && _values[y - 1, x + 1] == current
                                && _values[y + 1, x - 1] == current;

                bool diagonalMirrored = _values[y - 1, x + 1] == opposite
                                        && _values[y + 1, x - 1] == opposite
                                        && _values[y - 1, x - 1] == current
                                        && _values[y + 1, x + 1] == current;

                bool lineVertical = _values[y + 1, x] == current
                                    && _values[y, x - 1] == opposite
                                    && _values[y, x + 1] == opposite
                                    && _values[y + 1, x - 1] == opposite
                                    && _values[y + 1, x + 1] == opposite;

                bool lineHorizontal = _values[y, x - 1] == current
                                      && _values[y - 1, x] == opposite
                                      && _values[y + 1, x] == opposite
                                      && _values[y - 1, x - 1] == opposite
                                      && _values[y + 1, x - 1] == opposite;
                
                if (diagonal)
                {
                    _values[y - 1, x - 1] = current;
                    _values[y + 1, x + 1] = current;
                    changed = true;
                }
                else if (diagonalMirrored)
                {
                    _values[y - 1, x + 1] = current;
                    _values[y + 1, x - 1] = current;
                    changed = true;
                }

                if (lineVertical)
                {
                    _values[y, x] = opposite;
                    _values[y + 1, x] = opposite;
                    changed = true;
                }
                else if (lineHorizontal)
                {
                    _values[y, x] = opposite;
                    _values[y, x - 1] = opposite;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private void ApplyToTilemap()
    {
        var half = settings.chunkSize / 2;

        for (int y = -half; y < half; y++)
        {
            for (int x = -half; x < half; x++)
            {
                var tilePos = new Vector3Int(x, y, 0);
                _tilemap.SetTile(tilePos, GetTileByValue(_values[y + half, x + half]));
            }
        }
    }

    private void PadTilemap()
    {
        var half = settings.chunkSize / 2;
        
        for (int i = -half - 2; i < half + 2; i++)
        {
            var up1 = new Vector3Int(i, -half - 1, 0);
            var up2 = new Vector3Int(i, -half - 2, 0);

            var down1 = new Vector3Int(i, half, 0);
            var down2 = new Vector3Int(i, half + 1, 0);
            
            var left1 = new Vector3Int(-half - 1, i, 0);
            var left2 = new Vector3Int(-half - 2, i, 0);
            
            var right1 = new Vector3Int(half, i, 0);
            var right2 = new Vector3Int(half + 1, i, 0);

            var allPos = new[] { up1, up2, down1, down2, left1, left2, right1, right2 };

            foreach (var pos in allPos)
            {
                _tilemap.SetTile(pos, GetTileByValue(TileType.Dirt));
            }
        }
    }
}
