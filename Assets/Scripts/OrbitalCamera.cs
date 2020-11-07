using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalCamera : MonoBehaviour
{
    public string MouseXAxis = "Mouse X";
    public string MouseYAxis = "Mouse Y";
    public float RotationSensitivity = 3f;
    public float ZoomSensitivity = 2f;
    public float MinZoom = 2f;
    public float MaxZoom = 100f;

    private Vector3 target;
    private Vector3 translation;
    private bool isUsed = false;
    private new Camera camera;

    private void Awake()
    {
        this.camera = GetComponent<Camera>();
    }

    private void Start()
    {
        this.Target = Vector3.zero;
    }

    private void Update()
    {
        this.transform.position = this.Target + this.translation;

        if (!this.isUsed) return;

        if (Input.mouseScrollDelta.y != 0)
        {
            Zoom(-Input.mouseScrollDelta.y * this.ZoomSensitivity);
        }
    }

    private void LateUpdate()
    {
        if (!this.isUsed) return;

        if (Input.GetMouseButton(1))
        {
            float horizontal = Input.GetAxis(this.MouseXAxis) * this.RotationSensitivity;
            float vertical = -Input.GetAxis(this.MouseYAxis) * this.RotationSensitivity;

            this.transform.RotateAround(this.Target, Vector3.up, horizontal);
            this.transform.RotateAround(this.Target, this.transform.right, vertical);

            this.translation = this.transform.position - this.Target;
        }
    }

    private void Zoom(float amount)
    {
        float magnitude = this.translation.magnitude;
        Vector3 direction = this.translation.normalized;

        float newMagnitude = Mathf.Clamp(magnitude + amount, this.MinZoom, this.MaxZoom);

        this.translation = direction * newMagnitude;
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

    public Vector3 Target
    {
        get => this.target;
        set
        {
            if (value != null)
            {
                this.target = value;
                this.translation = this.transform.position - value;
            }
        }
    }

    public Camera Camera => this.camera;
}
