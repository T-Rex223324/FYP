using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    // A dictionary to hold queues of our hidden objects
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public GameObject SpawnFromPool(GameObject prefab, Vector3 position)
    {
        string tag = prefab.name;

        if (!poolDictionary.ContainsKey(tag))
        {
            poolDictionary[tag] = new Queue<GameObject>();
        }

        GameObject objectToSpawn = null;

        // Find the first hidden object in the pool and reuse it!
        if (poolDictionary[tag].Count > 0)
        {
            objectToSpawn = poolDictionary[tag].Dequeue();
            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
        }
        else
        {
            // If the pool is empty, we must instantiate a new one
            objectToSpawn = Instantiate(prefab, position, Quaternion.identity);

            // CRITICAL TRICK: We remove the "(Clone)" text Unity adds to the name.
            // This ensures it goes back into the correct pool later!
            objectToSpawn.name = tag;
        }

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false); // Hide it instead of destroying it!

        string tag = obj.name;
        if (!poolDictionary.ContainsKey(tag))
        {
            poolDictionary[tag] = new Queue<GameObject>();
        }

        poolDictionary[tag].Enqueue(obj);
    }
}