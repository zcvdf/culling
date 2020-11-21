using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class VisibiliyBuffersAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<VisibleOctreeNode>(entity);
        dstManager.AddBuffer<VisibleOctreeCluster>(entity);
        dstManager.AddBuffer<VisibleLeafInClusterCount>(entity);
    }
}
