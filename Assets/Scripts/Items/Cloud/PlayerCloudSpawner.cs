using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCloudSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cloudSpawnPosition;
    // 云平台生成位置

    [SerializeField] private CloudPlatform cloudPlatformPrefab;
    // 云平台预制体


    [Header("Preview Settings")]
    [SerializeField] private Color previewColor = new Color(0.25f, 1f, 0.25f, 0.45f);
    // 预览颜色
    // 默认是半透明绿色

    [SerializeField] private bool previewFollowSpawnPosition = true;
    // 预览是否跟随生成位置移动


    private GameObject previewCloudObject;
    // 预览云平台实例

    private SpriteRenderer[] previewSpriteRenderers;
    // 预览云平台上的 SpriteRenderer

    private Collider2D[] previewColliders;
    // 预览云平台上的 Collider2D

    private CloudPlatform previewCloudPlatform;
    // 预览对象上的 CloudPlatform 脚本

    private bool isPreviewEnabled;
    // 当前是否开启预览


    private void Update()
    {
        if (!isPreviewEnabled)
        {
            return;
        }

        if (!previewFollowSpawnPosition)
        {
            return;
        }

        UpdatePreviewPosition();
    }


    private void OnDisable()
    {
        DisablePreviewCloudSpawnPosition();
    }


    /// <summary>
    /// 开启云平台生成位置预览。
    /// </summary>
    public void EnablePreviewCloudSpawnPosition()
    {
        if (cloudPlatformPrefab == null || cloudSpawnPosition == null)
        {
            Debug.LogWarning
            (
                "PlayerCloudSpawner 缺少云平台预制体或生成位置。",
                this
            );

            return;
        }

        if (previewCloudObject == null)
        {
            CreatePreviewCloud();
        }

        isPreviewEnabled = true;

        previewCloudObject.SetActive(true);

        UpdatePreviewPosition();
    }


    /// <summary>
    /// 关闭云平台生成位置预览。
    /// </summary>
    public void DisablePreviewCloudSpawnPosition()
    {
        isPreviewEnabled = false;

        if (previewCloudObject == null)
        {
            return;
        }

        previewCloudObject.SetActive(false);
    }


    /// <summary>
    /// 生成真实云平台。
    /// </summary>
    public bool SpawnCloudPlatform()
    {
        if (cloudPlatformPrefab == null || cloudSpawnPosition == null)
        {
            Debug.LogWarning
            (
                "PlayerCloudSpawner 缺少云平台预制体或生成位置。",
                this
            );

            return false;
        }

        CloudPlatform newCloudPlatform =
            Instantiate
            (
                cloudPlatformPrefab,
                cloudSpawnPosition.position,
                cloudSpawnPosition.rotation
            );

        newCloudPlatform.Initialize();

        return true;
    }


    /// <summary>
    /// 创建预览云平台。
    /// </summary>
    private void CreatePreviewCloud()
    {
        previewCloudObject =
            Instantiate
            (
                cloudPlatformPrefab.gameObject,
                cloudSpawnPosition.position,
                cloudSpawnPosition.rotation
            );

        previewCloudObject.name =
            "CloudPlatform_Preview";

        previewCloudPlatform =
            previewCloudObject.GetComponent<CloudPlatform>();

        if (previewCloudPlatform != null)
        {
            // 预览对象不能执行真实云平台的生命周期。
            previewCloudPlatform.enabled = false;
        }

        previewColliders =
            previewCloudObject.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < previewColliders.Length; i++)
        {
            if (previewColliders[i] == null)
            {
                continue;
            }

            // 预览云平台不应该产生碰撞。
            previewColliders[i].enabled = false;
        }

        previewSpriteRenderers =
            previewCloudObject.GetComponentsInChildren<SpriteRenderer>();

        ApplyPreviewColor();
    }


    /// <summary>
    /// 更新预览云平台位置。
    /// </summary>
    private void UpdatePreviewPosition()
    {
        if (previewCloudObject == null || cloudSpawnPosition == null)
        {
            return;
        }

        previewCloudObject.transform.position =
            cloudSpawnPosition.position;

        previewCloudObject.transform.rotation =
            cloudSpawnPosition.rotation;
    }


    /// <summary>
    /// 应用预览颜色。
    /// </summary>
    private void ApplyPreviewColor()
    {
        if (previewSpriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < previewSpriteRenderers.Length; i++)
        {
            SpriteRenderer currentSpriteRenderer =
                previewSpriteRenderers[i];

            if (currentSpriteRenderer == null)
            {
                continue;
            }

            currentSpriteRenderer.color =
                previewColor;
        }
    }
}