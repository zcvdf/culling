using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WorldOccluderExtentsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = this.transform.lossyScale;

        var localRight = this.transform.right;
        var localRightLength = scale.x * 5f;
        var localUp = this.transform.forward;
        var localUpLength = scale.z * 5f;

        var extents = new WorldOccluderExtents
        {
            LocalRight = localRight,
            LocalRightLength = localRightLength,
            LocalUp = localUp,
            LocalUpLength = localUpLength,
        };

        dstManager.AddComponentData(entity, extents);
    }
}
