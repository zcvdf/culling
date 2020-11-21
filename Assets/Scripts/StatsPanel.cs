using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private Text text;

    void Update()
    {
        if (!Main.DisplayStats) return;

        this.text.text =
              $"FPS : {Stats.FPS}\n"
            + "\n"
            + $"Total Entity Number : {Stats.TotalEntityNumber}\n"
            + $"Visible Octree Clusters : {Stats.VisibleOctreeClusters}\n"
            + "\n"
            + $"Total Culled : {Stats.TotalCulledPercentage:0.0}%\n"
            + $"\tCulled By Octree Clusters : {Stats.CulledByOctreeClustersPercentage:0.0}%\n"
            + $"\tCulled By Octree Nodes : {Stats.CulledByOctreeNodesPercentage:0.0}%\n"
            + $"\tCulled By Frustrum Planes : {Stats.CulledByFrustrumPlanesPercentage:0.0}%\n"
            + $"\tCulled By Sphere Occluders : {Stats.CulledBySphereOccludersPercentage:0.0}%\n"
            + $"\tCulled By Quad Occluders : {Stats.CulledByQuadOccludersPercentage:0.0}%\n";
    }
}
