using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewerCamera : MonoBehaviour
{
    [SerializeField] float rotationSensitivity = 500f;
    [SerializeField] float moveSpeed = 20f;
    private bool isUsed = false;
    private new Camera camera;

    private void Awake()
    {
        this.camera = GetComponent<Camera>();
    }

    void Update()
    {
        if (!this.isUsed) return;

        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        var mouseX = Input.GetAxisRaw("Mouse X");
        var mouseY = Input.GetAxisRaw("Mouse Y");

        this.transform.Rotate(Vector3.up, Time.deltaTime * this.rotationSensitivity * mouseX);
        this.transform.Rotate(Vector3.left, Time.deltaTime * this.rotationSensitivity * mouseY);

        this.transform.position += this.transform.forward * Time.deltaTime * this.moveSpeed * vertical;
        this.transform.position += this.transform.right * Time.deltaTime * this.moveSpeed * horizontal;
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

    public Camera Camera => this.camera;
}
