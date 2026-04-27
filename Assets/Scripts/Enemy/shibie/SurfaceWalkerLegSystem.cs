using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceWalkerLegSystem : MonoBehaviour
{
    [System.Serializable]
    public class Leg
    {
        public Transform target;      // 腿的IK目标
        public Vector3 restOffset;    // 初始位置偏移

        public Vector3 worldPos;      // 当前目标点
        public bool isMoving;

        public float moveThreshold = 0.4f; // 触发移动距离
    }

    public SurfaceWalker2D sw;
    public Transform body;
    private Vector3 lastBodyPos;
    private Vector3 bodyVelocity;

    public Leg[] legs = new Leg[6];

    public float legMoveSpeed = 8f;

    void Start()
    {
        // 初始化6条腿的初始位置（你给的）
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-1.0f, -0.35f, 0),
            new Vector3(-0.87f, -0.31f, 0),
            new Vector3(-0.72f, -0.34f, 0),
            new Vector3(-0.49f, -0.28f, 0),
            new Vector3(-0.34f, -0.29f, 0),
            new Vector3(-0.19f, -0.28f, 0),
        };

        for (int i = 0; i < legs.Length; i++)
        {
            legs[i].restOffset = offsets[i];
            legs[i].worldPos = body.position + offsets[i];
        }
    }

    void Update()
    {
        UpdateLegs();
    }
    void LateUpdate()
    {
        bodyVelocity = (body.position - lastBodyPos) / Time.deltaTime;
        lastBodyPos = body.position;
    }

    void UpdateLegs()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            UpdateLeg(i);
        }
    }

    Vector3 PredictBodyPosition(float timeAhead)
    {
        return body.position + bodyVelocity * timeAhead;
    }

    void UpdateLeg(int i)
    {
        var leg = legs[i];
        var mgr = TileMapGuideManager.Instance;

        Vector2 edgeDir = (mgr.GetEdge(sw.EdgeIndex).b - mgr.GetEdge(sw.EdgeIndex).a).normalized;

        Vector3 forwardOffset = (Vector3)edgeDir * 0.5f;

        Vector3 desiredPos = body.position + forwardOffset + leg.restOffset;

        float dist = Vector3.Distance(leg.worldPos, desiredPos);

        if (!leg.isMoving && dist > leg.moveThreshold)
        {
            leg.isMoving = true;

            Vector3 rawTarget = FindGroundPoint(desiredPos);

            leg.worldPos = ClampToRestRange(desiredPos, rawTarget, 0.3f);
        }

        leg.target.position = Vector3.MoveTowards(
            leg.target.position,
            leg.worldPos,
            legMoveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(leg.target.position, leg.worldPos) < 0.01f)
        {
            leg.isMoving = false;
        }

        legs[i] = leg;
    }

    Vector3 ClampToRestRange(Vector3 restCenter, Vector3 target, float maxRadius)
    {
        Vector3 dir = target - restCenter;

        float dist = dir.magnitude;

        if (dist <= maxRadius)
            return target;

        return restCenter + dir.normalized * maxRadius;
    }

    // =================================================
    // 落点计算（关键：贴edge）
    // =================================================
    Vector3 FindGroundPoint(Vector3 pos)
    {
        var mgr = TileMapGuideManager.Instance;

        int edgeIndex = mgr.FindClosestEdgeIndex(pos);
        Edge e = mgr.GetEdge(edgeIndex);

        // 投影到线段
        Vector3 a = e.a;
        Vector3 b = e.b;

        Vector3 ab = (b - a);
        float t = Vector3.Dot(pos - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }


}
