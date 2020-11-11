using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlaneIntersectTest : MonoBehaviour
{
    [SerializeField] Transform plane0;
    [SerializeField] Transform plane1;
    [SerializeField] Transform marker;

    void Start()
    {
        
    }

    void Update()
    {
        var p0 = new Plane(this.plane0.up, this.plane0.position);
        var quad = new Quad();
        quad.Center = this.plane1.position;
        quad.LocalRight = this.plane1.right * this.plane1.lossyScale.x * 5f;
        quad.LocalUp = this.plane1.forward * this.plane1.lossyScale.z * 5f;
        quad.Normal = this.plane1.up;

        var mat = this.plane1.GetComponent<MeshRenderer>().material;
        if (CullingSystem.Intersect(p0, quad))
        {
            mat.color = Color.red;
        }
        else
        {
            mat.color = Color.white;
        }
    }
}
