// FloatingOrigin.cs
// Written by Peter Stirling
// 11 November 2010
// Uploaded to Unify Community Wiki on 11 November 2010
// Updated to Unity 5.x particle system by Tony Lovell 14 January, 2016
// fix to ensure ALL particles get moved by Tony Lovell 8 September, 2016
// URL: http://wiki.unity3d.com/index.php/Floating_Origin
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class FloatingOrigin : MonoBehaviour
{
    public float threshold = 4000.0f;
    private EndlessWorld endlessWorld;

    //private ChunkGeneration chunkGeneration;
    public Transform movingObjectsFolder;
    public Transform targetTransform;

    //public GameObject player;

    private void Start()
    {
        endlessWorld = FindObjectOfType<EndlessWorld>();
    }

    void Update()
    {
        Vector3 cameraPosition = targetTransform.position;
        cameraPosition.y = 0f;

        if (cameraPosition.magnitude > threshold)
        {
            Vector3 originDeltaVec3 = Vector3.zero - cameraPosition;
            Vector2 originDelta = new Vector2(originDeltaVec3.x, originDeltaVec3.z);
            endlessWorld.deltaOrigin += originDelta;

            //for (int z = 0; z < SceneManager.sceneCount; z++)
            //{
            //	foreach (GameObject g in SceneManager.GetSceneAt(z).GetRootGameObjects())
            //	{
            //		g.transform.position -= cameraPosition;
            //	}
            //}

            movingObjectsFolder.position -= cameraPosition;

            Debug.Log("recentering, origin delta += " + originDelta);

            // Might not work. I don't know.
            FindObjectOfType<GrassGeneration>()
                .UpdateGrassPositions();
        }
    }
}
