using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;


public class SurfaceWalker2D : MonsterBase
{
    public float moveSpeed = 3f;
    public float fallSpeed = 6f;
    public Transform bodyVisual;
    
    protected override void Init()
    {
        ai = new SurfaceWalkerUtilityAI();
        motor = new SurfaceWalkerMotor();

        var mgr = TileMapGuideManager.Instance;

        EdgeIndex = mgr.FindClosestEdgeIndex(transform.position);
        CurrentEdge = mgr.GetEdge(EdgeIndex);

        Target =
            Vector2.Distance(transform.position, CurrentEdge.a) <
            Vector2.Distance(transform.position, CurrentEdge.b)
            ? CurrentEdge.b
            : CurrentEdge.a;

        HasEdge = true;
    }

    public void UpdateVisualOffset()
    {
        if (bodyVisual == null) return;

        Vector2 dir = (CurrentEdge.b - CurrentEdge.a).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        bodyVisual.localPosition = normal * 0.3f;
    }
}