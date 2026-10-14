using UnityEngine;

public class StageGoal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
            CurvedStageGame.Instance?.ClearStage();
    }
}
