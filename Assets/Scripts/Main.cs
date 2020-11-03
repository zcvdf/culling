using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
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
    public static float4 EntityOutFrumstrumColor;
    public static float4 EntityInFrustrumColor;

    [SerializeField] float rotationSensitivity = 20f;
    [SerializeField] new Camera camera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;

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
        WorldToNDC = this.camera.projectionMatrix * this.camera.worldToCameraMatrix;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = this.camera.transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawFrustum(Vector3.zero, this.camera.fieldOfView, this.camera.farClipPlane, this.camera.nearClipPlane, this.camera.aspect);
    }
}
