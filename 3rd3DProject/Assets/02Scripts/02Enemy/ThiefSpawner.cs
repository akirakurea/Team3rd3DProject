using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ThiefSpawner : MonoBehaviour
{
    [SerializeField] private GameObject thiefPrefab;
    
    [SerializeField] private Transform spawnBase;

    private readonly List<GameObject> activeRats = new List<GameObject>();
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundStarted += HandleRoundStarted;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundStarted -= HandleRoundStarted;
    }
   
    private void HandleRoundStarted(int round, int ratsThisRound)
    {
        if(spawnRoutine != null) StopCoroutine(spawnRoutine);
        
        activeRats.Clear();
        spawnRoutine = StartCoroutine(SpawnRoundRoutine(ratsThisRound));
    }

    private IEnumerator SpawnRoundRoutine(int totalToSpawn)
    {
        var wait = new WaitForSeconds(GameManager.Instance.SpawnInterval);
        int spawned = 0;
        while(spawned < totalToSpawn)
        {
            activeRats.RemoveAll(r => r == null);

            if(activeRats.Count < GameManager.Instance.MaxActiveRats)
            {
                SpawnThief();
                spawned++;
            }
            yield return wait;
        }
    }

    private void SpawnThief()
    {
        if (thiefPrefab == null)
        {
            Debug.LogWarning("도둑 프리팹 미할당!!");
            return;
        }

        GameObject rat = spawnBase != null
      ? Instantiate(thiefPrefab, spawnBase.position, spawnBase.rotation)
      : Instantiate(thiefPrefab, transform.position, Quaternion.identity);

        activeRats.Add(rat);
    }
}
