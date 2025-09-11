using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Slot References")]
    public GameObject slot1, slot2, slot3, flashlightSlot;
    public Image slot1Image, slot2Image, slot3Image, flashlightSlotImage;
    public Text slot1Text, slot2Text, slot3Text, flashlightSlotText;

    [Header("Spawn Points")]
    public Transform spawnPoint;
    public Transform spawnPointOri;
    public Transform spawnPointRun;
    public Transform spawnPointRunOri;

    private GameObject[] slots;
    private GameObject[] storedItems;
    private Text[] slotTexts;
    private Image[] slotImages;
    private GameObject currentItem;
    private GameObject equippedFlashlight;
    private Player playerScript;

    private Dictionary<int, float> throwableCooldowns = new Dictionary<int, float>();
    private float transitionSpeed = 10f;
    private bool isMoving = false;

    void Start()
    {
        slots = new GameObject[] { slot1, slot2, slot3, flashlightSlot };
        storedItems = new GameObject[slots.Length];
        slotTexts = new Text[] { slot1Text, slot2Text, slot3Text, flashlightSlotText };
        slotImages = new Image[] { slot1Image, slot2Image, slot3Image, flashlightSlotImage };
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] != null && storedItems[i] == null)
            {
                slotTexts[i].text = (i + 1).ToString();
            }
        }
        spawnPoint.position = spawnPointOri.position;
        spawnPoint.rotation = spawnPointOri.rotation;

        spawnPoint.localPosition = spawnPoint.position;
        spawnPoint.localRotation = spawnPoint.rotation;

        

    }
    void Update()
    {
        HandleInput();
        UpdateMovementState();

        spawnPoint.position = spawnPointOri.position;
        spawnPoint.rotation = spawnPointOri.rotation;


    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) AccessSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AccessSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AccessSlot(2);
        if (Input.GetKeyDown(KeyCode.F)) ToggleFlashlight();
        if (Input.GetKeyDown(KeyCode.Q)) DropCurrentItem();
    }

    private void UpdateMovementState()
    {
        isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                  Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
    }

   

  

    public void StoreItem(GameObject item)
    {
        if (item == null) return;

        if (item.CompareTag("Flashlight"))
        {
            StoreFlashlight(item);
            return;
        }

        for (int i = 0; i < storedItems.Length - 1; i++)
        {
            if (storedItems[i] == null)
            {
                storedItems[i] = item;
                item.SetActive(false);
                slotTexts[i].text = item.name;

                var spriteRenderer = item.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null && slotImages[i] != null)
                {
                    slotImages[i].sprite = spriteRenderer.sprite;
                    slotImages[i].enabled = true;
                }
                return;
            }
        }
    }

    private void StoreFlashlight(GameObject flashlight)
    {
        if (flashlight == null) return;

        if (equippedFlashlight != null)
        {
            equippedFlashlight.SetActive(false);
            StoreItem(equippedFlashlight);
        }

        equippedFlashlight = flashlight;
        flashlight.SetActive(false);

        int flashlightIndex = slots.Length - 1;
        slotTexts[flashlightIndex].text = flashlight.name;

        var spriteRenderer = flashlight.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && slotImages[flashlightIndex] != null)
        {
            slotImages[flashlightIndex].sprite = spriteRenderer.sprite;
            slotImages[flashlightIndex].enabled = true;
        }
    }

    public void ToggleFlashlight()
    {
        if (equippedFlashlight != null)
        {
            Flashlight flashlight = equippedFlashlight.GetComponent<Flashlight>();
            if (flashlight != null)
            {
                flashlight.ToggleLight();
            }
        }
    }

    public void AccessSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= storedItems.Length) return;
        if (storedItems[slotIndex] == null) return;

        if (currentItem != null)
        {
            StoreItem(currentItem);
            currentItem.transform.parent = null;
        }

        currentItem = storedItems[slotIndex];
        storedItems[slotIndex] = null;

        slotTexts[slotIndex].text = (slotIndex + 1).ToString();
        slotImages[slotIndex].sprite = null;
        slotImages[slotIndex].enabled = false;

        Rigidbody rb = currentItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        currentItem.SetActive(true);
        currentItem.transform.parent = spawnPoint;
        currentItem.transform.localPosition = Vector3.zero;
        currentItem.transform.localRotation = Quaternion.identity;

        if (currentItem.CompareTag("Gun"))
        {
            Gun gun = currentItem.GetComponent<Gun>();
            if (gun != null) gun.Equip(true);
        }
    }

    public void DropCurrentItem()
    {
        if (currentItem == null) return;

        currentItem.SetActive(true);
        currentItem.transform.parent = null;
        currentItem.transform.position = spawnPoint.position;
        currentItem.transform.rotation = spawnPoint.rotation;

        Rigidbody rb = currentItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (currentItem.CompareTag("Gun"))
        {
            Gun gun = currentItem.GetComponent<Gun>();
            if (gun != null) gun.Equip(false);
        }

        ClearItemFromUI(currentItem);
        currentItem = null;
    }

    private void ClearItemFromUI(GameObject item)
    {
        for (int i = 0; i < storedItems.Length; i++)
        {
            if (storedItems[i] == item)
            {
                storedItems[i] = null;
                slotTexts[i].text = (i + 1).ToString();
                slotImages[i].sprite = null;
                slotImages[i].enabled = false;
                break;
            }
        }
    }

    public void StoreThrowable(GameObject throwable, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;

        if (storedItems[slotIndex] == null)
        {
            storedItems[slotIndex] = throwable;
            slotTexts[slotIndex].text = throwable.name;

            var spriteRenderer = throwable.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && slotImages[slotIndex] != null)
            {
                slotImages[slotIndex].sprite = spriteRenderer.sprite;
                slotImages[slotIndex].enabled = true;
            }

            throwableCooldowns[slotIndex] = 0f;
        }
    }

    public void UseThrowable(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        if (storedItems[slotIndex] == null) return;

        if (throwableCooldowns.ContainsKey(slotIndex) && Time.time < throwableCooldowns[slotIndex]) return;

        Instantiate(storedItems[slotIndex], spawnPoint.position, spawnPoint.rotation);
        throwableCooldowns[slotIndex] = Time.time + 5f;

        storedItems[slotIndex] = null;
        slotTexts[slotIndex].text = (slotIndex + 1).ToString();
        slotImages[slotIndex].sprite = null;
        slotImages[slotIndex].enabled = false;
    }
}