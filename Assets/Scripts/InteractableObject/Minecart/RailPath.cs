using UnityEngine;

public class RailPath : MonoBehaviour
{
    public Transform[] points;

    private void Awake()
    {
        points = GetComponentsInChildren<Transform>();

        System.Array.Sort(points,
            (a, b) => a.name.CompareTo(b.name));
    }
}