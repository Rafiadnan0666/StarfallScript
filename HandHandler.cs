using UnityEngine;

public class HandHandler : MonoBehaviour
{
    public GameObject kanan;
    public GameObject kiri;

    [SerializeField] private Transform TarKanan;
    [SerializeField] private Transform TarKiri;

    [SerializeField] private float lerpSpeed = 10f;

    private GameObject currentItem;

    void Update()
    {
        if (currentItem != null)
        {
            kanan.transform.position = Vector3.Lerp(kanan.transform.position, currentItem.transform.position, Time.deltaTime * lerpSpeed);
            kiri.transform.position = Vector3.Lerp(kiri.transform.position, currentItem.transform.position, Time.deltaTime * lerpSpeed);
            kanan.transform.rotation = Quaternion.Lerp(kiri.transform.rotation, TarKiri.transform.rotation, Time.deltaTime * lerpSpeed);
            kanan.transform.rotation = Quaternion.Slerp(kanan.transform.rotation, currentItem.transform.rotation, Time.deltaTime * lerpSpeed);
            kiri.transform.rotation = Quaternion.Slerp(kiri.transform.rotation, currentItem.transform.rotation, Time.deltaTime * lerpSpeed);
        }
        else
        {
            kanan.transform.position = Vector3.Lerp(kanan.transform.position, TarKanan.position, Time.deltaTime * lerpSpeed);
            kiri.transform.position = Vector3.Lerp(kiri.transform.position, TarKiri.position, Time.deltaTime * lerpSpeed);

            kanan.transform.rotation = Quaternion.Slerp(kanan.transform.rotation, TarKanan.rotation, Time.deltaTime * lerpSpeed);
            kiri.transform.rotation = Quaternion.Slerp(kiri.transform.rotation, TarKiri.rotation, Time.deltaTime * lerpSpeed);
        }
    }

    public void HandleItem(bool equip, GameObject item)
    {
        if (equip)
        {
            currentItem = item;
        }
        else
        {
            currentItem = null;
        }
    }
}
