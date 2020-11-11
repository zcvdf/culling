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
        var p1 = new Plane(this.plane1.up, this.plane1.position);

        var point = CullingSystem.Intersect(p0, p1);
        this.marker.position = point;
    }
}
