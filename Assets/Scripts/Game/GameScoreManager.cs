using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GameScoreManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Collectibles")]
    [SerializeField, Min(1)] private int coinCount = 10;
    [SerializeField, Min(1)] private int pointsPerCoin = 1;
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

    private Text scoreText;
    private Text weaponText;
    private Text promptText;
    private Text dialogueText;
    private PlayerGunController gunController;
    private Transform rightHand;
    private GameObject equippedGun;
    private int score;

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
        SpawnNpc();
        UpdateScoreText();
        UpdateWeaponText("None");
    }

    public void AddScore(int amount)
    {
        score += Mathf.Max(0, amount);
        UpdateScoreText();
    }

    public void EquipGun(GameObject gunPrefab, string weaponName)
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

        equippedGun = gunPrefab != null ? Instantiate(gunPrefab, holdTarget) : CreateFallbackGun(holdTarget, weaponName);
        equippedGun.name = $"Equipped {weaponName}";
        equippedGun.transform.localPosition = new Vector3(0.12f, 0.02f, 0.04f);
        equippedGun.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
        equippedGun.transform.localScale = gunPrefab != null ? Vector3.one * 0.28f : Vector3.one;
        DisableGameplayPhysics(equippedGun);

        if (gunController == null)
        {
            SetupPlayerWeaponController();
        }

        gunController.SetEquippedGun(equippedGun.transform);
        UpdateWeaponText(weaponName);
        ShowDialogue($"Equipped {weaponName}. Left click to shoot.");
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

        scoreText = CreateText(canvas.transform, "Score Text", "Score: 0", 24, TextAnchor.UpperLeft, new Vector2(20f, -18f), new Vector2(220f, 42f));
        weaponText = CreateText(canvas.transform, "Weapon Text", "Weapon: None", 20, TextAnchor.UpperLeft, new Vector2(20f, -54f), new Vector2(340f, 36f));
        promptText = CreateText(canvas.transform, "Interaction Prompt", string.Empty, 24, TextAnchor.LowerCenter, new Vector2(0f, 92f), new Vector2(760f, 42f));
        dialogueText = CreateText(canvas.transform, "Dialogue Text", string.Empty, 18, TextAnchor.LowerCenter, new Vector2(0f, 34f), new Vector2(940f, 64f));
        CreateCompass(canvas.transform);
    }

    private void CreateCompass(Transform canvasTransform)
    {
        RectTransform panel = CreatePanel(canvasTransform, "Top Compass", new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(300f, 46f));

        Text strip = CreateText(panel, "Compass Labels", "W      N      E      S", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(280f, 24f));
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
        }
        else
        {
            CreateFallbackGun(pickup.transform, weaponName);
        }

        pickup.AddComponent<GunPickup>().Initialize(this, prefab, weaponName);
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
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateWeaponText(string weaponName)
    {
        if (weaponText != null)
        {
            weaponText.text = $"Weapon: {weaponName}";
        }
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

    private GameObject CreateFallbackGun(Transform parent, string weaponName)
    {
        GameObject root = new GameObject($"{weaponName} Runtime Model");
        root.transform.SetParent(parent, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Gun Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0.14f, 0f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.16f, 0.16f);
        body.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Runtime Gun Dark", new Color(0.08f, 0.08f, 0.09f));

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Gun Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(-0.08f, -0.16f, 0f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        handle.transform.localScale = new Vector3(0.16f, 0.32f, 0.14f);
        handle.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Runtime Gun Handle", new Color(0.18f, 0.12f, 0.07f));

        DisableGameplayPhysics(root);
        return root;
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
}
