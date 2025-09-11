//using UnityEngine;
//using Photon.Pun;
//using UnityEngine.UI;
//using ExitGames.Client.Photon;
//using Photon.Realtime;

//public class CreateAndJoinRoom : MonoBehaviourPunCallbacks
//{
//    public InputField createInput;
//    public InputField joinInput;
//    public Text missionDisplay;
//    public Button createButton;
//    public Button joinButton;
//    public Button readyButton;
//    public Text readyStatus;

//    public GameObject[] treePrefabs;
//    public GameObject[] housePrefabs;

//    string[] missions = new string[] {
//        "Capture the Outpost",
//        "Escort the Convoy",
//        "Destroy the Barricade",
//        "Activate the Relay Tower",
//        "Defend the Base",
//        "Fetch an Artifact"
//    };

//    string[][] missionSteps = new string[][] {
//        new string[] { "Reach the outpost", "Eliminate enemies", "Secure the flag" },
//        new string[] { "Locate the convoy", "Defend against attackers", "Escort to destination" },
//        new string[] { "Find the barricade", "Plant explosives", "Destroy the barricade" },
//        new string[] { "Reach the tower", "Activate systems", "Defend until active" },
//        new string[] { "Setup defenses", "Fend off attackers", "Hold until reinforcements" },
//        new string[] { "Locate the artifact", "Retrieve it", "Bring it to extraction point" }
//    };

//    string selectedMission;
//    string[] selectedSteps;
//    private bool isReady = false;

//    void Start()
//    {
//        createButton.onClick.AddListener(CreateRoom);
//        joinButton.onClick.AddListener(OnJoinedRoom);
//        readyButton.onClick.AddListener(ToggleReadyStatus);
//    }

//    public void CreateRoom()
//    {
//        if (!string.IsNullOrEmpty(createInput.text))
//        {
//            int missionIndex = Random.Range(0, missions.Length);
//            selectedMission = missions[missionIndex];
//            selectedSteps = missionSteps[missionIndex];

//            // Store terrain seed and mission in room properties
//            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable
//            {
//                { "Mission", selectedMission },
//                { "Steps", selectedSteps },
//                { "TerrainSeed", Random.Range(0, 10000) }
//            };

//            RoomOptions roomOptions = new RoomOptions
//            {
//                MaxPlayers = 4,
//                CustomRoomProperties = roomProperties,
//                CustomRoomPropertiesForLobby = new string[] { "Mission" }
//            };

//            PhotonNetwork.CreateRoom(createInput.text, roomOptions);
//        }
//    }

//    public override void OnJoinedRoom()
//    {
//        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("Mission", out object mission) &&
//            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("Steps", out object steps) &&
//            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("TerrainSeed", out object seed))
//        {
//            string missionName = mission.ToString();
//            string[] missionSteps = steps as string[];
//            int terrainSeed = (int)seed;

//            Debug.Log("Mission: " + missionName);
//            Debug.Log("Terrain Seed: " + terrainSeed);

            
//            GenerateTerrain(terrainSeed);
//            SendMissionToQuestHandler(missionName, missionSteps);
//        }

//        PhotonNetwork.LoadLevel("Main");
//    }


//    private void GenerateTerrain(int seed)
//    {
//        float[,] noiseMap = Generate.GenerationNoiseMap(100, 100, 0.1f); 
//        if (treePrefabs == null || treePrefabs.Length == 0)
//        {
//            Debug.LogError("Tree prefabs are not assigned or empty!");
//        }

//        if (housePrefabs == null || housePrefabs.Length == 0)
//        {
//            Debug.LogError("House prefabs are not assigned or empty!");
//        }

//        for (int x = 0; x < 100; x++)
//        {
//            for (int z = 0; z < 100; z++)
//            {
//                Vector3 position = new Vector3(x * 10f, 0f, z * 10f);
              
//                    Generate.PlaceObject(treePrefabs[Random.Range(0, treePrefabs.Length)], position);
//                    Generate.PlaceObject(housePrefabs[Random.Range(0, housePrefabs.Length)], position + new Vector3(5, 0, 5)); // Offset houses slightly
                

//            }
//        }
//    }

//    private void SendMissionToQuestHandler(string missionName, string[] missionSteps)
//    {
//        GameObject questHandlerObj = GameObject.Find("QuestHandler");
//        if (questHandlerObj != null)
//        {
//            QuestHandler questHandler = questHandlerObj.GetComponent<QuestHandler>();
//            questHandler.InitializeQuest(missionName, missionSteps);
//        }
//    }

//    private void ToggleReadyStatus()
//    {
//        isReady = !isReady;
//        readyStatus.text = "Ready: " + (isReady ? "Yes" : "No");
//    }
//}
