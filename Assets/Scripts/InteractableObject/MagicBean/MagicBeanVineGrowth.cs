using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔豆藤蔓生长：LineRenderer + 自定义平铺 Shader，沿生长方向连续重复魔豆2/3，顶端魔豆1。
/// </summary>
[DisallowMultipleComponent]
public class MagicBeanVineGrowth : MonoBehaviour
{
    private static Shader lineSpriteShader;

    [Header("Target")]
    [SerializeField] private Transform growTarget;
    [SerializeField] private Transform vineRoot;

    [Header("LineRenderer")]
    [SerializeField] private LineRenderer pathLine;
    [SerializeField] private Transform segmentRoot;
    [SerializeField] private bool showPathLineInPlayMode;
    [Tooltip("藤蔓线段粗细（世界单位）。0 表示使用贴图原始宽度")]
    [SerializeField] private float segmentLineWidth;

    [Header("Sprites")]
    [SerializeField] private Sprite headSprite;
    [SerializeField] private Sprite bodySpriteA;
    [SerializeField] private Sprite bodySpriteB;
    [SerializeField] private string headSpriteResourcePath = "textures/地图资源/InteractableObject/魔豆/魔豆1";
    [SerializeField] private string bodySpriteAResourcePath = "textures/地图资源/InteractableObject/魔豆/魔豆2";
    [SerializeField] private string bodySpriteBResourcePath = "textures/地图资源/InteractableObject/魔豆/魔豆3";

    [Header("Growth")]
    [SerializeField] private float growSpeed = 4f;
    [SerializeField] private float segmentLengthOverride;

    [Header("Visual")]
    [SerializeField] private string sortingLayerName = "InteractableObject";
    [SerializeField] private int sortingOrder;

    [Header("Climb")]
    [SerializeField] private Transform climbZone;
    [SerializeField] private BoxCollider2D climbCollider;
    [SerializeField] private float climbWidth;
    [SerializeField] private string climbZoneLayerName = "CanCollideWithPlayer";

    [Header("Destructible Wall")]
    [SerializeField] private DestructibleWall destructibleWall;
    [SerializeField] private bool permanentWallDestroy;

    [Header("Audio")]
    [SerializeField] private string growSfxName = "魔豆生长音效";

    private readonly List<LineRenderer> segmentLines = new List<LineRenderer>();
    private SpriteRenderer headRenderer;
    private Coroutine growCoroutine;
    private bool hasFinished;

    public bool IsGrowing => growCoroutine != null;
    public bool HasFinished => hasFinished;
    public Transform GrowTarget => growTarget;

    private void Awake()
    {
        if (vineRoot == null)
        {
            vineRoot = transform;
        }

        if (segmentRoot == null)
        {
            segmentRoot = transform;
        }

        EnsurePathLine();
        EnsureHeadRenderer();
        EnsureClimbCollider();
        ResolveSprites();
        ResetVisual();
    }

    private void OnValidate()
    {
        if (vineRoot == null)
        {
            vineRoot = transform;
        }

        growSpeed = Mathf.Max(0.01f, growSpeed);
        climbWidth = Mathf.Max(0.01f, climbWidth > 0.001f ? climbWidth : GetDefaultClimbWidth());
        PreviewPathInEditor();
    }

    public void StartGrowth()
    {
        if (growTarget == null || hasFinished || growCoroutine != null)
        {
            return;
        }

        if (pathLine != null && !showPathLineInPlayMode)
        {
            pathLine.enabled = false;
        }

        growCoroutine = StartCoroutine(GrowToTargetRoutine());
    }

    public void ResetVisual()
    {
        if (growCoroutine != null)
        {
            StopCoroutine(growCoroutine);
            growCoroutine = null;
        }

        hasFinished = false;
        ClearSegmentLines();

        if (pathLine != null)
        {
            pathLine.positionCount = 1;
            pathLine.SetPosition(0, GetVineStartWorld());
            pathLine.enabled = !Application.isPlaying || showPathLineInPlayMode;
        }

        if (headRenderer != null)
        {
            headRenderer.enabled = false;
        }

        if (climbZone != null)
        {
            climbZone.gameObject.SetActive(false);
        }
    }

    private IEnumerator GrowToTargetRoutine()
    {
        if (!string.IsNullOrWhiteSpace(growSfxName))
        {
            AudioManager.Instance?.PlaySFX(growSfxName);
        }

        TryTriggerDestructibleWall();

        Vector3 start = GetVineStartWorld();
        Vector3 end = growTarget.position;
        float totalDistance = Vector3.Distance(start, end);

        if (totalDistance <= 0.001f)
        {
            FinishGrowth(start, end);
            growCoroutine = null;
            yield break;
        }

        Vector3 direction = (end - start) / totalDistance;
        float segmentLength = GetSegmentLength();
        float traveled = 0f;

        SetPathPoint(0, start);

        while (traveled < totalDistance - 0.001f)
        {
            float nextTraveled = Mathf.Min(traveled + segmentLength, totalDistance);
            Vector3 segmentStart = start + direction * traveled;
            Vector3 segmentEnd = start + direction * nextTraveled;

            AddSegmentLine(segmentStart, segmentEnd, PickRandomBodySprite());
            AppendPathPoint(segmentEnd);
            UpdateHead(segmentEnd);
            UpdateClimbZone(start, segmentEnd, direction);

            traveled = nextTraveled;

            if (traveled >= totalDistance - 0.001f)
            {
                break;
            }

            float stepDuration = segmentLength / growSpeed;

            if (stepDuration > 0f)
            {
                yield return new WaitForSeconds(stepDuration);
            }
        }

        FinishGrowth(start, end);
        growCoroutine = null;
    }

    private void TryTriggerDestructibleWall()
    {
        if (destructibleWall == null || destructibleWall.IsDestroyed)
        {
            return;
        }

        destructibleWall.NotifyWallDestroy(permanentWallDestroy);
    }

    private void FinishGrowth(Vector3 start, Vector3 end)
    {
        hasFinished = true;
        AppendPathPoint(end);
        UpdateHead(end);
        UpdateClimbZone(start, end, (end - start).normalized);

        if (headRenderer != null)
        {
            headRenderer.enabled = headSprite != null;
            headRenderer.transform.position = end;
        }
    }

    private void EnsurePathLine()
    {
        if (pathLine == null)
        {
            pathLine = GetComponent<LineRenderer>();
        }

        if (pathLine == null)
        {
            pathLine = gameObject.AddComponent<LineRenderer>();
        }

        ConfigurePathLine(pathLine);
    }

    private void EnsureClimbCollider()
    {
        EnsureClimbZone();
    }

    private void EnsureClimbZone()
    {
        if (climbZone == null)
        {
            Transform existing = transform.Find("ClimbZone");

            if (existing != null)
            {
                climbZone = existing;
            }
        }

        if (climbZone == null)
        {
            GameObject zoneObject = new GameObject("ClimbZone");
            climbZone = zoneObject.transform;
            climbZone.SetParent(transform, false);
        }

        if (climbCollider == null)
        {
            climbCollider = climbZone.GetComponent<BoxCollider2D>();
        }

        if (climbCollider == null)
        {
            climbCollider = climbZone.gameObject.AddComponent<BoxCollider2D>();
        }

        climbCollider.isTrigger = true;

        if (climbZone.GetComponent<MagicBeanVineClimbZone>() == null)
        {
            climbZone.gameObject.AddComponent<MagicBeanVineClimbZone>();
        }

        ApplyClimbZoneLayer(climbZone.gameObject);
        climbZone.gameObject.SetActive(false);
    }

    private void ApplyClimbZoneLayer(GameObject zoneObject)
    {
        if (zoneObject == null || string.IsNullOrWhiteSpace(climbZoneLayerName))
        {
            return;
        }

        int layer = LayerMask.NameToLayer(climbZoneLayerName);

        if (layer >= 0)
        {
            zoneObject.layer = layer;
        }
    }

    private void ResolveSprites()
    {
        if (headSprite == null && !string.IsNullOrWhiteSpace(headSpriteResourcePath))
        {
            headSprite = Resources.Load<Sprite>(headSpriteResourcePath);
        }

        if (bodySpriteA == null && !string.IsNullOrWhiteSpace(bodySpriteAResourcePath))
        {
            bodySpriteA = Resources.Load<Sprite>(bodySpriteAResourcePath);
        }

        if (bodySpriteB == null && !string.IsNullOrWhiteSpace(bodySpriteBResourcePath))
        {
            bodySpriteB = Resources.Load<Sprite>(bodySpriteBResourcePath);
        }

        if (headRenderer != null && headSprite != null)
        {
            headRenderer.sprite = headSprite;
            ApplyHeadScale();
        }
    }

    private void EnsureHeadRenderer()
    {
        if (headRenderer != null)
        {
            return;
        }

        Transform headTransform = transform.Find("Head");

        if (headTransform == null)
        {
            GameObject headObject = new GameObject("Head");
            headTransform = headObject.transform;
            headTransform.SetParent(transform, false);
        }

        headRenderer = headTransform.GetComponent<SpriteRenderer>();

        if (headRenderer == null)
        {
            headRenderer = headTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        headRenderer.sprite = headSprite;
        headRenderer.sortingLayerName = sortingLayerName;
        headRenderer.sortingOrder = sortingOrder + 1;
        headRenderer.enabled = false;
        ApplyHeadScale();
    }

    private void ConfigurePathLine(LineRenderer lineRenderer)
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.sortingLayerName = sortingLayerName;
        lineRenderer.sortingOrder = sortingOrder - 1;
    }

    private void ConfigureSegmentLine(LineRenderer lineRenderer)
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sortingLayerName = sortingLayerName;
        lineRenderer.sortingOrder = sortingOrder;
    }

    private void AddSegmentLine(Vector3 segmentStart, Vector3 segmentEnd, Sprite bodySprite)
    {
        if (bodySprite == null)
        {
            return;
        }

        Vector3 delta = segmentEnd - segmentStart;
        float segmentDistance = delta.magnitude;

        if (segmentDistance <= 0.001f)
        {
            return;
        }

        Vector3 direction = delta / segmentDistance;
        float halfLength = segmentDistance * 0.5f;
        Vector3 midpoint = (segmentStart + segmentEnd) * 0.5f;

        GameObject segmentObject = new GameObject($"Segment_{segmentLines.Count + 1}");
        segmentObject.transform.SetParent(segmentRoot, true);
        segmentObject.transform.SetPositionAndRotation(
            midpoint,
            Quaternion.FromToRotation(Vector3.up, direction));

        LineRenderer segmentLine = segmentObject.AddComponent<LineRenderer>();
        ConfigureSegmentLine(segmentLine);

        float segmentWidth = GetSegmentLineWidth(bodySprite);
        segmentLine.startWidth = segmentWidth;
        segmentLine.endWidth = segmentWidth;
        segmentLine.positionCount = 2;
        segmentLine.SetPosition(0, new Vector3(0f, -halfLength, 0f));
        segmentLine.SetPosition(1, new Vector3(0f, halfLength, 0f));
        segmentLine.material = CreateLineSegmentMaterial(bodySprite, segmentDistance);

        segmentLines.Add(segmentLine);
    }

    private static Material CreateLineSegmentMaterial(Sprite sprite, float segmentDistance)
    {
        if (lineSpriteShader == null)
        {
            lineSpriteShader = Shader.Find("Interactable/MagicBeanLineSprite");
        }

        if (lineSpriteShader == null)
        {
            Debug.LogError("未找到 Shader: Interactable/MagicBeanLineSprite");
            return null;
        }

        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;

        Material material = new Material(lineSpriteShader);
        material.SetTexture("_MainTex", texture);
        material.SetVector(
            "_SpriteRect",
            new Vector4(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height));

        float spriteHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
        material.SetFloat("_TileCount", Mathf.Max(0.01f, segmentDistance / spriteHeight));

        return material;
    }

    private Sprite PickRandomBodySprite()
    {
        if (bodySpriteA != null && bodySpriteB != null)
        {
            return Random.value < 0.5f ? bodySpriteA : bodySpriteB;
        }

        if (bodySpriteA != null)
        {
            return bodySpriteA;
        }

        return bodySpriteB;
    }

    private float GetSegmentLength()
    {
        if (segmentLengthOverride > 0.001f)
        {
            return segmentLengthOverride;
        }

        if (bodySpriteA != null)
        {
            return Mathf.Max(0.1f, bodySpriteA.bounds.size.y);
        }

        if (bodySpriteB != null)
        {
            return Mathf.Max(0.1f, bodySpriteB.bounds.size.y);
        }

        return 0.5f;
    }

    private Vector3 GetVineStartWorld()
    {
        return vineRoot != null ? vineRoot.position : transform.position;
    }

    private void SetPathPoint(int index, Vector3 worldPoint)
    {
        if (pathLine == null)
        {
            return;
        }

        pathLine.positionCount = index + 1;
        pathLine.SetPosition(index, worldPoint);
    }

    private void AppendPathPoint(Vector3 worldPoint)
    {
        if (pathLine == null)
        {
            return;
        }

        int index = pathLine.positionCount;
        pathLine.positionCount = index + 1;
        pathLine.SetPosition(index, worldPoint);
    }

    private void UpdateHead(Vector3 worldPoint)
    {
        if (headRenderer == null)
        {
            return;
        }

        headRenderer.enabled = headSprite != null;
        headRenderer.transform.position = worldPoint;
        ApplyHeadScale();
    }

    private void ApplyHeadScale()
    {
        if (headRenderer == null || headSprite == null)
        {
            return;
        }

        float targetWidth = GetTargetLineWidth();
        float spriteWidth = Mathf.Max(0.001f, headSprite.bounds.size.x);
        float uniformScale = targetWidth / spriteWidth;
        headRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, uniformScale);
    }

    private float GetTargetLineWidth()
    {
        if (segmentLineWidth > 0.001f)
        {
            return segmentLineWidth;
        }

        return GetDefaultClimbWidth();
    }

    private void UpdateClimbZone(Vector3 start, Vector3 end, Vector3 direction)
    {
        if (climbZone == null || climbCollider == null)
        {
            return;
        }

        float height = Vector3.Distance(start, end);

        if (height <= 0.01f)
        {
            climbZone.gameObject.SetActive(false);
            return;
        }

        Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.up;

        climbZone.gameObject.SetActive(true);
        climbZone.position = (start + end) * 0.5f;
        climbZone.rotation = Quaternion.FromToRotation(Vector3.up, normalizedDirection);
        climbCollider.offset = Vector2.zero;
        climbCollider.size = new Vector2(GetClimbWidth(), height);
    }

    private float GetSegmentLineWidth(Sprite bodySprite)
    {
        return GetTargetLineWidth();
    }

    private float GetDefaultClimbWidth()
    {
        if (bodySpriteA != null)
        {
            return bodySpriteA.bounds.size.x;
        }

        if (bodySpriteB != null)
        {
            return bodySpriteB.bounds.size.x;
        }

        return 0.5f;
    }

    private float GetClimbWidth()
    {
        if (climbWidth > 0.001f)
        {
            return climbWidth;
        }

        return GetTargetLineWidth();
    }

    private void ClearSegmentLines()
    {
        for (int i = 0; i < segmentLines.Count; i++)
        {
            if (segmentLines[i] != null)
            {
                Destroy(segmentLines[i].gameObject);
            }
        }

        segmentLines.Clear();
    }

    private void PreviewPathInEditor()
    {
        if (Application.isPlaying || growTarget == null || pathLine == null)
        {
            return;
        }

        Vector3 start = GetVineStartWorld();
        Vector3 end = growTarget.position;
        pathLine.positionCount = 2;
        pathLine.SetPosition(0, start);
        pathLine.SetPosition(1, end);
    }

    private void OnDrawGizmosSelected()
    {
        if (growTarget == null)
        {
            return;
        }

        Vector3 start = vineRoot != null ? vineRoot.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, growTarget.position);
        Gizmos.DrawWireSphere(growTarget.position, 0.15f);
    }
}
