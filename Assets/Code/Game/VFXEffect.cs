using UnityEngine;

public class VFXEffect : MonoBehaviour
{
    [SerializeField] private string[] clipNames = { "FireBall" };
    private void OnEnable()
    {
        if (clipNames.Length == 1)
        {
            int randomIndex = Random.Range(1, 4);
            AudioManager.instance.PlaySingleClipVariants(clipNames[0], transform.position, randomIndex);
        }
        else if (clipNames.Length > 1)
        {
            int randomIndex = Random.Range(0, clipNames.Length);
            AudioManager.instance.PlaySFX(clipNames[randomIndex], transform.position);
        }

    }

}
