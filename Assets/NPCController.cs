using UnityEngine;
using System.Collections;

public class NPCController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float flyHeight = 4f;
    [SerializeField] private float flyDuration = 2f; // how long it takes to fly up

    public void Interact()
    {
        StartCoroutine(FlyAndDestroy());
    }

    private IEnumerator FlyAndDestroy()
    {
        _animator.SetTrigger("Interact");
        yield return new WaitForSeconds(2f);

        _animator.SetTrigger("Fly");

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyHeight;

        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / flyDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Make sure it reaches the final height
        transform.position = targetPos;

        Destroy(gameObject);
    }
}
