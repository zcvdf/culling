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

        var extents = new WorldOccluderExtents
        {
            Right = this.transform.right * scale.x * 5f,
            Up = this.transform.up * scale.y * 5f
        };

        dstManager.AddComponentData(entity, extents);
    }
}
