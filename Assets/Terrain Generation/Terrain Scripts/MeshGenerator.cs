using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    private int size;
    private EndlessWorld endlessWorld;
    private float heightScale,
        worldScale;

    private int levelsOfDetail,
        lodLimit;

    Queue<MeshDataThread> meshQueue = new Queue<MeshDataThread>();

    private void Start()
    {
        endlessWorld = FindObjectOfType<EndlessWorld>();
        size = endlessWorld.size;
        heightScale = endlessWorld.heightScale;
        worldScale = endlessWorld.worldScale;
        levelsOfDetail = endlessWorld.levelsOfDetail;
        lodLimit = endlessWorld.lodLimit;

        // Debug.Log("Endless world lod: " + levelsOfDetail);
    }

    public void RequestMeshData(Action<MeshData[], Vector2Int> callback, MapData mapData)
    {
        TerrainMeshGeneration(callback, mapData); // Callback is OnReceiveMeshData(MeshData[] meshDatas, Vector2 pos).
    }

    private async void TerrainMeshGeneration(
        Action<MeshData[], Vector2Int> callback,
        MapData mapData
    )
    {
        float[,] heightMap = mapData.heightMap;
        Dictionary<Vector2, float> borderMap = mapData.borderMap;

        var result = await Task.Run(() =>
        {
            MeshData[] lodMeshes = new MeshData[
                (lodLimit + 2 > levelsOfDetail) ? levelsOfDetail : (lodLimit + 2)
            ]; // [0, 1, 2, max lod - 1]. 2 + 2 = 4

            float topLeftX = (size - 1) / -2f;
            float topLeftZ = (size - 1) / 2f;

            int[] borderTriangles = new int[(size + 1) * (size + 1) * 6];
            Vector3[] borderVertices = new Vector3[(size + 2) * (size + 2)];

            int meshIndex = 0;
            // Can use compute shader here.
            for (int lod = 0; lod < levelsOfDetail; lod++)
            {
                if (lod > lodLimit && lod != levelsOfDetail - 1)
                {
                    continue;
                }

                Vector3[] vertices = new Vector3[size * size];
                Vector2[] uvs = new Vector2[size * size];
                int[] triangles = new int[(size - 1) * (size - 1) * 6];

                int meshSimplicationIncrement = (int)Mathf.Pow(2, lod);
                int verticesPerLine = (size - 1) / meshSimplicationIncrement + 1;

                int begin = (lod == 0) ? -1 : 0,
                    end = (lod == 0) ? size + 1 : size;

                for (int i = 0, j = 0, x = begin; x < end; x += meshSimplicationIncrement)
                {
                    for (int y = begin; y < end; y += meshSimplicationIncrement)
                    {
                        if (x < 0 || x == size || y < 0 || y == size)
                        {
                            borderVertices[j] = new Vector3(
                                topLeftX + x,
                                borderMap[new Vector2(size - 1 - x, y)] * heightScale,
                                topLeftZ - y
                            );
                            j++;
                            continue;
                        }
                        else
                        {
                            borderVertices[j] = new Vector3(
                                topLeftX + x,
                                heightMap[size - 1 - x, y] * heightScale,
                                topLeftZ - y
                            );
                            j++;
                        }

                        vertices[i] = new Vector3(
                            topLeftX + x,
                            heightMap[size - 1 - x, y] * heightScale,
                            topLeftZ - y
                        );
                        uvs[i] = new Vector2(-x / (float)size, y / (float)size);
                        i++;
                    }
                }

                int tries = 0,
                    verts = 0,
                    bTries = 0,
                    bVerts = 0;
                for (int x = begin; x < end - 1; x += meshSimplicationIncrement)
                {
                    for (int y = begin; y < end - 1; y += meshSimplicationIncrement)
                    {
                        //triangles[tries] = verts;
                        //triangles[tries + 1] = verts + size + 1;
                        //triangles[tries + 2] = verts + size;
                        //triangles[tries + 3] = verts;
                        //triangles[tries + 4] = verts + 1;
                        //triangles[tries + 5] = verts + size + 1;

                        borderTriangles[bTries] = bVerts;
                        borderTriangles[bTries] = bVerts;
                        borderTriangles[bTries + 1] = bVerts + verticesPerLine + 2;
                        borderTriangles[bTries + 2] = bVerts + verticesPerLine + 2 + 1;
                        borderTriangles[bTries + 3] = bVerts;
                        borderTriangles[bTries + 4] = bVerts + verticesPerLine + 2 + 1;
                        borderTriangles[bTries + 5] = bVerts + 1;

                        bTries += 6;
                        bVerts++;

                        if (x < 0 || x >= size - 1 || y < 0 || y >= size - 1)
                        {
                            continue;
                        }

                        triangles[tries] = verts;
                        triangles[tries + 1] = verts + verticesPerLine;
                        triangles[tries + 2] = verts + verticesPerLine + 1;
                        triangles[tries + 3] = verts;
                        triangles[tries + 4] = verts + verticesPerLine + 1;
                        triangles[tries + 5] = verts + 1;

                        if (verts + verticesPerLine + 1 >= 16641)
                        {
                            // Debug.Log(tries + " " + new Vector2(x, y));
                        }

                        tries += 6;
                        verts++;
                    }
                    bVerts++;
                    if (x >= 0 && x < size - 1)
                    {
                        verts++;
                    }
                }

                lodMeshes[meshIndex++] = new MeshData(
                    vertices,
                    triangles,
                    uvs,
                    size > 256,
                    worldScale,
                    borderVertices,
                    borderTriangles
                );
            }
            // Debug.Log("var resutl lod meshes length: " + lodMeshes.Length);
            // Debug.Log("Intended levels of details:" + levelsOfDetail);
            return lodMeshes;
        });

        // Debug.Log("Done with filling up lodMeshes");

        lock (meshQueue)
        {
            meshQueue.Enqueue(new MeshDataThread(result, mapData.position, callback));
        }
    }

    private void Update()
    {
        if (meshQueue.Count > 0)
        {
            MeshDataThread now = meshQueue.Dequeue();
            now.CallBack();
        }

        Terrain terrain = new Terrain();
        TerrainData terrainData = new TerrainData();
    }
}

public class MeshDataThread
{
    MeshData[] meshData;
    Vector2Int pos;
    Action<MeshData[], Vector2Int> callback;

    public MeshDataThread(
        MeshData[] _meshData,
        Vector2Int _pos,
        Action<MeshData[], Vector2Int> _callback
    )
    {
        meshData = _meshData;
        callback = _callback;
        pos = _pos;
    }

    public void CallBack()
    {
        callback(meshData, pos);
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    bool IndexFormat32 = false;

    Vector3[] borderNormals;

    public MeshData(
        Vector3[] _vertices,
        int[] _triangels,
        Vector2[] _uvs,
        bool _IndexFormat32,
        float worldScale,
        Vector3[] _borderVertices,
        int[] _borderTriangles
    )
    {
        vertices = _vertices;
        triangles = _triangels;
        uvs = _uvs;
        IndexFormat32 = _IndexFormat32;

        borderNormals = CalculateBorderNormals(_borderVertices, _borderTriangles);
    }

    Vector3[] CalculateBorderNormals(Vector3[] borderVertices, int[] borderTriangles)
    {
        Vector3[] vertexNormals = new Vector3[borderVertices.Length];
        int triangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(
                vertexIndexA,
                vertexIndexB,
                vertexIndexC,
                borderVertices
            );
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC, Vector3[] vertsList)
    {
        Vector3 pointA = vertsList[indexA];
        Vector3 pointB = vertsList[indexB];
        Vector3 pointC = vertsList[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh GenerateMesh(int lod)
    {
        Mesh mesh = new Mesh();

        if (IndexFormat32)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();

        if (lod == 0)
        {
            int mapSize = (int)MathF.Sqrt(vertices.Length);
            ApplyBorderNormals(mesh, vertices, borderNormals, mapSize);
        }

        //mesh.RecalculateTangents();
        return mesh;
    }

    void ApplyBorderNormals(Mesh mesh, Vector3[] vertices, Vector3[] borderNormals, int mapSize)
    {
        Vector3[] newNormals = mesh.normals;
        int vert = 0,
            bVert = 0;
        for (int i = -1; i <= mapSize; i++)
        {
            for (int j = -1; j <= mapSize; j++)
            {
                if (
                    (i == 0 || i == mapSize - 1 || j == 0 || j == mapSize - 1)
                    && i != -1
                    && j != -1
                    && j != mapSize
                    && i != mapSize
                )
                {
                    if (vert == newNormals.Length)
                    {
                        Debug.Log("vert out of range:  " + vert + "   " + new Vector2(i, j));
                    }
                    newNormals[vert] = borderNormals[bVert];
                }

                bVert++;
                if (i < 0 || j < 0 || i == mapSize || j == mapSize)
                {
                    continue;
                }
                vert++;
            }
        }
        mesh.normals = newNormals;
    }
}
