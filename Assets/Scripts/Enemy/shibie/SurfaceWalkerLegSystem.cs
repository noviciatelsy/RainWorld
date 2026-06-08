using UnityEngine;

public class SurfaceWalkerLegSystem : MonoBehaviour
{
    [System.Serializable]
    public class Leg
    {
        public Transform target;
        public Vector3 restOffset;

        public Vector3 worldPos;
        public bool isMoving;

        public float moveThreshold = 0.4f;
    }

    public SurfaceWalker2D sw;
    public Transform body;

    public Leg[] legs = new Leg[6];
    public float legMoveSpeed = 8f;

    private Vector3 lastBodyPos;
    private Vector3 bodyVelocity;

    private void Start()
    {
        if (body == null)
        {
            body = transform;
        }

        Vector3[] offsets =
        {
            new Vector3(-1.0f, -0.35f, 0),
            new Vector3(-0.87f, -0.31f, 0),
            new Vector3(-0.72f, -0.34f, 0),
            new Vector3(-0.49f, -0.28f, 0),
            new Vector3(-0.34f, -0.29f, 0),
            new Vector3(-0.19f, -0.28f, 0),
        };

        Transform anchor = body != null ? body : transform;

        for (int i = 0; i < legs.Length; i++)
        {
            legs[i].restOffset = offsets[i];
            legs[i].worldPos = anchor.position + offsets[i];
        }
    }

    private void Update()
    {
        UpdateLegs();
    }

    private void LateUpdate()
    {
        Transform anchor = body != null ? body : transform;
        bodyVelocity = (anchor.position - lastBodyPos) / Time.deltaTime;
        lastBodyPos = anchor.position;
    }

    private void UpdateLegs()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            UpdateLeg(i);
        }
    }

    private void UpdateLeg(int i)
    {
        Leg leg = legs[i];
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || body == null)
        {
            return;
        }

        Vector2 bodyPos = body.position;
        int edgeIndex = mgr.FindClosestEdgeIndex(bodyPos);
        Edge e = mgr.GetEdge(edgeIndex);

        Vector2 a = e.a;
        Vector2 b = e.b;

        Vector2 dir = (b - a).normalized;
        float length = Vector2.Distance(a, b);

        float tOnEdge = GetTOnEdge(bodyPos, a, b);
        float basePos = tOnEdge * length;

        float spacing = 0.25f;
        float offset = (i - legs.Length * 0.5f) * spacing;

        float forward = bodyVelocity.magnitude * 0.25f;

        float finalPos = basePos + offset + forward;
        finalPos = Mathf.Clamp(finalPos, 0, length);

        Vector2 desired = a + dir * finalPos;

        float dist = Vector3.Distance(leg.worldPos, desired);

        if (!leg.isMoving && dist > leg.moveThreshold)
        {
            leg.isMoving = true;
            leg.worldPos = desired;
        }

        if (leg.target != null)
        {
            leg.target.position = Vector3.MoveTowards(
                leg.target.position,
                leg.worldPos,
                legMoveSpeed * Time.deltaTime
            );
        }

        if (leg.target != null &&
            Vector3.Distance(leg.target.position, leg.worldPos) < 0.01f)
        {
            leg.isMoving = false;
        }

        legs[i] = leg;
    }

    private static float GetTOnEdge(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        return Mathf.Clamp01(t);
    }
}
