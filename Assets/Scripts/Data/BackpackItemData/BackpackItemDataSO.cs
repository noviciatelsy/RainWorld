using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/BackpackItem Data", fileName = "BackpackItemData - ")]
public class BackpackItemDataSO : ScriptableObject
{
    public Sprite itemSprite;
    public float pixelAmount = 100; // RectTranform的每格尺寸
    public Vector2Int imageSize = new Vector2Int(0, 0); // 道具图像应该长几格，宽几格
    public Vector2Int[] occupationArea ; // 道具实际所占格子
}
