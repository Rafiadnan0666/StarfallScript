using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClass : MonoBehaviour
{
    public enum PlayerClassType
    {
        Assault,
        Sniper
        //Demolitions,
        //Medic
    }

    [SerializeField] private Transform OriginGun;
    [SerializeField] private Transform SpawnGun;

    [System.Serializable]
    public class ClassInfo
    {
        public PlayerClassType classType;
        public string className;
        public GameObject primaryGun;
        public GameObject[] throwables = new GameObject[3];
        public float health;
        public float movementSpeed;
        public float playerScale;
    }

    public ClassInfo[] classes;
    [SerializeField] private PlayerClassType currentClassType = PlayerClassType.Assault;

    private GameObject activeWeapon;
    private GameObject activeThrowable;

    void Start()
    {
        DisableAllWeaponsAndThrowables();
        ApplyClass(currentClassType);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetActiveWeapon(GetClass(currentClassType).primaryGun);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetActiveThrowable(0);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetActiveThrowable(1);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetActiveThrowable(2);

        if (Input.GetKeyDown(KeyCode.F1))
            SwitchClass(PlayerClassType.Assault);
        if (Input.GetKeyDown(KeyCode.F2))
            SwitchClass(PlayerClassType.Sniper);
    }

    void ApplyClass(PlayerClassType classType)
    {
        Player player = GetComponent<Player>();
        if (player == null) return;

        DisableAllWeaponsAndThrowables();

        ClassInfo selectedClass = GetClass(classType);
        if (selectedClass == null) return;

        player.health = selectedClass.health;
        player.speed = selectedClass.movementSpeed;
        transform.localScale = Vector3.one * selectedClass.playerScale;

        SetActiveWeapon(selectedClass.primaryGun);
    }

    ClassInfo GetClass(PlayerClassType classType)
    {
        foreach (var classInfo in classes)
        {
            if (classInfo.classType == classType)
                return classInfo;
        }
        return null;
    }

    void SetActiveWeapon(GameObject weapon)
    {
        if (activeWeapon != null)
            activeWeapon.SetActive(false);

        if (weapon != null)
        {
            weapon.SetActive(true);
            weapon.transform.position = transform.position;
            weapon.transform.rotation = transform.rotation;
            activeWeapon = weapon;
            weapon.GetComponent<BoxCollider>().enabled = false;
            weapon.GetComponent<Rigidbody>().isKinematic = true;
           
            weapon.transform.position = OriginGun.position;

        }
    }

    void SetActiveThrowable(int index)
    {
        ClassInfo selectedClass = GetClass(currentClassType);
        if (index >= selectedClass.throwables.Length) return;

        if (activeThrowable != null)
            activeThrowable.SetActive(false);

        GameObject throwable = selectedClass.throwables[index];

        if (throwable != null)
        {
            throwable.SetActive(true);
            throwable.transform.position = transform.position;
            throwable.transform.rotation = transform.rotation;
            activeThrowable = throwable;
            throwable.GetComponent<Rigidbody>().isKinematic = true;
            throwable.transform.position = OriginGun.position;
        }
    }

    void DisableAllWeaponsAndThrowables()
    {
        foreach (var classInfo in classes)
        {
            if (classInfo.primaryGun != null)
                classInfo.primaryGun.SetActive(false);

            foreach (var throwable in classInfo.throwables)
            {
                if (throwable != null)
                    throwable.SetActive(false);
            }
        }
    }

    void SwitchClass(PlayerClassType newClassType)
    {
        if (newClassType == currentClassType) return;

        currentClassType = newClassType;
        ApplyClass(newClassType);
    }
}
