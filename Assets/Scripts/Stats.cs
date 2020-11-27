using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatsDetails
{
    None,
    Normal,
    Advanced,
}

public static class Stats
{
    public static StatsDetails Details = StatsDetails.None;

    public static int FPS;

    public static int TotalEntityNumber;
    public static int VisibleOctreeClusters;
    public static int VisibleOctreeLeafs;

    public static int CulledByOctreeNodes;
    public static int CulledByFrustrumPlanes;
    public static int CulledBySphereOccluders;
    public static int CulledByQuadOccluders;
    public static int AtRootOctreeLayer;

    public static int TotalCulled => CulledByOctreeNodes 
        + CulledByFrustrumPlanes + CulledBySphereOccluders + CulledByQuadOccluders;

    public static float TotalCulledPercentage => AsPercentage(TotalCulled);

    public static float CulledByOctreeNodesPercentage => AsPercentage(CulledByOctreeNodes);

    public static float CulledByFrustrumPlanesPercentage => AsPercentage(CulledByFrustrumPlanes);

    public static float CulledBySphereOccludersPercentage => AsPercentage(CulledBySphereOccluders);

    public static float CulledByQuadOccludersPercentage => AsPercentage(CulledByQuadOccluders);

    public static float AtRootOctreeLayerPercentage => AsPercentage(AtRootOctreeLayer);

    private static float AsPercentage(int entityCount)
    {
        return 100f * (entityCount / (float)TotalEntityNumber);
    }

    public static void NextDetailsLevel()
    {
        var detail = (int)Details + 1;

        if (detail > 2)
        {
            detail = 0;
        }

        Details = (StatsDetails)detail;
    }
}
