using System.Net;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName= "ScriptableObjects/ItemData")]
public class ItemData : ScriptableObject
{
    public Sprite itemIcon;
    public string RPCName;

    [Header("Parameters (Optional)")]
    public float range; 
    public float power;
}