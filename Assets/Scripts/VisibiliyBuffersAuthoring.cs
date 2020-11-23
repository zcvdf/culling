using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class VisibilityBuffer : IComponentData
{
    public NativeHashSet<ulong> Layer0;
    public NativeHashSet<ulong> Layer1;
}

public class VisibiliyBuffersAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<VisibleOctreeNode>(entity);
        dstManager.AddBuffer<VisibleOctreeCluster>(entity);
        dstManager.AddBuffer<VisibleNodeInClusterCount>(entity);

        var visibilityBuffer = new VisibilityBuffer
        {
            Layer0 = new NativeHashSet<ulong>(16, Allocator.Persistent),
            Layer1 = new NativeHashSet<ulong>(64, Allocator.Persistent),
        };

        dstManager.AddComponentData(entity, visibilityBuffer);
    }
}
