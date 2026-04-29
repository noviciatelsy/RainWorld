using UnityEngine;

[CreateAssetMenu(menuName = "Setup/BackpackItem Data", fileName = "BackpackItemData - ")]
public class BackpackItemDataSO : ScriptableObject
{
    public float pixelAmount = 80; // RectTranform的每格尺寸
    public Vector2Int imageSize = new Vector2Int(1, 1); // 道具图像应该长几格，宽几格
    public Vector2Int[] occupationArea; // 道具实际所占格子

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (pixelAmount <= 0)
        {
            pixelAmount = 80;
        }

        if (imageSize.x < 1)
        {
            imageSize.x = 1;
        }

        if (imageSize.y < 1)
        {
            imageSize.y = 1;
        }

        if (occupationArea == null || occupationArea.Length == 0)
        {
            occupationArea = new Vector2Int[]
            {
                new Vector2Int(1, 1)
            };
        }
    }
#endif
}