using UnityEngine;

/// <summary>
/// Spinning, bobbing collectible. Picked up when the player gets close enough.
/// </summary>
public class Coin : MonoBehaviour
{
    public float spinSpeed = 120f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    public float pickupRadius = 1.3f;

    Transform player;
    Vector3 basePosition;
    float bobOffset;

    public void Init(Transform playerTransform)
    {
        player = playerTransform;
        basePosition = transform.position;
        bobOffset = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;

        if (player && (player.position - transform.position).sqrMagnitude < pickupRadius * pickupRadius)
            GameManager.Instance.CollectCoin(this);
    }
}
