using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateVisibleOctreeIDs : SystemBase
{
    protected override void OnUpdate()
    {
        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        this.Entities.ForEach((DynamicBuffer<VisibleOctreeID> visibleIDs) =>
        {
            UpdateVisibleOctreeNodes(frustrumPlanes, frustrumAABB, visibleIDs);
        })
        .ScheduleParallel();
    }

    static void UpdateVisibleOctreeNodes(WorldFrustrumPlanes planes, AABB frustrumAABB, DynamicBuffer<VisibleOctreeID> visible)
    {
        visible.Clear();

        int3 minID0;
        int3 maxID0;
        Octree.GetMixMaxIDLayer0(frustrumAABB, out minID0, out maxID0);

        for (int x0 = minID0.x; x0 <= maxID0.x; ++x0)
        {
            for (int y0 = minID0.y; y0 <= maxID0.y; ++y0)
            {
                for (int z0 = minID0.z; z0 <= maxID0.z; ++z0)
                {
                    var id0 = new int3(x0, y0, z0);

                    if (Math.IsCubeInFrustrum(Octree.IDLayer0ToPoint(id0), Octree.Node0Extent, planes))
                    {
                        int3 minID1;
                        int3 maxID1;
                        Octree.GetMixMaxIDChild0(id0, out minID1, out maxID1);

                        for (int x1 = minID1.x; x1 < maxID1.x; ++x1)
                        {
                            for (int y1 = minID1.y; y1 < maxID1.y; ++y1)
                            {
                                for (int z1 = minID1.z; z1 < maxID1.z; ++z1)
                                {
                                    var id1 = new int3(x1, y1, z1);

                                    if (Math.IsCubeInFrustrum(Octree.IDLayer1ToPoint(id1), Octree.Node1Extent, planes))
                                    {
                                        var id = new OctreeID
                                        {
                                            Value = Octree.PackID(id1),
                                        };

                                        visible.Add(new VisibleOctreeID { Value = id });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

#if ENABLE_ASSERTS
        AssertNoDupplicate(visible);
#endif
    }

    static void AssertNoDupplicate(DynamicBuffer<VisibleOctreeID> ids)
    {
        for (int i = 0; i < ids.Length; ++i)
        {
            var a = ids[i].Value.Value;

            for (int j = 0; j < ids.Length; ++j)
            {
                if (i == j) continue;

                var b = ids[j].Value.Value;

                Debug.Assert(a != b);
            }
        }
    }
}
