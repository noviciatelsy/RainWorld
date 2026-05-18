using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鼹鼠偷窃/定身意图
/// </summary>
public struct MoleStealIntent : IIntent { }

/// <summary>
/// 鼹鼠随机游走意图
/// </summary>
//public struct MoleIdleIntent : IIntent
//{
//    public List<Vector2> strictPath;
//}

/// <summary>
/// 鼹鼠钻洞传送意图
/// </summary>
public struct MoleUseCaveIntent : IIntent
{
    public MoleCave targetCave;
}