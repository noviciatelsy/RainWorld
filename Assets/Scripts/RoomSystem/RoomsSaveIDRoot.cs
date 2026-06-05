using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class RoomsSaveIDRoot : MonoBehaviour
{
    private const string RoomSaveIDFieldName = "roomSaveID";

    [Header("房间 ID 自动维护")]
    [SerializeField] private bool autoMaintainRoomIDsInEditor = true;

#if UNITY_EDITOR
    private bool hasScheduledMaintain;

    private void OnEnable()
    {
        ScheduleMaintainRoomIDs();
    }

    private void OnValidate()
    {
        ScheduleMaintainRoomIDs();
    }

    private void OnTransformChildrenChanged()
    {
        ScheduleMaintainRoomIDs();
    }

    private void ScheduleMaintainRoomIDs()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!autoMaintainRoomIDsInEditor)
        {
            return;
        }

        if (hasScheduledMaintain)
        {
            return;
        }

        hasScheduledMaintain = true;

        // OnValidate 可能被 Unity 在比较特殊的编辑器时机调用。
        // 延迟到下一次编辑器更新里再改序列化数据会更稳。
        EditorApplication.delayCall += MaintainRoomIDsDelayed;
    }

    private void MaintainRoomIDsDelayed()
    {
        hasScheduledMaintain = false;

        if (this == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            return;
        }

        if (!autoMaintainRoomIDsInEditor)
        {
            return;
        }

        // 只允许在编辑 Rooms.prefab 本体时自动生成。
        // 不允许在场景里的 Rooms.prefab 实例上生成，避免制造场景 Override。
        if (!IsEditingRoomsPrefabAsset())
        {
            return;
        }

        GenerateMissingOrDuplicateRoomIDs();
    }

    [ContextMenu("Generate Missing Or Duplicate Room IDs")]
    private void GenerateMissingOrDuplicateRoomIDsByContextMenu()
    {
        if (Application.isPlaying)
        {
            return;
        }

        GenerateMissingOrDuplicateRoomIDs();
    }

    private void GenerateMissingOrDuplicateRoomIDs()
    {
        RoomController[] rooms = GetComponentsInChildren<RoomController>(true);

        HashSet<string> usedRoomIDs = new HashSet<string>();

        int generatedCount = 0;
        int duplicateFixedCount = 0;

        for (int i = 0; i < rooms.Length; i++)
        {
            RoomController room = rooms[i];

            if (room == null)
            {
                continue;
            }

            SerializedObject serializedRoom = new SerializedObject(room);
            SerializedProperty roomSaveIDProperty = serializedRoom.FindProperty(RoomSaveIDFieldName);

            if (roomSaveIDProperty == null)
            {
                Debug.LogWarning($"房间 {room.name} 找不到字段 {RoomSaveIDFieldName}。");
                continue;
            }

            string currentRoomID = roomSaveIDProperty.stringValue;
            bool shouldGenerateNewID = false;
            bool wasDuplicate = false;

            if (string.IsNullOrWhiteSpace(currentRoomID))
            {
                shouldGenerateNewID = true;
            }
            else if (usedRoomIDs.Contains(currentRoomID))
            {
                shouldGenerateNewID = true;
                wasDuplicate = true;
            }

            if (shouldGenerateNewID)
            {
                Undo.RecordObject(room, "Generate Room Save ID");

                string newRoomID = GenerateNewRoomSaveID();
                roomSaveIDProperty.stringValue = newRoomID;

                serializedRoom.ApplyModifiedProperties();

                // 这里很关键：
                // room 可能是 Rooms.prefab 里的嵌套房间预制体实例。
                // 记录 Prefab Instance Property Modification 后，
                // 这个 roomSaveID 会作为 Rooms.prefab 里的实例 Override 保存下来。
                PrefabUtility.RecordPrefabInstancePropertyModifications(room);
                EditorUtility.SetDirty(room);

                if (wasDuplicate)
                {
                    duplicateFixedCount++;
                }
                else
                {
                    generatedCount++;
                }

                currentRoomID = newRoomID;
            }

            usedRoomIDs.Add(currentRoomID);
        }

        if (generatedCount > 0 || duplicateFixedCount > 0)
        {
            EditorUtility.SetDirty(this);

            Debug.Log(
                $"Rooms 房间 ID 维护完成：{name}\n" +
                $"新生成 ID：{generatedCount}\n" +
                $"修复重复 ID：{duplicateFixedCount}"
            );
        }
    }

    private bool IsEditingRoomsPrefabAsset()
    {
        // 情况 1：直接处理 Prefab Asset。
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return true;
        }

        // 情况 2：正在 Prefab Mode 里编辑 Rooms.prefab。
        PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        if (currentPrefabStage != null && currentPrefabStage.prefabContentsRoot == gameObject)
        {
            return true;
        }

        return false;
    }

    private string GenerateNewRoomSaveID()
    {
        return Guid.NewGuid().ToString("N");
    }
#endif
}