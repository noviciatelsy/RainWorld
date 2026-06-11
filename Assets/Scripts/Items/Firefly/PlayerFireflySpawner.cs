using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFireflySpawner : MonoBehaviour
{
    [SerializeField] private Transform fireflySpawnPosition;
    [SerializeField] private Fly2D fireflyPrefab;

    public void SpawnFireFly()
    {
        if(fireflyPrefab != null)
        {
            Instantiate(fireflyPrefab.gameObject,fireflySpawnPosition.position,Quaternion.identity);
        }
    }
}
