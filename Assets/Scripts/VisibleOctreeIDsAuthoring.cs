using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class VisibleOctreeIDsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<VisibleOctreeLeaf>(entity);
        dstManager.AddBuffer<VisibleOctreeCluster>(entity);
        dstManager.AddBuffer<VisibleLeafInClusterCount>(entity);
    }
}
