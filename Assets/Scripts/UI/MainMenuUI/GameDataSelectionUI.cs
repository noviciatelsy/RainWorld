using UnityEngine;

public class GameDataSelectionUI : MonoBehaviour
{
    private GameDataOption[] gameDataOptions;

    private UI_PanelOpenCloseAnimation panelOpenCloseAnimation;

    private void Awake()
    {
        panelOpenCloseAnimation = GetComponent<UI_PanelOpenCloseAnimation>();
        gameDataOptions = GetComponentsInChildren<GameDataOption>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (panelOpenCloseAnimation != null)
        {
            panelOpenCloseAnimation.PlayClose();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("GameDataSelectionUI刷新失败：场景中没有SaveManager");
            return;
        }

        if (gameDataOptions == null || gameDataOptions.Length < GameData.GameDataSlotCount)
        {
            Debug.LogWarning("GameDataSelectionUI刷新失败：gameDataOptions数量不足");
            return;
        }

        for (int i = 0; i < GameData.GameDataSlotCount; i++)
        {
            GameDataSlot slot = SaveManager.Instance.GetGameDataSlot(i);

            if (gameDataOptions[i] != null)
            {
                gameDataOptions[i].SetOption(i, slot, this);
            }
        }
    }

    public void LoadGame(int mySlotIndex)
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        bool success = SaveManager.Instance.SelectGameRunDataSlot(mySlotIndex);

        if (success == false)
        {
            Refresh();
            return;
        }

        EnterGameScene();
    }

    public void DeleteGame(int mySlotIndex)
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        bool success = SaveManager.Instance.DeleteGameRunDataSlot(mySlotIndex);

        if (success)
        {
            Refresh();
        }
    }

    public void NewGame(int mySlotIndex)
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        bool success = SaveManager.Instance.CreateNewGameRunDataInSlot(mySlotIndex);

        if (success == false)
        {
            Refresh();
            return;
        }

        EnterGameScene();
    }

    private void EnterGameScene()
    {
      
    }
}