using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private Text text;

    void Start()
    {

    }

    void Update()
    {
        this.text.text =
            $"Total Entity Number : {Stats.TotalEntityNumber}\n"
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
