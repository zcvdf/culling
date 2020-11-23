using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

// Ugly hack to make a compile-time array of 16 elements
public struct VisibleSets
{
    private NativeHashSet<ulong> layerSet0;
    private NativeHashSet<ulong> layerSet1;
    private NativeHashSet<ulong> layerSet2;
    private NativeHashSet<ulong> layerSet3;
    private NativeHashSet<ulong> layerSet4;
    private NativeHashSet<ulong> layerSet5;
    private NativeHashSet<ulong> layerSet6;
    private NativeHashSet<ulong> layerSet7;
    private NativeHashSet<ulong> layerSet8;
    private NativeHashSet<ulong> layerSet9;
    private NativeHashSet<ulong> layerSet10;
    private NativeHashSet<ulong> layerSet11;
    private NativeHashSet<ulong> layerSet12;
    private NativeHashSet<ulong> layerSet13;
    private NativeHashSet<ulong> layerSet14;
    private NativeHashSet<ulong> layerSet15;

    public void Setup()
    {
        for (int i = 0; i < this.Length; ++i)
        {
            this[i] = new NativeHashSet<ulong>(16, Allocator.Persistent);
        }

        for (int i = this.Length; i < 16; ++i)
        {
            this[i] = new NativeHashSet<ulong>(1, Allocator.Persistent);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < this.Length; ++i)
        {
            this[i].Clear();
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < 16; ++i)
        {
            this[i].Dispose();
        }
    }

    public NativeHashSet<ulong> this[int index]
    {
        get
        {
            switch (index)
            {
                case 0: return this.layerSet0;
                case 1: return this.layerSet1;
                case 2: return this.layerSet2;
                case 3: return this.layerSet3;
                case 4: return this.layerSet4;
                case 5: return this.layerSet5;
                case 6: return this.layerSet6;
                case 7: return this.layerSet7;
                case 8: return this.layerSet8;
                case 9: return this.layerSet9;
                case 10: return this.layerSet10;
                case 11: return this.layerSet11;
                case 12: return this.layerSet12;
                case 13: return this.layerSet13;
                case 14: return this.layerSet14;
                case 15: return this.layerSet15;
            }

            Debug.LogError($"Index out of range {index}");
            return default;
        }

        set
        {
            switch (index)
            {
                case 0: this.layerSet0 = value; return;
                case 1: this.layerSet1 = value; return;
                case 2: this.layerSet2 = value; return;
                case 3: this.layerSet3 = value; return;
                case 4: this.layerSet4 = value; return;
                case 5: this.layerSet5 = value; return;
                case 6: this.layerSet6 = value; return;
                case 7: this.layerSet7 = value; return;
                case 8: this.layerSet8 = value; return;
                case 9: this.layerSet9 = value; return;
                case 10: this.layerSet10 = value; return;
                case 11: this.layerSet11 = value; return;
                case 12: this.layerSet12 = value; return;
                case 13: this.layerSet13 = value; return;
                case 14: this.layerSet14 = value; return;
                case 15: this.layerSet15 = value; return;
            }

            Debug.LogError($"Index out of range {index}");
        }
    }

    public int Length => Octree.LeafLayer + 1;
}

public class VisibilityBuffer : IComponentData
{
    public VisibleSets Value;
}

public class VisibiliyBuffersAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var visibleSets = new VisibleSets();
        visibleSets.Setup();

        dstManager.AddComponentData(entity, new VisibilityBuffer { Value = visibleSets });
    }
}