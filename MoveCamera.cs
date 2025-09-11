using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;

    void Update() {
        if (player != null) {
            transform.position = player.position;
        }
    }
}
