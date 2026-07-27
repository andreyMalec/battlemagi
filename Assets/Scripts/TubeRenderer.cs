using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TubeRenderer : MonoBehaviour {
    [SerializeField] private float radius = 0.05f;
    [SerializeField] private int radialSegments = 8;

    private Mesh mesh;

    private void Awake() {
        mesh = new Mesh {
            name = "Tube Mesh"
        };
        mesh.MarkDynamic();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void Clear() {
        mesh.Clear();
    }

    public void Build(IReadOnlyList<Vector3> points) {
        mesh.Clear();

        if (points == null || points.Count < 2)
            return;

        var l = points.Last();

        int rings = points.Count;
        int vertsPerRing = radialSegments;

        var vertices = new Vector3[rings * vertsPerRing];
        var normals = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length];

        float totalLength = 0;

        for (int i = 1; i < points.Count; i++)
            totalLength += Vector3.Distance(points[i - 1], points[i]);

        float currentLength = 0;

        for (int i = 0; i < rings; i++) {
            Vector3 forward;

            if (i == 0)
                forward = (points[1] - points[0]).normalized;
            else if (i == rings - 1)
                forward = (points[i] - points[i - 1]).normalized;
            else
                forward = (points[i + 1] - points[i - 1]).normalized;

            Quaternion rotation = Quaternion.LookRotation(forward);

            Vector3 right = rotation * Vector3.right;
            Vector3 up    = rotation * Vector3.up;

            if (i > 0)
                currentLength += Vector3.Distance(points[i - 1], points[i]);

            float v = totalLength > 0 ? currentLength / totalLength : 0;

            for (int s = 0; s < radialSegments; s++) {
                float angle = Mathf.PI * 2f * s / radialSegments;

                Vector3 normal =
                    right * Mathf.Cos(angle) +
                    up * Mathf.Sin(angle);

                int index = i * radialSegments + s;
                float r = i / (float)(rings - 1);
                float currentRadius = radius * Mathf.Sin(r * Mathf.PI);
                vertices[index] = transform.parent.InverseTransformPoint(points[i] + normal * currentRadius) - transform.parent.InverseTransformPoint(l);
                // vertices[index] = points[i] + normal * radius - l;
                normals[index] = transform.parent.InverseTransformDirection(normal);
                // normals[index] = normal;
                uv[index] = new Vector2((float)s / radialSegments, v);
            }
        }

        var triangles = new int[(rings - 1) * radialSegments * 6];
        int t = 0;

        for (int ring = 0; ring < rings - 1; ring++) {
            int current = ring * radialSegments;
            int next = (ring + 1) * radialSegments;

            for (int s = 0; s < radialSegments; s++) {
                int sNext = (s + 1) % radialSegments;

                triangles[t++] = current + s;
                triangles[t++] = next + s;
                triangles[t++] = current + sNext;

                triangles[t++] = current + sNext;
                triangles[t++] = next + s;
                triangles[t++] = next + sNext;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}