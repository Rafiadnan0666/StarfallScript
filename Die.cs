using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Die : MonoBehaviour
{
    [SerializeField] private Bosses1 Bosses1;
    [SerializeField] private Bosses2 Bosses2;
    [SerializeField] private Bosses3 Bosses3;

    [SerializeField] private Image hellime1;
    [SerializeField] private Image hellime2;
    [SerializeField] private Image hellime3;

    [SerializeField] private Text healthText1;
    [SerializeField] private Text healthText2;
    [SerializeField] private Text healthText3;

    public AudioSource audioSource;
    public AudioClip DeathClip;
    public GameObject Pintu;

    private bool doorOpened = false;

    void Update()
    {
        HandleBoss1();
        HandleBoss2();
        HandleBoss3();
    }

    private void HandleBoss1()
    {
        if (Bosses1 == null) return;

        // Update health bar and text
        hellime1.GetComponent<RectTransform>().sizeDelta = new Vector2(Bosses1.health, hellime1.GetComponent<RectTransform>().sizeDelta.y);
        healthText1.text = $"Dragon Health: {Bosses1.health:F0}";

        if (Bosses1.health <= 0)
        {
            HandleBossDeath(Bosses1, hellime1, healthText1);
        }
    }

    private void HandleBoss2()
    {
        if (Bosses2 == null) return;

        // Update health bar and text
        hellime2.GetComponent<RectTransform>().sizeDelta = new Vector2(Bosses2.health, hellime2.GetComponent<RectTransform>().sizeDelta.y);
        healthText2.text = $"Leviathan Health: {Bosses2.health:F0}";

        if (Bosses2.health <= 0)
        {
            HandleBossDeath(Bosses2, hellime2, healthText2);
        }
    }

    private void HandleBoss3()
    {
        if (Bosses3 == null) return;

        // Update health bar and text
        hellime3.GetComponent<RectTransform>().sizeDelta = new Vector2(Bosses3.health, hellime3.GetComponent<RectTransform>().sizeDelta.y);
        healthText3.text = $"Unknown Health: {Bosses3.health:F0}";

        if (Bosses3.health <= 0)
        {
            HandleBossDeath(Bosses3, hellime3, healthText3);
        }
    }

    private void HandleBossDeath(MonoBehaviour boss, Image healthBar, Text healthText)
    {
        boss.enabled = false;

        if (boss.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        audioSource.PlayOneShot(DeathClip);

        healthBar.enabled = false;
        healthText.enabled = false;

        if (!doorOpened)
        {
            StartCoroutine(OpenDoor());
            doorOpened = true;
        }
    }

    private IEnumerator OpenDoor()
    {
        Vector3 targetPosition = Pintu.transform.position + Vector3.up * 10f;
        Vector3 initialPosition = Pintu.transform.position;
        float elapsedTime = 0f;
        float duration = 2f;

        while (elapsedTime < duration)
        {
            Pintu.transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Pintu.transform.position = targetPosition;
    }
}
