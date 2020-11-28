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
    public Transform TargetTransform;

    private Vector3 target = Vector3.zero;
    private Vector3 translation;
    private bool isUsed = false;
    private new Camera camera;

    private void Awake()
    {
        this.camera = GetComponent<Camera>();
        Use(false);
    }

    private void Start()
    {
        UpdateTarget();

        this.translation = this.transform.position - this.target;
        if (this.translation == Vector3.zero)
        {
            this.translation -= this.transform.forward * 5f;
        }
    }

    private void LateUpdate()
    {
        UpdateTarget();

        if (!this.isUsed) return;

        if (Input.GetMouseButton(1))
        {
            float horizontal = Input.GetAxis(this.MouseXAxis) * this.RotationSensitivity;
            float vertical = -Input.GetAxis(this.MouseYAxis) * this.RotationSensitivity;

            this.transform.RotateAround(this.target, Vector3.up, horizontal);
            this.transform.RotateAround(this.target, this.transform.right, vertical);

            this.translation = this.transform.position - this.target;
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            SetZoom(-Input.mouseScrollDelta.y * this.ZoomSensitivity);
        }

        this.transform.position = this.target + this.translation;
    }

    private void UpdateTarget()
    {
        if (this.TargetTransform != null)
        {
            this.target = this.TargetTransform.position;
        }
        else
        {
            this.target = Vector3.zero;
        }
    }

    private void SetZoom(float amount)
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

    public Camera Camera => this.camera;

    public float Zoom => this.translation.magnitude;
}
