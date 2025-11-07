using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralTerrainGeneratorV2))]
public class ProceduralTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralTerrainGeneratorV2 generator = (ProceduralTerrainGeneratorV2)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Terrain"))
        {
            if (Application.isPlaying)
            {
                generator.RegenerateTerrain();
            }
            else
            {
                // In editor mode, use the seed and current terrain type from inspector
                generator.GenerateTerrainWithSeed(generator.seed, generator.currentTerrainTypeIndex);
            }
        }

        if (GUILayout.Button("Randomize Seed"))
        {
            generator.seed = System.DateTime.Now.GetHashCode();
            if (!Application.isPlaying)
            {
                generator.GenerateTerrainWithSeed(generator.seed, generator.currentTerrainTypeIndex);
            }
        }

        // Add buttons for quick terrain type testing
        GUILayout.Space(10);
        GUILayout.Label("Quick Terrain Types:", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Grassland"))
        {
            generator.currentTerrainTypeIndex = 0;
            if (!Application.isPlaying)
            {
                generator.GenerateTerrainWithSeed(generator.seed, 0);
            }
        }
        if (GUILayout.Button("Desert"))
        {
            generator.currentTerrainTypeIndex = 1;
            if (!Application.isPlaying)
            {
                generator.GenerateTerrainWithSeed(generator.seed, 1);
            }
        }
        if (GUILayout.Button("Black Soil"))
        {
            generator.currentTerrainTypeIndex = 2;
            if (!Application.isPlaying)
            {
                generator.GenerateTerrainWithSeed(generator.seed, 2);
            }
        }
        GUILayout.EndHorizontal();
    }
}