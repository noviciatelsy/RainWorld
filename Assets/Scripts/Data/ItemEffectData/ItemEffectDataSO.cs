using System;
using UnityEngine;

public class ItemEffectDataSO : ScriptableObject
{
    [TextArea]
    public string effectDescription; // 效果描述
    protected Player player;

    public virtual void Subscribe(Player player)
    {
        this.player = player; // 获取player
    }

    public virtual void Unsubscribe()
    {
        player = null; // 还原player
    }

    public virtual void StartHoldingItem()
    {

    }

    public virtual void EndHoldingItem()
    {

    }

    public virtual void MainUse()
    {

    }

    public virtual void SecondaryUse()
    {

    }
}