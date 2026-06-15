using UnityEngine;

/// <summary>
/// Fly 四翼扇动：在各自基准 local 旋转上叠加三角波摆动（接近匀速）。
/// </summary>
public class FlyWings : MonoBehaviour
{
    private static readonly Vector3[] DefaultBaseEulerAngles =
    {
        new Vector3(0f, 0f, 120f),
        new Vector3(0f, 0f, 180f),
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0f, -60f),
    };

    [System.Serializable]
    public class WingEntry
    {
        public Transform wing;
        [Tooltip("静止基准 localEulerAngles")]
        public Vector3 baseEulerAngles;
    }

    public WingEntry[] wings = new WingEntry[4];

    [Tooltip("完整摆动周期（秒）")]
    public float oscillationPeriod = 0.5f;

    [Tooltip("相对基准角摆动幅度（度，±）")]
    public float oscillationAmplitude = 15f;

    [Tooltip("摆动轴（翅膀局部空间）")]
    public Vector3 oscillationAxis = Vector3.forward;

    [Tooltip("翼1/4 同相、翼2/3 反相；关闭则四翼同相")]
    public bool alternatePairFlap = true;

    [Tooltip("整组取反：1-,2+,3+,4-（否则 1+,2-,3-,4+）")]
    public bool invertAlternateFlap = false;

    public bool flappingEnabled = true;

    private float elapsed;

    public void SetFlappingEnabled(bool enabled)
    {
        flappingEnabled = enabled;

        if (!enabled)
        {
            ApplyBaseRotations();
        }
    }

    public void ApplyBaseRotations()
    {
        for (int i = 0; i < wings.Length; i++)
        {
            WingEntry entry = wings[i];

            if (entry?.wing == null)
            {
                continue;
            }

            entry.wing.localRotation = Quaternion.Euler(entry.baseEulerAngles);
        }
    }

    private void OnEnable()
    {
        ApplyBaseRotations();
    }

    private void Update()
    {
        if (!flappingEnabled || wings == null || wings.Length == 0)
        {
            return;
        }

        float period = Mathf.Max(0.01f, oscillationPeriod);
        elapsed += Time.deltaTime;
        float triangle = ComputeTriangleWave(elapsed, period);
        Vector3 axis = oscillationAxis.sqrMagnitude > 0.0001f
            ? oscillationAxis.normalized
            : Vector3.forward;

        for (int i = 0; i < wings.Length; i++)
        {
            WingEntry entry = wings[i];

            if (entry?.wing == null)
            {
                continue;
            }

            float sign = GetPairFlapSign(i);
            float wobble = triangle * oscillationAmplitude * sign;
            Quaternion baseRot = Quaternion.Euler(entry.baseEulerAngles);
            Quaternion wobbleRot = Quaternion.AngleAxis(wobble, axis);
            entry.wing.localRotation = baseRot * wobbleRot;
        }
    }

    private static float ComputeTriangleWave(float time, float period)
    {
        float phase = (time % period) / period;
        float t01 = phase < 0.5f ? phase * 2f : 2f - phase * 2f;
        return t01 * 2f - 1f;
    }

    private float GetPairFlapSign(int wingIndex)
    {
        if (!alternatePairFlap)
        {
            return invertAlternateFlap ? -1f : 1f;
        }

        bool positivePair = wingIndex == 0 || wingIndex == 3;
        float sign = positivePair ? 1f : -1f;
        return invertAlternateFlap ? -sign : sign;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        wings = new WingEntry[4];

        for (int i = 0; i < wings.Length; i++)
        {
            wings[i] = new WingEntry
            {
                baseEulerAngles = DefaultBaseEulerAngles[i]
            };
        }
    }

    private void OnValidate()
    {
        if (wings == null || wings.Length == 0)
        {
            Reset();
        }
    }
#endif
}
