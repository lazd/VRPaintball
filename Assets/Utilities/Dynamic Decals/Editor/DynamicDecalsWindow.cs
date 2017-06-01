using UnityEngine;
using UnityEditor;
using System.Collections;

public class DynamicDecalsWindow : EditorWindow
{
    [MenuItem("Window/Dynamic Decals")]
    static void Init()
    {
        DynamicDecalsWindow window = (DynamicDecalsWindow)EditorWindow.GetWindow(typeof(DynamicDecalsWindow));
        window.minSize = new Vector2(300, 180);
        window.Show();
    }
    
    //Cached variables
    private string assetPath = "Assets/Dynamic Decals/Resources/Settings.asset";

    //Style Colors
    private Color fontColor = new Color(0.8f, 0.8f, 0.8f);

    private Color headerBGColor = new Color(0.55f, 0.55f, 0.55f);
    private Color primaryBGColor = new Color(0.43f, 0.43f, 0.43f);
    private Color secondaryBGColor = new Color(0.45f, 0.45f, 0.45f);

    //Box Styles
    private GUIStyle HeaderBox
    {
        get
        {
            if (headerBox == null)
            {
                headerBox = new GUIStyle(GUI.skin.box);
                headerBox.normal.background = MakeTex(1, 1, headerBGColor);
            }
            return headerBox;
        }
    }
    private GUIStyle headerBox;
    private GUIStyle PrimaryBox
    {
        get
        {
            if (primaryBox == null)
            {
                primaryBox = new GUIStyle(GUI.skin.box);
                primaryBox.normal.background = MakeTex(1, 1, primaryBGColor);
            }
            return primaryBox;
        }
    }
    private GUIStyle primaryBox;
    private GUIStyle SecondaryBox
    {
        get
        {
            if (secondaryBox == null)
            {
                secondaryBox = new GUIStyle(GUI.skin.box);
                secondaryBox.normal.background = MakeTex(1, 1, secondaryBGColor);
            }
            return secondaryBox;
        }
    }
    private GUIStyle secondaryBox;

    //Label Styles
    private GUIStyle HeaderLabel
    {
        get
        {
            if (headerLabel == null)
            {
                headerLabel = new GUIStyle(GUI.skin.label);
                headerLabel.fontSize = 12;
                headerLabel.fontStyle = FontStyle.Bold;
                headerLabel.alignment = TextAnchor.MiddleLeft;
                headerLabel.normal.textColor = fontColor;
            }
            return headerLabel;
        }
    }
    private GUIStyle headerLabel;
    private GUIStyle ContentLabel
    {
        get
        {
            if (contentLabel == null)
            {
                contentLabel = new GUIStyle(GUI.skin.label);
                contentLabel.fontSize = 10;
                contentLabel.fontStyle = FontStyle.Normal;
                contentLabel.alignment = TextAnchor.MiddleLeft;
                contentLabel.normal.textColor = fontColor;
            }
            return contentLabel;
        }
    }
    private GUIStyle contentLabel;
    private GUIStyle TabLabel
    {
        get
        {
            if (tabLabel == null)
            {
                tabLabel = new GUIStyle(GUI.skin.label);
                tabLabel.fontSize = 10;
                tabLabel.fontStyle = FontStyle.Normal;
                tabLabel.alignment = TextAnchor.MiddleCenter;
                tabLabel.normal.textColor = fontColor;
            }
            return tabLabel;
        }
    }
    private GUIStyle tabLabel;

    private GUIStyle SettingsLabel
    {
        get
        {
            if (settingsLabel == null)
            {
                settingsLabel = new GUIStyle(EditorStyles.boldLabel);
                settingsLabel.normal.textColor = fontColor;
            }
            return settingsLabel;
        }
    }
    private GUIStyle settingsLabel;
    private GUIStyle SettingsContent
    {
        get
        {
            if (settingsContent == null)
            {
                settingsContent = new GUIStyle(EditorStyles.label);
                settingsContent.normal.textColor = fontColor;
            }
            return settingsContent;
        }
    }
    private GUIStyle settingsContent;

    //Quality Settings Tab
    private int qualitySetting = 0;

    //ScrollView
    private Vector2 scrollPosition;

    //Metrics
    private float topBuffer = 10;
    private float spacerHeight = 20;

    private float tabHeight = 30;
    private float poolRowHeight = 30;
    private float generalSettingsHeight = 160;

    void OnEnable()
    {
        //Register undo/redo callback
        Undo.undoRedoPerformed += UndoRedo;
    }
    void OnDisable()
    {
        //De-register undo/redo callback
        Undo.undoRedoPerformed -= UndoRedo;
    }

    void OnGUI()
    {
        //Grab our settings
        DynamicDecalSettings settings = DynamicDecals.System.Settings;

        //Calculate required Rect Height
        float poolHeight = poolRowHeight * (settings.pools.Length + 3);
        float totalHeight = topBuffer + tabHeight + poolHeight + spacerHeight + generalSettingsHeight + 30;

        //Begin Change Check & ScrollView
        GUI.changed = false;
        Rect scrollRect = new Rect(0, 0, Screen.width, totalHeight);
        scrollPosition = GUI.BeginScrollView(new Rect(0, 0, Screen.width, Screen.height), scrollPosition, scrollRect);

        float horizontalPosition = topBuffer;

        //Quality Tabs
        Rect tabRect = new Rect(10, horizontalPosition, (Screen.height < totalHeight) ? scrollRect.width - 35 : scrollRect.width - 20, tabHeight);

        GUI.BeginGroup(tabRect, new GUIContent(""), SecondaryBox);
        QualityTabs(tabRect);
        GUI.EndGroup();

        horizontalPosition += tabHeight;

        //Pool Settings
        Rect poolRect = new Rect(10, horizontalPosition, (Screen.height < totalHeight) ? scrollRect.width - 35 : scrollRect.width - 20, poolHeight);

        GUI.BeginGroup(poolRect, new GUIContent(""), SecondaryBox);
        PoolSettings(poolRect, settings);
        GUI.EndGroup();

        horizontalPosition += poolHeight + spacerHeight;

        //General Settings
        Rect generalRect = new Rect(10, horizontalPosition, (Screen.height < totalHeight) ? scrollRect.width - 35 : scrollRect.width - 20, generalSettingsHeight);

        GUI.BeginGroup(generalRect, new GUIContent(""), SecondaryBox);
        GeneralSettings(generalRect, settings);
        GUI.EndGroup();

        //End Change Check & ScrollView
        GUI.EndScrollView();
        if (GUI.changed)
        {
            //If the asset already exists, mark it to be saved
            if (Resources.Load<DynamicDecalSettings>("Settings") != null) EditorUtility.SetDirty(settings);
            //If the asset doen't exist, create it
            else AssetDatabase.CreateAsset(settings, assetPath);
        }
    }
    void UndoRedo()
    {
        //Repaint the window to show changes immediately
        Repaint();
    }

    //GUI Sections
    private void QualityTabs(Rect Area)
    {
        //Quality Tabs
        int tabCount = QualitySettings.names.Length;
        float tabHeight = 30;
        float width = Area.width;
        float tabWidth = width / tabCount;

        GUI.Box(new Rect(0, 0, width, tabHeight), new GUIContent(""), SecondaryBox);
        for (int i = 0; i < tabCount; i++)
        {
            if (GUI.Button(new Rect((i * tabWidth), 1, tabWidth, tabHeight - 1), new GUIContent(""), (i == qualitySetting) ? HeaderBox : SecondaryBox))
            {
                //Switch to selected quality setting
                qualitySetting = i;

                //Unfocus controls on tab switch
                GUIUtility.keyboardControl = 0;
            }
            GUI.Label(new Rect((i * tabWidth), 1, tabWidth, tabHeight - 1), new GUIContent(QualitySettings.names[i]), TabLabel);
        }
    }
    private void PoolSettings(Rect Area, DynamicDecalSettings Settings)
    {
        //Collumn width
        float poolCollumnWidth = Area.width / 3;

        //Button Dimensions
        float buttonHeight = 16;
        float buttonMajorWidth = 80;
        float buttonMinorWidth = 20;

        //Header
        GUI.Box(new Rect(0, 0, Area.width, poolRowHeight), new GUIContent(""), HeaderBox);
        GUI.Label(new Rect(4, 0, poolCollumnWidth, poolRowHeight), "Title", HeaderLabel);
        GUI.Label(new Rect(4 + poolCollumnWidth, 0, poolCollumnWidth, poolRowHeight), "Limit", HeaderLabel);

        //Pool Content
        for (int i = 0; i < Settings.pools.Length; i++)
        {
            float rowheight = poolRowHeight * (1 + i);

            //background
            GUI.Box(new Rect(0, rowheight, Area.width, poolRowHeight), new GUIContent(""), (i % 2 != 0) ? PrimaryBox : SecondaryBox);

            //Title
            if (i == 0)
            {
                GUI.Label(new Rect(4, rowheight, poolCollumnWidth, poolRowHeight), "Default", ContentLabel);
                if (Settings.pools[i].title != "Default") Settings.pools[i].title = "Default";
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                string title = EditorGUI.TextField(new Rect(4, rowheight, poolCollumnWidth, poolRowHeight), Settings.pools[i].title, ContentLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    //Record state for undo
                    Undo.RecordObject(Settings, "Rename pool");

                    //Rename pool
                    Settings.pools[i].title = title;
                }
            }


            //Limit
            EditorGUI.BeginChangeCheck();
            int limit = EditorGUI.IntField(new Rect(4 + poolCollumnWidth, rowheight, poolCollumnWidth, poolRowHeight), Settings.pools[i].limits[qualitySetting], ContentLabel);
            if (EditorGUI.EndChangeCheck())
            {
                //Record state for undo
                Undo.RecordObject(Settings, "Resize pool");

                //Adjust pool size
                Settings.pools[i].limits[qualitySetting] = limit;
            }

            //Utility Buttons
            Rect utilityRect = new Rect(Area.width - (buttonMinorWidth * 3 + 16) - 12, rowheight, (buttonMinorWidth * 3 + 16), poolRowHeight);
            GUI.BeginGroup(utilityRect);
            
            //Cache GUI.enabled
            bool GUIEnabled = GUI.enabled;

            //Up
            if (i < 2) GUI.enabled = false;
            if (GUI.Button(new Rect(4, (utilityRect.height - buttonHeight) / 2, buttonMinorWidth, buttonHeight), "↑"))
            {
                //Record state for undo
                Undo.RecordObject(Settings, "Pool Up");

                //Move pool up
                Swap(Settings, i, i - 1);
            }
            //Restore GUI state
            GUI.enabled = GUIEnabled;

            //Down
            if (i == 0 || i == Settings.pools.Length - 1) GUI.enabled = false;
            if (GUI.Button(new Rect(buttonMinorWidth + 8, (utilityRect.height - buttonHeight) / 2, buttonMinorWidth, buttonHeight), "↓"))
            {
                //Record state for undo
                Undo.RecordObject(Settings, "Pool Down");

                //Move pool down
                Swap(Settings, i, i + 1);
            }
            //Restore GUI state
            GUI.enabled = GUIEnabled;

            //Remove
            if (i == 0) GUI.enabled = false;
            if (GUI.Button(new Rect(2 * buttonMinorWidth + 12, (utilityRect.height - buttonHeight) / 2, buttonMinorWidth, buttonHeight), "-"))
            {
                //Record state for undo
                Undo.RecordObject(Settings, "Pool Down");

                //Remove pool
                RemoveAt(Settings, i);
            }
            //Restore GUI state
            GUI.enabled = GUIEnabled;

            GUI.EndGroup();
        }

        //New Pool
        GUI.Box(new Rect(0, poolRowHeight * (1 + Settings.pools.Length), Area.width, poolRowHeight), new GUIContent(""), (Settings.pools.Length % 2 != 0) ? PrimaryBox : SecondaryBox);
        if (GUI.Button(new Rect((Area.width - buttonMajorWidth) / 2, poolRowHeight * (1 + Settings.pools.Length) + ((poolRowHeight - buttonHeight) / 2), buttonMajorWidth, buttonHeight), "+"))
        {
            //Record state for undo
            Undo.RecordObject(Settings, "Add Pool");

            //Add pool
            NewPool(Settings);
        }

        //Total
        GUI.Box(new Rect(0, poolRowHeight * (2 + Settings.pools.Length), Area.width, poolRowHeight), new GUIContent(""), HeaderBox);
        GUI.Label(new Rect(4, poolRowHeight * (2 + Settings.pools.Length), poolCollumnWidth, poolRowHeight), "Total", ContentLabel);

        //Calculate total
        float total = 0;
        for (int i = 0; i < Settings.pools.Length; i++) total += Settings.pools[i].limits[qualitySetting];
        GUI.Label(new Rect(4 + poolCollumnWidth, poolRowHeight * (2 + Settings.pools.Length), poolCollumnWidth, poolRowHeight), total.ToString(), ContentLabel);
    }
    private void GeneralSettings(Rect Area, DynamicDecalSettings Settings)
    {
        GUI.color = fontColor;

        GUILayout.BeginArea(new Rect(0, 0, Area.width, Area.height));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(new GUIContent("Mask Layer Names", "Set the names of the mask layer used by decals & decal masks."), SettingsLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < Settings.layerNames.Length; i++)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Layer " + (i + 1)), SettingsContent, GUILayout.Width(100));
            string layerName = EditorGUILayout.TextField(new GUIContent(""), Settings.layerNames[i], GUILayout.Width(Area.width - 100 - 20));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                //Record state for undo
                Undo.RecordObject(Settings, "Layer name");

                //Change layer name
                Settings.layerNames[i] = layerName;
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Force Forward Rendering", "Should you force the system to render decals in a forward render loop?"), SettingsLabel, GUILayout.Width(220));
        bool forceForward = EditorGUILayout.Toggle(new GUIContent(""), Settings.forceForward, GUILayout.Width(Area.width - 140 - 20));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            //Record state for undo
            Undo.RecordObject(Settings, "Force Forward");

            //Apply changes
            Settings.forceForward = forceForward;
            Settings.highPrecision = (Settings.forceForward) ? true: Settings.highPrecision;

            //Update shader LODs to avoid pass index errors
            DynamicDecals.System.UpdateLODs();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("High Precision Depth/Normals", "Should you render high precision depth and normal textures while in Forward Rendering?"), SettingsLabel, GUILayout.Width(220));
        bool GUIEnabled = GUI.enabled;
        GUI.enabled = !Settings.forceForward;
        bool highPrecision = EditorGUILayout.Toggle(new GUIContent(""), Settings.highPrecision, GUILayout.Width(Area.width - 140 - 20));
        GUI.enabled = GUIEnabled;
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            //Record state for undo
            Undo.RecordObject(Settings, "High Precision");

            //Change forward depth locking
            Settings.highPrecision = highPrecision;
        }
        EditorGUILayout.Space();        
        GUILayout.EndArea();
    }

    //Pool Functionality
    private void NewPool(DynamicDecalSettings Settings)
    {
        //Cache old pool
        PoolInstance[] oldPool = Settings.pools;

        //Extend array
        Settings.pools = new PoolInstance[oldPool.Length + 1];

        //Add back our old pools
        for (int i = 0; i < oldPool.Length; i++)
        {
            Settings.pools[i] = oldPool[i];
        }

        //Add our new pool
        Settings.pools[Settings.pools.Length - 1] = new PoolInstance("New", oldPool);
    }
    private void RemoveAt(DynamicDecalSettings Settings, int Index)
    {
        //Make sure index is valid
        if (Index > 0 && Index < Settings.pools.Length)
        {
            //Cache old pool
            PoolInstance[] oldPool = Settings.pools;

            //Reduce array size
            Settings.pools = new PoolInstance[oldPool.Length - 1];

            //Add back our old pools without the element
            int j = 0;
            for (int i = 0; i < oldPool.Length; i++)
            {
                if (i != Index)
                {
                    Settings.pools[j] = oldPool[i];
                    j++;
                }
            }
        }
        else
        {
            Debug.LogError("Index Invalid");
        }
    }
    private void Swap(DynamicDecalSettings Settings, int IndexA, int IndexB)
    {
        PoolInstance temp = Settings.pools[IndexA];
        Settings.pools[IndexA] = Settings.pools[IndexB];
        Settings.pools[IndexB] = temp;
    }

    //Style Generation
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        result.hideFlags = HideFlags.HideAndDontSave;
        return result;
    }
}