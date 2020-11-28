using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

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
    public static float4 EntityAtRootLayerColor;

    public static World World;
    public static EntityManager EntityManager;
    public static EntityQuery EntityQuery;
    public static ulong[][] VisibleOctreeNodes;

    public static bool IsLocked;
    public static bool ShowRootLayerEntities;

    [SerializeField] ViewerCamera viewerCamera;
    [SerializeField] OrbitalCamera orbitalCamera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;
    [SerializeField] Color entityOccludedColor;
    [SerializeField] Color rootOctreeLayerColor;
    [SerializeField] Material[] octreeLayerMaterials;
    [SerializeField] Material boundingAABBMaterial;
    [SerializeField] Color frustrumAABBColor;
    [SerializeField] Mesh cubeMesh;
    [SerializeField] MeshFilter frustrumPlanesMesh;
    [SerializeField] Canvas statsPanel;
    [SerializeField] float maxAABBDrawDistance = 500f;
    [SerializeField] bool lockOnStart = false;

    bool displayBoundingAABBs = false;
    int displayOctreeDepth = -1; // -1 means do not display anything
    bool displayFrustrumAABB = false;
    float edgesThickness = 0.1f;

    private void Awake()
    {
        EntityOutFrumstrumColor = this.entityOutFrumstrumColor.ToFloat4();
        EntityInFrustrumColor = this.entityInFrustrumColor.ToFloat4();
        EntityOccludedColor = this.entityOccludedColor.ToFloat4();
        EntityAtRootLayerColor = this.rootOctreeLayerColor.ToFloat4();
        SetShowRootLayerEntities(false);
    }

    private void Start()
    {
        this.frustrumPlanesMesh.GetComponent<MeshRenderer>().enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        this.viewerCamera.Use(true);
        this.statsPanel.enabled = false;
        SetLock(this.lockOnStart);
        HideStatsPanel();
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

    void LateUpdate()
    {
        this.edgesThickness = Mathf.Max(this.orbitalCamera.Zoom * 0.002f, 0.05f);

        this.viewerCamera.FrustrumEdgesThickness = this.edgesThickness;

        if (World != null && !World.Equals(null))
        {
            if (this.displayOctreeDepth != -1)
            {
                DrawVisibleOctreeNodes(this.displayOctreeDepth);
            }
        }
    }

    private void OnRenderObject()
    {
        if (World != null && !World.Equals(null))
        {
            if (this.displayBoundingAABBs)
            {
                DrawEntityAABBs();
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
            //ToggleLock();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NextStatsDetailsLevel();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ++this.displayOctreeDepth;
            if (this.displayOctreeDepth > Octree.LeafLayer) this.displayOctreeDepth = -1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ToggleShowRootLayerEntities();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            this.displayBoundingAABBs = !this.displayBoundingAABBs;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            this.displayFrustrumAABB = !this.displayFrustrumAABB;
        }
    }

    void DrawEntityAABBs()
    {
        var aabbs = EntityQuery.ToComponentDataArray<WorldRenderBounds>(Allocator.Temp);
        var count = aabbs.Length;

        var viewer = this.viewerCamera.IsUsed ? 
            this.viewerCamera.transform.position : this.orbitalCamera.transform.position;

        var maxDistSq = this.maxAABBDrawDistance * this.maxAABBDrawDistance;

        var matrices = new List<Matrix4x4>(1024);

        for (int i = 0; i < count; ++i)
        {
            var aabb = aabbs[i].Value;

            if (math.distancesq(viewer, aabb.Center) > maxDistSq) continue;

            var center = aabb.Center;
            var size = aabb.Extents * 2.05f; // Draw it a liitle bit bigger to avoid artefacts

            var matrix = Matrix4x4.TRS(center, Quaternion.identity, size);
            matrices.Add(matrix);

            Draw.AABBEdges(this.cubeMesh, this.boundingAABBMaterial.color.Opaque(), aabb.Extents, aabb.Center, 1f);

            if (matrices.Count == 1023)
            {
                Graphics.DrawMeshInstanced(this.cubeMesh, 0, this.boundingAABBMaterial, matrices);
                matrices.Clear();
            }
        }

        Graphics.DrawMeshInstanced(this.cubeMesh, 0, this.boundingAABBMaterial, matrices);
    }

    void DrawVisibleOctreeNodes(int layer)
    {
        var matID = math.min(layer, this.octreeLayerMaterials.Length - 1);
        var material = this.octreeLayerMaterials[matID];

        var nodes = VisibleOctreeNodes[layer];
        var size = Octree.NodeSize(layer);

        var batchCount = nodes.Length / 1023;

        // Add one additional batch for the rest
        if (nodes.Length % 1023 != 0)
        {
            ++batchCount;
        }

        var matrices = new List<Matrix4x4>(1023);

        for (int i = 0; i < batchCount; ++i)
        {
            matrices.Clear();

            for (int j = 0; j < 1023; ++j)
            {
                var k = i * 1023 + j;

                if (k >= nodes.Length) break; // End of the rest batch processing

                var node = Octree.UnpackID(nodes[k]);

                var center = Octree.NodeIDToPoint(node);

                var matrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * size);
                matrices.Add(matrix);

                Draw.AABBEdges(this.cubeMesh, material.color.Opaque(), size * 0.5f, center, this.edgesThickness);
            }

            Graphics.DrawMeshInstanced(this.cubeMesh, 0, material, matrices);
        }
    }

    void DrawFrustrumAABB()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = this.frustrumAABBColor;
        Gizmos.DrawCube(FrustrumAABB.Center, FrustrumAABB.Size);
    }

    void HideStatsPanel()
    {
        Stats.Details = StatsDetails.None;
        this.statsPanel.enabled = false;
    }

    void NextStatsDetailsLevel()
    {
        Stats.NextDetailsLevel();
        this.statsPanel.enabled = Stats.Details != StatsDetails.None;
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

    void SetShowRootLayerEntities(bool show)
    {
        ShowRootLayerEntities = show;
    }

    void ToggleShowRootLayerEntities()
    {
        SetShowRootLayerEntities(!ShowRootLayerEntities);
    }
}