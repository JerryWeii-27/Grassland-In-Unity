using System.Collections.Generic;
using UnityEngine;

public class EndlessWorld : MonoBehaviour
{
    public bool masterSwitch;

    // Camera position info.
    public Transform cameraPos;
    public static Vector2Int cameraChunkPos;
    private Vector2 lastChunkPos = new Vector2(float.MinValue, float.MinValue);

    // Chunk size and scale info.
    public int size = 0;
    public float worldScale = 1.0001f,
        heightScale = 1;
    public Vector2 chunkPosTestCoords = Vector2.zero;

    [Header("View Distance Settings")]
    public int renderDistance;
    public int levelsOfDetail;
    public int lodLimit;
    public int descendMultiplier;

    // Chunks waiting to be processed.
    private Queue<Vector2Int> chunkQueue = new Queue<Vector2Int>();

    // Chunk generation funcitons.
    private ChunkGeneration chunkGeneration;

    // Generated chunks.
    public int dictMaxSize = 0;
    public bool dictFull = false;
    public Dictionary<Vector2Int, TerrainChunk> terrainChunkDict =
        new Dictionary<Vector2Int, TerrainChunk>();
    public Queue<TerrainChunk> waitingForUpdate = new Queue<TerrainChunk>();
    Queue<Vector2Int> waitingForRemoval = new Queue<Vector2Int>();

    Queue<Vector2Int> lastUpdate = new Queue<Vector2Int>();
    Queue<Vector2Int> notGenerated = new Queue<Vector2Int>();

    public Queue<Vector2Int> leftOvers = new Queue<Vector2Int>();

    // Chunk Folder.
    public Transform chunkParent;

    // Chunk material.
    public Material chunkMaterial;

    // Destroy Self.
    public int additionalMemoryDistance;

    // Floating origin.
    public Vector2 deltaOrigin;

    void Start()
    {
        cameraPos = GameObject.FindGameObjectWithTag("MainCamera").transform;

        chunkGeneration = FindObjectOfType<ChunkGeneration>();

        worldScale = (worldScale > 1) ? worldScale : 1.0001f;

        for (int x = -renderDistance; x < renderDistance; x++)
        {
            for (int y = -renderDistance; y < renderDistance; y++)
            {
                if (x * x + y * y >= renderDistance * renderDistance)
                {
                    continue;
                }
                dictMaxSize++;
            }
        }
    }

    void Update()
    {
        // Chunk pos debugging.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Debugging chunk pos");
            DebugChunkPos();
        }

        if (terrainChunkDict.Count >= dictMaxSize)
        {
            dictFull = true;
        }

        // foreach(var x in terrainChunkDict.Keys)
        // {
        //     Debug.Log(x + " "+ terrainChunkDict[x].chunkPos + " " + terrainChunkDict[x].terrainObject.name);
        // }

        if (!masterSwitch)
        {
            return;
        }

        cameraChunkPos = CalculateChunkPos(
            new Vector2(cameraPos.position.x, cameraPos.position.z) - deltaOrigin
        );

        //UpdateBorderNormals(cameraChunkPos);

        UpdateChunksFromQueue();

        DestroyOutOfRange();

        if (cameraChunkPos == lastChunkPos)
        {
            return;
        }
        Debug.Log("cam pos:" + cameraChunkPos);

        lastChunkPos = cameraChunkPos;

        //Go through chunks updated last cycle
        ClearLastUpdate();

        //Insert all chunks within circle to queue
        EnqueueChunks();

        while (chunkQueue.Count > 0)
        {
            Vector2Int chunkNow = chunkQueue.Dequeue();
            int lod = (int)(1.0f * Mathf.Sqrt(chunkNow.x * chunkNow.x + chunkNow.y * chunkNow.y));

            chunkNow = new Vector2Int(
                (int)(chunkNow.x - cameraChunkPos.x),
                (int)(chunkNow.y - cameraChunkPos.y)
            );
            lastUpdate.Enqueue(chunkNow);

            bool exists = terrainChunkDict.ContainsKey(chunkNow);
            if (exists)
            {
                RetrieveChunk(chunkNow, lod);
            }
            else
            {
                //Debug.Log(chunkNow);
                chunkGeneration.GenerateChunk(chunkNow);
            }
        }
    }

    private void LateUpdate()
    {
        ClearLeftOvers();
    }

    private void ClearLastUpdate()
    {
        while (lastUpdate.Count > 0)
        {
            //terrainChunkDict[lastUpdate.Dequeue()].terrainObject.SetActive(false);
            Vector2Int now = lastUpdate.Dequeue();
            if (terrainChunkDict.ContainsKey(now))
            {
                terrainChunkDict[now].terrainObject.SetActive(false);
            }
            else
            {
                notGenerated.Enqueue(now);
            }
        }

        //while (notGenerated.Count > 0)
        //{
        //	lastUpdate.Enqueue(notGenerated.Dequeue());
        //}
    }

    private void DestroyOutOfRange()
    {
        foreach (KeyValuePair<Vector2Int, TerrainChunk> entry in terrainChunkDict)
        {
            if (
                Vector2.Distance(-entry.Key, cameraChunkPos)
                > additionalMemoryDistance + renderDistance + 0.1f
            )
            {
                Destroy(entry.Value.terrainObject);
                entry.Value.lodMeshes = new Mesh[0];

                waitingForRemoval.Enqueue(entry.Key);
            }
        }

        while (waitingForRemoval.Count > 0)
        {
            terrainChunkDict.Remove(waitingForRemoval.Dequeue());
        }
    }

    // Called in Update()
    private void UpdateChunksFromQueue()
    {
        while (waitingForUpdate.Count > 0)
        {
            TerrainChunk current = waitingForUpdate.Dequeue();
            float curX = current.chunkPos.x + cameraChunkPos.x;
            float curY = current.chunkPos.y + cameraChunkPos.y;

            int lod = (int)(
                1.0f * Mathf.Sqrt(curX * curX + curY * curY) // renderDistance * levelsOfDetail
            );
            lod = Mathf.Clamp(lod, 0, levelsOfDetail - 1);

            // Debug.Log("Current lod counts: " + current.lodMeshes.Length);

            if (lod > lodLimit)
            {
                lod = levelsOfDetail - 1;
            }

            current.UpdateChunk(lod, lodLimit);

            //Debug.Log(lod);
            //Generate details
            //if (lod == 0)
            //{
            //	//Debug.Log(current.chunkPos);
            //	detailsGenerator.UpdateDetails(current.terrainObject);
            //}

            // Adding new chunk to dictionary.
            terrainChunkDict.TryAdd(current.chunkPos, current);
            lastUpdate.Enqueue(current.chunkPos);
        }
    }

    private void RetrieveChunk(Vector2Int pos, int lod)
    {
        terrainChunkDict[pos].terrainObject.SetActive(true);
        terrainChunkDict[pos].UpdateChunk(lod, lodLimit);
        lastUpdate.Enqueue(pos);
    }

    private void EnqueueChunks()
    {
        for (int x = -renderDistance; x < renderDistance; x++)
        {
            for (int y = -renderDistance; y < renderDistance; y++)
            {
                if (x * x + y * y >= renderDistance * renderDistance)
                {
                    continue;
                }
                chunkQueue.Enqueue(new Vector2Int(x, y));
            }
        }
    }

    private void ClearLeftOvers()
    {
        while (leftOvers.Count > 0)
        {
            Vector2 now = leftOvers.Dequeue();
            terrainChunkDict.Remove(CalculateChunkPos(now - deltaOrigin));
        }
    }

    private void DebugChunkPos()
    {
        foreach (var chunk in terrainChunkDict.Values)
        {
            chunk.debugChunkPosMode = !chunk.debugChunkPosMode;
            chunk.DebugChunkPos();
        }
        Debug.Log(
            "Test coords: "
                + chunkPosTestCoords.ToString()
                + "\nChunk pos: "
                + CalculateChunkPos(chunkPosTestCoords)
        );
    }

    // Calculate chunkPos functions
    public Vector2Int CalculateChunkPos(Vector2 pos)
    {
        float x = pos.x,
            z = pos.y;
        return new Vector2Int(SmartDetermine(x), SmartDetermine(z));
    }

    public int SmartRound(float x)
    {
        return (int)(Mathf.Sign(x) * Mathf.Ceil(Mathf.Abs(x)));
    }

    private int SmartDetermine(float x)
    {
        // Somehow this is not needed.
        if (x > 0)
        {
            return SmartRound(
                Mathf.Max(x - 0.5f * (size - 1) * worldScale, 0) / (size - 1) / worldScale
            );
        }
        else if (x < 0)
        {
            return SmartRound(
                Mathf.Min(x + 0.5f * (size - 1) * worldScale, 0) / (size - 1) / worldScale
            );
        }
        else
        {
            return 0;
        }
    }
}
