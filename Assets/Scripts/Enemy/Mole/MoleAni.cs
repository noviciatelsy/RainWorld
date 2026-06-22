using UnityEngine;

/// <summary>
/// 鼹鼠动画总控：偷取爪显隐；爪心使用玩家世界坐标 + 偏移（绝对坐标）。
/// </summary>
public class MoleAni : MonoBehaviour
{
    [Tooltip("MoleClaw 预制体实例（可为子物体）")]
    public MoleClaw claw;

    [Tooltip("可选：用于判定朝向")]
    public Mole2D mole;

    [Tooltip("沿「玩家→鼹鼠」方向的世界距离偏移")]
    public float stealClawTowardMoleDistance = 0.1f;

    [Tooltip("叠加在玩家世界坐标上的固定偏移")]
    public Vector2 stealClawWorldOffset = Vector2.zero;

    private bool clawActive;
    private Vector2 lastPlayerWorldPos;
    private bool hasPlayerTarget;
    private Transform stealTargetTransform;
    private Transform clawOriginalParent;
    private Vector3 clawOriginalLocalPosition;
    private Quaternion clawOriginalLocalRotation;
    private Vector3 clawOriginalLocalScale;
    private bool clawUnparented;
    private bool clawRestorePending;

    private void Awake()
    {
        if (mole == null)
        {
            mole = GetComponent<Mole2D>();
        }

        if (claw == null)
        {
            claw = GetComponentInChildren<MoleClaw>(true);
        }

        if (claw != null)
        {
            CacheClawParentDefaults();
            claw.SetHiddenInstant();
            claw.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!clawRestorePending || claw == null || mole == null || mole.IsStompPaused)
        {
            return;
        }

        RestoreClawParent();
    }

    public void SetActivate(bool active)
    {
        if (claw == null)
        {
            return;
        }

        if (active)
        {
            if (clawActive && !claw.IsFadingOut)
            {
                return;
            }

            clawActive = true;
            claw.gameObject.SetActive(true);
            EnsureClawWorldSpace();
            claw.PlayAppear();
            RefreshClawTransform();
            return;
        }

        if (!clawActive && !claw.gameObject.activeSelf)
        {
            return;
        }

        clawActive = false;
        hasPlayerTarget = false;
        stealTargetTransform = null;
        claw.StopMove();

        if (!claw.gameObject.activeSelf)
        {
            TryRestoreClawParent();
            claw.SetHiddenInstant();
            return;
        }

        claw.PlayDisappear(() =>
        {
            TryRestoreClawParent();
            claw.SetHiddenInstant();
            claw.gameObject.SetActive(false);
        });
    }

    public void SetStealTarget(Transform target)
    {
        stealTargetTransform = target;
        hasPlayerTarget = target != null;

        if (hasPlayerTarget)
        {
            lastPlayerWorldPos = target.position;
            RefreshClawTransform();
        }
    }

    public void HideClawImmediate()
    {
        if (claw == null)
        {
            clawActive = false;
            hasPlayerTarget = false;
            stealTargetTransform = null;
            return;
        }

        clawActive = false;
        hasPlayerTarget = false;
        stealTargetTransform = null;
        claw.StopMove();
        TryRestoreClawParent();
        claw.SetHiddenInstant();
        claw.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!clawActive || claw == null || !hasPlayerTarget)
        {
            return;
        }

        EnsureClawWorldSpace();
        RefreshClawTransform();
    }

    private void CacheClawParentDefaults()
    {
        if (claw == null)
        {
            return;
        }

        clawOriginalParent = claw.transform.parent;
        clawOriginalLocalPosition = claw.transform.localPosition;
        clawOriginalLocalRotation = claw.transform.localRotation;
        clawOriginalLocalScale = claw.transform.localScale;
    }

    private void EnsureClawWorldSpace()
    {
        if (claw == null || clawUnparented)
        {
            return;
        }

        CacheClawParentDefaults();
        claw.transform.SetParent(null, true);
        clawUnparented = true;
        clawRestorePending = false;
    }

    private void TryRestoreClawParent()
    {
        if (ShouldDeferClawRestore())
        {
            clawRestorePending = true;
            return;
        }

        RestoreClawParent();
    }

    private bool ShouldDeferClawRestore()
    {
        return clawUnparented && mole != null && mole.IsStompPaused;
    }

    private void RestoreClawParent()
    {
        clawRestorePending = false;

        if (claw == null || !clawUnparented || clawOriginalParent == null)
        {
            clawUnparented = false;
            return;
        }

        claw.transform.SetParent(clawOriginalParent, false);
        claw.transform.localPosition = clawOriginalLocalPosition;
        claw.transform.localRotation = clawOriginalLocalRotation;
        claw.transform.localScale = clawOriginalLocalScale;
        claw.ResetVisualDefaults();
        clawUnparented = false;
    }

    private void RefreshClawTransform()
    {
        if (claw == null || !clawActive || !hasPlayerTarget)
        {
            return;
        }

        Vector2 playerPos = stealTargetTransform != null
            ? (Vector2)stealTargetTransform.position
            : lastPlayerWorldPos;
        Vector2 molePos = mole != null ? (Vector2)mole.transform.position : playerPos + Vector2.left;

        Vector2 toMole = molePos - playerPos;
        if (toMole.sqrMagnitude < 0.0001f)
        {
            toMole = Vector2.left;
        }

        Vector2 clawCenter = playerPos
            + toMole.normalized * stealClawTowardMoleDistance
            + stealClawWorldOffset;

        bool faceLeft = molePos.x < playerPos.x;
        claw.ClawMove(clawCenter, faceLeft);
    }
}
