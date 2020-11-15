using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

[UpdateAfter(typeof(TransformSystemGroup))]
public class UpdateVisibleOctreeIDs : SystemBase
{
    protected override void OnUpdate()
    {
        var visibleOctreeEntity = GetSingletonEntity<VisibleOctreeIDs>();
        var visibleOctreeIDs = GetBuffer<VisibleOctreeIDs>(visibleOctreeEntity);

        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        UpdateVisibleOctreeNodes(frustrumPlanes, frustrumAABB, visibleOctreeIDs);
    }

    static void UpdateVisibleOctreeNodes(WorldFrustrumPlanes planes, AABB frustrumAABB, DynamicBuffer<VisibleOctreeIDs> visible)
    {
        visible.Clear();

        Octree.ForEachBoundingNode0(frustrumAABB, (int3 id0) =>
        {
            var center0 = Octree.IDLayer0ToPoint(id0);
            var radius0 = Octree.Node0BoundingRadius;

            if (Math.IsInFrustrum(center0, radius0, planes))
            {
                var id = new OctreeID
                {
                    ID0 = id0,
                };
                var visibleID = new VisibleOctreeIDs
                {
                    Value = id
                };

                visible.Add(visibleID);

                /*Octree.ForEachNode0Childs(id0, (int3 id1) =>
                {
                    var center1 = Octree.IDLayer1ToPoint(id1);
                    var radius1 = Octree.Node1BoundingRadius;

                    if (IsInFrustrum(center1, radius1, planes))
                    {
                        var id = new OctreeID
                        {
                            ID0 = id0,
                            ID1 = id1,
                        };

                        visible.Add(id);
                    }
                });*/
            }
        });

#if ENABLE_ASSERTS
        AssertNoDupplicate(visible);
#endif
    }

    static void AssertNoDupplicate(DynamicBuffer<VisibleOctreeIDs> ids)
    {
        for (int i = 0; i < ids.Length; ++i)
        {
            var a0 = ids[i].Value.ID0;
            var a1 = ids[i].Value.ID1;

            for (int j = 0; j < ids.Length; ++j)
            {
                if (i == j) continue;

                var b0 = ids[j].Value.ID0;
                var b1 = ids[j].Value.ID1;

                Debug.Assert(math.any(a0 != b0) || math.any(a1 != b1));
            }
        }
    }
}
