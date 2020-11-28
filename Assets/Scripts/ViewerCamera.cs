using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewerCamera : MonoBehaviour
{
    [SerializeField] Mesh cubeMesh;
    [SerializeField] float rotationSensitivity = 500f;
    [SerializeField] float moveSpeed = 20f;
    [HideInInspector] public bool IsLocked = false;
    [HideInInspector] public float FrustrumEdgesThickness = 0.1f;

    private bool isUsed = false;
    private new Camera camera;
    private MeshRenderer frustrumRenderer;

    private void Awake()
    {
        this.frustrumRenderer = GetComponentInChildren<MeshRenderer>();
        this.camera = GetComponent<Camera>();
        Use(false);
    }

    void Update()
    {
        if (!this.isUsed) return;
        if (this.IsLocked) return;

        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        var mouseX = Input.GetAxisRaw("Mouse X");
        var mouseY = Input.GetAxisRaw("Mouse Y");

        this.transform.Rotate(Vector3.up, Time.deltaTime * this.rotationSensitivity * mouseX);
        this.transform.Rotate(Vector3.left, Time.deltaTime * this.rotationSensitivity * mouseY);

        this.transform.position += this.transform.forward * Time.deltaTime * this.moveSpeed * vertical;
        this.transform.position += this.transform.right * Time.deltaTime * this.moveSpeed * horizontal;
    }

    void OnRenderObject()
    {
        if (!this.isUsed)
        {
            Draw.FrustrumEdges(this.cubeMesh, this.frustrumRenderer.material.color.Opaque(), this.Camera, this.FrustrumEdgesThickness);
        }
    }

    public void Use(bool use)
    {
        this.isUsed = use;
        this.camera.enabled = use;
    }

    public void ToggleUse()
    {
        Use(!this.isUsed);
    }

    public void ToggleLock()
    {
        this.IsLocked = !this.IsLocked;
    }

    public Camera Camera => this.camera;
}
