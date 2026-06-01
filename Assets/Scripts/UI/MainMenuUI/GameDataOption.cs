using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameDataOption : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI gameRunDateText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button newGameButton;

    private int slotIndex = -1;
    private GameDataSelectionUI owner;

    public void SetOption(int mySlotIndex, GameDataSlot mySlot, GameDataSelectionUI myOwner)
    {
        slotIndex = mySlotIndex;
        owner = myOwner;

        bool isEmpty = mySlot == null || mySlot.IsEmpty();

        if (isEmpty)
        {
            ApplyEmptySlotStyle();
        }
        else
        {
            ApplyExistingSlotStyle(mySlot.runData);
        }
    }

    private void ApplyEmptySlotStyle()
    {
        if (gameRunDateText != null)
        {
            gameRunDateText.text = "无运行记录";
        }

        SetButtonVisible(newGameButton, true);
        SetButtonVisible(loadButton, false);
        SetButtonVisible(deleteButton, false);
    }

    private void ApplyExistingSlotStyle(GameRunData myRunData)
    {
        if (gameRunDateText != null)
        {
            gameRunDateText.text = BuildLastRunDateText(myRunData);
        }

        SetButtonVisible(newGameButton, false);
        SetButtonVisible(loadButton, true);
        SetButtonVisible(deleteButton, true);
    }

    private string BuildLastRunDateText(GameRunData myRunData)
    {
        if (myRunData == null || string.IsNullOrEmpty(myRunData.lastSaveTimeIso))
        {
            return "上次运行于\n未记录时间";
        }

        bool parseSuccess = DateTime.TryParse(
            myRunData.lastSaveTimeIso,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime parsedTime
        );

        if (parseSuccess == false)
        {
            return "上次运行于\n" + myRunData.lastSaveTimeIso;
        }

        DateTime localTime = parsedTime.ToLocalTime();

        return "上次运行于\n" + localTime.ToString("yyyy.M.d");
    }

    private void SetButtonVisible(Button myButton, bool myIsVisible)
    {
        if (myButton == null)
        {
            return;
        }

        myButton.gameObject.SetActive(myIsVisible);
        myButton.interactable = myIsVisible;
    }

    public void HandleLoadButtonClicked()
    {
        if (owner == null)
        {
            return;
        }

        owner.LoadGame(slotIndex);
    }

    public void HandleDeleteButtonClicked()
    {
        if (owner == null)
        {
            return;
        }

        owner.DeleteGame(slotIndex);
    }

    public void HandleNewGameButtonClicked()
    {
        if (owner == null)
        {
            return;
        }

        owner.NewGame(slotIndex);
    }
}