using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CameraExtention
{
    private static int[] m_VertOrder = new int[24]
    {
         0,1,2,3, // Near
         6,5,4,7, // Far
         0,4,5,1, // Left
         3,2,6,7, // Right
         1,5,6,2, // Top
         0,3,7,4  // Bottom
    };
    private static int[] m_Indices = new int[36]
    {
          0,  1,  2,  3,  0,  2, // Near
          4,  5,  6,  7,  4,  6, // Far
          8,  9, 10, 11,  8, 10, // Left
         12, 13, 14, 15, 12, 14, // Right
         16, 17, 18, 19, 16, 18, // Top
         20, 21, 22, 23, 20, 22, // Bottom
    };

    public static Vector3[] ComputeFrustrumWorldPoints(this Camera camera)
    {
        // Build frustrum points in NDC coordinates
        var v = new Vector3[8];
        v[0] = v[4] = new Vector3(0, 0, 0);
        v[1] = v[5] = new Vector3(0, 1, 0);
        v[2] = v[6] = new Vector3(1, 1, 0);
        v[3] = v[7] = new Vector3(1, 0, 0);
        v[0].z = v[1].z = v[2].z = v[3].z = camera.nearClipPlane;
        v[4].z = v[5].z = v[6].z = v[7].z = camera.farClipPlane;

        // Transformation NDC -> World
        for (int i = 0; i < v.Length; i++)
        {
            v[i] = camera.ViewportToWorldPoint(v[i]);
        }

        return v;
    }

    public static Vector3[] ComputeFrustrumLocalPoints(this Camera camera)
    {
        var v = camera.ComputeFrustrumWorldPoints();

        // Transformation World -> Local
        for (int i = 0; i < v.Length; i++)
        {
            v[i] = camera.transform.InverseTransformPoint(v[i]);
        }

        return v;
    }

    public static Mesh ComputeFrustumMesh(this Camera camera)
    {
        var mesh = new Mesh();
        var v = camera.ComputeFrustrumLocalPoints();

        var vertices = new Vector3[24];
        var normals = new Vector3[24];

        // Split vertices for each face (8 vertices -> 24 vertices)
        for (int i = 0; i < 24; i++)
        {
            vertices[i] = v[m_VertOrder[i]];
        }

        // Calculate faces normal
        for (int i = 0; i < 6; i++)
        {
            var faceNormal = Vector3.Cross(vertices[i * 4 + 2] - vertices[i * 4 + 1], vertices[i * 4 + 0] - vertices[i * 4 + 1]);
            normals[i * 4 + 0] = normals[i * 4 + 1] = normals[i * 4 + 2] = normals[i * 4 + 3] = faceNormal;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = m_Indices;

        return mesh;
    }

    public static AABB ComputeFrustrumAABB(this Camera camera)
    {
        var points = camera.ComputeFrustrumWorldPoints();

        var min = new float3(float.MaxValue);
        var max = new float3(float.MinValue);

        for (int i = 0; i < points.Length; ++i)
        {
            min = math.min(points[i], min);
            max = math.max(points[i], max);
        }

        var aabb = new MinMaxAABB();
        aabb.Min = min;
        aabb.Max = max;

        return aabb;
    }
}