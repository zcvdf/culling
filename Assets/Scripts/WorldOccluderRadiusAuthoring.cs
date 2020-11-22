using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WorldOccluderRadiusAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = this.transform.lossyScale;
        var maxScale = math.max(math.max(scale.x, scale.y), scale.z);

        var radius = new WorldOccluderRadius
        {
            Value = 0.5f * maxScale
        };

        dstManager.AddComponentData(entity, radius);
    }
}
