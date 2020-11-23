using Unity.Entities;
using UnityEngine;

public class VisibiliyBuffersAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var visibleSets = new VisibleSets();
        visibleSets.Setup();

        dstManager.AddComponentData(entity, new VisibleSetsComponent { Value = visibleSets });
    }
}