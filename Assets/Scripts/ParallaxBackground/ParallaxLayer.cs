using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    [SerializeField] private Transform background; // 背景transform
    [SerializeField] private float parallaxMultiplier; // 视差倍率
    [SerializeField] private float imageWidthOffset = 10; // 图像宽度补偿

    private float imageFullWidth; // 图像宽度
    private float imageHalfWidth; // 图像半宽

    public void CalculateImageWidth()
    {
        imageFullWidth = background.GetComponent<SpriteRenderer>().bounds.size.x; // 获取图像宽度
        imageHalfWidth = imageFullWidth / 2;
    }

    public void Move(float distanceToMove)
    {
        background.position += Vector3.right * distanceToMove * parallaxMultiplier;
        // 更新倍率计算后的距离
    }

    public void LoopBackground(float cameraLeftEdge, float cameraRightEdge)
    {
        float imageLeftEdge = (background.position.x - imageHalfWidth) + imageWidthOffset; // 获取图像左边缘位置
        float imageRightEdge = (background.position.x + imageHalfWidth) - imageWidthOffset; // 获取图像右边缘位置

        if (imageRightEdge < cameraLeftEdge) // 如果图像整体超出相机左侧
        {
            background.position += Vector3.right * imageFullWidth; // 循环图像位置
        }
        else if (imageLeftEdge > cameraRightEdge) // 如果图像整体超出相机右侧
        {
            background.position -= Vector3.right * imageFullWidth; // 循环图像位置
        }
    }
}
