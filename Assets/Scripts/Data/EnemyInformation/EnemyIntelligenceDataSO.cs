using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Setup/Enemy Intelligence Data/Enemy Intelligence Data", fileName = "EnemyIntelligenceData - ")]
public class EnemyIntelligenceDataSO : ScriptableObject
{
    [TextArea] public string intelligenceText;
    public string intelligenceName;
    public bool isImportant=false;
    public bool canBePurchased=true;
    public bool canBeLockedByNote=true;
    public int priceToPurchase = 0;

    [SerializeField, HideInInspector] private string saveID;
    public string SaveID => saveID;

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        string newSaveID = AssetDatabase.AssetPathToGUID(path);

        if (saveID != newSaveID)
        {
            saveID = newSaveID;
            EditorUtility.SetDirty(this);
        }

        if (isImportant)
        {
            canBeLockedByNote = false;
            canBePurchased = false;

        }
#endif
    }
}