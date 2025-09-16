using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class RetroHUDController : MonoBehaviour
{
    [System.Serializable]
    public class TagConfig
    {
        public string tag;
        public string label;
        public bool important;
        public Color tagColor = Color.white;
        public bool showDistance = true;
        public bool showAlways = false;
        public float maxViewDistance = 100f;
        public float minViewDistance = 3f;
        public string symbol = "◉"; // Retro symbol for this tag
        public float scale = 1.0f;
        [Range(0.1f, 1f)] public float opacity = 0.9f;
    }

    [System.Serializable]
    public class HUDConfig
    {
        public Color defaultColor = Color.green;
        public Color friendlyColor = Color.green;
        public Color enemyColor = Color.red;
        public Color neutralColor = Color.blue;
        public Color objectiveColor = Color.yellow;
        public Font font;
        public int fontSize = 16;
        public int objectivePanelWidth = 280;
        public int objectivePanelHeight = 220;
        public int panelPadding = 20;
        public int elementSpacing = 5;
        public bool showCompass = true;
        public Color compassColor = Color.cyan;
        public string[] compassLetters = new string[] { "N", "E", "S", "W" };
        public Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.85f);
        public Color panelBorderColor = new Color(0f, 0.8f, 0.4f, 0.6f);
        public int panelBorderWidth = 2;
    }

    public Camera mainCamera;
    public List<TagConfig> tagConfigs = new List<TagConfig>();
    public HUDConfig hudConfig = new HUDConfig();
    public bool showObjectivesPanel = true;
    public bool showDebugInfo = false;

    private Dictionary<string, TagConfig> configMap = new Dictionary<string, TagConfig>();
    private List<GameObject> allTargets = new List<GameObject>();
    private List<GameObject> objectiveTargets = new List<GameObject>();
    private Dictionary<GameObject, Health> healthCache = new Dictionary<GameObject, Health>();
    private Dictionary<GameObject, Objective> objectiveCache = new Dictionary<GameObject, Objective>();
    private GameObject portalBack;
    private GUIStyle[] cachedStyles;
    private GUIStyle panelStyle;
    private GUIStyle compassStyle;
    private GUIStyle compassLabelStyle;
    private Texture2D panelBackground;
    private Texture2D panelBorder;
    private StringBuilder debugBuilder = new StringBuilder();
    private float compassWidth = 240f;
    private float compassHeight = 40f;
    private float nextScanTime = 0f;
    private const float SCAN_INTERVAL = 0.5f;
    private float blinkTimer = 0f;
    private const float BLINK_INTERVAL = 1.5f;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Create panel background texture
        panelBackground = new Texture2D(1, 1);
        panelBackground.SetPixel(0, 0, hudConfig.panelBackgroundColor);
        panelBackground.Apply();

        // Create panel border texture
        panelBorder = new Texture2D(1, 1);
        panelBorder.SetPixel(0, 0, hudConfig.panelBorderColor);
        panelBorder.Apply();

        // Build config map first
        BuildConfigMap();

        // Initialize GUI styles
        InitializeStyles();

        // Find initial targets
        RebuildTargetLists();
        portalBack = GameObject.FindGameObjectWithTag("PortalBack");
    }

    void BuildConfigMap()
    {
        configMap.Clear();
        foreach (var cfg in tagConfigs)
        {
            if (!string.IsNullOrEmpty(cfg.tag) && !configMap.ContainsKey(cfg.tag))
            {
                configMap.Add(cfg.tag, cfg);
            }
        }
    }

    void InitializeStyles()
    {
        panelStyle = new GUIStyle();
        panelStyle.normal.background = panelBackground;
        panelStyle.padding = new RectOffset(12, 12, 8, 8);
        panelStyle.border = new RectOffset(6, 6, 6, 6);
        panelStyle.font = hudConfig.font;
        panelStyle.fontSize = hudConfig.fontSize;
        panelStyle.normal.textColor = hudConfig.defaultColor;
        panelStyle.wordWrap = true;

        compassStyle = new GUIStyle();
        compassStyle.font = hudConfig.font;
        compassStyle.fontSize = hudConfig.fontSize + 6;
        compassStyle.normal.textColor = hudConfig.compassColor;
        compassStyle.alignment = TextAnchor.UpperCenter;
        compassStyle.fontStyle = FontStyle.Bold;

        compassLabelStyle = new GUIStyle();
        compassLabelStyle.font = hudConfig.font;
        compassLabelStyle.fontSize = hudConfig.fontSize - 2;
        compassLabelStyle.normal.textColor = hudConfig.compassColor;
        compassLabelStyle.alignment = TextAnchor.UpperCenter;

        cachedStyles = new GUIStyle[tagConfigs.Count];
        for (int i = 0; i < tagConfigs.Count; i++)
        {
            var config = tagConfigs[i];
            cachedStyles[i] = new GUIStyle();
            cachedStyles[i].normal.textColor = config.tagColor;
            cachedStyles[i].font = hudConfig.font;
            cachedStyles[i].fontSize = Mathf.RoundToInt(hudConfig.fontSize * config.scale);
            cachedStyles[i].alignment = TextAnchor.MiddleLeft;
        }
    }

    void Update()
    {
        // Check if any objectives were destroyed
        CheckDestroyedObjectives();

        // Real-time scanning for new objects with time-based interval
        if (Time.time >= nextScanTime)
        {
            RebuildTargetLists();
            nextScanTime = Time.time + SCAN_INTERVAL;
        }

        // Update blink timer for animations
        blinkTimer += Time.deltaTime;
        if (blinkTimer > BLINK_INTERVAL * 2f) blinkTimer = 0;

        // Build debug info
        if (showDebugInfo)
        {
            BuildDebugInfo();
        }
    }

    void BuildDebugInfo()
    {
        debugBuilder.Length = 0;

        debugBuilder.AppendLine($"<{System.DateTime.Now:HH:mm:ss}> [SYSTEM STATUS]");
        debugBuilder.AppendLine($"FPS: {1f / Time.deltaTime:0}");
        debugBuilder.AppendLine($"TARGETS: {allTargets.Count}");
        debugBuilder.AppendLine($"OBJECTIVES: {objectiveTargets.Count}");
        debugBuilder.AppendLine($"CAMERA: {(mainCamera != null ? "OK" : "NULL")}");

        if (mainCamera != null)
        {
            debugBuilder.AppendLine($"POS: {mainCamera.transform.position}");
            debugBuilder.AppendLine($"ROT: {mainCamera.transform.eulerAngles}");
        }
    }

    void CheckDestroyedObjectives()
    {
        // Remove null/destroyed targets
        allTargets.RemoveAll(item => item == null);
        objectiveTargets.RemoveAll(item => item == null);
        
        // Check if all objectives are completed
        if (objectiveTargets.Count == 0)
        {
            EnablePortalBack();
        }
        else
        {
            if (portalBack != null)
            {
                portalBack.SetActive(true);
                portalBack.GetComponent<PortalBack>().enabled = false;
            }
        }
    }

    void EnablePortalBack()
    {
        if (portalBack != null)
        {
            portalBack.SetActive(true);
            portalBack.GetComponent<PortalBack>().enabled = true; 
        }
    }

    void RebuildTargetLists()
    {
        allTargets.Clear();
        objectiveTargets.Clear();

        foreach (var cfg in tagConfigs)
        {
            if (string.IsNullOrEmpty(cfg.tag)) continue;

            // Find all objects with this tag
            GameObject[] objs = GameObject.FindGameObjectsWithTag(cfg.tag);
            allTargets.AddRange(objs);

            // If it's an objective or important, add to separate list
            if (cfg.important || cfg.tag == "Objective")
            {
                objectiveTargets.AddRange(objs);
            }
        }
    }

    void OnGUI()
    {
        if (mainCamera == null) return;

        // Draw compass
        if (hudConfig.showCompass)
        {
            DrawCompass();
        }

        // Sort targets manually to avoid LINQ garbage
        allTargets.Sort((a, b) => 
            Vector3.Distance(mainCamera.transform.position, b.transform.position)
            .CompareTo(Vector3.Distance(mainCamera.transform.position, a.transform.position)));

        foreach (var target in allTargets)
        {
            if (target == null) continue;

            string tag = target.tag;
            if (!configMap.ContainsKey(tag)) continue;

            TagConfig cfg = configMap[tag];
            DrawTargetIndicator(target, cfg);
        }

        // Draw objectives panel
        if (showObjectivesPanel)
        {
            DrawObjectivesPanel();
        }

        // Draw debug info
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    void DrawCompass()
    {
        float centerX = Screen.width / 2f;
        float yPos = 15f;

        // Get camera's y rotation to determine compass direction
        float cameraYRotation = mainCamera.transform.eulerAngles.y;

        // Calculate which direction letter to show
        string directionLetter = GetCompassDirection(cameraYRotation);

        // Create the compass display with retro style
        string compassText = $"<size=+4>{directionLetter}</size>";
        if (blinkTimer < BLINK_INTERVAL)
        {
            compassText = $"<b>|</b> {directionLetter} <b>|</b>";
        }

        // Draw the compass
        GUI.Label(new Rect(centerX - compassWidth / 2, yPos, compassWidth, compassHeight), compassText, compassStyle);

        // Draw compass rose labels
        float roseRadius = compassWidth / 2 - 20;
        DrawCompassRose(centerX, yPos + compassHeight, roseRadius);
    }

    void DrawCompassRose(float centerX, float centerY, float radius)
    {
        float angleStep = 90f;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * angleStep;
            string direction = hudConfig.compassLetters[i];

            float x = centerX + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float y = centerY + Mathf.Cos(angle * Mathf.Deg2Rad) * radius;

            GUI.Label(new Rect(x - 10, y - 10, 20, 20), direction, compassLabelStyle);
        }
    }

    string GetCompassDirection(float yRotation)
    {
        // Normalize the rotation to 0-360
        float normalizedRotation = (yRotation % 360 + 360) % 360;

        // Determine direction based on angle
        if (normalizedRotation >= 337.5f || normalizedRotation < 22.5f) return hudConfig.compassLetters[0]; // N
        if (normalizedRotation >= 22.5f && normalizedRotation < 67.5f) return "NE";
        if (normalizedRotation >= 67.5f && normalizedRotation < 112.5f) return hudConfig.compassLetters[1]; // E
        if (normalizedRotation >= 112.5f && normalizedRotation < 157.5f) return "SE";
        if (normalizedRotation >= 157.5f && normalizedRotation < 202.5f) return hudConfig.compassLetters[2]; // S
        if (normalizedRotation >= 202.5f && normalizedRotation < 247.5f) return "SW";
        if (normalizedRotation >= 247.5f && normalizedRotation < 292.5f) return hudConfig.compassLetters[3]; // W
        return "NW";
    }

    void DrawTargetIndicator(GameObject target, TagConfig cfg)
    {
        if (target == null || mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.transform.position);

        // Check if object is behind camera
        if (screenPos.z < 0)
        {
            // For important objects behind camera, show edge indicator
            if (cfg.important || cfg.showAlways)
            {
                DrawOffScreenIndicator(target, cfg, screenPos);
            }
            return;
        }

        // Calculate distance
        float distance = Vector3.Distance(mainCamera.transform.position, target.transform.position);

        // Check distance constraints - always show if configured that way
        if (!cfg.showAlways && (distance > cfg.maxViewDistance || distance < cfg.minViewDistance))
        {
            // For important objects out of range, show edge indicator
            if (cfg.important)
            {
                DrawOffScreenIndicator(target, cfg, screenPos);
            }
            return;
        }

        Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
        int configIndex = tagConfigs.IndexOf(cfg);
        GUIStyle style = configIndex >= 0 && configIndex < cachedStyles.Length && cachedStyles[configIndex] != null ?
                         cachedStyles[configIndex] : panelStyle;

        // Apply opacity
        Color originalColor = style.normal.textColor;
        Color fadedColor = new Color(originalColor.r, originalColor.g, originalColor.b, cfg.opacity);
        style.normal.textColor = fadedColor;

        // Get components for display if needed
        string displayText = !string.IsNullOrEmpty(cfg.label) ? cfg.label : target.name;

        if (cfg.tag == "Objective")
        {
            Health health;
            if (!healthCache.TryGetValue(target, out health))
            {
                health = target.GetComponent<Health>();
                healthCache[target] = health;
            }

            if (health != null)
            {
                displayText += $" <color=#{ColorUtility.ToHtmlStringRGB(GetHealthColor(health.CurrentHealth / health.maxHealth))}>[{health.CurrentHealth}/{health.maxHealth}]</color>";
            }

            Objective obj;
            if (!objectiveCache.TryGetValue(target, out obj))
            {
                obj = target.GetComponent<Objective>();
                objectiveCache[target] = obj;
            }

            if (obj != null)
            {
                if (obj.isCompleted)
                {
                    displayText = $"<color=#00FF00>✓ {displayText}</color>";
                }
                else if (obj.missionDescriptionText != null && obj.missionNameText != null)
                {
                    obj.missionDescriptionText.text = $"Destroy the {obj.objectiveName} to complete the mission.";
                    obj.missionNameText.text = $"<color=yellow>{obj.objectiveName}</color>";
                }
            }
        }

        if (cfg.showDistance)
        {
            displayText += $" <color=#AAAAAA><{distance:F0}m></color>";
        }

        // Use the symbol from config
        string symbol = !string.IsNullOrEmpty(cfg.symbol) ? cfg.symbol : "◉";

        // On-screen indicator
        if (screenPos.x >= 0 && screenPos.x <= Screen.width &&
            screenPos.y >= 0 && screenPos.y <= Screen.height)
        {
            // Draw tag symbol with size based on distance
            float symbolSize = Mathf.Clamp(30f * (1f - distance / cfg.maxViewDistance), 12f, 30f) * cfg.scale;
            Rect symbolRect = new Rect(guiPos.x - symbolSize / 2, guiPos.y - symbolSize / 2, symbolSize, symbolSize);

            // Add subtle animation to important items
            if (cfg.important && blinkTimer < BLINK_INTERVAL)
            {
                symbolRect = new Rect(guiPos.x - symbolSize / 2 - 2, guiPos.y - symbolSize / 2 - 2, symbolSize + 4, symbolSize + 4);
            }

            GUI.Label(symbolRect, symbol, style);
            if (distance < cfg.maxViewDistance * 0.6f)
            {
                // Draw label with subtle background
                GUIContent content = new GUIContent($" {displayText} ");
                Vector2 textSize = style.CalcSize(content);

                // Draw text background
                DrawTextBox(new Rect(guiPos.x + symbolSize / 2, guiPos.y - textSize.y / 2, textSize.x, textSize.y),
                           new Color(0.1f, 0.1f, 0.1f, 0.7f));

                // Draw text
                GUI.Label(new Rect(guiPos.x + symbolSize / 2, guiPos.y - textSize.y / 2, textSize.x, textSize.y),
                         content, style);
            }
        }
        else if (cfg.important || cfg.showAlways)
        {
            // Off-screen indicator (edge of screen)
            DrawOffScreenIndicator(target, cfg, screenPos);
        }

        // Reset color
        style.normal.textColor = originalColor;
    }

    void DrawOffScreenIndicator(GameObject target, TagConfig cfg, Vector3 screenPos)
    {
        if (target == null) return;

        int configIndex = tagConfigs.IndexOf(cfg);
        GUIStyle style = configIndex >= 0 && configIndex < cachedStyles.Length && cachedStyles[configIndex] != null ?
                         cachedStyles[configIndex] : panelStyle;

        // Calculate direction to target
        Vector2 direction = (new Vector2(screenPos.x, Screen.height - screenPos.y) -
                           new Vector2(Screen.width / 2, Screen.height / 2)).normalized;

        // Calculate position on screen edge
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        float angle = Mathf.Atan2(direction.y, direction.x);

        // Find intersection point with screen bounds
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        float screenAspect = (float)Screen.width / Screen.height;

        // Calculate intersection with screen edges
        Vector2 edgePos;
        if (Mathf.Abs(cos) > Mathf.Abs(sin) * screenAspect)
        {
            float x = cos > 0 ? Screen.width : 0;
            float y = screenCenter.y + (x - screenCenter.x) * sin / cos;
            edgePos = new Vector2(x, y);
        }
        else
        {
            float y = sin > 0 ? Screen.height : 0;
            float x = screenCenter.x + (y - screenCenter.y) * cos / sin;
            edgePos = new Vector2(x, y);
        }

        // Clamp to screen edges with padding
        edgePos.x = Mathf.Clamp(edgePos.x, hudConfig.panelPadding, Screen.width - hudConfig.panelPadding);
        edgePos.y = Mathf.Clamp(edgePos.y, hudConfig.panelPadding, Screen.height - hudConfig.panelPadding);

        // Use the symbol from config
        string symbol = !string.IsNullOrEmpty(cfg.symbol) ? cfg.symbol : "◉";

        // Draw arrow indicator with rotation toward target
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle * Mathf.Rad2Deg - 90f, edgePos);

        // Add blinking effect for important items
        if (cfg.important && blinkTimer < BLINK_INTERVAL)
        {
            GUI.Label(new Rect(edgePos.x - 12, edgePos.y - 12, 24, 24), $"<b>{symbol}</b>", style);
        }
        else
        {
            GUI.Label(new Rect(edgePos.x - 10, edgePos.y - 10, 20, 20), symbol, style);
        }

        GUI.matrix = matrixBackup;
    }

    void DrawTextBox(Rect rect, Color backgroundColor)
    {
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, backgroundColor);
        bgTex.Apply();

        GUI.DrawTexture(rect, bgTex);

        // Draw border
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), panelBorder);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), panelBorder);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), panelBorder);
        GUI.DrawTexture(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), panelBorder);
    }

    void DrawObjectivesPanel()
    {
        float panelX = Screen.width - hudConfig.objectivePanelWidth - hudConfig.panelPadding;
        float panelY = hudConfig.panelPadding + (hudConfig.showCompass ? compassHeight + 20 : 0);

        // Draw panel background with border
        GUI.DrawTexture(new Rect(panelX - hudConfig.panelBorderWidth, panelY - hudConfig.panelBorderWidth,
            hudConfig.objectivePanelWidth + hudConfig.panelBorderWidth * 2,
            hudConfig.objectivePanelHeight + hudConfig.panelBorderWidth * 2), panelBorder);

        GUILayout.BeginArea(new Rect(panelX, panelY, hudConfig.objectivePanelWidth, hudConfig.objectivePanelHeight), panelStyle);
        GUILayout.Label("<size=+2><b>OBJECTIVES</b></size>", panelStyle);

        if (objectiveTargets.Count == 0)
        {
            GUILayout.Label("<i>No active objectives</i>", panelStyle);
        }
        else
        {
            foreach (var objective in objectiveTargets)
            {
                if (objective == null) continue;

                GUILayout.BeginHorizontal();

                // Status indicator
                Objective objComp = objective.GetComponent<Objective>();
                string status = (objComp != null && objComp.isCompleted) ?
                    "<color=#00FF00>✓</color>" : "<color=#FF6600>●</color>";

                GUILayout.Label(status, panelStyle, GUILayout.Width(20));

                // Objective name
                TagConfig cfg = configMap.ContainsKey(objective.tag) ? configMap[objective.tag] : null;
                string objectiveName = cfg != null && !string.IsNullOrEmpty(cfg.label) ? cfg.label : objective.name;

                // Add distance for objectives
                float distance = Vector3.Distance(mainCamera.transform.position, objective.transform.position);
                string distanceText = $"<color=#AAAAAA><{distance:F0}m></color>";

                GUILayout.Label($"{objectiveName} {distanceText}", panelStyle);

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndArea();
    }

    void DrawDebugInfo()
    {
        float panelX = hudConfig.panelPadding;
        float panelY = Screen.height - 150 - hudConfig.panelPadding;

        // Draw panel background with border
        GUI.DrawTexture(new Rect(panelX - hudConfig.panelBorderWidth, panelY - hudConfig.panelBorderWidth,
            300 + hudConfig.panelBorderWidth * 2, 150 + hudConfig.panelBorderWidth * 2), panelBorder);

        GUILayout.BeginArea(new Rect(panelX, panelY, 300, 150), panelStyle);
        GUILayout.Label(debugBuilder.ToString(), panelStyle);
        GUILayout.EndArea();
    }

    Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.7f) return Color.green;
        if (healthPercent > 0.3f) return Color.yellow;
        return Color.red;
    }

    void OnValidate()
    {
        // Update config map when inspector changes
        BuildConfigMap();

        // Update styles when inspector changes
        if (Application.isPlaying)
        {
            InitializeStyles();
        }
    }
}