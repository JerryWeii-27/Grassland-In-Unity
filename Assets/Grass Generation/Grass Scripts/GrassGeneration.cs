using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class GrassGeneration : MonoBehaviour
{
    /*
    1. Figure out where the character is in terms of grass chunk.
    2. Iterate through the chunks near the character that needs generation.
    3. If the chunk already exists, great.
    4. If it has not been generated yet, generate it.
    5. Swap out any chunks that are not needed in the list or dictionary.

    Chunk creation:
    1. Calculate position buffer of each grass in the chunk.
    2. Set position buffer to material.
    */

    struct VertsAndTris
    {
        public Vector3[] verts;
        public int[] tris;
    };

    [Header("Serialize Fields")]
    public GameObject terrainChunk; // Used for debugging.
    public GameObject marker; // Used for debugging.
    public Transform cameraPos;
    public ComputeShader calcGrassPos;
    public ComputeShader cullGrassShader;
    public float terrainXZStretch;

    [Header("Chunk Settings")]
    public bool cullingEnabled = false;
    public Mesh grassHighLodMesh,
        grassLowLodMesh;
    public Material grassHighLodMat,
        grassLowLodMat;
    public int totalInstanceCount; // Will be calculated in Start().
    public int chunkSizeInMeters;
    public int grassPerMeter;
    public int viewDistance; // View distance in chunks.
    public Vector3 boundsSize;
    public int randomIndexGridCnt = 3;

    public int cullAccuracy = 1;

    [Header("Grass Placement Settings")]
    public float xzDisplaceStr;
    public float yDisplaceStr;

    // Noise scale should not be an integer. Might not work if it is.
    public float xzNoiseScale;
    public float yNoiseScale;

    [Header("Grass LOD Settings")]
    public Vector2Int camChunkPos;
    public int highLODDistance = 1;
    public int minLOD = 100;
    public float lodGradient = 1;

    Dictionary<Vector2Int, GrassChunk> grassChunksDict = new();
    Dictionary<Vector2Int, GameObject> terrainChunkDict = new();
    HashSet<Vector2Int> wantedKeys = new HashSet<Vector2Int>();

    [Header("Current Status")]
    Matrix4x4 P;
    Matrix4x4 V;
    Matrix4x4 VP = new();

    public Vector2Int lastChunk = new(-114514, 54520);
    public Vector2Int curGrassChunk = new();

    private GrassChunk debugGrassChunk;
    private ComputeBuffer debugPosBuffer;
    private ComputeBuffer debugArgsBuffer;
    private Vector3[] debugPosVec;
    private uint[] debugArgs = { 0, 0, 0, 0, 0 };
    private Bounds debugBounds = new Bounds(new(0, 0, 0), new(50000, 50000, 50000));

    // Caches the meshes' data.
    private Dictionary<Vector2Int, VertsAndTris> meshCache = new();

    // Calculate terrain chunk.
    EndlessWorld endlessWorld;

    private void Start()
    {
        UnsafeUtility.SetLeakDetectionMode(
            Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace
        );

        grassHighLodMat.enableInstancing = true;
        grassLowLodMat.enableInstancing = true;

        GrassChunk.grassHighLodMesh = grassHighLodMesh;
        GrassChunk.grassLowLodMesh = grassLowLodMesh;
        GrassChunk.staticHighLodMat = grassHighLodMat;
        GrassChunk.staticLowLodMat = grassLowLodMat;

        // terrainChunkDict.Add(new(0, 0), terrainChunk); // For debug.

        totalInstanceCount = grassPerMeter * chunkSizeInMeters * grassPerMeter * chunkSizeInMeters;
        GrassChunk.totalInstanceCount = totalInstanceCount;

        GrassChunk.chunkSizeInMeters = chunkSizeInMeters;

        GrassChunk.chunkBounds = new Bounds(Vector3.zero, boundsSize);

        GrassChunk.grassPerMeter = grassPerMeter;

        GrassChunk.calcGrassPos = calcGrassPos;
        GrassChunk.cullGrassShader = cullGrassShader;
        GrassChunk.cullAccuracy = cullAccuracy;

        GrassChunk.cullingEnabled = cullingEnabled;

        GrassChunk.yDisplaceStr = yDisplaceStr;
        GrassChunk.xzDisplaceStr = xzDisplaceStr;
        GrassChunk.yNoiseScale = yNoiseScale;
        GrassChunk.xzNoiseScale = xzNoiseScale;

        // LOD
        GrassChunk.highLODDistance = highLODDistance;
        GrassChunk.minLOD = minLOD;
        GrassChunk.lodGradient = lodGradient;

        // Debug functions.
        // DebugSimple();
        // DebugGrassGen();
        endlessWorld = FindObjectOfType<EndlessWorld>();

        GrassChunk.terrainXZStretch = endlessWorld.worldScale;
        GrassChunk.vertsPerSide = endlessWorld.size;

        GrassChunk.randIndexGrid = new int[randomIndexGridCnt][];
        for (int j = 0; j < randomIndexGridCnt; j++)
        {
            GrassChunk.randIndexGrid[j] = new int[totalInstanceCount];
            for (int i = 0; i < totalInstanceCount; i++)
            {
                GrassChunk.randIndexGrid[j][i] = i;
            }

            for (int i = totalInstanceCount - 1; i >= 0; i--)
            {
                int k = Random.Range(0, i + 1);

                int temp = GrassChunk.randIndexGrid[j][i];
                GrassChunk.randIndexGrid[j][i] = GrassChunk.randIndexGrid[j][k];
                GrassChunk.randIndexGrid[j][k] = temp;
            }
        }

        return;
    }

    void DebugSimple()
    {
        // totalInstanceCount = 10;
        SetDebugArgsBuffer();

        Vector3[] pos = new Vector3[totalInstanceCount];
        for (int i = 0; i < pos.Length; i++)
        {
            pos[i] = new Vector3(i * 1.5f, 5 * Mathf.Sin(i / 5.0f), 5 * Mathf.Cos(i / 5.0f));
            print("pos[i]: " + pos[i]);
        }

        debugPosBuffer = new ComputeBuffer(totalInstanceCount, sizeof(float) * 3);
        debugPosBuffer.SetData(pos);
        grassHighLodMat.SetBuffer("_positionBuffer", debugPosBuffer);
        grassHighLodMat.enableInstancing = true;
    }

    private void Update()
    {
        P = Camera.main.projectionMatrix;
        V = Camera.main.transform.worldToLocalMatrix;
        VP = P * V;
        // print("VP: " + VP);

        if (!endlessWorld.dictFull)
        {
            return;
        }
        // print("Rendering grass.");
        // print(endlessWorld.terrainChunkDict[new(0, 0)].terrainObject.transform.lossyScale);

        // Entering new chunk.
        curGrassChunk = CalcGrassChunkPos(
            new Vector2(cameraPos.position.x, cameraPos.position.z) - endlessWorld.deltaOrigin
        );
        GrassChunk.camChunkPos = curGrassChunk;
        // print("curGrassChunk: " + curGrassChunk);

        if (curGrassChunk != lastChunk)
        {
            LookForNewChunks();
            CreateNewChunks();

            lastChunk = curGrassChunk;
        }

        RenderGrassChunks();
        // DebugRenderGrass();
    }

    private void RenderGrassChunks()
    {
        if (grassChunksDict.Count == 0)
        {
            return;
        }

        // print("cnt: " + grassChunksDict.Count);
        foreach (GrassChunk curChunk in grassChunksDict.Values)
        {
            GrassChunk.VP = VP;
            curChunk.RenderGrass();

            // Instantiate(marker, curChunk.debugGrassPos[0], Quaternion.identity);
        }

        return;
    }

    public void UpdateGrassPositions()
    {
        foreach (GrassChunk curChunk in grassChunksDict.Values)
        {
            curChunk.AdjustPosition(endlessWorld.deltaOrigin);

            // Instantiate(marker, curChunk.debugGrassPos[0], Quaternion.identity);
        }
    }

    private void LookForNewChunks()
    {
        wantedKeys.Clear();
        for (int y = -viewDistance; y <= viewDistance; y++)
        {
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                if (x * x + y * y > viewDistance * viewDistance)
                {
                    continue;
                }

                Vector2Int now = new Vector2Int(x, y) + curGrassChunk;
                wantedKeys.Add(now);
            }
        }

        foreach (Vector2Int key in grassChunksDict.Keys.ToList())
        {
            if (!wantedKeys.Contains(key))
            {
                grassChunksDict.Remove(key);
            }
            else
            {
                wantedKeys.Remove(key);
            }
        }
    }

    private void CreateNewChunks()
    {
        // Create new chunks
        foreach (Vector2Int now in wantedKeys)
        {
            Vector2 center = new Vector2(
                now.x * chunkSizeInMeters + chunkSizeInMeters / 2.0f,
                now.y * chunkSizeInMeters + chunkSizeInMeters / 2.0f
            );

            // Get terrain mesh data.
            Vector2Int curTerrainChunkKey = -endlessWorld.CalculateChunkPos(center);
            // print("curTerrainChunk: " + curTerrainChunk + " Center " + center);

            VertsAndTris vertsAndTris;

            if (meshCache.ContainsKey(curTerrainChunkKey))
            {
                vertsAndTris = meshCache[curTerrainChunkKey];
            }
            else
            {
                Mesh m = endlessWorld.terrainChunkDict[curTerrainChunkKey].lodMeshes[0];
                vertsAndTris.verts = m.vertices;
                vertsAndTris.tris = m.triangles;

                meshCache.Add(curTerrainChunkKey, vertsAndTris);
            }

            Vector3 terrainPos = endlessWorld
                .terrainChunkDict[curTerrainChunkKey]
                .terrainObject
                .transform
                .position;
            // print("terrainPos: " + terrainPos);

            GrassChunk newGrassChunk = new GrassChunk(
                now,
                new Vector2(
                    terrainPos.x - endlessWorld.deltaOrigin.x,
                    terrainPos.z - endlessWorld.deltaOrigin.y
                ),
                ref vertsAndTris.verts,
                ref vertsAndTris.tris,
                endlessWorld.deltaOrigin
            );

            grassChunksDict.TryAdd(now, newGrassChunk);

            // for (
            //     int i = 0;
            //     i < grassPerMeter * chunkSizeInMeters * grassPerMeter * chunkSizeInMeters;
            //     i += 40
            // )
            // {
            //     Instantiate(marker, newGrassChunk.debugGrassPos[i], Quaternion.identity);
            //     // print(i + " grassChunk.debugGrassPos[i]: " + newGrassChunk.debugGrassPos[i]);
            // }
        }
    }

    void DebugGrassGen()
    {
        print("Debuggin grass generation...");

        VertsAndTris vertsAndTris;
        Mesh m = terrainChunk.GetComponent<MeshFilter>().mesh;
        vertsAndTris.verts = m.vertices;
        vertsAndTris.tris = m.triangles;

        Vector2Int now = new(0, 0);
        Bounds chunkBounds = GrassChunk.chunkBounds;
        GrassChunk grassChunk =
            new(
                now,
                new(0, 0),
                ref vertsAndTris.verts,
                ref vertsAndTris.tris,
                endlessWorld.deltaOrigin
            );

        for (
            int i = 0;
            i < grassPerMeter * chunkSizeInMeters * grassPerMeter * chunkSizeInMeters;
            i += 1
        )
        {
            if (chunkBounds.Contains(grassChunk.debugGrassPos[i]))
            {
                Instantiate(
                    marker,
                    grassChunk.debugGrassPos[i] + new Vector3(0, 1, 0),
                    Quaternion.identity
                );
            }
            print(i + " grassChunk.debugGrassPos[i]: " + grassChunk.debugGrassPos[i]);
            // grassChunk.RenderGrass();
        }

        debugGrassChunk = grassChunk;
        grassChunk.highLodMat.enableInstancing = true;

        SetDebugArgsBuffer();
        grassHighLodMat = grassChunk.highLodMat;
        print("debugBounds: " + debugBounds);
        print("debugBounds: " + debugBounds);

        grassHighLodMat.enableInstancing = true;
    }

    void SetDebugArgsBuffer()
    {
        debugArgs[0] = grassHighLodMesh.GetIndexCount(0);
        debugArgs[1] = (uint)totalInstanceCount;
        debugArgs[2] = grassHighLodMesh.GetIndexStart(0);
        debugArgs[3] = grassHighLodMesh.GetBaseVertex(0);
        debugArgs[4] = 0;

        debugArgsBuffer = new ComputeBuffer(
            1,
            sizeof(uint) * 5,
            ComputeBufferType.IndirectArguments
        );
        debugArgsBuffer.SetData(debugArgs);
    }

    private void DebugRenderGrass()
    {
        Graphics.DrawMeshInstancedIndirect(
            grassHighLodMesh,
            0,
            grassHighLodMat,
            GrassChunk.chunkBounds,
            debugArgsBuffer
        );
    }

    private void VisualizeChunk(Vector2Int now)
    {
        Material newMat = new Material(marker.GetComponent<MeshRenderer>().sharedMaterial);

        newMat.color = GetChunkColor(now.x, now.y);
        Instantiate(marker, grassChunksDict[now].center, Quaternion.identity)
            .GetComponent<MeshRenderer>()
            .material = newMat;
    }

    private Color GetChunkColor(int x, int y)
    {
        // Generate distinct colors using the chunk coordinates
        float hue = Mathf.Abs((x * 0.618f + y * 0.618f) % 1.0f); // Using golden ratio to ensure distinct colors
        float saturation = 0.5f;
        float value = 0.8f;
        return Color.HSVToRGB(hue, saturation, value);
    }

    public Vector2Int CalcGrassChunkPos(Vector2 pos)
    {
        // Forgot why this is here, might be useful?
        // if (pos.x == 0)
        // {
        //     pos.x += 0.001f;
        // }
        // if (pos.y == 0)
        // {
        //     pos.y += 0.001f;
        // }

        int x = (int)Mathf.Floor(pos.x / chunkSizeInMeters);
        int y = (int)Mathf.Floor(pos.y / chunkSizeInMeters);

        return new Vector2Int(x, y);
    }

    private void OnDisable()
    {
        print("Releasing buffers.");
        if (debugArgsBuffer != null)
        {
            debugArgsBuffer.Release();
        }
        if (debugPosBuffer != null)
        {
            debugPosBuffer.Release();
        }
        foreach (GrassChunk grassChunk in grassChunksDict.Values)
        {
            grassChunk.FreeChunk();
        }
    }
}
