using UnityEngine;

public class SurfaceWalker2D : MonoBehaviour
{
    private Vector2Int currentCell;

    // ✔ edge index（0~3）
    private int edgeIndex = 0;

    public float moveSpeed = 5f;
    public float fallSpeed = 6f;

    private Vector2 worldPos;

    private bool grounded;

    void Start()
    {
        worldPos = transform.position;
    }

    void FixedUpdate()
    {
        worldPos = transform.position;

        currentCell = GroundCheckManager.Instance.WorldToCell(worldPos);
        grounded = GroundCheckManager.Instance.IsSolid(currentCell);

        if (!grounded)
        {
            worldPos += Vector2.down * fallSpeed * Time.fixedDeltaTime;
            Move();
            return;
        }

        StepEdge();
        Move();
    }

    // =================================================
    // ✔ 真正 edge traversal
    // =================================================
    void StepEdge()
    {
        int nextEdge = (edgeIndex + 1) % 4;

        Vector2 nextPoint =
            GroundCheckManager.Instance.EdgePoint(currentCell, nextEdge);

        worldPos = Vector2.MoveTowards(
            worldPos,
            nextPoint,
            moveSpeed * Time.fixedDeltaTime
        );

        // ✔ 到达则切换 edge
        if (Vector2.Distance(worldPos, nextPoint) < 0.01f)
        {
            edgeIndex = nextEdge;
        }
    }

    void Move()
    {
        transform.position = worldPos;
    }
}