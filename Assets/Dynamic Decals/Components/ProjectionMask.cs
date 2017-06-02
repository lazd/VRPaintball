using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/**
* The Mask Component. This component allows us to declare renderers as a part of one or more masking layers. Projections like decals, eraser or omni-decals can then choose to ignore that masking layer, or render only to that masking layer.
*/
[ExecuteInEditMode]
public class ProjectionMask : MonoBehaviour {

    /**
    * Mask Type
    * The type of mask determines what renderers are being masked. Standard will mask any attached renderers. Compound will mask the renderers of direct children.
    */
    public MaskType MaskType
    {
        get { return type; }
        set
        {
            if (type != value)
            {
                type = value;
                GrabInstances();
            }
        }
    }

    /**
    * Mask Layer 1. 
    * Should this object be apart of the first masking layer? Objects can be apart of multiple masking layers.
    */
    public bool Layer1
    {
        get { return layers[0]; }
        set
        {
            layers[0] = value;
            Mark();
        }
        
    }
    /**
    * Mask Layer 2. 
    * Should this object be apart of the second masking layer? Objects can be apart of multiple masking layers.
    */
    public bool Layer2
    {
        get { return layers[1]; }
        set
        {
            layers[1] = value;
            Mark();
        }

    }
    /**
    * Mask Layer 3. 
    * Should this object be apart of the third masking layer? Objects can be apart of multiple masking layers.
    */
    public bool Layer3
    {
        get { return layers[2]; }
        set
        {
            layers[2] = value;
            Mark();
        }

    }
    /**
    * Mask Layer 4. 
    * Should this object be apart of the fourth masking layer? Objects can be apart of multiple masking layers.
    */
    public bool Layer4
    {
        get { return layers[3]; }
        set
        {
            layers[3] = value;
            Mark();
        }

    }

    //Instances
    public List<MaskInstance> Instances
    {
        get { return instances; }
    }
    private List<MaskInstance> instances;

    //Material
    public Material Material
    {
        get
        {
            if (marked || material == null)
            {
                material = DynamicDecals.System.GetMaskMaterial(new MaskValue(layers[0], layers[1], layers[2], layers[3]));
                marked = false;
            }
            return material;
        }
    }
    private Material material;

    public void Mark()
    {
        marked = true;
    }
    private bool marked = true;

    //Backing Fields
    [SerializeField]
    private MaskType type;

    [SerializeField]
    private bool[] layers = new bool[4];

    private void OnEnable()
    {
        Register();
        GrabInstances();
    }
    private void OnDisable()
    {
        Deregister();
    }

    private void OnTransformChildrenChanged()
    {
        if (type == MaskType.Compound) GrabInstances();
    }
    #if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && type == MaskType.Compound) GrabInstances();
    }
    #endif

    private void Register()
    {
        if (this != null)
        {
            #if UNITY_EDITOR
            PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
            if (prefabType == PrefabType.ModelPrefab || prefabType == PrefabType.Prefab) return;
            #endif

            DynamicDecals.System.AddMask(this);
        }
    }
    private void Deregister()
    {
        if (this != null)
        {
            #if UNITY_EDITOR
            PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
            if (prefabType == PrefabType.ModelPrefab || prefabType == PrefabType.Prefab) return;
            #endif

            DynamicDecals.System.RemoveMask(this);
        }
    }
    public void GrabInstances()
    {
        if (instances == null) instances = new List<MaskInstance>();
        else instances.Clear();

        switch (type)
        {
            case MaskType.Standard:
                GrabRenderer(transform);
                break;
            case MaskType.Compound:
                GrabCompoundRenderers();
                break;
        }
    }

    private void GrabRenderer(Transform Transform)
    {
        if (Transform.GetComponent<MeshRenderer>() != null)
        {
            int subMeshCount = Transform.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
            instances.Add(new MaskInstance(Transform.GetComponent<MeshRenderer>(), subMeshCount));
        }
        if (Transform.GetComponent<SkinnedMeshRenderer>() != null)
        {
            int subMeshCount = Transform.GetComponent<SkinnedMeshRenderer>().sharedMesh.subMeshCount;
            instances.Add(new MaskInstance(Transform.GetComponent<SkinnedMeshRenderer>(), subMeshCount));
        }
    }
    private void GrabCompoundRenderers()
    {
        foreach (Transform child in transform) GrabRenderer(child);
    }
}

public struct MaskValue
{
    public bool layer1;
    public bool layer2;
    public bool layer3;
    public bool layer4;

    public MaskValue(bool Layer1, bool Layer2, bool Layer3, bool Layer4)
    {
        layer1 = Layer1;
        layer2 = Layer2;
        layer3 = Layer3;
        layer4 = Layer4;
    }
}
public enum MaskType { Standard, Compound };
public struct MaskInstance
{
    public Renderer renderer;
    public int subMeshCount;

    public MaskInstance(Renderer Renderer, int SubMeshCount)
    {
        renderer = Renderer;
        subMeshCount = SubMeshCount;
    }
}