using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("추적")]
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField] private float followSmoothTime = 0.15f;

    [Header("지형 감지")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayStartHeight = 3f;
    [SerializeField] private float rayDistance = 8f;
    [SerializeField] private float sampleDistance = 2f;

    [Header("회전")]
    [SerializeField] private float angleInfluence = 1f;
    [SerializeField] private float maxRotation = 35f;
    [SerializeField] private float rotationSmoothTime = 0.25f;

    private Vector3 followVelocity;
    private float currentAngle;
    private float angleVelocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
        RotateAlongTerrain();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime
        );
    }

    private void RotateAlongTerrain()
    {
        float direction = 1f;

        if (targetRigidbody != null &&
            Mathf.Abs(targetRigidbody.linearVelocity.x) > 0.05f)
        {
            direction = Mathf.Sign(targetRigidbody.linearVelocity.x);
        }

        Vector2 center = target.position;
        Vector2 backPoint =
            center - Vector2.right * direction * sampleDistance;
        Vector2 frontPoint =
            center + Vector2.right * direction * sampleDistance;

        bool backFound = TryGetGroundHeight(backPoint, out Vector2 backHit);
        bool frontFound = TryGetGroundHeight(frontPoint, out Vector2 frontHit);

        float desiredAngle = 0f;

        if (backFound && frontFound)
        {
            Vector2 terrainDirection = frontHit - backHit;

            desiredAngle = Mathf.Atan2(
                terrainDirection.y,
                Mathf.Abs(terrainDirection.x)
            ) * Mathf.Rad2Deg;

            desiredAngle *= direction;
            desiredAngle *= angleInfluence;
            desiredAngle = Mathf.Clamp(
                desiredAngle,
                -maxRotation,
                maxRotation
            );
        }

        currentAngle = Mathf.SmoothDampAngle(
            currentAngle,
            desiredAngle,
            ref angleVelocity,
            rotationSmoothTime
        );

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private bool TryGetGroundHeight(
        Vector2 samplePoint,
        out Vector2 hitPoint)
    {
        Vector2 origin =
            samplePoint + Vector2.up * rayStartHeight;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            rayDistance,
            groundLayer
        );

        if (hit.collider != null)
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Vector2 center = target.position;

        DrawSampleRay(center - Vector2.right * sampleDistance);
        DrawSampleRay(center + Vector2.right * sampleDistance);
    }

    private void DrawSampleRay(Vector2 samplePoint)
    {
        Vector2 origin =
            samplePoint + Vector2.up * rayStartHeight;

        Gizmos.DrawLine(
            origin,
            origin + Vector2.down * rayDistance
        );
    }
}