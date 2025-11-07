using UnityEngine;
using Photon.Pun;

[ExecuteInEditMode]
public class ProceduralTerrainGeneratorV2 : MonoBehaviourPunCallbacks
{
    [Header("Terrain Settings")]
    public int terrainWidth = 512;
    public int terrainLength = 512;
    public int terrainHeight = 50;

    [Header("Noise Settings")]
    public float scale = 80f;
    public float offsetX = 0f;
    public float offsetY = 0f;

    [Header("Island Settings")]
    [Range(0f, 1f)] public float islandSize = 0.5f;
    [Range(0f, 1f)] public float edgeFalloff = 0.5f;

    [Header("Seed Settings")]
    [Tooltip("Leave 0 to auto-randomize each play")]
    public int seed = 0;
    public bool randomizeOnPlay = true;

    [Header("Noise Variation")]
    public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Terrain Type System")]
    public TerrainType[] terrainTypes;
    public int currentTerrainTypeIndex = 0;

    [Header("Terrain Layers (Textures)")]
    public TerrainLayer waterLayer;
    public TerrainLayer sandLayer;
    public TerrainLayer grassLayer;
    public TerrainLayer rockLayer;
    public TerrainLayer snowLayer;

    [System.Serializable]
    public class TerrainType
    {
        public string name = "New Terrain Type";
        [Range(0f, 1f)] public float sandHeight = 0.3f;
        [Range(0f, 1f)] public float grassHeight = 0.45f;
        [Range(0f, 1f)] public float rockHeight = 0.6f;
        [Range(0f, 1f)] public float snowHeight = 0.8f;
        public Color primaryColor = Color.green;
        public float primaryColorStrength = 0.7f;
    }

    private Terrain terrain;
    private int currentSeed;
    private bool isSeedSynchronized = false;
    private int synchronizedTerrainTypeIndex = 0;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found!");
            return;
        }

        // Initialize default terrain types if none exist
        if (terrainTypes == null || terrainTypes.Length == 0)
        {
            InitializeDefaultTerrainTypes();
        }

        // Only generate terrain if we're in a networked game
        if (PhotonNetwork.IsConnected)
        {
            InitializeSynchronizedTerrain();
        }
        else
        {
            // Fallback for single-player/testing
            GenerateTerrainWithSeed(System.DateTime.Now.GetHashCode(), currentTerrainTypeIndex);
        }
    }

    void InitializeDefaultTerrainTypes()
    {
        terrainTypes = new TerrainType[3];

        // Grass Type
        terrainTypes[0] = new TerrainType()
        {
            name = "Grassland",
            sandHeight = 0.25f,
            grassHeight = 0.4f,
            rockHeight = 0.65f,
            snowHeight = 0.85f,
            primaryColor = new Color(0.2f, 0.6f, 0.2f),
            primaryColorStrength = 0.8f
        };

        // Desert/Sand Type
        terrainTypes[1] = new TerrainType()
        {
            name = "Desert",
            sandHeight = 0.6f,  // More sand area
            grassHeight = 0.75f,
            rockHeight = 0.85f,
            snowHeight = 0.95f, // Very little snow
            primaryColor = new Color(0.9f, 0.8f, 0.5f),
            primaryColorStrength = 0.9f
        };

        // Black Soil Type
        terrainTypes[2] = new TerrainType()
        {
            name = "Black Soil",
            sandHeight = 0.2f,
            grassHeight = 0.35f,
            rockHeight = 0.6f,
            snowHeight = 0.8f,
            primaryColor = new Color(0.1f, 0.1f, 0.1f),
            primaryColorStrength = 0.6f
        };
    }

    void InitializeSynchronizedTerrain()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Master client generates and broadcasts the seed and terrain type
            if (randomizeOnPlay && seed == 0)
            {
                currentSeed = System.DateTime.Now.GetHashCode();
            }
            else
            {
                currentSeed = seed;
            }

            // Get terrain type from room properties if available
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("TerrainType"))
            {
                synchronizedTerrainTypeIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["TerrainType"];
            }
            else
            {
                synchronizedTerrainTypeIndex = currentTerrainTypeIndex;
            }

            // Store seed and terrain type in room properties for synchronization
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "TerrainSeed", currentSeed },
                { "TerrainType", synchronizedTerrainTypeIndex }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Debug.Log($"Master client generated: Seed={currentSeed}, TerrainType={synchronizedTerrainTypeIndex}");
            GenerateTerrainWithSeed(currentSeed, synchronizedTerrainTypeIndex);
        }
        else
        {
            // Non-master clients wait for data from room properties
            Debug.Log("Waiting for terrain data from master client...");

            // Check if data already exists in room properties
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("TerrainSeed") &&
                PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("TerrainType"))
            {
                currentSeed = (int)PhotonNetwork.CurrentRoom.CustomProperties["TerrainSeed"];
                synchronizedTerrainTypeIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["TerrainType"];
                Debug.Log($"Found existing terrain data: Seed={currentSeed}, TerrainType={synchronizedTerrainTypeIndex}");
                GenerateTerrainWithSeed(currentSeed, synchronizedTerrainTypeIndex);
                isSeedSynchronized = true;
            }
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Non-master clients receive the data when master client sets it
        if (!PhotonNetwork.IsMasterClient)
        {
            bool shouldRegenerate = false;

            if (propertiesThatChanged.ContainsKey("TerrainSeed"))
            {
                currentSeed = (int)propertiesThatChanged["TerrainSeed"];
                shouldRegenerate = true;
            }

            if (propertiesThatChanged.ContainsKey("TerrainType"))
            {
                synchronizedTerrainTypeIndex = (int)propertiesThatChanged["TerrainType"];
                shouldRegenerate = true;
            }

            if (shouldRegenerate)
            {
                Debug.Log($"Received terrain data: Seed={currentSeed}, TerrainType={synchronizedTerrainTypeIndex}");
                GenerateTerrainWithSeed(currentSeed, synchronizedTerrainTypeIndex);
                isSeedSynchronized = true;
            }
        }
    }

    void OnValidate()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();

        if (terrain != null && Application.isPlaying == false)
        {
            // Use the seed directly in editor
            currentSeed = seed;
            GenerateTerrainWithSeed(currentSeed, currentTerrainTypeIndex);
        }
    }

    public void GenerateTerrainWithSeed(int terrainSeed, int terrainTypeIndex)
    {
        if (terrain == null)
        {
            terrain = GetComponent<Terrain>();
            if (terrain == null) return;
        }

        // Validate terrain type index
        if (terrainTypes == null || terrainTypes.Length == 0)
        {
            InitializeDefaultTerrainTypes();
        }

        terrainTypeIndex = Mathf.Clamp(terrainTypeIndex, 0, terrainTypes.Length - 1);
        TerrainType currentType = terrainTypes[terrainTypeIndex];

        Debug.Log($"Generating {currentType.name} terrain with seed: {terrainSeed}");

        // Apply dimensions
        TerrainData data = terrain.terrainData;
        if (data == null) return;

        data.heightmapResolution = 513;
        data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        float[,] heights = GenerateHeights(data.heightmapResolution, terrainSeed);
        data.SetHeights(0, 0, heights);

        // Apply textures based on height and terrain type
        ApplyTerrainTextures(data, heights, currentType);
    }

    // Public method to set terrain type (call this from RoomOptionsManager)
    public void SetTerrainType(int terrainTypeIndex)
    {
        if (terrainTypes == null || terrainTypes.Length == 0) return;

        terrainTypeIndex = Mathf.Clamp(terrainTypeIndex, 0, terrainTypes.Length - 1);
        currentTerrainTypeIndex = terrainTypeIndex;

        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            // Broadcast the new terrain type to all clients
            synchronizedTerrainTypeIndex = terrainTypeIndex;
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "TerrainType", synchronizedTerrainTypeIndex }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            // Single player mode - regenerate immediately
            GenerateTerrainWithSeed(currentSeed, terrainTypeIndex);
        }
    }

    // Public method to force regeneration
    public void RegenerateTerrain()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            // Generate new seed and broadcast
            currentSeed = System.DateTime.Now.GetHashCode();
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "TerrainSeed", currentSeed },
                { "TerrainType", synchronizedTerrainTypeIndex }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            // Single player mode
            GenerateTerrainWithSeed(System.DateTime.Now.GetHashCode(), currentTerrainTypeIndex);
        }
    }

    float[,] GenerateHeights(int resolution, int terrainSeed)
    {
        // Same height generation as before...
        float[,] heights = new float[resolution, resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxDistance = resolution * islandSize;

        Random.InitState(terrainSeed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetXOctave = Random.Range(-10000f, 10000f);
            float offsetYOctave = Random.Range(-10000f, 10000f);
            octaveOffsets[i] = new Vector2(offsetXOctave, offsetYOctave);
        }

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseValue = 0f;
                float maxAmplitude = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float xCoord = (float)x / resolution * scale * frequency + offsetX + octaveOffsets[o].x;
                    float yCoord = (float)y / resolution * scale * frequency + offsetY + octaveOffsets[o].y;

                    noiseValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
                    maxAmplitude += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseValue /= maxAmplitude;

                float distance = Vector2.Distance(new Vector2(x, y), center);
                float mask = Mathf.Clamp01(1f - Mathf.Pow(distance / maxDistance, edgeFalloff));

                float rawHeight = noiseValue * mask;
                float step = 0.5f;
                float quantized = Mathf.Round(rawHeight / step) * step;

                heights[x, y] = quantized;
            }
        }

        return heights;
    }

    void ApplyTerrainTextures(TerrainData data, float[,] heights, TerrainType terrainType)
    {
        int resolution = data.alphamapResolution;
        float[,,] alphamaps = new float[resolution, resolution, 5];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float heightX = (x / (float)resolution) * (heights.GetLength(0) - 1);
                float heightY = (y / (float)resolution) * (heights.GetLength(1) - 1);

                int hx = Mathf.Clamp((int)heightX, 0, heights.GetLength(0) - 1);
                int hy = Mathf.Clamp((int)heightY, 0, heights.GetLength(1) - 1);

                float height = heights[hy, hx];

                // Use terrain type specific height thresholds
                float water = height < terrainType.sandHeight ? 1f : 0f;
                float sand = (height >= terrainType.sandHeight && height < terrainType.grassHeight) ? 1f : 0f;
                float grass = (height >= terrainType.grassHeight && height < terrainType.rockHeight) ? 1f : 0f;
                float rock = (height >= terrainType.rockHeight && height < terrainType.snowHeight) ? 1f : 0f;
                float snow = height >= terrainType.snowHeight ? 1f : 0f;

                // Apply primary color strength to emphasize the main terrain type
                if (terrainType.primaryColorStrength > 0)
                {
                    // Boost the primary terrain type based on the strength setting
                    if (terrainType.name.Contains("Grass"))
                    {
                        grass *= (1f + terrainType.primaryColorStrength);
                    }
                    else if (terrainType.name.Contains("Desert"))
                    {
                        sand *= (1f + terrainType.primaryColorStrength);
                    }
                    else if (terrainType.name.Contains("Black Soil"))
                    {
                        // For black soil, we might want to darken the grass areas
                        grass *= (1f + terrainType.primaryColorStrength * 0.5f);
                    }
                }

                // Smooth transitions between layers
                if (height >= terrainType.sandHeight - 0.05f && height < terrainType.sandHeight + 0.05f)
                {
                    float blend = (height - (terrainType.sandHeight - 0.05f)) / 0.1f;
                    water = 1f - blend;
                    sand = blend;
                }
                if (height >= terrainType.grassHeight - 0.05f && height < terrainType.grassHeight + 0.05f)
                {
                    float blend = (height - (terrainType.grassHeight - 0.05f)) / 0.1f;
                    sand = 1f - blend;
                    grass = blend;
                }
                if (height >= terrainType.rockHeight - 0.05f && height < terrainType.rockHeight + 0.05f)
                {
                    float blend = (height - (terrainType.rockHeight - 0.05f)) / 0.1f;
                    grass = 1f - blend;
                    rock = blend;
                }
                if (height >= terrainType.snowHeight - 0.05f && height < terrainType.snowHeight + 0.05f)
                {
                    float blend = (height - (terrainType.snowHeight - 0.05f)) / 0.1f;
                    rock = 1f - blend;
                    snow = blend;
                }

                // Normalize and assign
                float total = water + sand + grass + rock + snow;
                alphamaps[y, x, 0] = water / total;
                alphamaps[y, x, 1] = sand / total;
                alphamaps[y, x, 2] = grass / total;
                alphamaps[y, x, 3] = rock / total;
                alphamaps[y, x, 4] = snow / total;
            }
        }

        data.SetAlphamaps(0, 0, alphamaps);
    }

    // For debugging - display current terrain info
    void OnGUI()
    {
        if (Application.isPlaying && PhotonNetwork.IsConnected)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 250));
            GUILayout.Label($"Terrain Seed: {currentSeed}");
            GUILayout.Label($"Terrain Type: {terrainTypes[synchronizedTerrainTypeIndex].name}");
            GUILayout.Label($"Is Master Client: {PhotonNetwork.IsMasterClient}");
            GUILayout.Label($"Seed Synchronized: {isSeedSynchronized}");

            if (GUILayout.Button("Regenerate Terrain") && PhotonNetwork.IsMasterClient)
            {
                RegenerateTerrain();
            }

            GUILayout.EndArea();
        }
    }
}