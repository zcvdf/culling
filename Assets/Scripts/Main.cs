using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public static class Const
{
    public const float SQRT3 = 1.73205080f;
}

public static class MainExt
{
    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    public static float NearPlaneHalfWidth(this Camera camera)
    {
        return math.tan(math.radians(camera.fieldOfView * 0.5f)) * camera.nearClipPlane * camera.aspect;
    }

    public static float NearPlaneHalfHeight(this Camera camera)
    {
        return math.tan(math.radians(camera.fieldOfView * 0.5f)) * camera.nearClipPlane;
    }

    public static Plane[] ComputeFrustrumPlanes(this Camera camera)
    {
        return GeometryUtility.CalculateFrustumPlanes(camera);
    }

    public static void DrawFrustrum(this Camera camera, Color color)
    {
        if (camera == null) return;

        Gizmos.matrix = camera.transform.localToWorldMatrix;
        Gizmos.color = color;
        Gizmos.DrawFrustum(Vector3.zero, camera.fieldOfView, camera.farClipPlane, camera.nearClipPlane, camera.aspect);
    }
}

public class Main : MonoBehaviour
{
    public static float3 Viewer;
    public static float3 NearPlaneCenter;
    public static Quad NearPlane;
    public static float4x4 WorldToNDC;
    public static Plane[] FrustrumPlanes;
    public static AABB FrustrumAABB;
    public static float4 EntityOutFrumstrumColor;
    public static float4 EntityInFrustrumColor;
    public static float4 EntityOccludedColor;

    public static World World;
    public static EntityManager EntityManager;
    public static EntityQuery EntityQuery;

    [SerializeField] ViewerCamera viewerCamera;
    [SerializeField] OrbitalCamera orbitalCamera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;
    [SerializeField] Color entityOccludedColor;
    [SerializeField] Color boudingSphereColor;
    [SerializeField] Color octreeColorLayer0;
    [SerializeField] Color frustrumAABBColor;
    [SerializeField] MeshFilter frustrumPlanesMesh;
    bool displayBoundingSpheres = false;
    int displayOctreeLayer = -1;
    bool displayFrustrumAABB = false;

    private void Awake()
    {
        EntityOutFrumstrumColor = this.entityOutFrumstrumColor.ToFloat4();
        EntityInFrustrumColor = this.entityInFrustrumColor.ToFloat4();
        EntityOccludedColor = this.entityOccludedColor.ToFloat4();
    }

    private void Start()
    {
        this.frustrumPlanesMesh.GetComponent<MeshRenderer>().enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        this.viewerCamera.Use(true);
        this.viewerCamera.IsLocked = true;
    }

    private void Update()
    {
        Inputs();

        this.frustrumPlanesMesh.mesh = this.viewerCamera.Camera.ComputeFrustumMesh();
        FrustrumPlanes = this.viewerCamera.Camera.ComputeFrustrumPlanes();
        FrustrumAABB = this.viewerCamera.Camera.ComputeFrustrumAABB();
        WorldToNDC = this.viewerCamera.Camera.projectionMatrix * this.viewerCamera.Camera.worldToCameraMatrix;
        Viewer = this.viewerCamera.transform.position;
        NearPlaneCenter = this.viewerCamera.transform.position + this.viewerCamera.transform.forward * this.viewerCamera.Camera.nearClipPlane;

        var nearPlane = new Quad();
        nearPlane.Center = NearPlaneCenter;
        nearPlane.LocalRight = this.viewerCamera.transform.right * this.viewerCamera.Camera.NearPlaneHalfWidth();
        nearPlane.LocalUp = this.viewerCamera.transform.up * this.viewerCamera.Camera.NearPlaneHalfHeight();
        nearPlane.Normal = FrustrumPlanes[4].normal;
        NearPlane = nearPlane;
    }

    private void OnDrawGizmos()
    {
        this.viewerCamera.Camera.DrawFrustrum(Color.yellow);

        if (World != null && !World.Equals(null))
        {
            if (this.displayBoundingSpheres)
            {
                DrawEntityBoundingSpheres();
            }

            if (this.displayOctreeLayer != -1)
            {
                DrawOctree();
            }

            if (this.displayFrustrumAABB)
            {
                DrawFrustrumAABB();
            }
        }
    }

    void Inputs()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.viewerCamera.ToggleUse();
            this.orbitalCamera.ToggleUse();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            this.viewerCamera.ToggleLock();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ++this.displayOctreeLayer;
            if (this.displayOctreeLayer >= 2) this.displayOctreeLayer = -1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            this.displayFrustrumAABB = !this.displayFrustrumAABB;
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            this.displayBoundingSpheres = !this.displayBoundingSpheres;
        }
    }

    void DrawEntityBoundingSpheres()
    {
        var translations = EntityQuery.ToComponentDataArray<Translation>(Allocator.Temp);
        var radiuses = EntityQuery.ToComponentDataArray<WorldBoundingRadius>(Allocator.Temp);

        for (int i = 0; i < translations.Length; ++i)
        {
            var center = translations[i].Value;
            var radius = radiuses[i].Value;

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = this.boudingSphereColor;
            Gizmos.DrawSphere(center, radius);
        }
    }

    void DrawOctree()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = this.octreeColorLayer0;

        if (this.displayOctreeLayer == 0)
        {
            Octree.ForEachBoundingNode0(FrustrumAABB, (int3 id) =>
            {
                var center = Octree.IDLayer0ToPoint(id);
                var size = new float3(Octree.Node0Size);

                Gizmos.DrawWireCube(center, size);
            });
        }
        else if (this.displayOctreeLayer == 1)
        {
            Octree.ForEachBoundingNode1(FrustrumAABB, (int3 id) =>
            {
                var center = Octree.IDLayer1ToPoint(id);
                var size = new float3(Octree.Node1Size);

                Gizmos.DrawWireCube(center, size);
            });
        }
    }

    void DrawFrustrumAABB()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = this.frustrumAABBColor;
        Gizmos.DrawCube(FrustrumAABB.Center, FrustrumAABB.Size);
    }
}