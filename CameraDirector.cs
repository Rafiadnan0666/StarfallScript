using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

public class CinematicCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public string targetTag = "Jet";
    private List<Transform> targets = new List<Transform>();
    private Transform currentTarget;

    [Header("Camera Settings")]
    public float orbitDistance = 50f;
    public float orbitSpeed = 20f;
    public float transitionSpeed = 2f;
    public float zoomInDistance = 20f;
    public float flybyDistance = 100f;
    public float smoothDampTime = 0.5f;
    public Vector3 cameraOffset = new Vector3(0, 5f, 0);

    [Header("Shot Timing")]
    public float minShotDuration = 3f;
    public float maxShotDuration = 8f;
    private float shotDuration;
    private float nextShotTime;

    [Header("Post Process Settings")]
    public VolumeProfile cinematicProfile;
    private Volume volume;
    private DepthOfField dof;
    private MotionBlur motionBlur;
    private ColorAdjustments colorAdjust;
    private FilmGrain filmGrain;
    private LensDistortion lensDistortion;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private Bloom bloom;

    [Header("Camera Modes")]
    private int currentMode = 0;
    private int previousMode = -1;
    private Vector3 velocity = Vector3.zero;
    private Quaternion rotationVelocity = Quaternion.identity;

    [Header("Camera Paths")]
    private List<Vector3> cameraPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private float pathProgress = 0f;

    void Start()
    {
        // Find all targets
        FindTargets();

        // Set up post-processing
        SetupPostProcessing();

        // Initialize first shot
        PickNewTarget();
        SelectNextShot();
        ApplyCinematicLook();
    }

    void Update()
    {
        RefreshTargets();

        if (currentTarget == null)
        {
            PickNewTarget();
            if (currentTarget == null) return;
        }

        // Change shot after duration
        if (Time.time > nextShotTime)
        {
            SelectNextShot();
            ApplyCinematicLook();
            nextShotTime = Time.time + shotDuration;
        }

        // Smooth move depending on shot mode
        switch (currentMode)
        {
            case 0: OrbitShot(); break;
            case 1: ZoomInShot(); break;
            case 2: FlybyShot(); break;
            case 3: FollowShot(); break;
            case 4: PathShot(); break;
            case 5: LowAngleShot(); break;
            case 6: TopDownShot(); break;
        }
    }

    void RefreshTargets()
    {
        // Clean up destroyed/null targets
        targets = targets.Where(t => t != null).ToList();

        // If count changed or empty → repopulate
        if (targets.Count == 0 || Time.frameCount % 60 == 0) // refresh every 60 frames too
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag(targetTag);
            targets = objs.Select(obj => obj.transform).ToList();
        }
    }


    void FindTargets()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(targetTag);
        targets = objs.Select(obj => obj.transform).ToList();
    }

    void SetupPostProcessing()
    {
        // Find or create post-process Volume
        volume = FindObjectOfType<Volume>();
        if (volume == null)
        {
            GameObject volObj = new GameObject("CinematicPostFX");
            volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;

            if (cinematicProfile != null)
            {
                volume.profile = cinematicProfile;
            }
            else
            {
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }
        }

        // Get or add post-processing effects
        if (!volume.profile.TryGet(out dof))
            dof = volume.profile.Add<DepthOfField>(true);
        if (!volume.profile.TryGet(out motionBlur))
            motionBlur = volume.profile.Add<MotionBlur>(true);
        if (!volume.profile.TryGet(out colorAdjust))
            colorAdjust = volume.profile.Add<ColorAdjustments>(true);
        if (!volume.profile.TryGet(out filmGrain))
            filmGrain = volume.profile.Add<FilmGrain>(true);
        if (!volume.profile.TryGet(out lensDistortion))
            lensDistortion = volume.profile.Add<LensDistortion>(true);
        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);
        if (!volume.profile.TryGet(out chromaticAberration))
            chromaticAberration = volume.profile.Add<ChromaticAberration>(true);
        if (!volume.profile.TryGet(out bloom))
            bloom = volume.profile.Add<Bloom>(true);
    }

    void SelectNextShot()
    {
        previousMode = currentMode;

        // Avoid repeating the same shot
        do
        {
            currentMode = Random.Range(0, 7);
        } while (currentMode == previousMode && Random.value > 0.3f);

        shotDuration = Random.Range(minShotDuration, maxShotDuration);
        PickNewTarget();

        // Prepare for path shot if selected
        if (currentMode == 4)
        {
            GenerateCameraPath();
        }
    }

    void PickNewTarget()
    {
        RefreshTargets();
        if (targets.Count == 0) return;

        // Prefer targets not equal to current one
        if (targets.Count > 1 && currentTarget != null)
        {
            var availableTargets = targets.Where(t => t != currentTarget).ToList();
            if (availableTargets.Count > 0)
                currentTarget = availableTargets[Random.Range(0, availableTargets.Count)];
            else
                currentTarget = targets[Random.Range(0, targets.Count)];
        }
        else
        {
            currentTarget = targets[Random.Range(0, targets.Count)];
        }
    }

    void OrbitShot()
    {
        float angle = Time.time * orbitSpeed;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)) * orbitDistance;
        Vector3 desiredPos = currentTarget.position + offset + cameraOffset;

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Smooth lookat
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void ZoomInShot()
    {
        Vector3 desiredPos = currentTarget.position - currentTarget.forward * zoomInDistance + cameraOffset;

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Smooth lookat
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void FlybyShot()
    {
        Vector3 flyDir = currentTarget.right;
        Vector3 desiredPos = currentTarget.position + flyDir * flybyDistance + Vector3.up * 20f;

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Smooth lookat
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void FollowShot()
    {
        // Follow behind the target
        Vector3 desiredPos = currentTarget.position - currentTarget.forward * zoomInDistance + Vector3.up * 10f;

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Look slightly ahead of the target
        Vector3 lookAheadPoint = currentTarget.position + currentTarget.forward * 20f;
        Quaternion targetRotation = Quaternion.LookRotation(lookAheadPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void PathShot()
    {
        if (cameraPath.Count == 0) return;

        // Move along the path
        pathProgress += Time.deltaTime * 0.5f;
        if (pathProgress >= 1f)
        {
            pathProgress = 0f;
            currentPathIndex = (currentPathIndex + 1) % cameraPath.Count;
        }

        int nextIndex = (currentPathIndex + 1) % cameraPath.Count;
        Vector3 desiredPos = Vector3.Lerp(cameraPath[currentPathIndex], cameraPath[nextIndex], pathProgress);

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Look at the target
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void LowAngleShot()
    {
        // Low angle shot looking up at the target
        Vector3 desiredPos = currentTarget.position - currentTarget.forward * 30f - Vector3.up * 10f;

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Look at the target from below
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void TopDownShot()
    {
        // Top down shot looking down at the target
        Vector3 desiredPos = currentTarget.position + Vector3.up * 50f;

        // Smooth position transition
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothDampTime);

        // Look down at the target
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }

    void GenerateCameraPath()
    {
        cameraPath.Clear();

        // Generate a circular path around the target
        int points = 8;
        for (int i = 0; i < points; i++)
        {
            float angle = i * Mathf.PI * 2f / points;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle)) * orbitDistance;
            cameraPath.Add(currentTarget.position + offset + cameraOffset);
        }

        currentPathIndex = 0;
        pathProgress = 0f;
    }

    void ApplyCinematicLook()
    {
        if (dof != null)
        {
            dof.mode.value = DepthOfFieldMode.Bokeh;
            dof.focusDistance.value = Vector3.Distance(transform.position, currentTarget.position);
            dof.focalLength.value = Random.Range(50f, 100f);
            dof.aperture.value = Random.Range(1.8f, 5.6f);
        }

        if (motionBlur != null)
        {
            motionBlur.intensity.value = Random.Range(0.1f, 0.4f);
        }

        if (colorAdjust != null)
        {
            colorAdjust.saturation.value = Random.Range(-15f, 5f);
            colorAdjust.contrast.value = Random.Range(5f, 20f);
            colorAdjust.postExposure.value = Random.Range(-0.2f, 0.2f);
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = Random.Range(0.1f, 0.3f);
            filmGrain.type.value = (FilmGrainLookup)Random.Range(0, 3);
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = Random.Range(-0.1f, 0.1f);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Random.Range(0.1f, 0.3f);
            vignette.smoothness.value = Random.Range(0.2f, 0.5f);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Random.Range(0.1f, 0.2f);
        }

        if (bloom != null)
        {
            bloom.intensity.value = Random.Range(0.5f, 1.5f);
            bloom.threshold.value = Random.Range(0.8f, 1.2f);
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (cameraPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < cameraPath.Count; i++)
            {
                Gizmos.DrawSphere(cameraPath[i], 2f);
                if (i < cameraPath.Count - 1)
                    Gizmos.DrawLine(cameraPath[i], cameraPath[i + 1]);
            }
            Gizmos.DrawLine(cameraPath[cameraPath.Count - 1], cameraPath[0]);
        }
    }
}