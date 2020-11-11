using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlaneIntersectTest : MonoBehaviour
{
    [SerializeField] Transform plane0;
    [SerializeField] Transform plane1;
    [SerializeField] Transform marker;
    Quad quad0;
    Quad quad1;

    void Start()
    {
        
    }

    Vector3 p;
    Vector3 d;

    void Update()
    {
        var quad0 = new Quad();
        quad0.Center = this.plane0.position;
        quad0.LocalRight = this.plane0.right * this.plane0.localScale.x * 5f;
        quad0.LocalUp = this.plane0.forward * this.plane0.localScale.z * 5f;
        quad0.Normal = this.plane0.up;
        this.quad0 = quad0;

        var quad1 = new Quad();
        quad1.Center = this.plane1.position;
        quad1.LocalRight = this.plane1.right * this.plane1.localScale.x * 5f;
        quad1.LocalUp = this.plane1.forward * this.plane1.localScale.z * 5f;
        quad1.Normal = this.plane1.up;
        this.quad1 = quad1;

        var mat0 = this.plane0.GetComponent<MeshRenderer>().material;
        var mat1 = this.plane1.GetComponent<MeshRenderer>().material;

        float3 point;
        float3 direction;
        if (CullingSystem.Intersect(new Plane(quad0.Normal, quad0.Center), new Plane(quad1.Normal, quad1.Center), out point, out direction))
        {
            this.marker.position = point;
            this.p = point;
            this.d = direction;
        }

        if (CullingSystem.Intersect(quad0, quad1))
        {
            mat0.color = Color.red;
            mat1.color = Color.red;
        }
        else
        {
            mat0.color = Color.white;
            mat1.color = Color.white;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(this.p - this.d * 100f, this.p + this.d * 100f);
        Gizmos.DrawSphere(this.quad0.Center + this.quad0.LocalUp - this.quad0.LocalRight, 0.2f);
        Gizmos.DrawSphere(this.quad0.Center + this.quad0.LocalUp + this.quad0.LocalRight, 0.2f);
        Gizmos.DrawSphere(this.quad0.Center - this.quad0.LocalUp + this.quad0.LocalRight, 0.2f);
        Gizmos.DrawSphere(this.quad0.Center - this.quad0.LocalUp - this.quad0.LocalRight, 0.2f);
                                                                                          
        Gizmos.DrawSphere(this.quad1.Center + this.quad1.LocalUp - this.quad1.LocalRight, 0.2f);
        Gizmos.DrawSphere(this.quad1.Center + this.quad1.LocalUp + this.quad1.LocalRight, 0.2f);
        Gizmos.DrawSphere(this.quad1.Center - this.quad1.LocalUp + this.quad1.LocalRight, 0.2f);
        Gizmos.DrawSphere(this.quad1.Center - this.quad1.LocalUp - this.quad1.LocalRight, 0.2f);

        Gizmos.DrawSphere(this.quad0.Center + this.quad0.Normal, 0.2f);
        Gizmos.DrawSphere(this.quad1.Center + this.quad1.Normal, 0.2f);
    }
}
