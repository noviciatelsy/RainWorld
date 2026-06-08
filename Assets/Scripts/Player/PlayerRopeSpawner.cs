using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRopeSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rope ropePrefab;
    // 要生成的绳子预制体

    [SerializeField] private Transform ropeSpawnPosition;
    // 绳子生成参考点
    // 也就是玩家身上的 RopeSpawnPosition

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;
    // 地面检测 LayerMask

    [SerializeField, Min(0.01f)] private float minimumSpawnLength = 0.25f;
    // 允许生成的最短绳长
    // 因为你的 Rope 至少需要 Head + Tail，也就是 0.25

    [SerializeField, Min(0.25f)] private float maxRopeLength = 10f;
    // 绳子的最大生成长度

    [SerializeField, Min(0f)] private float groundClearance = 0f;
    // 绳尾距离地面的预留距离
    // 如果严格按照“Min(距离地面, 最大绳长)”生成，就设为 0


    [Header("Preview Settings")]
    [SerializeField] private Color previewColor = new Color(0f, 1f, 0f, 0.35f);
    // 预览绳子的颜色，默认绿色半透明

    [SerializeField] private int previewSortingOrderOffset = 100;
    // 让预览绳子显示得更靠前一点，避免被场景物体挡住

    private Rope previewRope;
    // 用于预览的绳子实例

    private SpriteRenderer[] previewSpriteRenderers;
    // 预览绳子的所有 SpriteRenderer

    private Collider2D[] previewColliders;
    // 预览绳子的所有 Collider2D

    private bool isPreviewEnabled;
    // 当前是否开启预览

    private void LateUpdate()
    {
        if (isPreviewEnabled == false)
        {
            return;
        }

        UpdatePreviewRope();
    }

    private void Update()
    {
        // 测试按键
        if(Input.GetKeyDown(KeyCode.J))
        {
            TogglePreview();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            SpawnRope();
        }

    }

    public void SetPreviewEnabled(bool enabled)
    {
        isPreviewEnabled = enabled;

        if (enabled)
        {
            ShowPreviewRope();
        }
        else
        {
            HidePreviewRope();
        }
    }

    public void TogglePreview()
    {
        SetPreviewEnabled(isPreviewEnabled == false);
    }

    public Rope SpawnRope()
    {
        if (CanUseRopeSpawner() == false)
        {
            return null;
        }

        float actualRopeLength = CalculateActualRopeLength();

        if (actualRopeLength < minimumSpawnLength)
        {
            return null;
        }

        Vector3 spawnPosition = CalculateRopeCenterPosition(actualRopeLength);


        Rope newRope = Instantiate(ropePrefab, spawnPosition, Quaternion.identity);

        newRope.SetRopeLength(actualRopeLength);
        // 生成后设置实际绳长

        return newRope;
    }

    private void ShowPreviewRope()
    {
        if (previewRope == null)
        {
            CreatePreviewRope();
        }

        if (previewRope == null)
        {
            return;
        }

        previewRope.gameObject.SetActive(true);
        UpdatePreviewRope();
    }

    private void HidePreviewRope()
    {
        if (previewRope == null)
        {
            return;
        }

        previewRope.gameObject.SetActive(false);
    }

    private void CreatePreviewRope()
    {
        if (CanUseRopeSpawner() == false)
        {
            return;
        }

        previewRope = Instantiate(ropePrefab);
        previewRope.name = ropePrefab.name + "_Preview";

        previewColliders = previewRope.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D previewCollider in previewColliders)
        {
            previewCollider.enabled = false;
            // 预览绳子不参与触发器检测
            // 否则玩家只是预览时就会进入 RopeArea
        }

        previewSpriteRenderers = previewRope.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer spriteRenderer in previewSpriteRenderers)
        {
            spriteRenderer.color = previewColor;
            spriteRenderer.sortingOrder += previewSortingOrderOffset;
        }

        previewRope.gameObject.SetActive(false);
    }

    private void UpdatePreviewRope()
    {
        if (previewRope == null)
        {
            return;
        }

        if (CanUseRopeSpawner() == false)
        {
            previewRope.gameObject.SetActive(false);
            return;
        }

        float actualRopeLength = CalculateActualRopeLength();

        if (actualRopeLength < minimumSpawnLength)
        {
            previewRope.gameObject.SetActive(false);
            return;
        }

        previewRope.gameObject.SetActive(true);

        previewRope.SetRopeLength(actualRopeLength);
        previewRope.transform.position = CalculateRopeCenterPosition(actualRopeLength);
        previewRope.transform.rotation = Quaternion.identity;
    }

    private float CalculateActualRopeLength()
    {
        if (ropeSpawnPosition == null)
        {
            return 0f;
        }

        float distanceToGround = maxRopeLength;

        RaycastHit2D hit = Physics2D.Raycast
        (
            ropeSpawnPosition.position,
            Vector2.down,
            maxRopeLength,
            groundLayerMask
        );

        if (hit.collider != null)
        {
            distanceToGround = hit.distance;
        }

        float actualRopeLength = Mathf.Min(distanceToGround, maxRopeLength);

        actualRopeLength -= groundClearance;
        actualRopeLength = Mathf.Max(0f, actualRopeLength);

        return actualRopeLength;
    }

    private Vector3 CalculateRopeCenterPosition(float actualRopeLength)
    {
        return ropeSpawnPosition.position + Vector3.down * actualRopeLength * 0.5f;
    }

    private bool CanUseRopeSpawner()
    {
        if (ropePrefab == null)
        {
            Debug.LogWarning($"{nameof(PlayerRopeSpawner)} 缺少 ropePrefab 引用。", this);
            return false;
        }

        if (ropeSpawnPosition == null)
        {
            Debug.LogWarning($"{nameof(PlayerRopeSpawner)} 缺少 ropeSpawnPosition 引用。", this);
            return false;
        }

        return true;
    }

}