using System.Collections;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// IMPORTANT: This script now uses an Object Pooling system.
// Please ensure you have a GameObject in your scene with the PoolManager.cs script attached.
// You will need to create pools with the following tags: "bullet", "muzzleFlash".
public class Gun : MonoBehaviour
{
    public enum GunType { Pistol, MachineGun, Sniper }
    public GunType gunType;

    private float aimTransitionSpeed = 5f; // Adjust for smoother transitions
    private Vector3 targetCameraPosition;
    [Header("Aim Settings")]
    public Vector3 aimPositionOffset = new Vector3(-0.15f, 0.05f, -0.16f); // Adjust this manually
    private Vector3 originalGunLocalPosition;

    public Transform bulletTip;

    public Camera playerCamera;
    [SerializeField] private Camera AimCam;


    public GameObject Middle;
    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;

    public AudioClip fireSound;
    public AudioClip reloadStartSound; // Sound for the initial rotation
    public AudioClip[] reloadIterationSounds;
    public AudioSource audioSource;

    public CameraShake cameraShake;

    public float aimSpeed = 5f;
    public float scopedFOV = 30f;
    private float normalFOV;
    private Camera mainCamera;

    public int maxAmmo = 100;
    public int currentAmmo;
    public TMP_Text ammoText;

    public GameObject RightTarget;
    public GameObject LeftTarget;
    public GameObject GunTargetLeft;
    public GameObject LeftTargetOrigin;
    public GameObject GunTargetRight;

    public Transform RightIdle;
    public Transform LeftIdle;

    private float fireRate = 0.1f;
    private float nextFireTime = 0f;

    public bool isEquipped = false;
    private bool isScoped = false;
    public bool isFullAuto = false;

    [Header("Head Bob Settings")]
    public float headBobSpeed = 6f;
    public float headBobAmount = 0.05f;
    private Vector3 originalCameraPosition;
    private float bobTimer;

    [Header("Blowback Settings")]
    public float blowbackDistance = 0.1f;
    public float blowbackSpeed = 10f;
    private Vector3 originalGunPosition;
    private bool isBlowbackActive = false;

    [SerializeField] private int recoil;

    [SerializeField] private GameObject lefttar;
    [SerializeField] private Transform originleft;
    [SerializeField] private Transform scopePosition;

    private bool isReloading = false;
    private Player playerScript;

    void Start()
    {
        mainCamera = playerCamera;
        normalFOV = mainCamera.fieldOfView;
        currentAmmo = maxAmmo;
        originalCameraPosition = playerCamera.transform.localPosition;
        originalGunPosition = transform.localPosition;
        UpdateAmmoUI();
        originleft.transform.position = GunTargetLeft.transform.position;
        AimCam.gameObject.SetActive(false);
        playerScript = FindObjectOfType<Player>();
    }

    void Update()
    {
        mainCamera.fieldOfView = mainCamera.fieldOfView;

        if (isEquipped)
        {
            playerCamera.gameObject.SetActive(true);

            LeftTarget.transform.position = GunTargetLeft.transform.position;
            LeftTarget.transform.rotation = GunTargetLeft.transform.rotation;

            RightTarget.transform.position = GunTargetRight.transform.position;
            RightTarget.transform.rotation = GunTargetRight.transform.rotation;

            if (Input.GetMouseButtonDown(1))
            {
                isScoped = !isScoped;
                StartCoroutine(AimGun(isScoped));
            }


            Vector3 defaultPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            Quaternion defaultRotation = transform.rotation;

            this.gameObject.GetComponent<Collider>().enabled = false;
            if (isFullAuto && Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireRate;

                Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x - recoil, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * scopedFOV);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Fire();

                Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x - 15, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * scopedFOV);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, defaultRotation, Time.deltaTime * scopedFOV);
                transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * 3);
            }

            if (playerScript != null && playerScript.sprinting)
            {
                Quaternion targetRotation = Quaternion.Euler(20.265f, -35.556f, 40f);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * aimSpeed);
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * aimSpeed);
            }

            HandleHeadBob();

            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                StartCoroutine(Reload());
            }
        }
        else
        {
            mainCamera.fieldOfView = mainCamera.fieldOfView;
            LeftTarget.transform.position = LeftIdle.transform.position;
            RightTarget.transform.position = RightIdle.transform.position;
            this.gameObject.GetComponent<Collider>().enabled = true;
        }
    }

    void HandleHeadBob()
    {
        if (playerScript != null && playerScript.sprinting)
        {
            bobTimer += Time.deltaTime * headBobSpeed;

            float bobOffset = Mathf.Sin(bobTimer) * headBobAmount;
            Vector3 newPosition = originalCameraPosition + new Vector3(0f, bobOffset, 0f);

            playerCamera.transform.localPosition = newPosition;
        }
        else
        {
            bobTimer = 0f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, originalCameraPosition, Time.deltaTime * aimSpeed);
        }
    }

    void Fire()
    {
        if (currentAmmo > 0 && !isBlowbackActive)
        {
            currentAmmo--;
            UpdateAmmoUI();
            // Use the pool manager to spawn a bullet
            PoolManager.Instance.SpawnFromPool("bullet", bulletTip.position, bulletTip.rotation);

            if (audioSource != null && fireSound != null)
            {
                audioSource.PlayOneShot(fireSound);
            }
            else
            {
                Debug.LogWarning("Fire sound or AudioSource is not assigned!");
            }

            if (muzzleFlashPrefab != null)
            {
                // Use the pool manager to spawn a muzzle flash
                GameObject muzzleFlash = PoolManager.Instance.SpawnFromPool("muzzleFlash", bulletTip.position, bulletTip.rotation);
                // Deactivate muzzle flash after a short time
                StartCoroutine(DeactivateAfterTime(muzzleFlash, 0.1f));
            }

            if (cameraShake != null)
            {
                //StartCoroutine(cameraShake.Shake(0.1f, 0.2f));
            }
        }
        else if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
        }
    }

    IEnumerator DeactivateAfterTime(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        obj.SetActive(false);
    }

    IEnumerator Reload()
    {
        isReloading = true;

        float reloadTime = 2f;
        Vector3 loweredPosition = GunTargetLeft.transform.position + Vector3.down * 0.2f;

        if (audioSource != null && reloadStartSound != null)
        {
            audioSource.PlayOneShot(reloadStartSound);
        }

        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(startRotation.eulerAngles.x, startRotation.eulerAngles.y, startRotation.eulerAngles.z + -60f);

        float rotationTime = 0.5f;
        float rotationElapsed = 0f;

        while (rotationElapsed < rotationTime)
        {
            rotationElapsed += Time.deltaTime;
            float t = rotationElapsed / rotationTime;
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        float halfTime = (reloadTime - rotationTime) / 2f;
        float elapsedTime = 0f;
        while (elapsedTime < halfTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfTime;
            LeftTarget.transform.position = Vector3.Lerp(GunTargetLeft.transform.position, loweredPosition, t);
            yield return null;
        }
        elapsedTime = 0f;
        while (elapsedTime < halfTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfTime;
            LeftTarget.transform.position = Vector3.Lerp(loweredPosition, originleft.transform.position, t);
            yield return null;
        }
        LeftTarget.transform.position = GunTargetLeft.transform.position;

        transform.localRotation = startRotation; // Reset rotation

        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        isReloading = false;
    }



    IEnumerator AimGun(bool enable)
    {
        float targetFOV = enable ? scopedFOV : normalFOV;
        float elapsedTime = 0f;
        float duration = 0.3f; // adjust for smoothness
        float startFOV = mainCamera.fieldOfView;

        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = enable ? aimPositionOffset : originalGunLocalPosition;

        Middle.SetActive(!enable);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }

        transform.localPosition = targetPos;
        mainCamera.fieldOfView = targetFOV;
    }




    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
        }
    }

    public void Equip(bool equip)
    {
        isEquipped = equip;

        if (equip)
        {
            GetComponent<BoxCollider>().enabled = false;
            Debug.Log("Gun equipped");
            LeftTarget.gameObject.transform.position = GunTargetLeft.transform.position;
            LeftTarget.gameObject.transform.rotation = GunTargetLeft.transform.rotation;
            GunTargetLeft.transform.position = lefttar.transform.position;
        }
        else
        {
            GetComponent<BoxCollider>().enabled = true;
            Debug.Log("Gun unequipped");
            mainCamera.fieldOfView = normalFOV;
            playerCamera.transform.localPosition = originalCameraPosition; // Reset camera position
            LeftTarget.gameObject.transform.rotation = LeftTargetOrigin.transform.rotation;
            LeftTarget.gameObject.transform.position = LeftTargetOrigin.transform.position;
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }
    }
}