using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private Camera mainCamera;
    private float lastCameraPositionX; // 主相机上一帧的x位置
    private float cameraHalfWidth; // 相机视野半宽

    [SerializeField] private ParallaxLayer[] backgroundLayers; // 各层级背景

    private void Awake()
    {
        mainCamera = Camera.main;
        cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        InitializeLayers();
    }

    private void Update()
    {
        float currentCameraPositionX = mainCamera.transform.position.x; // 获取当前主相机x位置
        float distanceToMove = currentCameraPositionX - lastCameraPositionX; // 需移动距离
        lastCameraPositionX = currentCameraPositionX; // 更新上一帧x位置

        float cameraLeftEdge = currentCameraPositionX - cameraHalfWidth; // 相机左边缘位置
        float cameraRightEdge = currentCameraPositionX + cameraHalfWidth; // 相机右边缘位置

        foreach (ParallaxLayer layer in backgroundLayers)
        {
            layer.Move(distanceToMove);
            // 移动各层级的背景

            layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
            // 当超出相机边界时循环背景图像位置
        }
    }



    //private void FixedUpdate()
    //{
    //    float currentCameraPositionX = mainCamera.transform.position.x; // 获取当前主相机x位置
    //    float distanceToMove = currentCameraPositionX - lastCameraPositionX; // 需移动距离
    //    lastCameraPositionX = currentCameraPositionX; // 更新上一帧x位置

    //    float cameraLeftEdge = currentCameraPositionX - cameraHalfWidth; // 相机左边缘位置
    //    float cameraRightEdge = currentCameraPositionX + cameraHalfWidth; // 相机右边缘位置

    //    foreach (ParallaxLayer layer in backgroundLayers)
    //    {
    //        layer.Move(distanceToMove);
    //        // 移动各层级的背景

    //        layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
    //        // 当超出相机边界时循环背景图像位置
    //    }
    //}

    private void InitializeLayers()
    {
        foreach (ParallaxLayer layer in backgroundLayers)
        {
            layer.CalculateImageWidth();
            // 计算各层级图像宽度
        }
    }
}
