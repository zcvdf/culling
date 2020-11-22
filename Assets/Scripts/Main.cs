using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static float3 Viewer;
    public static float3 NearPlaneCenter;
    public static Quad NearPlane;
    public static float4x4 WorldToNDC;
    public static WorldFrustrumPlanes FrustrumPlanes;
    public static AABB FrustrumAABB;
    public static float4 EntityOutFrumstrumColor;
    public static float4 EntityInFrustrumColor;
    public static float4 EntityOccludedColor;

    public static World World;
    public static EntityManager EntityManager;
    public static EntityQuery EntityQuery;
    public static VisibleOctreeNode[] VisibleOctreeNodes;
    public static VisibleOctreeCluster[] VisibleOctreeClusters;

    public static bool IsLocked;
    public static bool DisplayStats;

    [SerializeField] ViewerCamera viewerCamera;
    [SerializeField] OrbitalCamera orbitalCamera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;
    [SerializeField] Color entityOccludedColor;
    [SerializeField] Color boudingSphereColor;
    [SerializeField] Color octreeClustersColor = Color.white;
    [SerializeField] Color[] octreeLayerColors = new Color[1] { Color.white };
    [SerializeField] Color frustrumAABBColor;
    [SerializeField] MeshFilter frustrumPlanesMesh;
    [SerializeField] Canvas statsPanel;
    [SerializeField] bool lockOnStart = false;

    bool displayBoundingSpheres = false;
    int displayOctreeDepth = -1; // -1 means do not display anything
    bool displayFrustrumAABB = false;
    bool displayOctreeClusters = false;

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
        this.statsPanel.enabled = false;
        SetLock(this.lockOnStart);
        SetStatsPanelVisible(false);
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
        nearPlane.Normal = FrustrumPlanes.Near.normal;
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

            if (this.displayOctreeClusters)
            {
                DrawVisibleClusters();
            }

            if (this.displayOctreeDepth != -1)
            {
                DrawVisibleOctreeNodes();
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
            ToggleLock();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleStatsPanelVisible();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            this.displayOctreeClusters = !this.displayOctreeClusters;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ++this.displayOctreeDepth;
            if (this.displayOctreeDepth > Octree.LeafLayer) this.displayOctreeDepth = -1;
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

    void DrawVisibleClusters()
    {
        Gizmos.matrix = Matrix4x4.identity;

        foreach (var packedNode in VisibleOctreeClusters)
        {
            var node = Octree.UnpackID(packedNode.Value);

            var center = Octree.ClusterIDToPoint(node.xyz);
            var size = Octree.ClusterSize;

            Gizmos.color = this.octreeClustersColor;
            Gizmos.DrawCube(center, new float3(size));

            Draw.CubeWireframe(center, size * 0.5f, this.octreeClustersColor.Opaque());
        }
    }

    void DrawVisibleOctreeNodes()
    {
        if (this.displayOctreeDepth < 0) return;

        Gizmos.matrix = Matrix4x4.identity;

        foreach (var packedNode in VisibleOctreeNodes)
        {
            var node = Octree.UnpackID(packedNode.Value);

            if (this.displayOctreeDepth != 0 && node.w != this.displayOctreeDepth) continue;

            var colorID = math.min(node.w - 1, this.octreeLayerColors.Length - 1);
            var octreeColor = this.octreeLayerColors[colorID];

            var center = Octree.NodeIDToPoint(node);
            var size = Octree.NodeSize(node.w);
            
            Gizmos.color = octreeColor;
            Gizmos.DrawCube(center, new float3(size));

            Draw.CubeWireframe(center, size * 0.5f, octreeColor.Opaque());
        }
    }

    void DrawFrustrumAABB()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = this.frustrumAABBColor;
        Gizmos.DrawCube(FrustrumAABB.Center, FrustrumAABB.Size);
    }

    void SetStatsPanelVisible(bool visible)
    {
        DisplayStats = visible;
        this.statsPanel.enabled = visible;
    }

    void ToggleStatsPanelVisible()
    {
        SetStatsPanelVisible(!DisplayStats);
    }

    void SetLock(bool locked)
    {
        IsLocked = locked;
        this.viewerCamera.IsLocked = locked;
    }

    void ToggleLock()
    {
        SetLock(!IsLocked);
    }
}

public static class Draw
{
    public static void CubeWireframe(float3 center, float extent, Color color)
    {
        var x = new float3(extent, 0, 0);
        var y = new float3(0, extent, 0);
        var z = new float3(0, 0, extent);

        GL.Begin(GL.LINES);
        GL.Color(color);

        GL.Vertex(center + x + y - z);
        GL.Vertex(center - x + y - z);

        GL.Vertex(center - x + y - z);
        GL.Vertex(center - x + y + z);

        GL.Vertex(center - x + y + z);
        GL.Vertex(center + x + y + z);

        GL.Vertex(center + x + y + z);
        GL.Vertex(center + x + y - z);


        GL.Vertex(center + x - y - z);
        GL.Vertex(center - x - y - z);

        GL.Vertex(center - x - y - z);
        GL.Vertex(center - x - y + z);

        GL.Vertex(center - x - y + z);
        GL.Vertex(center + x - y + z);

        GL.Vertex(center + x - y + z);
        GL.Vertex(center + x - y - z);


        GL.Vertex(center + x + y - z);
        GL.Vertex(center + x - y - z);

        GL.Vertex(center + x + y + z);
        GL.Vertex(center + x - y + z);

        GL.Vertex(center - x + y + z);
        GL.Vertex(center - x - y + z);

        GL.Vertex(center - x + y - z);
        GL.Vertex(center - x - y - z);

        GL.End();
    }
}

public static class MiscExt
{
    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    public static Color Opaque(this Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }
}