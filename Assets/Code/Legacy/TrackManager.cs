using UnityEngine;
using System.Collections.Generic;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance;

    [Header("Track Checkpoint List (Ordered)")]
    public List<Transform> checkpoints = new List<Transform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
