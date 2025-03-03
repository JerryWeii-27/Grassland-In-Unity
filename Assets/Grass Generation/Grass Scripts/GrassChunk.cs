using UnityEngine;

public class GrassChunk
{
    // Chunk settings.
    public static int chunkSizeInMeters;
    public static int grassPerMeter;
    public static int totalInstanceCount;
    public static Bounds chunkBounds;
    public static Mesh grassHighLodMesh;
    public static Mesh grassLowLodMesh;

    public static Material staticHighLodMat,
        staticLowLodMat;

    // Terrain chunk info.
    public static float terrainXZStretch;
    public static int vertsPerSide;

    // Culling.
    public static bool cullingEnabled;
    public static Matrix4x4 VP;

    public static ComputeShader cullGrassShader;
    public bool renderChunk;
    public static int cullAccuracy;

    // This chunk's data.
    public Material highLodMat,
        lowLodMat;
    public Vector2Int chunkPos;
    public Vector3 center;

    // Grass init compute shader.
    public static ComputeShader calcGrassPos;
    public ComputeBuffer argsBuffer,
        argsBufferLowLod;
    public ComputeBuffer posBuffer,
        shortPosBuffer;

    // Grass placement settings.
    public static float yDisplaceStr;
    public static float xzDisplaceStr;
    public static float yNoiseScale;
    public static float xzNoiseScale;

    // Lod
    public static Vector2Int camChunkPos;
    public static int highLODDistance;
    public static int minLOD;
    public static float lodGradient;

    // Delta origin.
    public Vector2 deltaOrigin;

    // Random index.
    public static int[][] randIndexGrid;

    // Debug variables.
    public Vector3[] debugGrassPos;

    public GrassChunk(
        Vector2Int _chunkPos,
        Vector2 _terrainChunkPos,
        ref Vector3[] refVerts,
        ref int[] refTris,
        Vector2 _deltaOrigin
    )
    {
        // Set variables
        chunkPos = _chunkPos;
        deltaOrigin = _deltaOrigin;

        // New mesh material.
        highLodMat = new Material(staticHighLodMat);
        highLodMat.enableInstancing = true;

        lowLodMat = new Material(staticLowLodMat);
        lowLodMat.enableInstancing = true;

        // Center is the world position of the grass chunk.
        center = new Vector3(
            chunkPos.x * chunkSizeInMeters + chunkSizeInMeters / 2.0f,
            0,
            chunkPos.y * chunkSizeInMeters + chunkSizeInMeters / 2.0f
        );

        // Setting some needed variables.
        calcGrassPos.SetFloat(Shader.PropertyToID("_terrainXZStretch"), terrainXZStretch);
        calcGrassPos.SetInt(Shader.PropertyToID("_vertsPerSide"), vertsPerSide);
        calcGrassPos.SetFloat(Shader.PropertyToID("_chunkSizeInMeters"), chunkSizeInMeters);
        calcGrassPos.SetFloat(Shader.PropertyToID("_grassPerMeter"), grassPerMeter);
        calcGrassPos.SetInt(
            Shader.PropertyToID("_grassPerSide"),
            grassPerMeter * chunkSizeInMeters
        );
        calcGrassPos.SetInt(Shader.PropertyToID("_totalInstanceCount"), totalInstanceCount);

        // float[] grassChunkPos = { center.x - deltaOrigin.x, center.z - deltaOrigin.y };
        // float[] terrainChunkPos =
        // {
        //     _terrainChunkPos.x - deltaOrigin.x,
        //     _terrainChunkPos.y - deltaOrigin.y,
        // };

        // Setting grass chunk position and terrain chunk position.
        float[] grassChunkPos = { center.x, center.z };
        float[] terrainChunkPos = { _terrainChunkPos.x, _terrainChunkPos.y };

        // Debug.Log("Center: " + center + " terrainChunkPos: " + _terrainChunkPos);

        calcGrassPos.SetFloats("_grassChunkPos", grassChunkPos);
        calcGrassPos.SetFloats("_terrainWorldXZPos", terrainChunkPos);

        float[] deltaOriginFloats = { _deltaOrigin.x, _deltaOrigin.y };
        calcGrassPos.SetFloats("_deltaOriginNow", deltaOriginFloats);

        // Set terrain mesh data buffers.
        ComputeBuffer terrainVerts = new ComputeBuffer(refVerts.Length, sizeof(float) * 3);
        ComputeBuffer terrainTris = new ComputeBuffer(refTris.Length, sizeof(int));
        terrainVerts.SetData(refVerts);
        terrainTris.SetData(refTris);
        calcGrassPos.SetBuffer(0, "_terrainVerts", terrainVerts);
        calcGrassPos.SetBuffer(0, "_terrainTris", terrainTris);

        // Set random displacement variables.
        calcGrassPos.SetFloat(Shader.PropertyToID("_yDisplaceStr"), yDisplaceStr);
        calcGrassPos.SetFloat(Shader.PropertyToID("_xzDisplaceStr"), xzDisplaceStr);

        calcGrassPos.SetFloat(Shader.PropertyToID("_yNoiseScale"), yNoiseScale);
        calcGrassPos.SetFloat(Shader.PropertyToID("_xzNoiseScale"), xzNoiseScale);

        // Filling up the buffer.
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(totalInstanceCount, sizeof(int));
        randomIndexBuffer.SetData(randIndexGrid[Random.Range(0, randIndexGrid.Length)]);
        calcGrassPos.SetBuffer(0, "_randIndex", randomIndexBuffer);

        // Set grass position output buffer.
        posBuffer = new ComputeBuffer(totalInstanceCount, sizeof(float) * 3);
        calcGrassPos.SetBuffer(0, "_grassDataBuffer", posBuffer);

        // Debug.Log("Dispatching calcGrass");
        calcGrassPos.Dispatch(
            0,
            Mathf.CeilToInt(grassPerMeter * chunkSizeInMeters / 8.0f),
            Mathf.CeilToInt(grassPerMeter * chunkSizeInMeters / 8.0f),
            1
        );
        terrainVerts.Release();
        terrainTris.Release();

        shortPosBuffer = new ComputeBuffer(cullAccuracy, sizeof(float) * 3);
        Vector3[] shortPosVec = new Vector3[cullAccuracy];
        posBuffer.GetData(shortPosVec, 0, 0, cullAccuracy);
        shortPosBuffer.SetData(shortPosVec);

        highLodMat.SetBuffer("_positionBuffer", posBuffer);
        lowLodMat.SetBuffer("_positionBuffer", posBuffer);

        SetArgsBuffer();

        // debugGrassPos = new Vector3[totalInstanceCount];
        // posBuffer.GetData(debugGrassPos);

        // MonoBehaviour.Instantiate(new GameObject(), debugGrassPos[0], Quaternion.identity);
    }

    public void AdjustPosition(Vector2 newDeltaOrigin)
    {
        float[] oldDeltaOrigin = { deltaOrigin.x, deltaOrigin.y };
        float[] newDeltaOriginFloat = { newDeltaOrigin.x, newDeltaOrigin.y };

        calcGrassPos.SetFloats("_deltaOriginNow", newDeltaOriginFloat);
        calcGrassPos.SetFloats("_deltaOriginOld", oldDeltaOrigin);

        calcGrassPos.SetInt(
            Shader.PropertyToID("_grassPerSide"),
            grassPerMeter * chunkSizeInMeters
        );

        calcGrassPos.SetBuffer(1, "_grassDataBuffer", posBuffer);
        calcGrassPos.Dispatch(
            1,
            Mathf.CeilToInt(grassPerMeter * chunkSizeInMeters / 8.0f),
            Mathf.CeilToInt(grassPerMeter * chunkSizeInMeters / 8.0f),
            1
        );

        highLodMat.SetBuffer("_positionBuffer", posBuffer);
        lowLodMat.SetBuffer("_positionBuffer", posBuffer);

        deltaOrigin = newDeltaOrigin;
    }

    public void RenderGrass()
    {
        if (cullingEnabled)
        {
            CullGrass();
        }

        uint uintLOD = (uint)Mathf.FloorToInt(CalcLod());

        if (uintLOD != 1.0f)
        {
            Graphics.DrawMeshInstancedIndirect(
                grassLowLodMesh,
                0,
                lowLodMat,
                chunkBounds,
                argsBufferLowLod
            );
        }
        else
        {
            Graphics.DrawMeshInstancedIndirect(
                grassHighLodMesh,
                0,
                highLodMat,
                chunkBounds,
                argsBuffer
            );
        }
    }

    int CalcLod()
    {
        float distance = Vector2Int.Distance(chunkPos, camChunkPos);

        distance = Mathf.Clamp(distance + 1 - highLODDistance, 1, minLOD);

        distance *= lodGradient * Mathf.Pow(Mathf.Clamp(distance - 1.0f, 0, 1), 2);

        return Mathf.FloorToInt(Mathf.Clamp(distance, 1, minLOD));
    }

    void CullGrass()
    {
        cullGrassShader.SetBuffer(1, "_argBuffer", argsBuffer);
        cullGrassShader.Dispatch(1, 1, 1, 1);

        cullGrassShader.SetInt(Shader.PropertyToID("_totalInstanceCount"), totalInstanceCount);
        cullGrassShader.SetMatrix(Shader.PropertyToID("MATRIX_VP"), VP);
        cullGrassShader.SetBuffer(0, "_argBuffer", argsBuffer);
        cullGrassShader.SetBuffer(0, "_posBuffer", shortPosBuffer);
        cullGrassShader.Dispatch(0, Mathf.CeilToInt(cullAccuracy / 64.0f), 1, 1);

        cullGrassShader.SetBuffer(2, "_argBuffer", argsBuffer);
        cullGrassShader.SetBuffer(2, "_argBufferLowLod", argsBufferLowLod);
        cullGrassShader.SetInt(Shader.PropertyToID("_lodFactor"), CalcLod());
        cullGrassShader.Dispatch(2, 1, 1, 1);
        // Debug.Log(cullDispatchParam);
    }

    void SetArgsBuffer()
    {
        // High lod.
        uint[] args = { 0, 0, 0, 0, 0 };
        args[0] = grassHighLodMesh.GetIndexCount(0);
        args[1] = (uint)totalInstanceCount;
        args[2] = grassHighLodMesh.GetIndexStart(0);
        args[3] = grassHighLodMesh.GetBaseVertex(0);
        args[4] = 0;

        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Low lod.
        args[0] = grassLowLodMesh.GetIndexCount(0);
        args[1] = (uint)totalInstanceCount;
        args[2] = grassLowLodMesh.GetIndexStart(0);
        args[3] = grassLowLodMesh.GetBaseVertex(0);
        args[4] = 0;

        argsBufferLowLod = new ComputeBuffer(
            1,
            sizeof(uint) * 5,
            ComputeBufferType.IndirectArguments
        );
        argsBufferLowLod.SetData(args);
    }

    public void FreeChunk()
    {
        posBuffer.Release();
        argsBuffer.Release();
        argsBufferLowLod.Release();
    }
}
