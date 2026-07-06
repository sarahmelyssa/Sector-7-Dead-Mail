using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckpointGameManager : MonoBehaviour
{
    private const int ParcelsToWin = 10;
    private const int MaxMistakes = 3;

    private readonly List<RouteRecord> routes = new List<RouteRecord>
    {
        new RouteRecord("AURORA", "A-17", 8, "white seal", new Color(0.82f, 0.82f, 0.76f)),
        new RouteRecord("BRINE", "B-04", 5, "blue seal", new Color(0.18f, 0.34f, 0.63f)),
        new RouteRecord("CINDER", "C-31", 12, "red seal", new Color(0.56f, 0.18f, 0.14f)),
        new RouteRecord("DUSK", "D-09", 6, "black seal", new Color(0.12f, 0.12f, 0.16f))
    };

    private Font uiFont;
    private Text statsText;
    private Text timerText;
    private Text dossierText;
    private Text rulesText;
    private Text feedbackText;
    private Text centerText;
    private Text logText;
    private TextMesh quotaWorldText;
    private GameObject manifestRoot;
    private Text manifestTitleText;
    private Text manifestFromText;
    private Text manifestToText;
    private Text manifestChecklistText;
    private Text manifestRoutesText;
    private Text manifestFooterText;
    private Text manifestBadgeText;

    private Camera mainCamera;
    private BoothPlayerController playerController;
    private PackageController currentPackage;
    private AudioSource audioSource;
    private AudioClip okClip;
    private AudioClip errorClip;
    private AudioClip knockClip;
    private AudioClip anomalyClip;

    private GameObject activeAnomaly;
    private float anomalyTimeRemaining;
    private int processedParcels;
    private int score;
    private int mistakes;
    private int caseIndex;
    private float inspectionTimeRemaining;
    private bool shiftEnded;
    private bool inspecting;
    private bool manifestOpen = true;
    private string eventLog = "";

    private readonly Vector3 parcelSpawn = new Vector3(0f, 1.05f, 9f);
    private readonly Vector3 scannerPoint = new Vector3(0f, 1.05f, 1.85f);
    private readonly Vector3 dispatchExit = new Vector3(-5.5f, 1.05f, 6.5f);
    private readonly Vector3 quarantineExit = new Vector3(5.5f, 1.05f, 6.5f);

    private void Awake()
    {
        Application.targetFrameRate = 60;
        BuildScene();
        BuildInterface();
        BuildAudio();
        SpawnNextPackage();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            TryReportAnomaly();
        }

        if (keyboard.wKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
        {
            ToggleManifest();
        }

        UpdateAnomalyThreat();
        HandlePackageRotation(keyboard);

        if (shiftEnded || !inspecting)
        {
            return;
        }

        inspectionTimeRemaining -= Time.deltaTime;
        if (inspectionTimeRemaining <= 0f)
        {
            ResolveDecision(false, true);
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            ResolveDecision(true, false);
        }
        else if (keyboard.qKey.wasPressedThisFrame)
        {
            ResolveDecision(false, false);
        }

        UpdateHud();
    }

    public void BeginInspection(PackageController package)
    {
        if (shiftEnded)
        {
            return;
        }

        currentPackage = package;
        inspecting = true;
        manifestOpen = true;
        if (manifestRoot != null)
        {
            manifestRoot.SetActive(true);
        }
        inspectionTimeRemaining = Mathf.Max(16f, 36f - processedParcels * 1.8f);
        feedbackText.text = "Encomenda no scanner. Confere etiqueta, peso, selo e sinais anomalos.";
        PlayTone(knockClip, 0.8f);
        UpdateHud();
    }

    public void ReportPlayerBump(string obstacleName)
    {
        if (shiftEnded)
        {
            return;
        }

        feedbackText.text = "Barulho no posto. O turno nao gosta de distrações.";
        AddLog("Choque no escritorio: " + obstacleName);
    }

    private void ResolveDecision(bool dispatched, bool forcedByTimer)
    {
        if (!inspecting || currentPackage == null)
        {
            return;
        }

        ParcelProfile profile = currentPackage.Profile;
        bool correct = dispatched == profile.ShouldDispatch;
        processedParcels++;
        inspecting = false;

        if (correct)
        {
            int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, inspectionTimeRemaining) * 2f);
            score += 100 + timeBonus;
            feedbackText.text = dispatched
                ? "Despacho aprovado. A encomenda seguia o codigo."
                : "Quarentena correta. Motivo: " + profile.FailureReason;
            PlayTone(okClip, 0.75f);
        }
        else
        {
            mistakes++;
            string action = forcedByTimer ? "Tempo esgotado" : (dispatched ? "Despachaste" : "Quarentenaste");
            feedbackText.text = action + " errado. Motivo real: " + profile.FailureReason;
            PlayTone(errorClip, 0.85f);
        }

        AddLog((correct ? "OK" : "ERRO") + " - " + profile.LabelId);
        currentPackage.Leave(dispatched);
        currentPackage = null;

        if (mistakes >= MaxMistakes)
        {
            EndShift(false);
            return;
        }

        if (processedParcels >= ParcelsToWin)
        {
            EndShift(true);
            return;
        }

        MaybeSpawnRoomAnomaly();
        UpdateHud();
        StartCoroutine(SpawnAfterDelay());
    }

    private void HandlePackageRotation(Keyboard keyboard)
    {
        if (currentPackage == null || shiftEnded)
        {
            return;
        }

        float direction = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            direction += 1f;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            direction -= 1f;
        }

        if (Mathf.Abs(direction) > 0f)
        {
            currentPackage.transform.Rotate(Vector3.up, direction * 95f * Time.deltaTime, Space.World);
        }
    }

    private void ToggleManifest()
    {
        manifestOpen = !manifestOpen;
        if (manifestRoot != null)
        {
            manifestRoot.SetActive(manifestOpen);
        }

        feedbackText.text = manifestOpen ? "Manifesto levantado." : "Manifesto baixado. Inspeciona a caixa.";
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(1.05f);
        SpawnNextPackage();
    }

    private void SpawnNextPackage()
    {
        ParcelProfile profile = BuildProfile(caseIndex++);
        GameObject packageObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        packageObject.name = "Package_" + profile.LabelId;
        packageObject.tag = "Package";
        packageObject.transform.position = parcelSpawn;
        packageObject.transform.localScale = profile.Size * 0.82f;

        packageObject.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.68f, 0.44f, 0.24f), "package_cardboard", new Vector2(2f, 2f));

        var body = packageObject.AddComponent<Rigidbody>();
        body.mass = profile.WeightKg;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var controller = packageObject.AddComponent<PackageController>();
        controller.Configure(this, profile, scannerPoint, dispatchExit, quarantineExit);

        AddPackageLabel(packageObject.transform, profile);
        feedbackText.text = "A esteira trouxe uma nova encomenda.";
        UpdateHud();
    }

    private ParcelProfile BuildProfile(int index)
    {
        RouteRecord route = routes[index % routes.Count];
        string labelId = "S13-" + (410 + index).ToString();
        string destination = route.Name;
        string permit = route.PermitCode;
        string seal = route.RequiredSeal;
        int weight = Mathf.Clamp(route.MaxWeightKg - 1, 2, 10);
        bool sealIntact = true;
        string visibleSignal = "normal";

        switch (index % 10)
        {
            case 1:
                permit = "VOID-00";
                break;
            case 2:
                weight = route.MaxWeightKg + 4;
                break;
            case 3:
                sealIntact = false;
                break;
            case 4:
                visibleSignal = "label shifts";
                break;
            case 5:
                destination = "NULL HALL";
                break;
            case 7:
                seal = "mirror seal";
                break;
            case 8:
                visibleSignal = "inside knocking";
                break;
        }

        bool shouldDispatch = destination == route.Name
            && permit == route.PermitCode
            && seal == route.RequiredSeal
            && weight <= route.MaxWeightKg
            && sealIntact
            && visibleSignal == "normal";

        string failure = shouldDispatch ? "tudo conforme" : BuildFailureReason(route, destination, permit, seal, weight, sealIntact, visibleSignal);
        Vector3 size = new Vector3(1.1f + weight * 0.035f, 0.7f + weight * 0.02f, 0.85f + weight * 0.025f);

        return new ParcelProfile(labelId, destination, permit, seal, weight, sealIntact, visibleSignal, shouldDispatch, failure, route.BoxColor, size);
    }

    private string BuildFailureReason(RouteRecord route, string destination, string permit, string seal, int weight, bool sealIntact, string signal)
    {
        if (destination != route.Name)
        {
            return "destino nao existe nas rotas do turno";
        }
        if (permit != route.PermitCode)
        {
            return "licenca postal invalida";
        }
        if (seal != route.RequiredSeal)
        {
            return "selo errado para a rota";
        }
        if (weight > route.MaxWeightKg)
        {
            return "peso acima do limite da rota";
        }
        if (!sealIntact)
        {
            return "selo quebrado";
        }
        if (signal != "normal")
        {
            return "sinal anomalo detectado";
        }

        return "anomalia nao catalogada";
    }

    private void MaybeSpawnRoomAnomaly()
    {
        if (activeAnomaly != null || processedParcels < 2)
        {
            return;
        }

        float chance = Mathf.Min(0.62f, 0.25f + processedParcels * 0.05f);
        if (Random.value > chance)
        {
            return;
        }

        Vector3[] spots =
        {
            new Vector3(-3.9f, 2.2f, -7.8f),
            new Vector3(3.6f, 1.6f, -8.2f),
            new Vector3(0.3f, 2.9f, -9.4f),
            new Vector3(-4.2f, 1.15f, -5.8f)
        };

        activeAnomaly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        activeAnomaly.name = "Reportable Anomaly";
        activeAnomaly.tag = "Anomaly";
        activeAnomaly.transform.position = spots[Random.Range(0, spots.Length)];
        activeAnomaly.transform.localScale = Vector3.one * 0.55f;
        activeAnomaly.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.92f, 0.08f, 0.18f));
        var collider = activeAnomaly.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        anomalyTimeRemaining = Mathf.Max(8f, 15f - processedParcels * 0.7f);

        feedbackText.text = "Algo mudou no escritorio. Olha para a anomalia e carrega F.";
        AddLog("ALARME: anomalia ambiental");
        PlayTone(anomalyClip, 0.9f);
    }

    private void UpdateAnomalyThreat()
    {
        if (activeAnomaly == null || shiftEnded)
        {
            return;
        }

        anomalyTimeRemaining -= Time.deltaTime;
        float pulse = 0.5f + Mathf.Sin(Time.time * 8f) * 0.16f;
        activeAnomaly.transform.localScale = Vector3.one * pulse;
        activeAnomaly.transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);

        if (anomalyTimeRemaining > 0f)
        {
            return;
        }

        mistakes++;
        feedbackText.text = "Anomalia ignorada. O setor cobrou um erro.";
        AddLog("ERRO - anomalia ignorada");
        PlayTone(errorClip, 0.9f);
        Destroy(activeAnomaly);
        activeAnomaly = null;

        if (mistakes >= MaxMistakes)
        {
            EndShift(false);
        }

        UpdateHud();
    }

    private void TryReportAnomaly()
    {
        if (activeAnomaly == null || shiftEnded)
        {
            feedbackText.text = "Nada anomalo reportado agora.";
            return;
        }

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 25f) && hit.collider.CompareTag("Anomaly"))
        {
            score += 75;
            feedbackText.text = "Anomalia reportada e isolada.";
            AddLog("OK - anomalia reportada");
            PlayTone(okClip, 0.75f);
            Destroy(activeAnomaly);
            activeAnomaly = null;
            UpdateHud();
        }
        else
        {
            feedbackText.text = "Relatorio falhou. Aponta a camera para a anomalia.";
        }
    }

    private void EndShift(bool victory)
    {
        shiftEnded = true;
        inspecting = false;
        centerText.gameObject.SetActive(true);
        centerText.text = victory
            ? "06:00\nCOTA CUMPRIDA\n\nR para outro turno"
            : "GAME OVER\nO setor reclamou o teu cracha.\n\nR para reiniciar";
        feedbackText.text = victory ? "Sobreviveste ao turno do Setor 13." : "Demasiados erros ou anomalias ignoradas.";
        UpdateHud();
    }

    private void BuildScene()
    {
        RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.13f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.045f);
        RenderSettings.fogDensity = 0.025f;

        CreateBox("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 21f), new Color(0.95f, 0.95f, 0.90f), "Untagged", "floor_tiles", new Vector2(6f, 11f));
        CreateBox("Rear Hall Wall", new Vector3(0f, 2f, -10.35f), new Vector3(12f, 4f, 0.25f), new Color(0.72f, 0.74f, 0.70f), "Obstacle", "wall_grime", new Vector2(6f, 2f));
        CreateBox("Front Wall", new Vector3(0f, 2f, 10.35f), new Vector3(12f, 4f, 0.25f), new Color(0.72f, 0.74f, 0.70f), "Obstacle", "wall_grime", new Vector2(6f, 2f));
        CreateBox("Left Wall", new Vector3(-6f, 2f, 0f), new Vector3(0.25f, 4f, 21f), new Color(0.72f, 0.74f, 0.70f), "Obstacle", "wall_grime", new Vector2(10f, 2f));
        CreateBox("Right Wall", new Vector3(6f, 2f, 0f), new Vector3(0.25f, 4f, 21f), new Color(0.72f, 0.74f, 0.70f), "Obstacle", "wall_grime", new Vector2(10f, 2f));
        CreateBox("Scanner Desk", new Vector3(0f, 0.58f, -0.85f), new Vector3(5.45f, 1.02f, 1.02f), new Color(0.85f, 0.85f, 0.78f), "Obstacle", "desk_metal", new Vector2(3.5f, 1.2f));
        CreateBox("Inspection Plinth", new Vector3(0f, 0.52f, 1.7f), new Vector3(2.45f, 0.24f, 1.45f), new Color(0.85f, 0.85f, 0.78f), "Obstacle", "desk_metal", new Vector2(2f, 1.2f));
        CreateBox("Conveyor Belt", new Vector3(0f, 0.48f, 5.5f), new Vector3(1.75f, 0.16f, 8.8f), new Color(0.92f, 0.92f, 0.90f), "Obstacle", "conveyor_belt", new Vector2(2f, 8f));
        CreateBox("Screen Frame", new Vector3(0f, 2.52f, 3.05f), new Vector3(4.1f, 1.15f, 0.16f), new Color(0.70f, 0.70f, 0.55f), "Obstacle", "desk_metal", new Vector2(2f, 1f));
        CreateBox("Dead Monitor", new Vector3(0f, 2.52f, 2.94f), new Vector3(3.65f, 0.78f, 0.08f), new Color(0.75f, 0.75f, 0.60f), "Obstacle", "monitor_scanlines", new Vector2(1f, 1f));
        CreateBox("Quota Display", new Vector3(-2.28f, 1.24f, 1.04f), new Vector3(0.78f, 0.34f, 0.12f), new Color(0.85f, 0.82f, 0.50f), "Obstacle", "monitor_scanlines", new Vector2(1f, 1f));
        CreateBox("Question Note", new Vector3(-2.55f, 2.98f, 2.70f), new Vector3(0.42f, 0.40f, 0.035f), new Color(1.0f, 0.92f, 0.35f), "Obstacle", "paper_manifest", new Vector2(1f, 1f));
        CreateBox("Left Shelf", new Vector3(-4.85f, 1.55f, 2.6f), new Vector3(0.38f, 2.75f, 1.85f), new Color(0.70f, 0.56f, 0.42f), "Obstacle", "desk_metal", new Vector2(1f, 2f));
        CreateBox("Right Shelf", new Vector3(4.85f, 1.55f, 2.6f), new Vector3(0.38f, 2.75f, 1.85f), new Color(0.70f, 0.56f, 0.42f), "Obstacle", "desk_metal", new Vector2(1f, 2f));
        CreateBox("Rear Clock", new Vector3(-3.2f, 1.95f, -6.9f), new Vector3(1.18f, 0.40f, 0.1f), new Color(0.85f, 0.82f, 0.52f), "Obstacle", "monitor_scanlines", new Vector2(1f, 1f));
        CreateBox("Rear Notice Board", new Vector3(3.15f, 1.9f, -7.2f), new Vector3(1.55f, 0.92f, 0.1f), new Color(0.85f, 0.62f, 0.42f), "Obstacle", "paper_manifest", new Vector2(1f, 1f));
        CreateBox("Ceiling Alarm", new Vector3(0f, 3.55f, -6.65f), new Vector3(0.38f, 0.38f, 0.38f), new Color(0.55f, 0.02f, 0.02f), "Obstacle");
        CreateBox("Left Rear Counter", new Vector3(-4.5f, 0.62f, -6.9f), new Vector3(1.35f, 0.95f, 4.4f), new Color(0.80f, 0.80f, 0.74f), "Obstacle", "desk_metal", new Vector2(1f, 4f));
        CreateBox("Right Rear Counter", new Vector3(4.5f, 0.62f, -6.9f), new Vector3(1.35f, 0.95f, 4.4f), new Color(0.80f, 0.80f, 0.74f), "Obstacle", "desk_metal", new Vector2(1f, 4f));
        CreateBox("Rear Hazard Track", new Vector3(0f, 0.01f, -6.6f), new Vector3(1.15f, 0.035f, 6.6f), new Color(0.95f, 0.86f, 0.45f), "Untagged", "hazard_stripes", new Vector2(1f, 6f));
        CreateControlButton("X", new Vector3(-2.15f, 1.12f, -1.38f), new Color(0.50f, 0.10f, 0.07f), "button_red");
        CreateControlButton("<", new Vector3(-0.82f, 1.12f, -1.38f), new Color(0.34f, 0.34f, 0.72f), "button_purple");
        CreateControlButton(">", new Vector3(0.82f, 1.12f, -1.38f), new Color(0.34f, 0.34f, 0.72f), "button_purple");
        CreateControlButton("O", new Vector3(2.15f, 1.12f, -1.38f), new Color(0.05f, 0.42f, 0.16f), "button_green");
        quotaWorldText = AddWorldText("00/10", new Vector3(-2.28f, 1.24f, 0.96f), 0.16f, new Color(0.96f, 0.91f, 0.58f), TextAnchor.MiddleCenter);
        AddWorldText("?", new Vector3(-2.83f, 3.05f, 2.62f), 0.28f, Color.black, TextAnchor.MiddleCenter);
        CreateBox("Dispatch Gate", dispatchExit + new Vector3(0f, 0.6f, -0.9f), new Vector3(1.4f, 1.2f, 0.2f), new Color(0.17f, 0.45f, 0.28f), "Obstacle");
        CreateBox("Quarantine Gate", quarantineExit + new Vector3(0f, 0.6f, -0.9f), new Vector3(1.4f, 1.2f, 0.2f), new Color(0.58f, 0.16f, 0.18f), "Obstacle");

        GameObject inspectionZone = CreateBox("Scanner Trigger", scannerPoint, new Vector3(2.6f, 2.2f, 1.45f), new Color(1f, 0.83f, 0.24f, 0.12f), "InspectionZone");
        inspectionZone.GetComponent<Collider>().isTrigger = true;
        inspectionZone.GetComponent<Renderer>().enabled = false;

        GameObject player = new GameObject("Night Shift Inspector");
        player.transform.position = new Vector3(0f, 0.05f, -3.05f);
        player.AddComponent<CapsuleCollider>();
        player.AddComponent<Rigidbody>();
        playerController = player.AddComponent<BoothPlayerController>();

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
        }
        mainCamera.fieldOfView = 58f;
        mainCamera.nearClipPlane = 0.05f;
        mainCamera.backgroundColor = new Color(0.015f, 0.018f, 0.024f);
        mainCamera.enabled = true;
        playerController.Configure(mainCamera, this);

        var light = new GameObject("Flickering Fluorescent").AddComponent<Light>();
        light.type = LightType.Point;
        light.transform.position = new Vector3(0f, 4f, -1f);
        light.range = 12f;
        light.intensity = 1.05f;

        var scannerLight = new GameObject("Scanner Red Light").AddComponent<Light>();
        scannerLight.type = LightType.Point;
        scannerLight.transform.position = new Vector3(0f, 2.3f, 0.7f);
        scannerLight.color = new Color(1f, 0.12f, 0.10f);
        scannerLight.range = 5f;
        scannerLight.intensity = 1.3f;

        var alarmLight = new GameObject("Rear Alarm Light").AddComponent<Light>();
        alarmLight.type = LightType.Point;
        alarmLight.transform.position = new Vector3(0f, 3.2f, -6.7f);
        alarmLight.color = new Color(1f, 0.02f, 0.01f);
        alarmLight.range = 7f;
        alarmLight.intensity = 0.75f;
    }

    private void BuildInterface()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        var canvasObject = new GameObject("Sector 13 HUD");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        statsText = CreateText("Stats", canvasObject.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -72f), new Vector2(-26f, -18f), 25, TextAnchor.MiddleLeft, new Color(0.90f, 0.86f, 0.58f));
        timerText = CreateText("Timer", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, -132f), new Vector2(120f, -82f), 32, TextAnchor.MiddleCenter, new Color(1f, 0.22f, 0.18f));
        manifestRoot = CreateManifestDocument(canvasObject.transform);
        rulesText = CreatePanelText("Rules", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-430f, 150f), new Vector2(-24f, -120f), 19);
        rulesText.transform.parent.gameObject.SetActive(false);
        feedbackText = CreatePanelText("Feedback", canvasObject.transform, new Vector2(0.26f, 0f), new Vector2(0.74f, 0f), new Vector2(0f, 24f), new Vector2(0f, 108f), 23);
        logText = CreateText("Log", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-360f, 116f), new Vector2(360f, 220f), 18, TextAnchor.UpperCenter, new Color(0.86f, 0.88f, 0.83f));
        centerText = CreateText("Center Result", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -190f), new Vector2(420f, 190f), 46, TextAnchor.MiddleCenter, Color.white);
        centerText.gameObject.SetActive(false);

        UpdateRulesText();
    }

    private void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        okClip = CreateToneClip("ok", 660f, 0.14f);
        errorClip = CreateToneClip("error", 150f, 0.24f);
        knockClip = CreateToneClip("scanner", 260f, 0.08f);
        anomalyClip = CreateToneClip("anomaly", 92f, 0.35f);
    }

    private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color, string tagName, string textureName = null, Vector2? textureScale = null)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.tag = tagName;
        box.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color, textureName, textureScale);
        return box;
    }

    private void CreateControlButton(string label, Vector3 position, Color color, string textureName)
    {
        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = label + " Button";
        button.transform.position = position;
        button.transform.localScale = new Vector3(0.56f, 0.13f, 0.45f);
        button.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color, textureName);

        AddWorldText(label, position + new Vector3(0f, 0.10f, -0.06f), 0.12f, new Color(0.92f, 0.90f, 0.76f), TextAnchor.MiddleCenter);
    }

    private TextMesh AddWorldText(string content, Vector3 position, float size, Color color, TextAnchor anchor)
    {
        GameObject textObject = new GameObject("World Text " + content);
        textObject.transform.position = position;
        textObject.transform.rotation = Quaternion.Euler(64f, 0f, 0f);

        var mesh = textObject.AddComponent<TextMesh>();
        mesh.text = content;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = anchor;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        return mesh;
    }

    private void AddPackageLabel(Transform packageRoot, ParcelProfile profile)
    {
        GameObject label = GameObject.CreatePrimitive(PrimitiveType.Cube);
        label.name = "Visible Label";
        label.transform.SetParent(packageRoot);
        label.transform.localPosition = new Vector3(0f, 0.08f, -0.515f);
        label.transform.localScale = new Vector3(0.74f, 0.34f, 0.035f);
        label.GetComponent<Renderer>().sharedMaterial = CreateMaterial(profile.ShouldDispatch ? Color.white : new Color(1f, 0.18f, 0.18f), "package_label_green");
        Destroy(label.GetComponent<Collider>());

        GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tape.name = "Route Tape";
        tape.transform.SetParent(packageRoot);
        tape.transform.localPosition = new Vector3(0.42f, 0.03f, -0.535f);
        tape.transform.localScale = new Vector3(0.14f, 1.08f, 0.04f);
        tape.GetComponent<Renderer>().sharedMaterial = CreateMaterial(profile.BoxColor);
        Destroy(tape.GetComponent<Collider>());

        AddLocalText(packageRoot, profile.LabelId + "\nTO " + profile.Destination, new Vector3(-0.08f, 0.12f, -0.56f), Quaternion.Euler(0f, 180f, 0f), 0.055f, Color.black);
    }

    private void AddLocalText(Transform parent, string content, Vector3 localPosition, Quaternion localRotation, float size, Color color)
    {
        GameObject textObject = new GameObject("Package Label Text");
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = localRotation;
        textObject.transform.localScale = Vector3.one;

        var mesh = textObject.AddComponent<TextMesh>();
        mesh.text = content;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
    }

    private Material CreateMaterial(Color color, string textureName = null, Vector2? textureScale = null)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        Texture2D texture = LoadTexture(textureName);
        if (texture != null)
        {
            material.mainTexture = texture;
            material.mainTextureScale = textureScale ?? Vector2.one;
        }
        return material;
    }

    private Texture2D LoadTexture(string textureName)
    {
        if (string.IsNullOrEmpty(textureName))
        {
            return null;
        }

        Texture2D texture = Resources.Load<Texture2D>("Sector13/" + textureName);
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
        }

        return texture;
    }

    private Text CreatePanelText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize)
    {
        var panel = new GameObject(name + " Panel");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = offsetMin;
        panelRect.offsetMax = offsetMax;
        var image = panel.AddComponent<Image>();
        image.color = new Color(0.015f, 0.018f, 0.022f, 0.86f);

        return CreateText(name, panel.transform, Vector2.zero, Vector2.one, new Vector2(18f, 14f), new Vector2(-18f, -14f), fontSize, TextAnchor.UpperLeft, new Color(0.92f, 0.93f, 0.88f));
    }

    private GameObject CreateManifestDocument(Transform parent)
    {
        var panel = new GameObject("Package Manifest Document");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 28f);
        panelRect.sizeDelta = new Vector2(560f, 705f);
        var image = panel.AddComponent<Image>();
        Texture2D paperTexture = LoadTexture("paper_manifest");
        if (paperTexture != null)
        {
            image.sprite = Sprite.Create(paperTexture, new Rect(0f, 0f, paperTexture.width, paperTexture.height), new Vector2(0.5f, 0.5f), 100f);
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0.64f, 0.58f, 0.46f, 0.94f);
        }

        Color ink = new Color(0.16f, 0.10f, 0.09f);
        Color line = new Color(0.22f, 0.16f, 0.14f, 0.82f);
        Color shade = new Color(0.24f, 0.16f, 0.14f, 0.12f);

        CreateDocumentRect("Outer Border", panel.transform, Vector2.zero, new Vector2(500f, 645f), line);
        CreateDocumentLine("Header Bottom", panel.transform, new Vector2(0f, 188f), new Vector2(500f, 4f), line);
        CreateDocumentLine("Title Bottom", panel.transform, new Vector2(0f, 118f), new Vector2(500f, 4f), line);
        CreateDocumentLine("Footer Top", panel.transform, new Vector2(0f, -272f), new Vector2(500f, 4f), line);
        CreateDocumentLine("Header Vertical", panel.transform, new Vector2(-155f, 235f), new Vector2(4f, 90f), line);
        CreateDocumentLine("Header Row", panel.transform, new Vector2(84f, 235f), new Vector2(320f, 4f), line);
        CreateDocumentRect("Icon Box", panel.transform, new Vector2(-208f, 235f), new Vector2(92f, 92f), line);
        CreateDocumentFilled("Title Band", panel.transform, new Vector2(0f, 153f), new Vector2(500f, 64f), shade);
        CreateDocumentFilled("Footer Band", panel.transform, new Vector2(0f, -298f), new Vector2(500f, 48f), new Color(0.18f, 0.12f, 0.11f, 0.16f));

        manifestBadgeText = CreateAnchoredText("Manifest Icon", panel.transform, new Vector2(-208f, 235f), new Vector2(86f, 86f), 38, TextAnchor.MiddleCenter, ink);
        manifestBadgeText.text = "PKG";
        manifestFromText = CreateAnchoredText("Manifest From", panel.transform, new Vector2(88f, 258f), new Vector2(300f, 36f), 22, TextAnchor.MiddleLeft, ink);
        manifestToText = CreateAnchoredText("Manifest To", panel.transform, new Vector2(88f, 213f), new Vector2(300f, 36f), 22, TextAnchor.MiddleLeft, ink);
        manifestTitleText = CreateAnchoredText("Manifest Title", panel.transform, new Vector2(0f, 151f), new Vector2(480f, 54f), 34, TextAnchor.MiddleCenter, ink);
        manifestChecklistText = CreateAnchoredText("Manifest Checklist", panel.transform, new Vector2(0f, -65f), new Vector2(460f, 330f), 23, TextAnchor.UpperLeft, ink);
        manifestRoutesText = CreateAnchoredText("Manifest Routes", panel.transform, new Vector2(0f, -218f), new Vector2(460f, 88f), 17, TextAnchor.UpperLeft, new Color(0.20f, 0.13f, 0.11f));
        manifestFooterText = CreateAnchoredText("Manifest Footer", panel.transform, new Vector2(0f, -302f), new Vector2(460f, 44f), 17, TextAnchor.MiddleCenter, new Color(0.17f, 0.10f, 0.09f));

        manifestTitleText.fontStyle = FontStyle.Bold;
        manifestFooterText.fontStyle = FontStyle.Bold;
        dossierText = manifestChecklistText;
        return panel;
    }

    private Text CreateAnchoredText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor, Color color)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private void CreateDocumentFilled(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var rect = new GameObject(name);
        rect.transform.SetParent(parent, false);
        var rectTransform = rect.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rect.AddComponent<Image>().color = color;
    }

    private void CreateDocumentLine(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        CreateDocumentFilled(name, parent, anchoredPosition, size, color);
    }

    private void CreateDocumentRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        float thickness = 4f;
        CreateDocumentLine(name + " Top", parent, anchoredPosition + new Vector2(0f, size.y * 0.5f), new Vector2(size.x, thickness), color);
        CreateDocumentLine(name + " Bottom", parent, anchoredPosition + new Vector2(0f, -size.y * 0.5f), new Vector2(size.x, thickness), color);
        CreateDocumentLine(name + " Left", parent, anchoredPosition + new Vector2(-size.x * 0.5f, 0f), new Vector2(thickness, size.y), color);
        CreateDocumentLine(name + " Right", parent, anchoredPosition + new Vector2(size.x * 0.5f, 0f), new Vector2(thickness, size.y), color);
    }

    private Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, TextAnchor anchor, Color color)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private void UpdateHud()
    {
        statsText.text = "SETOR 13: TURNO NOTURNO  |  Pontos: " + score
            + "  |  Erros: " + mistakes + "/" + MaxMistakes
            + "  |  Cota: " + processedParcels + "/" + ParcelsToWin
            + "  |  Anomalia: " + (activeAnomaly == null ? "limpo" : Mathf.CeilToInt(anomalyTimeRemaining) + "s");

        timerText.text = inspecting ? Mathf.CeilToInt(inspectionTimeRemaining).ToString() : "--";
        if (quotaWorldText != null)
        {
            quotaWorldText.text = processedParcels.ToString("00") + "/" + ParcelsToWin.ToString("00");
        }

        UpdateManifest();

        logText.text = eventLog;
    }

    private void UpdateManifest()
    {
        if (manifestTitleText == null)
        {
            return;
        }

        manifestTitleText.text = "Package Contents";
        manifestRoutesText.text = "APPROVED ROUTES\n" + BuildCompactRouteSummary();
        manifestFooterText.text = "CONFIDENTIAL  -  compliance is not a courtesy";

        if (currentPackage == null)
        {
            manifestBadgeText.text = "PKG";
            manifestFromText.text = "From: --";
            manifestToText.text = "To: --";
            manifestChecklistText.text =
                "Awaiting package...\n\n"
                + "- Mouse: look\n"
                + "- W / Tab: lower manifest\n"
                + "- S / RMB: look behind\n"
                + "- A / D: rotate package\n"
                + "- E: dispatch    Q: quarantine\n"
                + "- F: report anomaly";
            return;
        }

        ParcelProfile profile = currentPackage.Profile;
        manifestBadgeText.text = profile.SealIntact ? "PKG" : "!";
        manifestFromText.text = "From: " + profile.LabelId;
        manifestToText.text = "To: " + profile.Destination;
        manifestChecklistText.text =
            "- Permit must match route\n"
            + "  " + profile.PermitCode + "\n"
            + "- Seal must match route\n"
            + "  " + profile.Seal + "\n"
            + "- Seal must be intact\n"
            + "  " + (profile.SealIntact ? "intact" : "broken") + "\n"
            + "- Weight within limit\n"
            + "  " + profile.WeightKg + " kg\n"
            + "- Scan must be normal\n"
            + "  " + profile.VisibleSignal + "\n\n"
            + "W/Tab lower paper\nA/D rotate box\nE dispatch   Q quarantine";
    }

    private void UpdateRulesText()
    {
        string registry = "";
        foreach (RouteRecord route in routes)
        {
            registry += route.Name + " | " + route.PermitCode + " | max " + route.MaxWeightKg + "kg\n"
                + "Selo: " + route.RequiredSeal + "\n\n";
        }

        rulesText.text =
            "CODIGO DO TURNO\n\n"
            + "1. Destino, licenca e selo devem bater com a rota.\n"
            + "2. Peso acima do limite vai para quarentena.\n"
            + "3. Selo quebrado nunca despacha.\n"
            + "4. Sinais anomalos tambem invalidam a caixa.\n"
            + "5. Anomalias no escritorio: olha e carrega F.\n"
            + "6. O inspetor fica preso ao posto; S vira para tras.\n"
            + "7. Tres erros encerram o turno.\n\n"
            + "ROTAS APROVADAS\n\n"
            + registry;
    }

    private string BuildCompactRouteSummary()
    {
        string summary = "";
        foreach (RouteRecord route in routes)
        {
            summary += route.Name + " " + route.PermitCode + " " + route.RequiredSeal + " <=" + route.MaxWeightKg + "kg\n";
        }

        return summary;
    }

    private void AddLog(string message)
    {
        eventLog = message + "\n" + eventLog;
        string[] lines = eventLog.Split('\n');
        if (lines.Length > 5)
        {
            eventLog = string.Join("\n", lines, 0, 5);
        }
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float fade = 1f - i / (float)sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * 0.25f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayTone(AudioClip clip, float volume)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}

public class RouteRecord
{
    public readonly string Name;
    public readonly string PermitCode;
    public readonly int MaxWeightKg;
    public readonly string RequiredSeal;
    public readonly Color BoxColor;

    public RouteRecord(string name, string permitCode, int maxWeightKg, string requiredSeal, Color boxColor)
    {
        Name = name;
        PermitCode = permitCode;
        MaxWeightKg = maxWeightKg;
        RequiredSeal = requiredSeal;
        BoxColor = boxColor;
    }
}

public class ParcelProfile
{
    public readonly string LabelId;
    public readonly string Destination;
    public readonly string PermitCode;
    public readonly string Seal;
    public readonly int WeightKg;
    public readonly bool SealIntact;
    public readonly string VisibleSignal;
    public readonly bool ShouldDispatch;
    public readonly string FailureReason;
    public readonly Color BoxColor;
    public readonly Vector3 Size;

    public ParcelProfile(string labelId, string destination, string permitCode, string seal, int weightKg, bool sealIntact, string visibleSignal, bool shouldDispatch, string failureReason, Color boxColor, Vector3 size)
    {
        LabelId = labelId;
        Destination = destination;
        PermitCode = permitCode;
        Seal = seal;
        WeightKg = weightKg;
        SealIntact = sealIntact;
        VisibleSignal = visibleSignal;
        ShouldDispatch = shouldDispatch;
        FailureReason = failureReason;
        BoxColor = boxColor;
        Size = size;
    }
}
