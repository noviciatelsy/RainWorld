using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可破坏墙壁：收到破坏通知后，按指定顺序播放碎块缩小并抛飞，最后进入破坏状态。
/// 所有碎块动画由本组件统一 Update 驱动，避免大量 Coroutine / 组件开销。
/// </summary>
[DisallowMultipleComponent]
public class DestructibleWall : MonoBehaviour, IDestructibleWallNotify
{
    [Serializable]
    public class Fragment
    {
        public Transform fragmentTransform;
        public SpriteRenderer spriteRenderer;
    }

    [Header("State")]
    [SerializeField] private bool isDestroyed;
    [SerializeField] private bool isPermanentDestroy;

    [Header("Fragments")]
    [Tooltip("留空则从 fragmentRoot 下自动收集（不含 deeper 子层级时可手动指定）")]
    [SerializeField] private Transform fragmentRoot;

    [SerializeField] private Fragment[] fragments;

    [Header("Break Order")]
    [SerializeField] private DestructibleWallSortDirection primarySort = DestructibleWallSortDirection.TopToBottom;
    [SerializeField] private DestructibleWallSortDirection secondarySort = DestructibleWallSortDirection.LeftToRight;
    [Tooltip("主优先级分组容差（行/列）；0 则自动估算")]
    [SerializeField] private float rowGroupTolerance;

    [Header("Break Animation")]
    [Tooltip("true：同一主分组（行/列）内所有碎块同时开始破碎；false：逐个碎块依次破碎")]
    [SerializeField] private bool breakPrimaryGroupTogether = true;
    [Tooltip("相邻破碎批次之间的间隔（秒）；整组模式为行与行之间，逐个模式为碎块与碎块之间")]
    [SerializeField] private float fragmentBreakInterval = 0.03f;
    [SerializeField] private float breakDuration = 0.35f;
    [SerializeField] private float scatterDistance = 0.55f;
    [SerializeField] private float scatterDistanceRandomMin = 0.75f;
    [SerializeField] private float scatterDistanceRandomMax = 1.35f;
    [SerializeField] private float scatterWobbleStrength = 0.35f;
    [SerializeField] private float scatterVerticalDrift = 0.25f;
    [SerializeField] private float scatterOutwardBias = 0.35f;
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Final State")]
    [Tooltip("true：最终 SetActive(false)；false：仅 scale 归零")]
    [SerializeField] private bool deactivateFragmentsOnComplete = true;

    [Header("Colliders")]
    [Tooltip("Ground 层碰撞体，破坏开始时直接 Destroy")]
    [SerializeField] private Collider2D groundCollider;

    [Tooltip("其它阻挡碰撞体，破坏时仅禁用")]
    [SerializeField] private Collider2D[] wallColliders;

    public bool IsDestroyed => isDestroyed;
    public bool IsPermanentDestroy => isPermanentDestroy;

    private FragmentRuntime[] runtimeFragments;
    private int[][] breakGroups;
    private int nextGroupIndex;
    private float nextBreakTime;
    private bool isBreaking;
    private int activeBreakCount;

    private struct FragmentRuntime
    {
        public Transform Transform;
        public Vector3 InitialLocalScale;
        public Vector3 InitialLocalPosition;
        public Vector3 ScatterDirection;
        public Vector3 ScatterTangent;
        public float ScatterDistanceMultiplier;
        public float WobblePhase;
        public float VerticalDrift;
        public float BreakStartTime;
        public bool IsBreaking;
        public bool IsFinished;
    }

    private struct FragmentSortEntry
    {
        public int Index;
        public float PrimaryBand;
        public float SecondaryValue;
    }

    private void Awake()
    {
        EnsureFragmentReferences();
        CacheRuntimeFragments();
        EnsureWallColliders();
        EnsureGroundCollider();

        if (isDestroyed)
        {
            ApplyDestroyedStateImmediate(skipAnimation: true);
        }
    }

    private void Update()
    {
        if (!isBreaking)
        {
            return;
        }

        float now = Time.time;

        if (nextGroupIndex < breakGroups.Length && now >= nextBreakTime)
        {
            BeginBreakGroup(breakGroups[nextGroupIndex]);
            nextGroupIndex++;
            nextBreakTime = now + fragmentBreakInterval;
        }

        UpdateActiveBreaks(now);
    }

    /// <summary>
    /// 外部破坏通知入口。
    /// </summary>
    public void NotifyWallDestroy(bool permanentDestroy = false)
    {
        if (isDestroyed || isBreaking)
        {
            return;
        }

        if (permanentDestroy)
        {
            isPermanentDestroy = true;
        }

        BeginBreakSequence();
    }

    /// <summary>
    /// 跳过破碎动画，直接处于已破坏状态（用于存档恢复等）。
    /// </summary>
    public void ForceDestroyedStateImmediate(bool permanentDestroy = false)
    {
        if (permanentDestroy)
        {
            isPermanentDestroy = true;
        }

        if (isDestroyed)
        {
            ApplyDestroyedStateImmediate(skipAnimation: true);
            return;
        }

        isBreaking = false;
        isDestroyed = true;
        ApplyDestroyedStateImmediate(skipAnimation: true);
    }

    public void ResetWallVisual()
    {
        isDestroyed = false;
        isBreaking = false;
        activeBreakCount = 0;
        nextGroupIndex = 0;
        nextBreakTime = 0f;

        SetWallCollidersEnabled(true);

        if (runtimeFragments == null)
        {
            return;
        }

        for (int i = 0; i < runtimeFragments.Length; i++)
        {
            ref FragmentRuntime runtime = ref runtimeFragments[i];
            if (runtime.Transform == null)
            {
                continue;
            }

            runtime.IsBreaking = false;
            runtime.IsFinished = false;
            runtime.Transform.gameObject.SetActive(true);
            runtime.Transform.localScale = runtime.InitialLocalScale;
            runtime.Transform.localPosition = runtime.InitialLocalPosition;
        }
    }

    private void BeginBreakSequence()
    {
        EnsureFragmentReferences();
        CacheRuntimeFragments();
        BuildBreakOrder();

        if (runtimeFragments == null || runtimeFragments.Length == 0)
        {
            isDestroyed = true;
            DestroyGroundCollider();
            SetWallCollidersEnabled(false);
            return;
        }

        isBreaking = true;
        activeBreakCount = 0;
        nextGroupIndex = 0;
        nextBreakTime = Time.time;
        DestroyGroundCollider();
        SetWallCollidersEnabled(false);
    }

    private void BeginBreakGroup(int[] groupIndices)
    {
        if (groupIndices == null)
        {
            return;
        }

        for (int i = 0; i < groupIndices.Length; i++)
        {
            BeginFragmentBreak(groupIndices[i]);
        }
    }

    private void BeginFragmentBreak(int runtimeIndex)
    {
        ref FragmentRuntime runtime = ref runtimeFragments[runtimeIndex];
        if (runtime.Transform == null || runtime.IsBreaking || runtime.IsFinished)
        {
            return;
        }

        runtime.IsBreaking = true;
        runtime.BreakStartTime = Time.time;
        activeBreakCount++;
    }

    private void UpdateActiveBreaks(float now)
    {
        if (activeBreakCount <= 0 && nextGroupIndex >= breakGroups.Length)
        {
            FinishBreakSequence();
            return;
        }

        float safeDuration = Mathf.Max(0.01f, breakDuration);

        for (int i = 0; i < runtimeFragments.Length; i++)
        {
            ref FragmentRuntime runtime = ref runtimeFragments[i];
            if (!runtime.IsBreaking || runtime.IsFinished || runtime.Transform == null)
            {
                continue;
            }

            float t = Mathf.Clamp01((now - runtime.BreakStartTime) / safeDuration);
            float shrink = shrinkCurve != null ? shrinkCurve.Evaluate(t) : 1f - t;
            float scatter = scatterCurve != null ? scatterCurve.Evaluate(t) : t;
            float travelDistance = scatterDistance * runtime.ScatterDistanceMultiplier;

            Vector3 linearOffset = runtime.ScatterDirection * (scatter * travelDistance);
            float wobble = Mathf.Sin((scatter * Mathf.PI * 2f) + runtime.WobblePhase)
                * scatterWobbleStrength
                * travelDistance;
            Vector3 wobbleOffset = runtime.ScatterTangent * wobble;
            Vector3 driftOffset = Vector3.down * (scatter * scatter * runtime.VerticalDrift * travelDistance);

            runtime.Transform.localScale = runtime.InitialLocalScale * shrink;
            runtime.Transform.localPosition =
                runtime.InitialLocalPosition + linearOffset + wobbleOffset + driftOffset;

            if (t < 1f)
            {
                continue;
            }

            ApplyFragmentDestroyedState(ref runtime);
            runtime.IsBreaking = false;
            runtime.IsFinished = true;
            activeBreakCount--;
        }

        if (activeBreakCount <= 0 && nextGroupIndex >= breakGroups.Length)
        {
            FinishBreakSequence();
        }
    }

    private void FinishBreakSequence()
    {
        isBreaking = false;
        isDestroyed = true;
    }

    private void ApplyDestroyedStateImmediate(bool skipAnimation)
    {
        if (runtimeFragments == null)
        {
            CacheRuntimeFragments();
        }

        if (runtimeFragments == null)
        {
            DestroyGroundCollider();
            SetWallCollidersEnabled(false);
            return;
        }

        for (int i = 0; i < runtimeFragments.Length; i++)
        {
            ref FragmentRuntime runtime = ref runtimeFragments[i];
            ApplyFragmentDestroyedState(ref runtime);
            runtime.IsFinished = true;
            runtime.IsBreaking = false;
        }

        activeBreakCount = 0;
        DestroyGroundCollider();
        SetWallCollidersEnabled(false);

        if (!skipAnimation)
        {
            isDestroyed = true;
        }
    }

    private void ApplyFragmentDestroyedState(ref FragmentRuntime runtime)
    {
        if (runtime.Transform == null)
        {
            return;
        }

        if (deactivateFragmentsOnComplete)
        {
            runtime.Transform.gameObject.SetActive(false);
            return;
        }

        runtime.Transform.localScale = Vector3.zero;
    }

    private void CacheRuntimeFragments()
    {
        if (fragments == null || fragments.Length == 0)
        {
            runtimeFragments = Array.Empty<FragmentRuntime>();
            breakGroups = Array.Empty<int[]>();
            return;
        }

        runtimeFragments = new FragmentRuntime[fragments.Length];
        for (int i = 0; i < fragments.Length; i++)
        {
            Transform fragmentTransform = fragments[i].fragmentTransform;
            if (fragmentTransform == null)
            {
                continue;
            }

            Vector3 outward = fragmentTransform.position - transform.position;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = UnityEngine.Random.insideUnitCircle;
            }

            outward.z = 0f;
            outward.Normalize();

            Vector2 randomDir2D = UnityEngine.Random.insideUnitCircle;
            if (randomDir2D.sqrMagnitude < 0.0001f)
            {
                randomDir2D = Vector2.right;
            }

            randomDir2D.Normalize();
            Vector2 blendedDir = Vector2.Lerp(randomDir2D, new Vector2(outward.x, outward.y), scatterOutwardBias);
            if (blendedDir.sqrMagnitude < 0.0001f)
            {
                blendedDir = randomDir2D;
            }

            blendedDir.Normalize();
            Vector3 scatterDirection = new Vector3(blendedDir.x, blendedDir.y, 0f);
            Vector3 scatterTangent = new Vector3(-scatterDirection.y, scatterDirection.x, 0f);

            runtimeFragments[i] = new FragmentRuntime
            {
                Transform = fragmentTransform,
                InitialLocalScale = fragmentTransform.localScale,
                InitialLocalPosition = fragmentTransform.localPosition,
                ScatterDirection = scatterDirection,
                ScatterTangent = scatterTangent,
                ScatterDistanceMultiplier = UnityEngine.Random.Range(
                    scatterDistanceRandomMin,
                    scatterDistanceRandomMax),
                WobblePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                VerticalDrift = UnityEngine.Random.Range(0f, scatterVerticalDrift)
            };
        }
    }

    private void BuildBreakOrder()
    {
        if (runtimeFragments == null || runtimeFragments.Length == 0)
        {
            breakGroups = Array.Empty<int[]>();
            return;
        }

        float tolerance = ResolveGroupTolerance();
        List<FragmentSortEntry> entries = new List<FragmentSortEntry>(runtimeFragments.Length);

        for (int i = 0; i < runtimeFragments.Length; i++)
        {
            Transform fragmentTransform = runtimeFragments[i].Transform;
            if (fragmentTransform == null)
            {
                continue;
            }

            Vector3 worldPosition = fragmentTransform.position;
            float primaryValue = GetPrimaryAxisValue(worldPosition, primarySort);
            float secondaryValue = GetSecondaryAxisValue(worldPosition, secondarySort);

            entries.Add(new FragmentSortEntry
            {
                Index = i,
                PrimaryBand = QuantizeAxis(primaryValue, tolerance),
                SecondaryValue = secondaryValue
            });
        }

        if (entries.Count == 0)
        {
            breakGroups = Array.Empty<int[]>();
            return;
        }

        List<float> bandKeys = new List<float>();
        for (int i = 0; i < entries.Count; i++)
        {
            float band = entries[i].PrimaryBand;
            if (!ContainsBand(bandKeys, band, tolerance * 0.25f))
            {
                bandKeys.Add(band);
            }
        }

        bandKeys.Sort((a, b) => CompareAxisValue(a, b, primarySort));

        List<int[]> groups = new List<int[]>(bandKeys.Count);
        for (int bandIndex = 0; bandIndex < bandKeys.Count; bandIndex++)
        {
            float bandKey = bandKeys[bandIndex];
            List<FragmentSortEntry> bandEntries = new List<FragmentSortEntry>();

            for (int i = 0; i < entries.Count; i++)
            {
                if (Mathf.Abs(entries[i].PrimaryBand - bandKey) <= tolerance * 0.25f)
                {
                    bandEntries.Add(entries[i]);
                }
            }

            bandEntries.Sort((a, b) => CompareAxisValue(a.SecondaryValue, b.SecondaryValue, secondarySort));

            if (breakPrimaryGroupTogether)
            {
                int[] bandIndices = new int[bandEntries.Count];
                for (int i = 0; i < bandEntries.Count; i++)
                {
                    bandIndices[i] = bandEntries[i].Index;
                }

                groups.Add(bandIndices);
                continue;
            }

            for (int i = 0; i < bandEntries.Count; i++)
            {
                groups.Add(new[] { bandEntries[i].Index });
            }
        }

        breakGroups = groups.ToArray();
    }

    private float ResolveGroupTolerance()
    {
        if (rowGroupTolerance > 0f)
        {
            return rowGroupTolerance;
        }

        return ComputeAutoGroupTolerance();
    }

    private float ComputeAutoGroupTolerance()
    {
        bool primaryUsesY = IsVerticalAxis(primarySort);
        float minGap = float.MaxValue;
        int validCount = 0;

        for (int i = 0; i < runtimeFragments.Length; i++)
        {
            Transform a = runtimeFragments[i].Transform;
            if (a == null)
            {
                continue;
            }

            validCount++;
            float primaryA = GetPrimaryAxisValue(a.position, primarySort);

            for (int j = i + 1; j < runtimeFragments.Length; j++)
            {
                Transform b = runtimeFragments[j].Transform;
                if (b == null)
                {
                    continue;
                }

                float primaryB = GetPrimaryAxisValue(b.position, primarySort);
                float gap = Mathf.Abs(primaryA - primaryB);
                if (gap > 0.001f && gap < minGap)
                {
                    minGap = gap;
                }
            }
        }

        if (validCount <= 1 || minGap >= float.MaxValue)
        {
            return 0.15f;
        }

        return Mathf.Max(0.05f, minGap * 0.45f);
    }

    private static bool ContainsBand(List<float> bandKeys, float band, float epsilon)
    {
        for (int i = 0; i < bandKeys.Count; i++)
        {
            if (Mathf.Abs(bandKeys[i] - band) <= epsilon)
            {
                return true;
            }
        }

        return false;
    }

    private static float QuantizeAxis(float value, float tolerance)
    {
        return Mathf.Round(value / tolerance) * tolerance;
    }

    private static bool IsVerticalAxis(DestructibleWallSortDirection direction)
    {
        return direction == DestructibleWallSortDirection.TopToBottom
            || direction == DestructibleWallSortDirection.BottomToTop;
    }

    private static float GetPrimaryAxisValue(Vector3 worldPosition, DestructibleWallSortDirection primary)
    {
        return IsVerticalAxis(primary) ? worldPosition.y : worldPosition.x;
    }

    private static float GetSecondaryAxisValue(Vector3 worldPosition, DestructibleWallSortDirection secondary)
    {
        return IsVerticalAxis(secondary) ? worldPosition.y : worldPosition.x;
    }

    private static int CompareAxisValue(float a, float b, DestructibleWallSortDirection direction)
    {
        switch (direction)
        {
            case DestructibleWallSortDirection.LeftToRight:
                return a.CompareTo(b);
            case DestructibleWallSortDirection.RightToLeft:
                return b.CompareTo(a);
            case DestructibleWallSortDirection.TopToBottom:
                return b.CompareTo(a);
            case DestructibleWallSortDirection.BottomToTop:
                return a.CompareTo(b);
            default:
                return 0;
        }
    }

    private void EnsureFragmentReferences()
    {
        if (fragmentRoot == null)
        {
            fragmentRoot = transform;
        }

        if (fragments != null && fragments.Length > 0)
        {
            return;
        }

        List<Fragment> collected = new List<Fragment>(fragmentRoot.childCount);
        for (int i = 0; i < fragmentRoot.childCount; i++)
        {
            Transform child = fragmentRoot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            collected.Add(new Fragment
            {
                fragmentTransform = child,
                spriteRenderer = child.GetComponent<SpriteRenderer>()
            });
        }

        fragments = collected.ToArray();
    }

    private void EnsureWallColliders()
    {
        EnsureGroundCollider();

        if (wallColliders != null && wallColliders.Length > 0)
        {
            return;
        }

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        if (allColliders == null || allColliders.Length == 0)
        {
            wallColliders = Array.Empty<Collider2D>();
            return;
        }

        List<Collider2D> collected = new List<Collider2D>(allColliders.Length);
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];
            if (collider == null || collider == groundCollider)
            {
                continue;
            }

            collected.Add(collider);
        }

        wallColliders = collected.ToArray();
    }

    private void EnsureGroundCollider()
    {
        if (groundCollider != null)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            return;
        }

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];
            if (collider != null && collider.gameObject.layer == groundLayer)
            {
                groundCollider = collider;
                return;
            }
        }
    }

    private void DestroyGroundCollider()
    {
        if (groundCollider == null)
        {
            return;
        }

        Destroy(groundCollider);
        groundCollider = null;
    }

    private void SetWallCollidersEnabled(bool enabled)
    {
        if (wallColliders == null)
        {
            return;
        }

        for (int i = 0; i < wallColliders.Length; i++)
        {
            if (wallColliders[i] != null)
            {
                wallColliders[i].enabled = enabled;
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Collect Fragments From Children")]
    private void EditorCollectFragments()
    {
        EnsureFragmentReferences();
        CacheRuntimeFragments();
    }

    [ContextMenu("Collect Colliders From Children")]
    private void EditorCollectColliders()
    {
        groundCollider = null;
        wallColliders = null;
        EnsureWallColliders();
    }
#endif
}
