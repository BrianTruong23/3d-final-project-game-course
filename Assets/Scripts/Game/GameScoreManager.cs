using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GameScoreManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Collectibles")]
    [SerializeField, Min(1)] private int coinCount = 25;
    [SerializeField, Min(1)] private int pointsPerCoin = 1;
    [SerializeField, Min(1)] private int coinsToWin = 20;
    [SerializeField, Min(1)] private int enemiesToWin = 5;
    [SerializeField] private Vector3 collectibleAreaCenter = new Vector3(789f, 49.8f, 582f);
    [SerializeField] private Vector2 collectibleAreaSize = new Vector2(42f, 36f);
    [SerializeField] private GameObject[] gunPrefabs;
    [SerializeField] private string[] gunNames =
    {
        "Revolver 1",
        "Revolver 2",
        "Revolver 3",
        "Shotgun 1",
        "Shotgun 2"
    };

    [Header("UI")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color markerColor = new Color(1f, 0.82f, 0.18f, 1f);

    [Header("Weapon Hold Tuning")]
    [SerializeField] private Vector3 equippedGunLocalPosition = new Vector3(0.08f, 0.04f, 0.02f);
    [SerializeField] private Vector3 equippedGunLocalEulerAngles = new Vector3(0f, 135f, 90f);
    [SerializeField] private float prefabGunScale = 0.4f;
    [SerializeField] private float fallbackGunScale = 0.65f;
    [SerializeField] private bool showWeaponHoldGizmo = true;
    [SerializeField] private float weaponHoldGizmoSize = 0.18f;
    [SerializeField] private bool showWeaponHoldPreview = true;
    [SerializeField] private Color weaponHoldPreviewColor = new Color(1f, 0.9f, 0.1f, 0.85f);

    private Text objectiveText;
    private Text scoreText;
    private Text enemyText;
    private Text weaponText;
    private Text chestText;
    private Text promptText;
    private Text dialogueText;
    private Text inventoryText;
    private GameObject inventoryPanel;
    private PlayerGunController gunController;
    private Transform rightHand;
    private GameObject equippedGun;
    private GameObject weaponHoldPreview;
    private readonly List<GunInventoryItem> acquiredGuns = new List<GunInventoryItem>();
    private int coinsCollected;
    private int enemiesDefeated;
    private int equippedGunIndex = -1;
    private bool hasWon;

    private readonly Color[] gunColors =
    {
        new Color(0.95f, 0.22f, 0.18f),
        new Color(0.18f, 0.52f, 0.95f),
        new Color(0.18f, 0.78f, 0.34f),
        new Color(0.95f, 0.76f, 0.18f),
        new Color(0.68f, 0.32f, 0.95f)
    };

    private sealed class GunInventoryItem
    {
        public GameObject Prefab { get; }
        public string Name { get; }
        public Color Color { get; }

        public GunInventoryItem(GameObject prefab, string name, Color color)
        {
            Prefab = prefab;
            Name = name;
            Color = color;
        }
    }

    private void Awake()
    {
        if (player == null)
        {
            ThirdPersonController controller = FindAnyObjectByType<ThirdPersonController>();
            if (controller != null)
            {
                player = controller.transform;
            }
        }

        SetupPlayerWeaponController();
        BuildUi();
        SpawnCoins();
        SpawnGunPickups();
        // SpawnNpc(); // Removed static guide NPC since AutoSetup handles NPCs now
        UpdateScoreText();
        UpdateEnemyText();
        UpdateObjectiveText();
        UpdateWeaponText("None");
        UpdateChestText();
        UpdateInventoryText();
    }

    private void Update()
    {
        ApplyEquippedGunHoldTransform();
        UpdateObjectiveText();
        CheckWinCondition();
        UpdateWeaponHoldPreview();

        if (WasInventoryKeyPressed())
        {
            ToggleInventory();
        }

        if (WasNumberKeyPressed(1))
        {
            EquipAcquiredGun(0);
        }
        else if (WasNumberKeyPressed(2))
        {
            EquipAcquiredGun(1);
        }
        else if (WasNumberKeyPressed(3))
        {
            EquipAcquiredGun(2);
        }
    }

    public void AddScore(int amount)
    {
        if (hasWon) return;

        coinsCollected += Mathf.Max(0, amount);
        UpdateScoreText();
        CheckWinCondition();
    }

    public void AddEnemyDefeat()
    {
        if (hasWon)
        {
            return;
        }

        enemiesDefeated++;
        UpdateEnemyText();
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (hasWon || coinsCollected < coinsToWin || enemiesDefeated < enemiesToWin)
        {
            return;
        }

        hasWon = true;
        ShowDialogue($"LEVEL CLEAR! You collected {coinsToWin} coins and defeated {enemiesToWin} enemies.");
        Invoke(nameof(LoadRestartScene), 4f);
    }

    private void LoadRestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("RestartScene");
    }

    public void EquipGun(GameObject gunPrefab, string weaponName, Color weaponColor)
    {
        int inventoryIndex = GetInventoryIndex(weaponName);
        bool isNewGun = inventoryIndex < 0;

        if (isNewGun)
        {
            acquiredGuns.Add(new GunInventoryItem(gunPrefab, weaponName, weaponColor));
            inventoryIndex = acquiredGuns.Count - 1;
            ShowDialogue($"Acquired {weaponName}. Chest: {acquiredGuns.Count} unique guns acquired.");
            UpdateChestText();
            UpdateInventoryText();
        }
        else
        {
            ShowDialogue($"{weaponName} is already in your chest. Unique gun count stays {acquiredGuns.Count}.");
        }

        EquipAcquiredGun(inventoryIndex, false);
    }

    private void EquipAcquiredGun(int inventoryIndex, bool showMessage = true)
    {
        if (inventoryIndex < 0 || inventoryIndex >= acquiredGuns.Count)
        {
            ShowDialogue($"No gun in slot {inventoryIndex + 1}. Press Y to view inventory.");
            return;
        }

        GunInventoryItem gun = acquiredGuns[inventoryIndex];
        equippedGunIndex = inventoryIndex;
        EquipGunModel(gun.Prefab, gun.Name, gun.Color);
        UpdateWeaponText(gun.Name);
        UpdateInventoryText();

        if (showMessage)
        {
            ShowDialogue($"Equipped {gun.Name}. Press F to shoot.");
        }
    }

    private void EquipGunModel(GameObject gunPrefab, string weaponName, Color weaponColor)
    {
        if (player == null)
        {
            return;
        }

        if (rightHand == null)
        {
            rightHand = FindChildRecursive(player, "Right_Hand");
        }

        Transform holdTarget = rightHand != null ? rightHand : player;
        if (equippedGun != null)
        {
            Destroy(equippedGun);
        }

        HideWeaponHoldPreview();

        equippedGun = gunPrefab != null ? Instantiate(gunPrefab, holdTarget) : CreateFallbackGun(holdTarget, weaponName);
        equippedGun.name = $"Equipped {weaponName}";
        ApplyEquippedGunHoldTransform(gunPrefab != null);
        DisableGameplayPhysics(equippedGun);
        ApplyColorToRenderers(equippedGun, weaponColor, $"{weaponName} Equipped Color");

        if (gunController == null)
        {
            SetupPlayerWeaponController();
        }

        gunController.SetEquippedGun(equippedGun.transform, weaponColor, equippedGunIndex);
    }

    private void ApplyEquippedGunHoldTransform()
    {
        if (equippedGun == null)
        {
            return;
        }

        ApplyEquippedGunHoldTransform(!equippedGun.name.Contains("Runtime Model"));
    }

    private void ApplyEquippedGunHoldTransform(bool usesPrefabScale)
    {
        if (equippedGun == null)
        {
            return;
        }

        equippedGun.transform.localPosition = equippedGunLocalPosition;
        equippedGun.transform.localRotation = Quaternion.Euler(equippedGunLocalEulerAngles);
        equippedGun.transform.localScale = Vector3.one * (usesPrefabScale ? prefabGunScale : fallbackGunScale);
        if (gunController != null)
        {
            gunController.RefreshEquippedGunRestPose();
        }
    }

    public void SetPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    public void ClearPrompt(string message)
    {
        if (promptText != null && promptText.text == message)
        {
            promptText.text = string.Empty;
        }
    }

    public void ShowDialogue(string message)
    {
        if (dialogueText != null)
        {
            dialogueText.text = message;
        }
    }

    private void BuildUi()
    {
        Canvas canvas = new GameObject("Gameplay HUD Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        objectiveText = CreateText(canvas.transform, "Objective Text", GetObjectiveMessage(), 20, TextAnchor.UpperLeft, new Vector2(20f, -18f), new Vector2(760f, 34f));
        scoreText = CreateText(canvas.transform, "Coin Status Text", $"Coins: 0/{coinsToWin}", 22, TextAnchor.UpperLeft, new Vector2(20f, -54f), new Vector2(240f, 34f));
        enemyText = CreateText(canvas.transform, "Enemy Status Text", $"Enemies: 0/{enemiesToWin}", 22, TextAnchor.UpperLeft, new Vector2(20f, -88f), new Vector2(260f, 34f));
        weaponText = CreateText(canvas.transform, "Weapon Text", "Weapon: None", 20, TextAnchor.UpperLeft, new Vector2(20f, -122f), new Vector2(340f, 34f));
        chestText = CreateText(canvas.transform, "Chest Status Text", "Chest: 0 unique guns", 20, TextAnchor.UpperLeft, new Vector2(20f, -154f), new Vector2(360f, 34f));
        promptText = CreateText(canvas.transform, "Interaction Prompt", string.Empty, 24, TextAnchor.LowerCenter, new Vector2(0f, 92f), new Vector2(760f, 42f));
        dialogueText = CreateText(canvas.transform, "Dialogue Text", string.Empty, 18, TextAnchor.LowerCenter, new Vector2(0f, 34f), new Vector2(940f, 64f));
        CreateText(canvas.transform, "Purpose Text", "Talk to an NPC for guidance.", 18, TextAnchor.LowerRight, new Vector2(-20f, 20f), new Vector2(350f, 36f));
        CreateInventoryPanel(canvas.transform);
        CreateCompass(canvas.transform);
    }

    private void CreateInventoryPanel(Transform canvasTransform)
    {
        RectTransform panel = CreatePanel(canvasTransform, "Inventory Panel", new Vector2(1f, 1f), new Vector2(-24f, -86f), new Vector2(360f, 220f));
        panel.pivot = new Vector2(1f, 1f);
        inventoryPanel = panel.gameObject;

        Text title = CreateText(panel, "Inventory Title", "Inventory", 24, TextAnchor.UpperLeft, new Vector2(16f, -14f), new Vector2(320f, 32f));
        title.color = new Color(1f, 0.92f, 0.55f);

        inventoryText = CreateText(panel, "Inventory Text", string.Empty, 18, TextAnchor.UpperLeft, new Vector2(16f, -54f), new Vector2(328f, 150f));
        inventoryPanel.SetActive(false);
    }

    private void CreateCompass(Transform canvasTransform)
    {
        RectTransform panel = CreatePanel(canvasTransform, "Top Compass", new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(300f, 46f));

        Text strip = CreateText(panel, "Compass Labels", "Nearest Enemy", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(280f, 24f));
        strip.color = new Color(1f, 1f, 1f, 0.78f);

        Text markerText = CreateText(panel, "Compass Marker", "\u25b2", 22, TextAnchor.MiddleCenter, new Vector2(0f, -1f), new Vector2(44f, 28f));
        markerText.color = markerColor;

        Text heading = CreateText(panel, "Heading Text", "N 000\u00b0", 14, TextAnchor.MiddleCenter, new Vector2(0f, -18f), new Vector2(120f, 18f));
        heading.color = new Color(1f, 1f, 1f, 0.86f);

        CompassMarkerBar compass = panel.gameObject.AddComponent<CompassMarkerBar>();
        compass.Initialize(player, markerText.rectTransform, heading);
    }

    private void SpawnCoins()
    {
        if (player != null)
        {
            collectibleAreaCenter = player.position + new Vector3(0f, 0.8f, 0f);
        }

        for (int i = 0; i < coinCount; i++)
        {
            float angle = i * Mathf.PI * 2f / coinCount;
            float radiusX = collectibleAreaSize.x * 0.5f;
            float radiusZ = collectibleAreaSize.y * 0.5f;
            Vector3 position = collectibleAreaCenter + new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
            position.y = SampleGroundHeight(position) + 1.05f;
            CreateCoin(position, i + 1);
        }
    }

    private void CreateCoin(Vector3 position, int index)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = $"Collectible Coin {index:00}";
        coin.transform.position = position;
        coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);

        Renderer renderer = coin.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateCoinMaterial();

        Collider collider = coin.GetComponent<Collider>();
        collider.isTrigger = true;
        AddTriggerBody(coin);

        coin.AddComponent<CoinCollectible>().Initialize(this, pointsPerCoin);
    }

    private void SpawnGunPickups()
    {
        Vector3 center = player != null ? player.position : collectibleAreaCenter;
        int gunCount = Mathf.Max(5, gunNames != null ? gunNames.Length : 0);

        for (int i = 0; i < gunCount; i++)
        {
            float offset = i - (gunCount - 1) * 0.5f;
            Vector3 position = center + new Vector3(offset * 3.1f, 0f, 9.5f + Mathf.Abs(offset) * 0.6f);
            position.y = SampleGroundHeight(position) + 1.1f;

            GameObject prefab = gunPrefabs != null && i < gunPrefabs.Length ? gunPrefabs[i] : null;
            string weaponName = GetGunName(i, prefab);
            CreateGunPickup(position, i + 1, prefab, weaponName);
        }
    }

    private void CreateGunPickup(Vector3 position, int index, GameObject prefab, string weaponName)
    {
        GameObject pickup = new GameObject($"Weapon Pickup {index:00} - {weaponName}");
        pickup.transform.position = position;

        SphereCollider trigger = pickup.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.15f;
        AddTriggerBody(pickup);

        if (prefab != null)
        {
            GameObject display = Instantiate(prefab, pickup.transform);
            display.name = $"{weaponName} Display";
            display.transform.localPosition = Vector3.zero;
            display.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            display.transform.localScale = Vector3.one * 0.42f;
            DisableGameplayPhysics(display);
            ApplyColorToRenderers(display, GetGunColor(index - 1), $"{weaponName} Pickup Color");
        }
        else
        {
            GameObject fallback = CreateFallbackGun(pickup.transform, weaponName);
            ApplyColorToRenderers(fallback, GetGunColor(index - 1), $"{weaponName} Pickup Color");
        }

        pickup.AddComponent<GunPickup>().Initialize(this, prefab, weaponName, GetGunColor(index - 1));
    }

    private void SpawnNpc()
    {
        Vector3 center = player != null ? player.position : collectibleAreaCenter;
        Vector3 position = center + new Vector3(7f, 0f, 8f);
        position.y = SampleGroundHeight(position);

        GameObject npc = new GameObject("Guide NPC");
        npc.transform.position = position;

        SphereCollider trigger = npc.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3f;
        AddTriggerBody(npc);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Guide NPC Body";
        body.transform.SetParent(npc.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
        body.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Runtime NPC Teal", new Color(0.1f, 0.62f, 0.58f));

        Collider bodyCollider = body.GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        TextMesh label = new GameObject("Guide Label").AddComponent<TextMesh>();
        label.transform.SetParent(npc.transform, false);
        label.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        label.text = "Guide";
        label.fontSize = 48;
        label.characterSize = 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;

        npc.AddComponent<NpcDialogue>().Initialize(this);
    }

    private float SampleGroundHeight(Vector3 position)
    {
        Vector3 rayStart = position + Vector3.up * 50f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 120f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return collectibleAreaCenter.y;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Coins: {coinsCollected}/{coinsToWin}";
        }
    }

    private void UpdateObjectiveText()
    {
        if (objectiveText != null)
        {
            objectiveText.text = GetObjectiveMessage();
        }

        UpdateScoreText();
        UpdateEnemyText();
    }

    private string GetObjectiveMessage()
    {
        return $"Objective: Collect {coinsToWin} coins and defeat {enemiesToWin} enemies to clear the level.";
    }

    private void UpdateEnemyText()
    {
        if (enemyText != null)
        {
            enemyText.text = $"Enemies: {enemiesDefeated}/{enemiesToWin}";
        }
    }

    private void UpdateWeaponText(string weaponName)
    {
        if (weaponText != null)
        {
            weaponText.text = $"Weapon: {weaponName}";
        }
    }

    private void UpdateChestText()
    {
        if (chestText != null)
        {
            chestText.text = $"Chest: {acquiredGuns.Count} unique guns";
        }
    }

    private void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        bool showInventory = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(showInventory);
        UpdateInventoryText();
    }

    private void UpdateInventoryText()
    {
        if (inventoryText == null)
        {
            return;
        }

        if (acquiredGuns.Count == 0)
        {
            inventoryText.text = "No unique guns acquired.\nCollect weapon pickups to fill the chest.\n\nPress Y to close.";
            return;
        }

        string inventory = $"Unique guns acquired: {acquiredGuns.Count}\n";
        int displayCount = Mathf.Min(acquiredGuns.Count, 3);
        for (int i = 0; i < displayCount; i++)
        {
            string equippedMarker = i == equippedGunIndex ? " (equipped)" : string.Empty;
            string colorHex = ColorUtility.ToHtmlStringRGB(acquiredGuns[i].Color);
            inventory += $"{i + 1}. <color=#{colorHex}>{acquiredGuns[i].Name}</color>{equippedMarker}\n";
        }

        if (acquiredGuns.Count > 3)
        {
            inventory += $"Other unique guns in chest: {acquiredGuns.Count - 3}\n";
        }

        inventory += "\nPress 1, 2, or 3 to equip.\nPress Y to close.";
        inventoryText.text = inventory;
    }

    private void SetupPlayerWeaponController()
    {
        if (player == null)
        {
            return;
        }

        rightHand = FindChildRecursive(player, "Right_Hand");
        gunController = player.GetComponent<PlayerGunController>();
        if (gunController == null)
        {
            gunController = player.gameObject.AddComponent<PlayerGunController>();
        }
    }

    private RectTransform CreatePanel(Transform parent, string objectName, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.AddComponent<Image>();
        image.color = panelColor;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private Text CreateText(Transform parent, string objectName, string value, int size, TextAnchor alignment, Vector2 anchoredPosition, Vector2 rectSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = size;
        text.alignment = alignment;
        text.color = textColor;

        RectTransform rect = text.rectTransform;
        if (alignment == TextAnchor.UpperLeft)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }
        else if (alignment == TextAnchor.LowerCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }
        else if (alignment == TextAnchor.LowerRight)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        rect.anchorMax = rect.anchorMin;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = rectSize;
        return text;
    }

    private static Material CreateCoinMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "Runtime Coin Gold";
        material.color = new Color(1f, 0.72f, 0.12f);
        return material;
    }

    private string GetGunName(int index, GameObject prefab)
    {
        if (gunNames != null && index < gunNames.Length && !string.IsNullOrWhiteSpace(gunNames[index]))
        {
            return gunNames[index];
        }

        return prefab != null ? prefab.name.Replace("_", " ") : $"Gun {index + 1}";
    }

    private int GetInventoryIndex(string weaponName)
    {
        for (int i = 0; i < acquiredGuns.Count; i++)
        {
            if (acquiredGuns[i].Name == weaponName)
            {
                return i;
            }
        }

        return -1;
    }

    private Color GetGunColor(int index)
    {
        return gunColors[Mathf.Abs(index) % gunColors.Length];
    }

    private GameObject CreateFallbackGun(Transform parent, string weaponName)
    {
        GameObject root = new GameObject($"{weaponName} Runtime Model");
        root.transform.SetParent(parent, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Gun Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0.12f, 0f, 0f);
        body.transform.localScale = new Vector3(0.3f, 0.08f, 0.06f);
        body.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Runtime Gun Dark", new Color(0.08f, 0.08f, 0.09f));

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Gun Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0.0f, -0.1f, 0f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        handle.transform.localScale = new Vector3(0.07f, 0.18f, 0.05f);
        handle.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Runtime Gun Handle", new Color(0.18f, 0.12f, 0.07f));

        DisableGameplayPhysics(root);
        return root;
    }

    private static void ApplyColorToRenderers(GameObject root, Color color, string materialName)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = CreateRuntimeMaterial(materialName, color);
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void DisableGameplayPhysics(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>())
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    private static void AddTriggerBody(GameObject root)
    {
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private static Material CreateRuntimeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        return material;
    }

    private static bool WasInventoryKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
        {
            return true;
        }
#endif
        return Input.GetKeyDown(KeyCode.Y);
    }

    private void OnValidate()
    {
        coinCount = Mathf.Max(1, coinCount);
        pointsPerCoin = Mathf.Max(1, pointsPerCoin);
        coinsToWin = Mathf.Max(1, coinsToWin);
        enemiesToWin = Mathf.Max(1, enemiesToWin);
        prefabGunScale = Mathf.Max(0.01f, prefabGunScale);
        fallbackGunScale = Mathf.Max(0.01f, fallbackGunScale);
        weaponHoldGizmoSize = Mathf.Max(0.02f, weaponHoldGizmoSize);
        UpdateObjectiveText();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showWeaponHoldGizmo)
        {
            return;
        }

        Transform holdTarget = GetWeaponHoldTargetForGizmo();
        if (holdTarget == null)
        {
            return;
        }

        Vector3 worldPosition = holdTarget.TransformPoint(equippedGunLocalPosition);
        Quaternion worldRotation = holdTarget.rotation * Quaternion.Euler(equippedGunLocalEulerAngles);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(worldPosition, weaponHoldGizmoSize);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(worldPosition, worldPosition + worldRotation * Vector3.right * weaponHoldGizmoSize * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(worldPosition, worldPosition + worldRotation * Vector3.up * weaponHoldGizmoSize * 2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(worldPosition, worldPosition + worldRotation * Vector3.forward * weaponHoldGizmoSize * 3f);
    }

    private Transform GetWeaponHoldTargetForGizmo()
    {
        Transform sourcePlayer = player;
        if (sourcePlayer == null)
        {
            ThirdPersonController controller = FindAnyObjectByType<ThirdPersonController>();
            if (controller != null)
            {
                sourcePlayer = controller.transform;
            }
        }

        if (sourcePlayer == null)
        {
            return null;
        }

        Transform hand = FindChildRecursive(sourcePlayer, "Right_Hand");
        return hand != null ? hand : sourcePlayer;
    }

    private void UpdateWeaponHoldPreview()
    {
        if (!showWeaponHoldPreview || equippedGun != null)
        {
            HideWeaponHoldPreview();
            return;
        }

        Transform holdTarget = GetWeaponHoldTargetForGizmo();
        if (holdTarget == null)
        {
            HideWeaponHoldPreview();
            return;
        }

        if (weaponHoldPreview == null)
        {
            weaponHoldPreview = CreateFallbackGun(holdTarget, "Weapon Hold Preview");
            weaponHoldPreview.name = "Weapon Hold Preview";
            ApplyColorToRenderers(weaponHoldPreview, weaponHoldPreviewColor, "Runtime Weapon Hold Preview");
        }
        else if (weaponHoldPreview.transform.parent != holdTarget)
        {
            weaponHoldPreview.transform.SetParent(holdTarget, false);
        }

        weaponHoldPreview.SetActive(true);
        weaponHoldPreview.transform.localPosition = equippedGunLocalPosition;
        weaponHoldPreview.transform.localRotation = Quaternion.Euler(equippedGunLocalEulerAngles);
        weaponHoldPreview.transform.localScale = Vector3.one * fallbackGunScale;
    }

    private void HideWeaponHoldPreview()
    {
        if (weaponHoldPreview != null)
        {
            weaponHoldPreview.SetActive(false);
        }
    }

    private static bool WasNumberKeyPressed(int number)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (number == 1 && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                return true;
            }

            if (number == 2 && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                return true;
            }

            if (number == 3 && Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif
        return number switch
        {
            1 => Input.GetKeyDown(KeyCode.Alpha1),
            2 => Input.GetKeyDown(KeyCode.Alpha2),
            3 => Input.GetKeyDown(KeyCode.Alpha3),
            _ => false
        };
    }
}
