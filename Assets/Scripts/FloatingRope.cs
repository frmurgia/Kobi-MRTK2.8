using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FloatingCord : MonoBehaviour
{
    [Header("Anchors")]
    public Transform startObject;
    public Transform endObject;

    [Header("Optional grab handles (middle points)")]
    public List<Transform> handles = new List<Transform>();

    [Header("Shape")]
    [Range(4, 128)] public int segments = 32;
    [Tooltip("Sag verticale se non ci sono handle.")]
    public float sag = 0.1f; // metri

    [Tooltip("Larghezza variabile della corda (in metri)")]
    public AnimationCurve width = new AnimationCurve(
        new Keyframe(0f, 0.008f), // inizio sottile
        new Keyframe(0.5f, 0.012f), // centro più spesso
        new Keyframe(1f, 0.008f)  // fine sottile
    );

    [Header("Fluttuazione")]
    public float noiseAmplitude = 0.015f; // metri
    public float noiseFrequency = 0.5f;   // Hz
    public float followLerp = 10f;        // reattività (più alto = più rigido)

    LineRenderer lr;
    float seed;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = segments + 1;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.widthCurve = width;
        seed = Random.value * 1000f;
    }

    void Update()
    {
        if (!startObject || !endObject) return;

        var cps = GetOrderedControlPoints(); // start + (handles) + end

        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            Vector3 p = SampleSpline(cps, u);

            // Sag se non ci sono handle
            if (cps.Count <= 2 && sag > 0f)
            {
                float s = Mathf.Sin(u * Mathf.PI); // 0..1..0
                p += Vector3.down * sag * s;
            }

            // Rumore per fluttuazione
            Vector3 tangent = (SampleSpline(cps, Mathf.Min(1f, u + 0.01f)) -
                               SampleSpline(cps, Mathf.Max(0f, u - 0.01f))).normalized;

            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            Vector3 up = Vector3.Cross(tangent, side).normalized;

            float n1 = Mathf.PerlinNoise(seed + u * 2f, Time.time * noiseFrequency);
            float n2 = Mathf.PerlinNoise(seed + 50f + u * 2f, Time.time * noiseFrequency);
            p += (side * (n1 - 0.5f) + up * (n2 - 0.5f)) * (noiseAmplitude * 2f);

            // Interpolazione morbida
            Vector3 cur = lr.GetPosition(i);
            if (cur == Vector3.zero) cur = p;
            p = Vector3.Lerp(cur, p, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

            lr.SetPosition(i, p);
        }
    }

    // --- Helpers ---

    List<Vector3> GetOrderedControlPoints()
    {
        var list = new List<Vector3>(2 + handles.Count);
        Vector3 a = startObject.position;
        Vector3 b = endObject.position;
        Vector3 dir = (b - a).normalized;

        list.Add(a);

        if (handles != null && handles.Count > 0)
        {
            var temp = new List<(float t, Vector3 p)>();
            foreach (var h in handles)
            {
                if (!h) continue;
                float t = Vector3.Dot(h.position - a, dir);
                temp.Add((t, h.position));
            }
            temp.Sort((x, y) => x.t.CompareTo(y.t));
            foreach (var e in temp) list.Add(e.p);
        }

        list.Add(b);
        return list;
    }

    Vector3 SampleSpline(List<Vector3> cps, float u)
    {
        if (cps.Count == 2) return Vector3.Lerp(cps[0], cps[1], u);

        float f = u * (cps.Count - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, cps.Count - 2);
        float t = Mathf.Clamp01(f - i);

        Vector3 p0 = cps[Mathf.Max(i - 1, 0)];
        Vector3 p1 = cps[i];
        Vector3 p2 = cps[i + 1];
        Vector3 p3 = cps[Mathf.Min(i + 2, cps.Count - 1)];

        return CatmullRom(p0, p1, p2, p3, t);
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
