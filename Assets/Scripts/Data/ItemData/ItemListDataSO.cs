using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/Item Data/Item List", fileName = "Lists Of Items - ")]
public class ItemListDataSO : ScriptableObject
{
    public ItemDataSO[] itemList;

    public ItemDataSO GetItemData(string saveID)
    {
        return itemList.FirstOrDefault(item => item != null && item.saveID == saveID); // 根据id获取对应SO
        // 在 itemList 里找第一个满足条件的元素找不到就返回 null
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill With All ItemData")]
    public void CollectItemsData()
    {
        // "t:ItemDataSO" 是一个搜索过滤字符串，意思是：
        //“找所有类型是 ItemDataSO 的资源（t = type）”
        string[] guids = AssetDatabase.FindAssets("t:ItemDataSO"); // 在项目里找到所有的 ItemDataSO 资源的 GUID
        itemList = guids
         .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
         .Where(item => item != null)
         .ToArray();

        EditorUtility.SetDirty(this);
        // EditorUtility.SetDirty(this)：
        // 告诉 Unity：“这个 ScriptableObject（也就是 ItemListDataSO）已经被修改了，需要保存”

        AssetDatabase.SaveAssets();
        // AssetDatabase.SaveAssets()：
        // 真正执行一次保存，把所有被标记 dirty 的资源写回磁盘
    }
#endif
}
