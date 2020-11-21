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
            + $"Visible Octree Leafs : {Stats.VisibleOctreeLeafs}\n";
    }
}
