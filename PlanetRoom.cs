//using Photon.Pun;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class PlanetRoom : MonoBehaviourPunCallbacks
//{
//    public string roomName;

//    void OnMouseDown()
//    {
//        if (PhotonNetwork.IsConnected)
//        {
//            Debug.Log($"Joining Room: {roomName}");
//            PhotonNetwork.JoinRoom(roomName);
//        }
//    }

//    public override void OnJoinedRoom()
//    {
//        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
//        PhotonNetwork.LoadLevel("Game");
//    }
//}
