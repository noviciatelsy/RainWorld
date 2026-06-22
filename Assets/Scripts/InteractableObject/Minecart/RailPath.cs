using System.Collections.Generic;
using UnityEngine;

public class RailPath : MonoBehaviour
{
    public Transform[] points;

    public bool HasValidPath => points != null && points.Length >= 2;

    private void Awake()
    {
        RefreshPoints();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RefreshPoints();
        }
    }
#endif

    public void RefreshPoints()
    {
        List<Transform> collected = new List<Transform>(transform.childCount);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                collected.Add(child);
            }
        }

        collected.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        points = collected.ToArray();
    }
}
