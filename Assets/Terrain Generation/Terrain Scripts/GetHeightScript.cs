using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetHeightScript : MonoBehaviour
{
    private EndlessWorld endlessWorld;
    private int size;
    private float worldScale;

    public bool debugMode;

    private void Start()
    {
        endlessWorld = FindObjectOfType<EndlessWorld>();
        size = endlessWorld.size;
        worldScale = endlessWorld.worldScale;
    }

    // If object in moving objects folder, pass in localposition instead of position
    public float GetHeight(Vector2 xzPos)
    {
        Vector2Int chunkPos = endlessWorld.CalculateChunkPos(xzPos);
        Vector2 floatChunkPos = chunkPos;

        // Position relative to chunk the grass is on top of.
        Vector2 localPos2D = (xzPos - floatChunkPos * (size - 1) * worldScale) / worldScale;

        // Somehow this is needed. I don't really get this.
        chunkPos *= -1;

        // Get the triangles and vertices of the chunk the grass is on top of.
        ref int[] triangles = ref endlessWorld.terrainChunkDict[chunkPos].triangles;
        ref Vector3[] vertices = ref endlessWorld.terrainChunkDict[chunkPos].vertices;

        // x index and y index of the quad.
        int xPos = (int)(localPos2D.x + (size - 1) / 2),
            yPos = (int)(-localPos2D.y + (size - 1) / 2);

        int quadIndex = xPos * (size - 1) + yPos,
            b = xPos - yPos;

        localPos2D.y *= -1;
        int triIndex = (localPos2D.y <= localPos2D.x - b) ? quadIndex * 2 : quadIndex * 2 + 1;

        Vector3 v1 = vertices[triangles[triIndex * 3 + 0]] * worldScale;
        Vector3 v2 = vertices[triangles[triIndex * 3 + 1]] * worldScale;
        Vector3 v3 = vertices[triangles[triIndex * 3 + 2]] * worldScale;

        v1.y /= worldScale;
        v2.y /= worldScale;
        v3.y /= worldScale;

        Vector3 sideAB = v2 - v1;
        Vector3 sideAC = v3 - v1;

        Vector3 triNormal = Vector3.Cross(sideAB, sideAC).normalized;

        float d = -Vector3.Dot(triNormal, v1);

        float pointHeight =
            (
                -triNormal.x * localPos2D.x * worldScale
                - triNormal.z * -localPos2D.y * worldScale
                - d
            ) / triNormal.y;

        return pointHeight;

        // if (debugMode && x == 0 && y == 0)
        // {
        //     Debug.Log("Chunkpos: " + chunkPos + "  local pos to chunk  " + localPos2D);

        //     Debug.Log(xPos + " " + yPos + " s  " + triIndex);

        //     Debug.Log(pointHeight + "  Corresponding Pos: " + new Vector2(localPos2D.x * endlessWorld.worldScale, -localPos2D.y * endlessWorld.worldScale));
        //     Debug.Log(player.transform.localPosition);
        // }
    }
}
