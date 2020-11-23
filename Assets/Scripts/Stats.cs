using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Stats
{
    public static int FPS;

    public static int TotalEntityNumber;
    public static int VisibleOctreeClusters;
    public static int VisibleOctreeLeafs;

    public static int CulledByOctreeClusters;
    public static int CulledByOctreeNodes;
    public static int CulledByFrustrumPlanes;
    public static int CulledBySphereOccluders;
    public static int CulledByQuadOccluders;
    public static int AtRootOctreeLayer;

    public static int TotalCulled => CulledByOctreeClusters + CulledByOctreeNodes 
        + CulledByFrustrumPlanes + CulledBySphereOccluders + CulledByQuadOccluders;

    public static float TotalCulledPercentage => AsPercentage(TotalCulled);

    public static float CulledByOctreeClustersPercentage => AsPercentage(CulledByOctreeClusters);

    public static float CulledByOctreeNodesPercentage => AsPercentage(CulledByOctreeNodes);

    public static float CulledByFrustrumPlanesPercentage => AsPercentage(CulledByFrustrumPlanes);

    public static float CulledBySphereOccludersPercentage => AsPercentage(CulledBySphereOccluders);

    public static float CulledByQuadOccludersPercentage => AsPercentage(CulledByQuadOccluders);

    public static float AtRootOctreeLayerPercentage => AsPercentage(AtRootOctreeLayer);

    private static float AsPercentage(int entityCount)
    {
        return 100f * (entityCount / (float)TotalEntityNumber);
    }
}
