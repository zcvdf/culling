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
}

public class Main : MonoBehaviour
{
    public static float4x4 WorldToNDC;
    public static Plane[] FrustrumPlanes;
    public static float4 EntityOutFrumstrumColor;
    public static float4 EntityInFrustrumColor;

    public static World World;
    public static EntityManager EntityManager;
    public static EntityQuery EntityQuery;

    [SerializeField] float rotationSensitivity = 20f;
    [SerializeField] new Camera camera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;
    [SerializeField] Color boudingSphereColor;
    bool displayBoundingSphere = false;

    private void Awake()
    {
        EntityOutFrumstrumColor = this.entityOutFrumstrumColor.ToFloat4();
        EntityInFrustrumColor = this.entityInFrustrumColor.ToFloat4();
    }

    private void Update()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        this.camera.transform.Rotate(this.transform.up, Time.deltaTime * this.rotationSensitivity * horizontal);
        this.camera.transform.Rotate(this.transform.right, Time.deltaTime * this.rotationSensitivity * vertical);

        FrustrumPlanes = GeometryUtility.CalculateFrustumPlanes(this.camera);

        WorldToNDC = this.camera.projectionMatrix * this.camera.worldToCameraMatrix;

        if (Input.GetKeyDown(KeyCode.A))
        {
            this.displayBoundingSphere = !this.displayBoundingSphere;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = this.camera.transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawFrustum(Vector3.zero, this.camera.fieldOfView, this.camera.farClipPlane, this.camera.nearClipPlane, this.camera.aspect);

        if (World.IsCreated)
        {
            if (this.displayBoundingSphere)
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
}
