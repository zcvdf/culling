using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public static class MainExt
{
    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
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
    public static float4x4 WorldToNDC;
    public static Plane[] FrustrumPlanes;
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
    [SerializeField] MeshFilter frustrumPlanesMesh;
    bool displayBoundingSpheres = false;

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
    }

    private void Update()
    {
        Inputs();

        this.frustrumPlanesMesh.mesh = this.viewerCamera.Camera.GenerateFrustumMesh();
        FrustrumPlanes = GeometryUtility.CalculateFrustumPlanes(this.viewerCamera.Camera);
        WorldToNDC = this.viewerCamera.Camera.projectionMatrix * this.viewerCamera.Camera.worldToCameraMatrix;
        Viewer = this.viewerCamera.transform.position;
    }

    private void OnDrawGizmos()
    {
        this.viewerCamera.Camera.DrawFrustrum(Color.yellow);

        if (World != null && !World.Equals(null))
        {
            if (this.displayBoundingSpheres)
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
        }
    }

    void Inputs()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.viewerCamera.ToggleUse();
            this.orbitalCamera.ToggleUse();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            this.displayBoundingSpheres = !this.displayBoundingSpheres;
        }
    }
}