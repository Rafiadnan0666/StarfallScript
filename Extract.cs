using UnityEngine;
using System.Collections;
using System.Data.SqlTypes;

public class Extract : MonoBehaviour
{
    public TMPro.TextMeshProUGUI countDown;
    public GameObject planePrefab; // Assign your plane prefab in the inspector
    public Transform PlaneSpawn;
    public Transform target;

    private IEnumerator Countdown()
    {
        int timeLeft = 10; // Example countdown time
        while (timeLeft > 0)
        {
            countDown.text = "Extracting in " + timeLeft + " seconds";
            yield return new WaitForSeconds(1);
            timeLeft--;
        }
        countDown.text = "Extraction complete!";
        // Here you can add code to play a sound or trigger any other actions after the countdown
    }

    //after countdown is complete spawn plane at plane spawn position then go down to target
    private void SpawnPlane()
    {
        GameObject plane = Instantiate(planePrefab, PlaneSpawn.position, Quaternion.identity);
        StartCoroutine(MovePlaneToTarget(plane));
    }


    private IEnumerator MovePlaneToTarget(GameObject plane)
    {
        // add some variation to the plane like rotation so it is not always facing the same direction like make it more naturel landing 
        // and also make it move down to the target position

        Vector3 startPosition = plane.transform.position;
        Vector3 targetPosition = target.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float startTime = Time.time;
        while (Vector3.Distance(plane.transform.position, targetPosition) > 0.1f)
        {
            float distCovered = (Time.time - startTime) * 5; // Speed of the plane
            float fractionOfJourney = distCovered / journeyLength;
            plane.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            plane.transform.rotation = Quaternion.LookRotation(targetPosition - plane.transform.position);
            
            plane.transform.rotation = Quaternion.Euler(0, 90, 0);
            yield return null;
        }
    }
    public void ActivateExtract()
    {
        StartCoroutine(Countdown());
        SpawnPlane();
    }
}