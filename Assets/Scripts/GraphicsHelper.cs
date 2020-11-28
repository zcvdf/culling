using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Draw
{
    static Material FrustrumEdgeMaterialInstance;
    static Material AABBEdgeMaterialInstance;

    public static void AABBEdges(Mesh cubeMesh, Color color, float3 extent, Vector3 center, float thickness = 1f)
    {
        var size = extent * 2f;
        var t = thickness;

        var x = new Vector3(extent.x, 0, 0);
        var y = new Vector3(0, extent.y, 0);
        var z = new Vector3(0, 0, extent.z);

        var centers = new Vector3[12]
        {
            center + x + z,
            center + x - z,

            center - x + z,
            center - x - z,

            center + y + z,
            center + y - z,

            center - y + z,
            center - y - z,

            center + x + y,
            center + x - y,

            center - x + y,
            center - x - y,
        };

        var scales = new Vector3[12]
        {
            new Vector3(t, size.y, t),
            new Vector3(t, size.y, t),

            new Vector3(t, size.y, t),
            new Vector3(t, size.y, t),

            new Vector3(size.x, t, t),
            new Vector3(size.x, t, t),

            new Vector3(size.x, t, t),
            new Vector3(size.x, t, t),

            new Vector3(t, t, size.z),
            new Vector3(t, t, size.z),

            new Vector3(t, t, size.z),
            new Vector3(t, t, size.z),
        };

        var edgeMatrices = new List<Matrix4x4>(12);
        for (int j = 0; j < 12; ++j)
        {
            edgeMatrices.Add(Matrix4x4.TRS(centers[j], Quaternion.identity, scales[j]));
        }

        AABBEdgeMaterial.color = color;
        Graphics.DrawMeshInstanced(cubeMesh, 0, AABBEdgeMaterial, edgeMatrices);
    }

    public static void FrustrumEdges(Mesh cubeMesh, Color color, Camera camera, float thickness = 1f)
    {
        var points = camera.ComputeFrustrumWorldPoints();
        var thicknessVector = new Vector3(thickness, thickness, 0);

        var centers = new Vector3[12]
        {
            Vector3.Lerp(points[0], points[1], 0.5f),
            Vector3.Lerp(points[1], points[2], 0.5f),
            Vector3.Lerp(points[2], points[3], 0.5f),
            Vector3.Lerp(points[3], points[0], 0.5f),

            Vector3.Lerp(points[0], points[4], 0.5f),
            Vector3.Lerp(points[1], points[5], 0.5f),
            Vector3.Lerp(points[2], points[6], 0.5f),
            Vector3.Lerp(points[3], points[7], 0.5f),

            Vector3.Lerp(points[4], points[5], 0.5f),
            Vector3.Lerp(points[5], points[6], 0.5f),
            Vector3.Lerp(points[6], points[7], 0.5f),
            Vector3.Lerp(points[7], points[4], 0.5f),
        };

        var rotations = new Quaternion[12]
        {
            Quaternion.LookRotation(points[1] - points[0]),
            Quaternion.LookRotation(points[2] - points[1]),
            Quaternion.LookRotation(points[3] - points[2]),
            Quaternion.LookRotation(points[0] - points[3]),

            Quaternion.LookRotation(points[4] - points[0]),
            Quaternion.LookRotation(points[5] - points[1]),
            Quaternion.LookRotation(points[6] - points[2]),
            Quaternion.LookRotation(points[7] - points[3]),

            Quaternion.LookRotation(points[5] - points[4]),
            Quaternion.LookRotation(points[6] - points[5]),
            Quaternion.LookRotation(points[7] - points[6]),
            Quaternion.LookRotation(points[4] - points[7]),
        };

        var scales = new Vector3[12]
        {
            thicknessVector + Vector3.forward * Vector3.Distance(points[1], points[0]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[2], points[1]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[3], points[2]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[0], points[3]),
            
            thicknessVector + Vector3.forward * Vector3.Distance(points[4], points[0]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[5], points[1]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[6], points[2]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[7], points[3]),
            
            thicknessVector + Vector3.forward * Vector3.Distance(points[5], points[4]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[6], points[5]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[7], points[6]),
            thicknessVector + Vector3.forward * Vector3.Distance(points[4], points[7]),
        };

        var edgeMatrices = new List<Matrix4x4>(12);
        for (int i = 0; i < 12; ++i)
        {
            edgeMatrices.Add(Matrix4x4.TRS(centers[i], rotations[i], scales[i]));
        }

        FrustrumEdgeMaterial.color = color;
        Graphics.DrawMeshInstanced(cubeMesh, 0, FrustrumEdgeMaterial, edgeMatrices);
    }

    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    public static Color Opaque(this Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }

    static Material FrustrumEdgeMaterial
    {
        get
        {
            if (FrustrumEdgeMaterialInstance == null)
            {
                FrustrumEdgeMaterialInstance = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                FrustrumEdgeMaterialInstance.enableInstancing = true;
            }

            return FrustrumEdgeMaterialInstance;
        }
    }

    static Material AABBEdgeMaterial
    {
        get
        {
            if (AABBEdgeMaterialInstance == null)
            {
                AABBEdgeMaterialInstance = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AABBEdgeMaterialInstance.enableInstancing = true;
            }

            return AABBEdgeMaterialInstance;
        }
    }
}
