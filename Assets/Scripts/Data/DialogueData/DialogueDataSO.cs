using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/DialogueData", fileName = "DialogueData - ")]
public class DialogueDataSO : ScriptableObject
{
    [Header("默认人物立绘")]
    [SerializeField] private Sprite defaultCharacterSprite;

    [Header("对话段落")]
    [SerializeField] private List<DialogueSegment> segments = new List<DialogueSegment>();

    public Sprite DefaultCharacterSprite => defaultCharacterSprite;
    public IReadOnlyList<DialogueSegment> Segments => segments;
}

[System.Serializable]
public class DialogueSegment
{
    [Header("本段专用立绘，不填则使用默认立绘")]
    [SerializeField] private Sprite characterSprite;

    [Header("本段对话文本")]
    [TextArea(2, 6)]
    [SerializeField] private string content;

    public Sprite CharacterSprite => characterSprite;
    public string Content => content;
}