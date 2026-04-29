using System.Collections.Generic;
using UnityEngine;

public static class BackpackItemShapeUtility
{
    public static ItemRotateState GetNextClockwise(ItemRotateState state)
    {
        switch (state)
        {
            case ItemRotateState.Rotate0:
                return ItemRotateState.Rotate90;

            case ItemRotateState.Rotate90:
                return ItemRotateState.Rotate180;

            case ItemRotateState.Rotate180:
                return ItemRotateState.Rotate270;

            case ItemRotateState.Rotate270:
                return ItemRotateState.Rotate0;

            default:
                return ItemRotateState.Rotate0;
        }
    }

    public static int GetClockwiseDegrees(ItemRotateState state)
    {
        switch (state)
        {
            case ItemRotateState.Rotate0:
                return 0;

            case ItemRotateState.Rotate90:
                return 90;

            case ItemRotateState.Rotate180:
                return 180;

            case ItemRotateState.Rotate270:
                return 270;

            default:
                return 0;
        }
    }

    public static Vector2Int GetRotatedImageSize(BackpackItemDataSO data, ItemRotateState state)
    {
        if (data == null)
        {
            return Vector2Int.zero;
        }

        Vector2Int originalSize = data.imageSize;

        switch (state)
        {
            case ItemRotateState.Rotate90:
            case ItemRotateState.Rotate270:
                return new Vector2Int(originalSize.y, originalSize.x);

            case ItemRotateState.Rotate0:
            case ItemRotateState.Rotate180:
            default:
                return originalSize;
        }
    }

    public static List<Vector2Int> GetRotatedOccupationArea(BackpackItemDataSO data, ItemRotateState state)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (data == null || data.occupationArea == null)
        {
            return result;
        }

        Vector2Int originalSize = data.imageSize;

        for (int i = 0; i < data.occupationArea.Length; i++)
        {
            // 你的 Inspector 填法是从 (1,1) 开始，所以这里必须 -1
            Vector2Int zeroBasedCell = new Vector2Int(
                data.occupationArea[i].x - 1,
                data.occupationArea[i].y - 1
            );

            if (zeroBasedCell.x < 0 || zeroBasedCell.y < 0)
            {
                Debug.LogWarning($"occupationArea 坐标不合法：{data.occupationArea[i]}，请从 (1,1) 开始填写。");
                continue;
            }

            if (zeroBasedCell.x >= originalSize.x || zeroBasedCell.y >= originalSize.y)
            {
                Debug.LogWarning(
                    $"occupationArea 坐标超出 imageSize。\n" +
                    $"坐标：{data.occupationArea[i]}\n" +
                    $"imageSize：{originalSize}"
                );

                continue;
            }

            result.Add(RotateCellClockwise(zeroBasedCell, originalSize, state));
        }

        return result;
    }

    private static Vector2Int RotateCellClockwise(Vector2Int zeroBasedCell, Vector2Int originalSize, ItemRotateState state)
    {
        int x = zeroBasedCell.x;
        int y = zeroBasedCell.y;

        int width = originalSize.x;
        int height = originalSize.y;

        switch (state)
        {
            case ItemRotateState.Rotate0:
                return new Vector2Int(x, y);

            case ItemRotateState.Rotate90:
                return new Vector2Int(y, width - 1 - x);

            case ItemRotateState.Rotate180:
                return new Vector2Int(width - 1 - x, height - 1 - y);

            case ItemRotateState.Rotate270:
                return new Vector2Int(height - 1 - y, x);

            default:
                return new Vector2Int(x, y);
        }
    }
}