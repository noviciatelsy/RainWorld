using UnityEngine;
using System.Collections;

public class JumpDoor : MonoBehaviour
{
    public int requiredCount = 3;

    private int count = 0;

    private float timer;

    public float resetTime = 10f;

    [SerializeField] private float openDistance = 1.5f;
    [SerializeField] private float openDuration = 0.3f;

    private Transform doorRoot;

    private bool isOpen = false;

    private void Awake()
    {
        doorRoot = transform.parent;
    }

    void Update()
    {
        if (count > 0)
        {
            timer += Time.deltaTime;

            if (timer > resetTime)
            {
                count = 0;
                timer = 0;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        count++;
        timer = 0;

        if (count >= requiredCount)
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        if (isOpen) return;

        isOpen = true;
        Debug.Log(doorRoot.name);
        StartCoroutine(OpenDoorCoroutine());
    }

    IEnumerator OpenDoorCoroutine()
    {
        Vector3 startPos = doorRoot.position;
        Vector3 targetPos = startPos + Vector3.up * 2f;

        float timer = 0;

        while (timer < openDuration)
        {
            timer += Time.deltaTime;

            doorRoot.position =
                Vector3.Lerp(
                    startPos,
                    targetPos,
                    timer / openDuration);

            yield return null;
        }

        doorRoot.position = targetPos;
    }

}