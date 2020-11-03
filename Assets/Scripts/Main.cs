using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static float4x4 WorldToNDC;

    [SerializeField] new Camera camera;

    private void Awake()
    {
    }

    private void Update()
    {
        this.camera.transform.Rotate(this.transform.up, Time.deltaTime * 10f);
        WorldToNDC = this.camera.projectionMatrix * this.camera.worldToCameraMatrix;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = this.camera.transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawFrustum(Vector3.zero, this.camera.fieldOfView, this.camera.farClipPlane, this.camera.nearClipPlane, this.camera.aspect);
    }
}
