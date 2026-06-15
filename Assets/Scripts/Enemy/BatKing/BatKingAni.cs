using UnityEngine;

/// <summary>
/// 蝙蝠王动画：逻辑与 BatAni 相同，使用 BatKingfly / BatKingAttack 状态。
/// </summary>
[DisallowMultipleComponent]
public class BatKingAni : BatAni
{
    protected override void ConfigureAnimationDefaults()
    {
        flyStateName = "BatKingfly";
        attackStateName = "BatKingAttack";
    }
}
