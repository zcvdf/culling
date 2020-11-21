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
    public static VisibleOctreeLeaf[] VisibleOctreeLeafs;
    public static VisibleOctreeCluster[] VisibleOctreeClusters;

    public static bool IsLocked;
    public static bool DisplayStats;

    [SerializeField] ViewerCamera viewerCamera;
    [SerializeField] OrbitalCamera orbitalCamera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;
    [SerializeField] Color entityOccludedColor;
    [SerializeField] Color boudingSphereColor;
    [SerializeField] Color octreeColor;
    [SerializeField] Color frustrumAABBColor;
    [SerializeField] MeshFilter frustrumPlanesMesh;
    [SerializeField] Canvas statsPanel;
    [SerializeField] bool lockOnStart = false;

    bool displayBoundingSpheres = false;
    int displayOctreeDepth = -1; // -1 means do not display anything
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
            ++this.displayOctreeDepth;
            if (this.displayOctreeDepth > Octree.Depth) this.displayOctreeDepth = -1;
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

    void DrawVisibleOctreeNodes()
    {
        var parentDepth = this.displayOctreeDepth;
        if (parentDepth < 0) return;

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = this.octreeColor;

        var parentNodesFromLeafs = new List<int3>();

        foreach (var visibleLeaf in VisibleOctreeLeafs)
        {
            var leafID = Octree.UnpackID(visibleLeaf.Value);
            var parentID = Octree.GetLeafParentNodeID(leafID.xyz, parentDepth);

            parentNodesFromLeafs.Add(parentID);
        }

        var parentNodes = parentNodesFromLeafs.Distinct();

        var parentNodeSize = Octree.NodeSize(parentDepth);

        foreach (var node in parentNodes)
        {
            var center = Octree.NodeIDToPoint(node, parentDepth);
            var size = new float3(parentNodeSize);

            Gizmos.DrawWireCube(center, size);
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