using UnityEngine;
using System.Collections;

public class DynamicDecalSettings : ScriptableObject
{
    public PoolInstance[] pools;
    public string[] layerNames;

    public bool highPrecision;
    public bool forceForward;

    public DynamicDecalSettings()
    {
        pools = new PoolInstance[] { new PoolInstance("Default", null) };

        layerNames = new string[] { "Layer 1", "Layer 2", "Layer 3", "Layer 4" };

        highPrecision = false;

        forceForward = false;
    }
}

[System.Serializable]
public class PoolInstance
{
    public int id;
    public string title;
    public int[] limits;

    public PoolInstance(string Title, PoolInstance[] CurrentInstances)
    {
        id = UniqueID(CurrentInstances);
        title = Title;

        //15 Quality Settings maximum
        limits = new int[15];
        //Set all defaults
        for(int i = 0; i < limits.Length; i++)
        {
            limits[i] = 250 + (i * 50);
        }
    }
    private int UniqueID(PoolInstance[] CurrentInstances)
    {
        //We use an ID instead of a name or an index to keep track of our pool as it allows us to rename and reorder pools while maintaining a hidden reference to them. 
        //Also lookup from a dictionary is faster than iterating over all pools for a given name.

        //Start at 0 (1 if not the first) and iterate upwards until we have an ID not currently in use.
        int ID = 0;
        bool Unique = false;

        if (CurrentInstances != null)
        {
            while (!Unique)
            {
                //ID, wan't unique. Increment and check again.
                ID++;
                Unique = true;
                //Start unique as true, then iterate over all instances to see if its otherwise.
                for (int i = 0; i < CurrentInstances.Length; i++)
                {
                    if (CurrentInstances[i] != null && ID == CurrentInstances[i].id) Unique = false;
                }
            }
        }

        //We have a unique ID! System falls apart if we have more than 2,147,483,647 pools at once. Seems unlikely.
        return ID;
    }
}