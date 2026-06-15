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
    private Transform clawOriginalParent;
    private bool clawUnparented;

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
            claw.SetHiddenInstant();
            claw.gameObject.SetActive(false);
        }
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
        claw.StopMove();

        if (!claw.gameObject.activeSelf)
        {
            RestoreClawParent();
            claw.SetHiddenInstant();
            return;
        }

        claw.PlayDisappear(() =>
        {
            RestoreClawParent();
            claw.SetHiddenInstant();
            claw.gameObject.SetActive(false);
        });
    }

    public void UpdateStealClaw(Vector2 playerWorldPos)
    {
        lastPlayerWorldPos = playerWorldPos;
        hasPlayerTarget = true;
        RefreshClawTransform();
    }

    private void LateUpdate()
    {
        if (!clawActive || claw == null)
        {
            return;
        }

        EnsureClawWorldSpace();
        RefreshClawTransform();
    }

    private void EnsureClawWorldSpace()
    {
        if (claw == null || clawUnparented)
        {
            return;
        }

        clawOriginalParent = claw.transform.parent;
        claw.transform.SetParent(null, true);
        clawUnparented = true;
    }

    private void RestoreClawParent()
    {
        if (claw == null || !clawUnparented)
        {
            return;
        }

        claw.transform.SetParent(clawOriginalParent, true);
        clawUnparented = false;
    }

    private void RefreshClawTransform()
    {
        if (claw == null || !clawActive || !hasPlayerTarget)
        {
            return;
        }

        Vector2 playerPos = lastPlayerWorldPos;
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
