using System.Collections.Generic;
using UnityEngine;

public class ChunkGeneration : MonoBehaviour
{
    private int size;
    private float worldScale;

    private MapGenerator mapGenerator;
    private MeshGenerator meshGenerator;
    private EndlessWorld endlessWorld;
    private int lodLimit;

    private Material mat;

    Queue<TerrainChunk> saveQueue = new Queue<TerrainChunk>();

    public void Start()
    {
        size = FindObjectOfType<EndlessWorld>().size;
        worldScale = FindObjectOfType<EndlessWorld>().worldScale;
        mat = FindObjectOfType<EndlessWorld>().chunkMaterial;

        mapGenerator = FindObjectOfType<MapGenerator>();
        meshGenerator = FindObjectOfType<MeshGenerator>();
        endlessWorld = FindObjectOfType<EndlessWorld>();

        TerrainChunk.lodLimit = endlessWorld.lodLimit;
        TerrainChunk.levelsOfDetail = endlessWorld.levelsOfDetail;
        TerrainChunk.descendMultiplier = endlessWorld.descendMultiplier;
    }

    public void GenerateChunk(Vector2 pos)
    {
        mapGenerator.RequestMapData(OnReceiveMapData, new float[2] { pos.x, pos.y });
    }

    public void OnReceiveMapData(MapData mapData)
    {
        meshGenerator.RequestMeshData(OnReceiveMeshData, mapData);
    }

    public void OnReceiveMeshData(MeshData[] meshDatas, Vector2Int pos)
    {
        //Gameobject created here.
        TerrainChunk terrainChunk = new TerrainChunk(meshDatas, pos, size, worldScale);
        //Debug.Log("Chunk created at " + pos);
        terrainChunk.terrainObject.GetComponent<MeshRenderer>().material = mat;

        endlessWorld.waitingForUpdate.Enqueue(terrainChunk);

        saveQueue.Enqueue(terrainChunk);
    }
}

public class TerrainChunk
{
    public static int lodLimit,
        levelsOfDetail,
        descendMultiplier;

    public GameObject terrainObject;
    public Mesh[] lodMeshes;

    public Vector3[] vertices;
    public int[] triangles;

    public Vector3[,] lowLODBorderVerts;

    public Vector2Int chunkPos;
    public int currentLevel = -1;

    public bool debugChunkPosMode;

    private TextMesh debugText;

    public TerrainChunk(MeshData[] meshData, Vector2Int _chunkPos, int size, float worldScale)
    {
        terrainObject = new GameObject(
            "T(" + ((int)_chunkPos.x).ToString() + ", " + ((int)_chunkPos.y).ToString() + ")",
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(MeshCollider),
            typeof(Rigidbody)
        );

        chunkPos = _chunkPos;

        Vector3 objectPos = new Vector3(-chunkPos.x, 0, -chunkPos.y) * (size - 1) * worldScale;

        Transform chunkParent = GameObject.FindGameObjectWithTag("TerrainParent").transform;
        terrainObject.transform.parent = chunkParent;
        terrainObject.transform.localPosition = objectPos;
        terrainObject.transform.localScale = new Vector3(worldScale, 1, worldScale);
        terrainObject.layer = LayerMask.NameToLayer("Landscape");
        terrainObject.GetComponent<MeshFilter>().mesh = new Mesh();

        lodMeshes = new Mesh[meshData.Length];

        Vector3[] lod0Norm = new Vector3[size * size];
        for (int i = 0; i < meshData.Length; i++)
        {
            lodMeshes[i] = meshData[i].GenerateMesh(i);
            if (i == 0)
            {
                lod0Norm = lodMeshes[0].normals;
                // Debug.Log("Size: " + size + " lodMeshes[0].normals: " + lodMeshes[0].normals.Length);
            }

            if (i != 0)
            {
                Vector3[] normArr = lodMeshes[i].normals;
                int trueLod = (i <= lodLimit) ? i : levelsOfDetail - 1;

                // Always the case.
                normArr[0] = lod0Norm[0];

                int increment = (int)Mathf.Pow(2, trueLod);
                int smallSize = (size - 1) / increment + 1;

                for (int j = 0; j < smallSize; j++)
                {
                    normArr[j] = lod0Norm[increment * j];
                    normArr[j * smallSize] = lod0Norm[increment * j * size];
                    normArr[smallSize - 1 + j * smallSize] = lod0Norm[
                        size - 1 + increment * j * size
                    ];
                    normArr[smallSize * smallSize - smallSize + j] = lod0Norm[
                        size * size - size + increment * j
                    ];
                }

                normArr[smallSize - 1] = lod0Norm[size - 1];
                normArr[smallSize * smallSize - smallSize] = lod0Norm[size * size - size];
                normArr[smallSize * smallSize - 1] = lod0Norm[size * size - 1];

                lodMeshes[i].normals = normArr;
            }
        }
        vertices = lodMeshes[0].vertices;
        triangles = lodMeshes[0].triangles;

        // lod0Normals = new Vector3[0];

        Rigidbody rb = terrainObject.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.isKinematic = true;

        terrainObject.GetComponent<MeshCollider>().enabled = true;

        // Create a TextMesh for debugging
        CreateDebugText();
    }

    private void CreateDebugText()
    {
        // Create a new GameObject for the TextMesh
        GameObject textObject = new GameObject("DebugText");
        textObject.transform.parent = terrainObject.transform;
        textObject.transform.localPosition = Vector3.up * 1000; // Position above the chunk

        // Add and configure the TextMesh component
        debugText = textObject.AddComponent<TextMesh>();
        debugText.text = chunkPos.x.ToString() + ", " + chunkPos.y.ToString();
        debugText.anchor = TextAnchor.MiddleCenter;
        debugText.alignment = TextAlignment.Center;
        debugText.fontSize = 500;
        debugText.color = Color.red;

        textObject.transform.localScale = new Vector3(1.6f, 1.6f, 1);
        textObject.transform.rotation = Quaternion.Euler(90, 0, 0);

        debugText.gameObject.SetActive(false);
    }

    public void UpdateChunk(int newLevel, int lodLimit)
    {
        // Vector2Int cameraChunkPos = -EndlessWorld.cameraChunkPos;
        // Vector2Int direction = chunkPos - cameraChunkPos;

        // if (direction.x == 1 && direction.y == 0)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk on the X axis. Direction: Right");
        // }
        // else if (direction.x == -1 && direction.y == 0)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk on the X axis. Direction: Left");
        // }
        // else if (direction.y == 1 && direction.x == 0)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk on the Y axis. Direction: Up");
        // }
        // else if (direction.y == -1 && direction.x == 0)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk on the Y axis. Direction: Down");
        // }
        // else if (direction.x == 1 && direction.y == 1)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk diagonally. Direction: Right-Up");
        // }
        // else if (direction.x == 1 && direction.y == -1)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk diagonally. Direction: Right-Down");
        // }
        // else if (direction.x == -1 && direction.y == 1)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk diagonally. Direction: Left-Up");
        // }
        // else if (direction.x == -1 && direction.y == -1)
        // {
        //     Debug.Log("Chunk is adjacent to the camera chunk diagonally. Direction: Left-Down");
        // }

        if (newLevel == 1)
        {
            newLevel = 0;
        }

        if (newLevel == currentLevel)
        {
            return;
        }

        // Update mesh for new LOD
        currentLevel = (newLevel > lodLimit) ? lodLimit + 1 : newLevel;
        terrainObject.GetComponent<MeshFilter>().mesh = lodMeshes[currentLevel];

        if (currentLevel <= 1 && lodMeshes.Length > 1)
        {
            terrainObject.GetComponent<MeshCollider>().enabled = true;
            terrainObject.GetComponent<MeshCollider>().sharedMesh = lodMeshes[0];
        }
        else if (terrainObject.GetComponent<MeshCollider>().enabled == true)
        {
            terrainObject.GetComponent<MeshCollider>().enabled = false;
        }

        // if (newLevel == 0 && lod0Normals.Length != 0)
        // {
        //     terrainObject.GetComponent<MeshFilter>().mesh.normals = lod0Normals;
        // }

        // Decrease chunk height according to distance?
        newLevel = (newLevel == 1) ? 0 : newLevel;

        terrainObject.transform.position = new Vector3(
            terrainObject.transform.position.x,
            -newLevel * descendMultiplier,
            terrainObject.transform.position.z
        );
    }

    public void DebugChunkPos()
    {
        if (debugChunkPosMode)
        {
            if (debugText != null)
            {
                debugText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (debugText != null)
            {
                debugText.gameObject.SetActive(false);
            }
        }
    }
}
