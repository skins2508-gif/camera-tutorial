using UnityEngine;

public class HazardRespawn : MonoBehaviour
{
    [SerializeField] private Vector2 respawnPoint = new Vector2(-17f, -6.45f);

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || other.GetComponent<PlayerController>() == null)
            return;

        if (CurvedStageGame.Instance != null)
            CurvedStageGame.Instance.Respawn(body);
        else
        {
            body.linearVelocity = Vector2.zero;
            body.position = respawnPoint;
        }
    }
}
