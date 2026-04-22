using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundCheckManager : MonoBehaviour
{
    public static GroundCheckManager Instance;

    public Tilemap tilemap;

    private HashSet<Vector2Int> solid = new();

    public static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(1,0),
        new Vector2Int(0,1),
        new Vector2Int(-1,0),
        new Vector2Int(0,-1)
    };

    void Awake()
    {
        Instance = this;
        Build();
    }

    void Build()
    {
        solid.Clear();

        BoundsInt b = tilemap.cellBounds;

        foreach (var p in b.allPositionsWithin)
        {
            if (tilemap.HasTile(p))
                solid.Add(new Vector2Int(p.x, p.y));
        }
    }

    public bool IsSolid(Vector2Int cell)
    {
        return solid.Contains(cell);
    }

    public Vector2Int WorldToCell(Vector2 world)
    {
        Vector3Int c = tilemap.WorldToCell(world);
        return new Vector2Int(c.x, c.y);
    }

    // ✔ 关键：tile corner（0,0 basis）
    public Vector2 CellCorner(Vector2Int cell)
    {
        return tilemap.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    // ✔ edge endpoint（真正边）
    public Vector2 EdgePoint(Vector2Int cell, int edgeIndex)
    {
        Vector2 c = CellCorner(cell);

        return edgeIndex switch
        {
            0 => c,                              // bottom-left
            1 => c + Vector2.right,             // bottom-right
            2 => c + Vector2.right + Vector2.up,// top-right
            _ => c + Vector2.up                 // top-left
        };
    }
}