using System;
using System.Collections.Generic;
using UnityEngine;


public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 当前已注册的所有玩家列表（只读视图，外部请不要修改）
    public IReadOnlyList<Player> AllPlayers
    {
        get
        {
            return players;
        }
    }


    // 当前玩家
    public Player CurrentPlayer
    {
        get
        {
            return currentPlayer;
        }
        private set
        {
            if (currentPlayer == value)
            {
                return;
            }

            currentPlayer = value;
            OnCurrentPlayerChanged?.Invoke(currentPlayer);
        }
    }

    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private Player currentPlayer;


    // 有玩家被注册时触发。
    public event Action<Player> OnPlayerRegistered;


    // 有玩家被移除时触发。
    public event Action<Player> OnPlayerUnregistered;


    // currentPlayer 发生变化时触发。
    public event Action<Player> OnCurrentPlayerChanged;



    // 注册一个玩家。
    // 在 Player.OnEnable / Start 中调用。
    internal void RegisterPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("尝试注册一个空的 Player。");
            return;
        }

        if (players.Contains(player))
        {
            // 已经在列表里了，不重复加入
            return;
        }

        players.Add(player);
        OnPlayerRegistered?.Invoke(player);

        if (CurrentPlayer == null)
        {
            CurrentPlayer = player;
        }
    }


    // 取消注册一个玩家。
    // 在 Player.OnDisable / OnDestroy 中调用
    internal void UnregisterPlayer(Player player)
    {
        if (player == null)
        {
            return;
        }

        if (players.Remove(player))
        {
            OnPlayerUnregistered?.Invoke(player);

            // 如果移除的是当前 LocalPlayer，尝试自动换一个（或者置空）
            if (CurrentPlayer == player)
            {
                CurrentPlayer = players.Count > 0 ? players[0] : null;
            }
        }
    }


    // 注册玩家
    // Player 自己在 OnEnable 里可以直接 PlayerManager.Register(this, isLocal)
    public static void Register(Player player)
    {
        if (Instance == null)
        {
            Debug.LogError("PlayerManager.Instance 还不存在，无法注册 Player。");
            return;
        }

        Instance.RegisterPlayer(player);
    }


    // 取消注册玩家
    public void Unregister(Player player)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.UnregisterPlayer(player);
    }


    // 尝试获取当前 LocalPlayer
    // 返回值表示是否获取成功
    public Player TryGetCurrentPlayer()
    {
        return currentPlayer;
    }

}
