//using Photon.Pun;
//using Photon.Realtime;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//namespace Starfall
//{
//    public class Deployment : MonoBehaviourPunCallbacks
//    {
//        public GameObject planetPrefab;
//        public Transform spawnArea;
//        public Button createRoomButton;

//        private List<GameObject> planets = new List<GameObject>();
//        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
//        [SerializeField] private Material[] materials;

//        void Start()
//        {
//            createRoomButton.onClick.AddListener(CreateRandomRoom);
//            materials = Resources.LoadAll<Material>("Materials");
//            PhotonNetwork.ConnectUsingSettings();
//        }

//        public override void OnConnectedToMaster()
//        {
//            Debug.Log("Connected to Master Server");
//            PhotonNetwork.JoinLobby();
//            PhotonNetwork.LoadLevel("Game");
//        }

//        public override void OnJoinedLobby()
//        {
//            Debug.Log("Joined Lobby");
//            PhotonNetwork.LoadLevel("Game");
//        }

//        void CreateRandomRoom()
//        {
//            string roomName = GenerateRandomName();
//            RoomOptions options = new RoomOptions { MaxPlayers = 4 };
//            PhotonNetwork.CreateRoom(roomName, options);
//            CreatePlanet(roomName);
//        }

//        void CreatePlanet(string roomName)
//        {
//            Vector3 randomPosition = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), 0);
//            GameObject planet = Instantiate(planetPrefab, randomPosition, Quaternion.identity, spawnArea);

//            if (materials.Length > 0)
//            {
//                Material randomMat = materials[Random.Range(0, materials.Length)];
//                planet.GetComponent<Renderer>().material = randomMat;
//            }

//            planet.name = roomName;
//            planets.Add(planet);

//            planet.AddComponent<BoxCollider>();
//            PlanetRoom planetRoom = planet.AddComponent<PlanetRoom>();
//            planetRoom.roomName = roomName;
//        }

//        string GenerateRandomName()
//        {
//            return "Planet" + alphabet[Random.Range(0, alphabet.Length)] + alphabet[Random.Range(0, alphabet.Length)] + Random.Range(100, 999);
//        }
//    }

//    public class PlanetRoom : MonoBehaviour
//    {
//        public string roomName;

//        void OnMouseDown()
//        {
//            if (PhotonNetwork.IsConnected)
//            {
//                //PhotonNetwork.JoinRoom(roomName);
//                PhotonNetwork.JoinLobby();
//                //Debug.Log($"Joining Room: {roomName}");
//                PhotonNetwork.LoadLevel("Game");
//            }
//        }
//    }
//}