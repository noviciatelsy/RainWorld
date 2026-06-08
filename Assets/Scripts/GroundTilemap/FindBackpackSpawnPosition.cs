using UnityEngine;
using UnityEngine.Tilemaps;

public class FindBackpackSpawnPosition : MonoBehaviour
{
    [Header("搜索范围")]
    [Tooltip("向左右最多搜索多少个单元格")]
    [SerializeField, Min(1)]
    private int horizontalSearchRadius = 12;

    [Tooltip("向上下最多搜索多少个单元格")]
    [SerializeField, Min(1)]
    private int verticalSearchRadius = 8;

    [Header("站立空间")]
    [Tooltip(
        "地面上方需要空出多少格高度。\n" +
        "如果玩家大约有两格高，则设置为 2。")]
    [SerializeField, Min(1)]
    private int clearanceHeightInCells = 1;

    [Tooltip(
        "向左右额外检查多少格空间。\n" +
        "0 表示只检查一列；1 表示检查左、中、右三列。")]
    [SerializeField, Min(0)]
    private int clearanceHalfWidthInCells = 0;

    [Tooltip(
        "是否要求地面瓦片的 Collider Type 不为 None。\n" +
        "通常建议开启。")]
    [SerializeField]
    private bool requireTileCollider = true;

    [Header("生成位置")]
    [Tooltip("最终背包位置相对于空白单元格中心的偏移")]
    [SerializeField]
    private Vector2 spawnOffset = new Vector2(0, 0.25f);

    private Tilemap solidTilemap;
    private Player player;
    private void Awake()
    {
        solidTilemap = GetComponent<Tilemap>();
    }

    private void OnEnable()
    {
        TrySubsribeToPlayer(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnCurrentPlayerChanged += TrySubsribeToPlayer;
    }


    private void OnDisable()
    {
        if(player != null)
        {
            player.GetComponent<PlayerVitals>().PlayerDied -= FindSpawnPosition;
        }
        PlayerManager.Instance.OnCurrentPlayerChanged -= TrySubsribeToPlayer;
    }
    private void TrySubsribeToPlayer(Player player)
    {
        if (player == null)
        {
            return;
        }
        this.player = player;
        player.GetComponent<PlayerVitals>().PlayerDied += FindSpawnPosition;
    }
    private void FindSpawnPosition()
    {
        Vector3 playerLastDeathPosition = SaveManager.Instance.GetRunTimeGameData().playerDiePosition;
        if (playerLastDeathPosition != Vector3.zero)
        {
            TryFindNearestSpawnPosition(playerLastDeathPosition, out Vector3 spawnPosition);
            SaveManager.Instance.GetRunTimeGameData().retrieveBackpackSpawnPosition=spawnPosition;
        }
    }

    /// <summary>
    /// 寻找距离指定世界坐标最近的安全背包生成位置。
    /// </summary>
    /// <param name="originWorldPosition">玩家死亡时的世界坐标</param>
    /// <param name="spawnWorldPosition">找到的背包生成坐标</param>
    /// <returns>是否成功找到位置</returns>
    public bool TryFindNearestSpawnPosition(
        Vector3 originWorldPosition,
        out Vector3 spawnWorldPosition)
    {
        return TryFindNearestSpawnPosition(
            originWorldPosition,
            out spawnWorldPosition,
            out _);
    }

    /// <summary>
    /// 寻找距离指定世界坐标最近的安全背包生成位置。
    /// </summary>
    /// <param name="originWorldPosition">玩家死亡时的世界坐标</param>
    /// <param name="spawnWorldPosition">找到的背包生成坐标</param>
    /// <param name="supportCell">背包下方的地面单元格</param>
    /// <returns>是否成功找到位置</returns>
    public bool TryFindNearestSpawnPosition(
        Vector3 originWorldPosition,
        out Vector3 spawnWorldPosition,
        out Vector3Int supportCell)
    {
        spawnWorldPosition = default;
        supportCell = default;

        if (solidTilemap == null)
        {
            return false;
        }

        // 将死亡位置转换到实体地形的单元格坐标。
        Vector3Int originCell =
            solidTilemap.WorldToCell(originWorldPosition);


        // 2D 游戏中，玩家和 Tilemap 的世界 Z 坐标有时并不完全相同。
        // 直接使用 WorldToCell 得到的 Z，可能会搜索到错误的 Tilemap 层。
        // 因此这里使用 Tilemap 实际边界的 Z 平面。
        originCell.z = solidTilemap.cellBounds.zMin;

        bool hasFoundPosition = false;
        float nearestDistanceSqr = float.PositiveInfinity;

        // 搜索指定矩形范围内的所有候选地面。
        for (
            int yOffset = -verticalSearchRadius;
            yOffset <= verticalSearchRadius;
            yOffset++)
        {
            for (
                int xOffset = -horizontalSearchRadius;
                xOffset <= horizontalSearchRadius;
                xOffset++)
            {
                Vector3Int candidateSupportCell =
                    originCell +
                    new Vector3Int(xOffset, yOffset, 0);

                if (!TryGetSpawnPositionAboveCell(
                    candidateSupportCell,
                    out Vector3 candidateSpawnPosition))
                {
                    continue;
                }

                /*
                 * 使用世界空间距离，而不是单元格曼哈顿距离。
                 *
                 * 这样即使 Grid 的 Cell Size 不是 1，
                 * 或 X、Y 单元格尺寸不同，距离结果仍然正确。
                 */
                Vector2 difference =
                    (Vector2)(candidateSpawnPosition -
                    originWorldPosition);

                float distanceSqr = difference.sqrMagnitude;

                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                spawnWorldPosition = candidateSpawnPosition;
                supportCell = candidateSupportCell;
                hasFoundPosition = true;
            }
        }

        return hasFoundPosition;
    }

    /// <summary>
    /// 检查指定地面瓦片上方是否适合生成背包。
    /// </summary>
    private bool TryGetSpawnPositionAboveCell(
        Vector3Int supportCell,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;

        // 候选地面必须是玩家真正能踩住的实体瓦片。
        if (!IsSolidCell(supportCell))
        {
            return false;
        }


        /*
         * 检查地面上方是否有足够的站立空间。
         *
         * clearanceHeightInCells = 2 时，会检查：
         * supportCell + (0, 1)
         * supportCell + (0, 2)
         */
        for (
            int xOffset = -clearanceHalfWidthInCells;
            xOffset <= clearanceHalfWidthInCells;
            xOffset++)
        {
            for (
                int height = 1;
                height <= clearanceHeightInCells;
                height++)
            {
                Vector3Int clearanceCell =
                    supportCell +
                    new Vector3Int(xOffset, height, 0);

                // 上方存在实体瓦片，玩家会卡住。
                if (IsSolidCell(clearanceCell))
                {
                    return false;
                }

            }
        }

        // 背包生成在地面正上方的空白单元格中心。
        Vector3Int standCell = supportCell + Vector3Int.up;

        Vector3 standCellCenter =
            solidTilemap.GetCellCenterWorld(standCell);

        spawnPosition =
            standCellCenter +
            new Vector3(spawnOffset.x, spawnOffset.y, 0f);

        return true;
    }

    /// <summary>
    /// 判断指定单元格是否是可以碰撞的实体地形。
    /// </summary>
    private bool IsSolidCell(Vector3Int cell)
    {
        if (!solidTilemap.HasTile(cell))
        {
            return false;
        }

        if (!requireTileCollider)
        {
            return true;
        }

        /*
         * 避免把只有贴图、没有碰撞的瓦片误认为地面。
         */
        return solidTilemap.GetColliderType(cell) !=
            Tile.ColliderType.None;
    }


}
