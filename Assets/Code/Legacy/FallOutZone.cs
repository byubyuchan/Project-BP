//using UnityEngine;
//using Photon.Pun;

//public class FallOutZone : MonoBehaviour
//{
//    private void OnTriggerEnter(Collider other)
//    {
//        PhotonView pv = other.GetComponent<PhotonView>();

//        if (pv != null && pv.IsMine)
//        {
//            PlayerRaceProgress progress = other.GetComponent<PlayerRaceProgress>();

//            if (progress != null)
//            {
//                progress.Respawn();
//            }
//        }
//    }
//}
