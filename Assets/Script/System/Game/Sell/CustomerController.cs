using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CustomerController : MonoBehaviour
{
    public float moveSpeed = 3f;

    public UnityEvent onBought;
    public UnityEvent onLeftEmpty;

    public IEnumerator MoveTo(Vector3 target)
    {
        while ((transform.position - target).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    public void NotifyBought() => onBought?.Invoke();
    public void NotifyLeftEmpty() => onLeftEmpty?.Invoke();
}