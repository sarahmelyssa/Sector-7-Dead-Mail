using TMPro;
using UnityEngine;

public class InspectionRoomBuilder : MonoBehaviour
{
    private const string InspectionViewVersionMarkerName = "InspectionView_v49";

    [SerializeField] private bool buildOnAwake = true;
    [SerializeField] private Transform packageSpawnPoint = null;

    [Header("Inspection Room Lighting")]
    [Tooltip("Main package pool light. Increase this if the box is not the brightest object.")]
    [SerializeField] private float overheadSpotlightIntensity = 14.2f;
    [Tooltip("How far the package spotlight reaches before fading out.")]
    [SerializeField] private float overheadSpotlightRange = 5.25f;
    [Tooltip("Smaller angle = tighter, more dramatic cone over the package.")]
    [SerializeField] private float overheadSpotlightAngle = 84f;
    [Tooltip("Small purple/blue fill. Keep low so the room does not become flat grey again.")]
    [SerializeField] private float purpleFillIntensity = 0.035f;
    // Core palette for the generated inspection room. Tune here before changing gameplay scripts.
    private readonly Color ambientDarkness = new Color(0.00015f, 0.00010f, 0.00035f);
    private readonly Color fogDarkPurple = new Color(0.0010f, 0.0004f, 0.0022f);
    private readonly Color darkWallColor = new Color(0.004f, 0.003f, 0.008f);
    private readonly Color darkFloorColor = new Color(0.003f, 0.0025f, 0.006f);
    private readonly Color darkMetalColor = new Color(0.015f, 0.014f, 0.022f);
    private readonly Color purpleAccentColor = new Color(0.255f, 0.105f, 0.520f);
    private readonly Color coldBlueGlowColor = new Color(0.070f, 0.095f, 0.270f);
    private readonly Color packageSpotlightColor = new Color(1.000f, 0.855f, 0.545f);

    private void Awake()
    {
        if (buildOnAwake)
        {
            BuildScene();
        }
    }

    public void BuildScene()
    {
        // Play Mode layout is generated here. Manual edits to these objects will not persist.
        GameObject sectorRoot = CheckpointBootstrap.GetOrCreateRoot(CheckpointBootstrap.RuntimeRootName, null);
        Transform roomRuntimeRoot = CheckpointBootstrap.GetOrCreateRoot(CheckpointBootstrap.RoomRootName, sectorRoot.transform).transform;
        Transform inspectionDeskRuntimeRoot = CheckpointBootstrap.GetOrCreateRoot(CheckpointBootstrap.InspectionDeskRootName, sectorRoot.transform).transform;
        Transform physicalButtonsRuntimeRoot = CheckpointBootstrap.GetOrCreateRoot(CheckpointBootstrap.PhysicalButtonsRootName, sectorRoot.transform).transform;
        Transform reportAreaRuntimeRoot = CheckpointBootstrap.GetOrCreateRoot(CheckpointBootstrap.ReportAreaRootName, sectorRoot.transform).transform;
        Transform packageRuntimeRoot = CheckpointBootstrap.GetOrCreateRoot(CheckpointBootstrap.PackageSystemRootName, sectorRoot.transform).transform;

        GameObject existingRoot = GameObject.Find("Inspection Room Root");
        if (existingRoot != null)
        {
            if (existingRoot.transform.Find(InspectionViewVersionMarkerName) != null)
            {
                if (existingRoot.transform.parent != roomRuntimeRoot)
                {
                    existingRoot.transform.SetParent(roomRuntimeRoot, false);
                }

                return;
            }

            existingRoot.name = "Inspection Room Root Old";
            Destroy(existingRoot);
        }

        ClearGeneratedChildren(inspectionDeskRuntimeRoot);
        ClearGeneratedChildren(physicalButtonsRuntimeRoot);
        ClearGeneratedChildren(reportAreaRuntimeRoot);
        ClearGeneratedChildren(packageRuntimeRoot);

        var root = new GameObject("Inspection Room Root");
        root.transform.SetParent(roomRuntimeRoot, false);
        var versionMarker = new GameObject(InspectionViewVersionMarkerName);
        versionMarker.transform.SetParent(root.transform, false);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientDarkness;
        RenderSettings.skybox = null;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.subtractiveShadowColor = Color.black;
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogDarkPurple;
        RenderSettings.fogDensity = 0.115f;
        DimSceneDirectionalLights();

        BuildRoom(root.transform);
        BuildReportMonitor(root.transform);
        BuildInspectionDesk(inspectionDeskRuntimeRoot, physicalButtonsRuntimeRoot, reportAreaRuntimeRoot, packageRuntimeRoot);
        BuildExitAndCorridor(root.transform);
        BuildLighting(root.transform);
        BuildPlayer(packageRuntimeRoot);
    }

    private void ClearGeneratedChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void DimSceneDirectionalLights()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
            {
                continue;
            }

            light.intensity = 0f;
            light.enabled = false;
            if (!light.name.Contains("DisabledByInspectionRoomBuilder"))
            {
                light.name += "_DisabledByInspectionRoomBuilder";
            }
        }
    }

    private void BuildRoom(Transform root)
    {
        CreateBox("Floor_AlmostBlack", root, new Vector3(0f, -0.05f, -1.35f), new Vector3(7.8f, 0.1f, 9.2f), new Color(0.004f, 0.004f, 0.007f));
        CreateBox("Ceiling_DarkPanels", root, new Vector3(0f, 2.72f, -1.35f), new Vector3(7.8f, 0.12f, 9.2f), new Color(0.006f, 0.005f, 0.010f));
        CreateBox("LeftWall_DarkPurpleCharcoal", root, new Vector3(-3.9f, 1.32f, -1.35f), new Vector3(0.15f, 2.72f, 9.2f), new Color(0.006f, 0.005f, 0.011f));
        CreateBox("RightWall_DarkPurpleCharcoal", root, new Vector3(3.9f, 1.32f, -1.35f), new Vector3(0.15f, 2.72f, 9.2f), new Color(0.006f, 0.005f, 0.011f));
        CreateBox("FrontInspectionWall_LostInShadow", root, new Vector3(0f, 1.32f, 3.25f), new Vector3(7.8f, 2.72f, 0.16f), new Color(0.006f, 0.005f, 0.011f));
        CreateBox("BackWall_LeftOfCorridor_AlmostBlack", root, new Vector3(-2.525f, 1.32f, -5.95f), new Vector3(2.75f, 2.72f, 0.16f), new Color(0.005f, 0.004f, 0.010f));
        CreateBox("BackWall_RightOfCorridor_AlmostBlack", root, new Vector3(2.525f, 1.32f, -5.95f), new Vector3(2.75f, 2.72f, 0.16f), new Color(0.005f, 0.004f, 0.010f));
        CreateBox("BackWall_AboveCorridor_AlmostBlack", root, new Vector3(0f, 2.42f, -5.95f), new Vector3(2.38f, 0.60f, 0.16f), new Color(0.005f, 0.004f, 0.010f));
        CreateBox("ExteriorLightBlocker_Front", root, new Vector3(0f, 1.32f, 3.72f), new Vector3(8.6f, 3.1f, 0.52f), Color.black);
        CreateBox("ExteriorLightBlocker_Left", root, new Vector3(-4.28f, 1.32f, -1.35f), new Vector3(0.52f, 3.1f, 9.8f), Color.black);
        CreateBox("ExteriorLightBlocker_Right", root, new Vector3(4.28f, 1.32f, -1.35f), new Vector3(0.52f, 3.1f, 9.8f), Color.black);
        CreateBox("ExteriorLightBlocker_CeilingCap", root, new Vector3(0f, 2.98f, -1.35f), new Vector3(8.6f, 0.42f, 9.8f), Color.black);
        CreateBox("ExteriorLightBlocker_FloorSkirt", root, new Vector3(0f, -0.28f, -1.35f), new Vector3(8.6f, 0.34f, 9.8f), Color.black);
        CreateBox("BackWall_CorridorCornerSeal_Left", root, new Vector3(-1.30f, 1.20f, -5.96f), new Vector3(0.18f, 2.20f, 0.30f), Color.black);
        CreateBox("BackWall_CorridorCornerSeal_Right", root, new Vector3(1.30f, 1.20f, -5.96f), new Vector3(0.18f, 2.20f, 0.30f), Color.black);
        CreateBox("BackWall_CorridorFloorSeal", root, new Vector3(0f, -0.06f, -5.96f), new Vector3(2.60f, 0.22f, 0.30f), Color.black);

    }

    private void BuildInspectionDesk(Transform deskRoot, Transform physicalButtonsRoot, Transform reportAreaRoot, Transform packageRoot)
    {
        var tableRoot = new GameObject("InspectionTable");
        tableRoot.transform.SetParent(deskRoot, false);

        CreateBox("InspectionTable_Surface_WornDarkMetal", tableRoot.transform, new Vector3(0f, 0.91f, 0.52f), new Vector3(4.70f, 0.22f, 1.72f), darkMetalColor);
        CreateBox("InspectionTable_FrontPanel_BlackMetal", tableRoot.transform, new Vector3(0f, 0.60f, -0.38f), new Vector3(4.95f, 0.58f, 0.14f), new Color(0.008f, 0.007f, 0.013f));
        CreateBox("InspectionTable_LeftLeg", tableRoot.transform, new Vector3(-2.12f, 0.35f, 0.62f), new Vector3(0.20f, 0.82f, 0.20f), new Color(0.007f, 0.006f, 0.012f));
        CreateBox("InspectionTable_RightLeg", tableRoot.transform, new Vector3(2.12f, 0.35f, 0.62f), new Vector3(0.20f, 0.82f, 0.20f), new Color(0.007f, 0.006f, 0.012f));

        CreateBox("PackageInspectionCenter_LitMat", tableRoot.transform, new Vector3(0f, 1.045f, 0.62f), new Vector3(1.52f, 0.045f, 1.02f), new Color(0.026f, 0.023f, 0.031f));
        CreateBox("InspectionMat_PurpleTrim_Front", tableRoot.transform, new Vector3(0f, 1.075f, 0.05f), new Vector3(1.62f, 0.020f, 0.030f), purpleAccentColor);
        CreateBox("InspectionMat_PurpleTrim_Back", tableRoot.transform, new Vector3(0f, 1.075f, 1.19f), new Vector3(1.62f, 0.020f, 0.030f), purpleAccentColor);
        BuildPhysicalButtons(physicalButtonsRoot);
        BuildPackagePath(packageRoot);
    }

    private void BuildExitAndCorridor(Transform root)
    {
        var corridorRoot = new GameObject("Generated_BackCorridor_Depth");
        corridorRoot.transform.SetParent(root, false);

        const float corridorStartZ = -6.05f;
        const float corridorEndZ = -64.25f;
        const float corridorLength = 58.20f;
        const float corridorCenterZ = -35.15f;
        Color stoneEnd = new Color(0f, 0f, 0.0015f);
        const string corridorWallMaterial = null;
        const string corridorFloorMaterial = null;
        const string corridorMetalMaterial = null;

        CreateBox("BackCorridor_EntranceFrame_Left", corridorRoot.transform, new Vector3(-1.18f, 1.17f, -5.86f), new Vector3(0.16f, 2.28f, 0.22f), new Color(0.010f, 0.009f, 0.015f), PrimitiveType.Cube, corridorMetalMaterial);
        CreateBox("BackCorridor_EntranceFrame_Right", corridorRoot.transform, new Vector3(1.18f, 1.17f, -5.86f), new Vector3(0.16f, 2.28f, 0.22f), new Color(0.010f, 0.009f, 0.015f), PrimitiveType.Cube, corridorMetalMaterial);
        CreateBox("BackCorridor_EntranceFrame_Top", corridorRoot.transform, new Vector3(0f, 2.26f, -5.86f), new Vector3(2.55f, 0.18f, 0.22f), new Color(0.010f, 0.009f, 0.015f), PrimitiveType.Cube, corridorMetalMaterial);
        CreateBox("BackCorridor_ThresholdStone", corridorRoot.transform, new Vector3(0f, 0.04f, -5.96f), new Vector3(2.38f, 0.08f, 0.70f), new Color(0.006f, 0.005f, 0.010f), PrimitiveType.Cube, corridorFloorMaterial);
        CreateDetailBox("BackCorridor_EntranceInnerLip_Left", corridorRoot.transform, new Vector3(-1.045f, 1.16f, -5.72f), new Vector3(0.045f, 2.08f, 0.070f), new Color(0.020f, 0.017f, 0.030f), corridorMetalMaterial);
        CreateDetailBox("BackCorridor_EntranceInnerLip_Right", corridorRoot.transform, new Vector3(1.045f, 1.16f, -5.72f), new Vector3(0.045f, 2.08f, 0.070f), new Color(0.020f, 0.017f, 0.030f), corridorMetalMaterial);
        CreateDetailBox("BackCorridor_EntranceInnerLip_Top", corridorRoot.transform, new Vector3(0f, 2.13f, -5.72f), new Vector3(2.16f, 0.045f, 0.070f), new Color(0.020f, 0.017f, 0.030f), corridorMetalMaterial);
        CreateCorridorRivetRow(corridorRoot.transform, -1.27f, 0.44f, 2.02f, -5.69f, corridorMetalMaterial);
        CreateCorridorRivetRow(corridorRoot.transform, 1.27f, 0.44f, 2.02f, -5.69f, corridorMetalMaterial);

        CreateBox("BackCorridor_LongFloor", corridorRoot.transform, new Vector3(0f, 0.00f, corridorCenterZ), new Vector3(1.76f, 0.08f, corridorLength), new Color(0.004f, 0.003f, 0.007f), PrimitiveType.Cube, corridorFloorMaterial);
        CreateBox("BackCorridor_LeftWall_Long", corridorRoot.transform, new Vector3(-0.98f, 1.10f, corridorCenterZ), new Vector3(0.14f, 2.20f, corridorLength), new Color(0.005f, 0.004f, 0.009f), PrimitiveType.Cube, corridorWallMaterial);
        CreateBox("BackCorridor_RightWall_Long", corridorRoot.transform, new Vector3(0.98f, 1.10f, corridorCenterZ), new Vector3(0.14f, 2.20f, corridorLength), new Color(0.005f, 0.004f, 0.009f), PrimitiveType.Cube, corridorWallMaterial);
        CreateBox("BackCorridor_LowCeiling_Long", corridorRoot.transform, new Vector3(0f, 2.22f, corridorCenterZ), new Vector3(2.08f, 0.10f, corridorLength), new Color(0.004f, 0.003f, 0.008f), PrimitiveType.Cube, corridorWallMaterial);
        CreateBox("BackCorridor_ExteriorBlack_Left", corridorRoot.transform, new Vector3(-1.42f, 1.06f, corridorCenterZ), new Vector3(0.62f, 2.50f, corridorLength + 0.65f), Color.black);
        CreateBox("BackCorridor_ExteriorBlack_Right", corridorRoot.transform, new Vector3(1.42f, 1.06f, corridorCenterZ), new Vector3(0.62f, 2.50f, corridorLength + 0.65f), Color.black);
        CreateBox("BackCorridor_ExteriorBlack_Top", corridorRoot.transform, new Vector3(0f, 2.58f, corridorCenterZ), new Vector3(2.85f, 0.56f, corridorLength + 0.65f), Color.black);

        for (int i = 0; i < 40; i++)
        {
            float depth = i / 39f;
            float z = Mathf.Lerp(corridorStartZ, corridorEndZ + 0.75f, depth);
            Color ribColor = Color.Lerp(new Color(0.006f, 0.004f, 0.012f), stoneEnd, Mathf.Pow(depth, 0.82f));
            Color floorColor = Color.Lerp(new Color(0.007f, 0.005f, 0.011f), Color.black, Mathf.Pow(depth, 0.78f));
            string prefix = "BackCorridor_Rib_" + i.ToString("00");

            CreateBox(prefix + "_LeftPost", corridorRoot.transform, new Vector3(-0.86f, 1.08f, z), new Vector3(0.070f, 2.06f, 0.080f), ribColor, PrimitiveType.Cube, corridorMetalMaterial);
            CreateBox(prefix + "_RightPost", corridorRoot.transform, new Vector3(0.86f, 1.08f, z), new Vector3(0.070f, 2.06f, 0.080f), ribColor, PrimitiveType.Cube, corridorMetalMaterial);
            CreateBox(prefix + "_CeilingBeam", corridorRoot.transform, new Vector3(0f, 2.10f, z), new Vector3(1.88f, 0.070f, 0.085f), ribColor, PrimitiveType.Cube, corridorMetalMaterial);
            CreateBox(prefix + "_FloorBreak", corridorRoot.transform, new Vector3(0f, 0.065f, z), new Vector3(1.48f, 0.018f, 0.058f), floorColor, PrimitiveType.Cube, corridorFloorMaterial);
        }

        var futureMarker = new GameObject("FutureAnomalyZone_Marker");
        futureMarker.transform.SetParent(corridorRoot.transform, false);
        futureMarker.transform.position = new Vector3(0f, 1.05f, -62.90f);

        CorridorFlashlightAnomalyController flashlightAnomaly = corridorRoot.AddComponent<CorridorFlashlightAnomalyController>();
        flashlightAnomaly.Configure(new Vector3(0.52f, 1.02f, -9.75f), new Vector3(0f, 1.24f, -9.90f));

        CreateCorridorFog(corridorRoot.transform);
    }

    private void BuildPhysicalButtons(Transform root)
    {
        var rail = new GameObject("PhysicalButton_ControlRail");
        rail.transform.SetParent(root, false);

        CreateBox("PhysicalButtonRail_DarkMetalBase", rail.transform, new Vector3(0f, 1.055f, -0.56f), new Vector3(1.72f, 0.055f, 0.26f), new Color(0.006f, 0.005f, 0.012f));
        CreatePhysicalButton("PhysicalButton_Reject", rail.transform, ButtonType.Reject, new Vector3(-0.56f, 1.105f, -0.56f));
        CreatePhysicalButton("PhysicalButton_RotateLeft", rail.transform, ButtonType.RotateLeft, new Vector3(-0.28f, 1.105f, -0.56f));
        CreatePhysicalButton("PhysicalButton_Report", rail.transform, ButtonType.ToggleReport, new Vector3(0f, 1.105f, -0.56f));
        CreatePhysicalButton("PhysicalButton_RotateRight", rail.transform, ButtonType.RotateRight, new Vector3(0.28f, 1.105f, -0.56f));
        CreatePhysicalButton("PhysicalButton_Accept", rail.transform, ButtonType.Accept, new Vector3(0.56f, 1.105f, -0.56f));
    }

    private void BuildReportMonitor(Transform root)
    {
        var monitorRoot = new GameObject("FrontMachine_ProcessingPanel");
        monitorRoot.transform.SetParent(root, false);

        Quaternion panelTextRotation = Quaternion.identity;

        const string panelMetalMaterial = null;
        const string panelBackingMaterial = null;
        Color panelShellColor = new Color(0.010f, 0.009f, 0.016f);
        Color frameColor = new Color(0.024f, 0.020f, 0.032f);
        Color displayColor = new Color(0.006f, 0.005f, 0.011f);
        Color separatorColor = new Color(0.048f, 0.036f, 0.070f);

        CreateBox("ProcessingPanel_WallRecessShadow", monitorRoot.transform, new Vector3(0f, 1.63f, 3.158f), new Vector3(5.55f, 2.04f, 0.070f), new Color(0.002f, 0.002f, 0.006f), PrimitiveType.Cube, panelBackingMaterial);
        CreateBox("ProcessingPanel_HeavyHousing", monitorRoot.transform, new Vector3(0f, 1.63f, 3.116f), new Vector3(5.28f, 1.86f, 0.125f), panelShellColor, PrimitiveType.Cube, panelMetalMaterial);
        CreateDetailBox("ProcessingPanel_Frame_Top", monitorRoot.transform, new Vector3(0f, 2.52f, 3.026f), new Vector3(5.20f, 0.080f, 0.052f), frameColor, panelMetalMaterial);
        CreateDetailBox("ProcessingPanel_Frame_Bottom", monitorRoot.transform, new Vector3(0f, 0.74f, 3.026f), new Vector3(5.20f, 0.080f, 0.052f), frameColor, panelMetalMaterial);
        CreateDetailBox("ProcessingPanel_Frame_Left", monitorRoot.transform, new Vector3(-2.62f, 1.63f, 3.026f), new Vector3(0.080f, 1.80f, 0.052f), frameColor, panelMetalMaterial);
        CreateDetailBox("ProcessingPanel_Frame_Right", monitorRoot.transform, new Vector3(2.62f, 1.63f, 3.026f), new Vector3(0.080f, 1.80f, 0.052f), frameColor, panelMetalMaterial);
        GameObject displayScreen = CreateBox("ProcessingPanel_MainOldLCD", monitorRoot.transform, new Vector3(0f, 1.63f, 2.988f), new Vector3(4.82f, 1.56f, 0.030f), displayColor);
        ApplyEmissiveColor(displayScreen.GetComponent<Renderer>(), new Color(0.060f, 0.040f, 0.095f), 0.26f);

        for (int i = 0; i < 3; i++)
        {
            float y = 2.09f - i * 0.365f;
            CreateDetailBox("ProcessingPanel_DisplaySeparator_" + i.ToString("00"), monitorRoot.transform, new Vector3(0f, y, 2.968f), new Vector3(4.44f, 0.010f, 0.018f), separatorColor, panelMetalMaterial);
        }

        CreatePanelScrew("ProcessingPanel_Screw_TopLeft", monitorRoot.transform, new Vector3(-2.39f, 2.39f, 2.954f), panelMetalMaterial);
        CreatePanelScrew("ProcessingPanel_Screw_TopRight", monitorRoot.transform, new Vector3(2.39f, 2.39f, 2.954f), panelMetalMaterial);
        CreatePanelScrew("ProcessingPanel_Screw_BottomLeft", monitorRoot.transform, new Vector3(-2.39f, 0.87f, 2.954f), panelMetalMaterial);
        CreatePanelScrew("ProcessingPanel_Screw_BottomRight", monitorRoot.transform, new Vector3(2.39f, 0.87f, 2.954f), panelMetalMaterial);

        TextMeshPro timerText = CreateWorldText("ProcessingPanel_TimerText", monitorRoot.transform, "HORA 00 AM", new Vector3(-2.06f, 2.25f, 2.944f), panelTextRotation, 0.540f, new Color(1.000f, 0.900f, 0.520f));
        timerText.characterSpacing = 1.0f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.outlineWidth = 0.22f;
        ConfigureTextBox(timerText, new Vector2(4.28f, 0.46f), TextAlignmentOptions.Left);

        TextMeshPro quotaText = CreateWorldText("ProcessingPanel_QuotaText", monitorRoot.transform, "PEDIDOS 00/10", new Vector3(-2.06f, 1.88f, 2.944f), panelTextRotation, 0.430f, new Color(0.965f, 0.925f, 1.000f));
        quotaText.characterSpacing = 0.6f;
        quotaText.fontStyle = FontStyles.Bold;
        quotaText.outlineWidth = 0.20f;
        ConfigureTextBox(quotaText, new Vector2(4.28f, 0.38f), TextAlignmentOptions.Left);

        TextMeshPro stateText = CreateWorldText("ProcessingPanel_StateText", monitorRoot.transform, "CAIXA 30s", new Vector3(-2.06f, 1.53f, 2.944f), panelTextRotation, 0.430f, new Color(0.820f, 1.000f, 0.860f));
        stateText.characterSpacing = 0.6f;
        stateText.fontStyle = FontStyles.Bold;
        stateText.outlineWidth = 0.20f;
        ConfigureTextBox(stateText, new Vector2(4.28f, 0.38f), TextAlignmentOptions.Left);

        TextMeshPro feedbackText = CreateWorldText("ProcessingPanel_ErrorsText", monitorRoot.transform, "ERROS 0/3", new Vector3(-2.06f, 1.18f, 2.944f), panelTextRotation, 0.430f, new Color(0.930f, 0.850f, 1.000f));
        feedbackText.characterSpacing = 0.6f;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.outlineWidth = 0.20f;
        ConfigureTextBox(feedbackText, new Vector2(4.28f, 0.38f), TextAlignmentOptions.Left);

        Renderer[] progressBlocks = new Renderer[10];
        for (int i = 0; i < progressBlocks.Length; i++)
        {
            float x = -1.89f + i * 0.42f;
            GameObject block = CreateBox("ProcessingPanel_ProgressBlock_" + i.ToString("00"), monitorRoot.transform, new Vector3(x, 0.92f, 2.946f), new Vector3(0.300f, 0.115f, 0.024f), new Color(0.140f, 0.090f, 0.220f));
            ApplyEmissiveColor(block.GetComponent<Renderer>(), new Color(0.140f, 0.090f, 0.220f), 0.34f);
            progressBlocks[i] = block.GetComponent<Renderer>();
        }

        Light panelGlow = CreatePointLight("ProcessingPanel_ReadabilityGlow", monitorRoot.transform, new Vector3(0f, 1.92f, 2.58f), new Color(0.740f, 0.640f, 1.000f), 1.08f, 3.15f);
        panelGlow.renderMode = LightRenderMode.ForcePixel;

        StationStatusMonitor statusMonitor = monitorRoot.AddComponent<StationStatusMonitor>();
        statusMonitor.SetTextTargets(null, null, null);
        statusMonitor.SetMachinePanelTargets(timerText, quotaText, stateText, feedbackText, progressBlocks);
    }

    private void BuildPackagePath(Transform root)
    {
        CreateConveyorPoint("ConveyorEntryPoint_Left", root, new Vector3(-2.48f, 1.39f, 0.62f), Quaternion.identity);
        CreateConveyorPoint("ConveyorEntryPoint_Right", root, new Vector3(2.48f, 1.39f, 0.62f), Quaternion.identity);
        CreateConveyorPoint("ConveyorEntryPoint", root, new Vector3(-2.48f, 1.39f, 0.62f), Quaternion.identity);
        Transform inspectionCenter = CreateConveyorPoint("PackageInspectionCenter", root, new Vector3(0f, 1.39f, 0.62f), Quaternion.identity);
        Transform activeSpawn = CreateConveyorPoint("ActivePackageSpawnPoint", root, new Vector3(0f, 1.39f, 0.62f), Quaternion.identity);
        CreateConveyorPoint("ConveyorExitPoint_Left", root, new Vector3(-2.78f, 1.39f, 0.62f), Quaternion.identity);
        CreateConveyorPoint("ConveyorExitPoint_Right", root, new Vector3(2.78f, 1.39f, 0.62f), Quaternion.identity);
        CreateConveyorPoint("ConveyorExitPoint", root, new Vector3(2.78f, 1.39f, 0.62f), Quaternion.identity);

        packageSpawnPoint = inspectionCenter != null ? inspectionCenter : activeSpawn;

    }

    private void BuildLighting(Transform root)
    {
        // Main tunable package light. Keep this as the brightest light in the room.
        Light packageSpotlight = CreateSpotLight(
            "Package_OverheadSpotlight",
            root,
            new Vector3(0f, 2.54f, 0.10f),
            Quaternion.Euler(90f, 0f, 0f),
            packageSpotlightColor,
            overheadSpotlightIntensity,
            overheadSpotlightRange,
            overheadSpotlightAngle
        );
        packageSpotlight.innerSpotAngle = Mathf.Max(8f, overheadSpotlightAngle * 0.40f);
        packageSpotlight.shadowStrength = 0.62f;
        packageSpotlight.shadowBias = 0.040f;
        packageSpotlight.shadowNormalBias = 0.48f;
        packageSpotlight.shadowNearPlane = 0.30f;
        packageSpotlight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
        var packageFlicker = packageSpotlight.gameObject.AddComponent<FlickeringLight>();
        packageFlicker.SetTargetLight(packageSpotlight);
        packageFlicker.Configure(overheadSpotlightIntensity * 0.88f, overheadSpotlightIntensity * 1.02f, 0.58f, false);
        var decisionLightFeedback = packageSpotlight.gameObject.AddComponent<DecisionLightFeedback>();
        decisionLightFeedback.Configure(packageSpotlight, packageSpotlightColor);

        Light mainLight = CreateSpotLight("MainDeskPurpleFillLight", root, new Vector3(0f, 2.48f, -0.75f), Quaternion.Euler(68f, 0f, 0f), new Color(0.260f, 0.120f, 0.520f), purpleFillIntensity, 2.70f, 62f);
        mainLight.shadowStrength = 0.48f;

        Light greenLight = CreatePointLight("DecisionGreenLight", root, new Vector3(0.62f, 1.20f, -0.56f), new Color(0.220f, 0.720f, 0.390f), 0f, 1.8f);
        greenLight.enabled = false;

        Light redLight = CreatePointLight("DecisionRedLight", root, new Vector3(-0.62f, 1.20f, -0.56f), new Color(0.850f, 0.060f, 0.130f), 0f, 1.8f);
        redLight.enabled = false;

        CreatePointLight("PlayerSeat_SubtlePurpleBounce", root, new Vector3(0f, 1.18f, -1.42f), new Color(0.105f, 0.075f, 0.210f), 0.035f, 1.65f);
        Light playerSeatSpotlight = CreateSpotLight(
            "PlayerSeat_OverheadSoftSpot",
            root,
            new Vector3(0f, 2.58f, -1.82f),
            Quaternion.Euler(90f, 0f, 0f),
            packageSpotlightColor,
            overheadSpotlightIntensity * 0.82f,
            overheadSpotlightRange * 0.92f,
            overheadSpotlightAngle
        );
        playerSeatSpotlight.innerSpotAngle = Mathf.Max(8f, overheadSpotlightAngle * 0.40f);
        playerSeatSpotlight.shadows = LightShadows.None;

        CreateCorridorPurpleFlicker(root, "BackCorridor_PurpleFlicker_Entrance", new Vector3(0f, 1.55f, -7.15f), 4.60f, 0.52f, 1.30f, 0.28f);
        CreateCorridorPurpleFlicker(root, "BackCorridor_PurpleFlicker_NearDepth", new Vector3(0f, 1.52f, -13.60f), 5.15f, 0.38f, 1.00f, 0.25f);
        CreateCorridorPurpleFlicker(root, "BackCorridor_PurpleFlicker_MidDepth", new Vector3(0f, 1.48f, -22.40f), 5.45f, 0.24f, 0.68f, 0.22f);
        CreateCorridorPurpleFlicker(root, "BackCorridor_PurpleFlicker_FarDepth", new Vector3(0f, 1.44f, -33.80f), 5.70f, 0.13f, 0.42f, 0.20f);
        CreateCorridorPurpleFlicker(root, "BackCorridor_PurpleFlicker_VanishingPoint", new Vector3(0f, 1.40f, -47.60f), 6.00f, 0.055f, 0.20f, 0.17f);
    }

    private void CreateCorridorPurpleFlicker(Transform root, string name, Vector3 position, float range, float minimum, float maximum, float speed)
    {
        Color purple = new Color(0.310f, 0.080f, 0.650f);
        Light purpleLight = CreatePointLight(name, root, position, purple, minimum, range);
        purpleLight.enabled = true;
        purpleLight.cullingMask = ~0;
        purpleLight.lightmapBakeType = LightmapBakeType.Realtime;
        var flicker = purpleLight.gameObject.AddComponent<FlickeringLight>();
        flicker.SetTargetLight(purpleLight);
        flicker.Configure(minimum, maximum, speed, false);
    }

    private void BuildAtmosphereParticles(Transform root)
    {
        var dustObject = new GameObject("Purple Dust Motes");
        dustObject.transform.SetParent(root, false);
        dustObject.transform.position = new Vector3(0f, 1.65f, -0.45f);

        ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.012f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.009f, 0.026f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.430f, 0.250f, 0.760f, 0.08f), new Color(0.160f, 0.740f, 0.560f, 0.05f));
        main.maxParticles = 70;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 4.5f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5.5f, 1.55f, 3.9f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.018f, 0.018f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.002f, 0.020f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.014f, 0.014f);

        ParticleSystemRenderer renderer = dustObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial(new Color(0.620f, 0.460f, 1f, 0.22f));
        particles.Play();
    }

    private void CreateCorridorFog(Transform root)
    {
        var fogObject = new GameObject("BackCorridor_HeavyDepthFog");
        fogObject.transform.SetParent(root, false);
        fogObject.transform.position = new Vector3(0f, 1.18f, -36.35f);

        ParticleSystem particles = fogObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(13.0f, 24f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.40f, 1.32f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.018f, 0.010f, 0.032f, 0.225f), new Color(0.002f, 0.001f, 0.007f, 0.205f));
        main.maxParticles = 240;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 17.5f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.95f, 1.65f, 52.0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.012f, 0.012f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.004f, 0.010f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.030f, 0.006f);

        ParticleSystemRenderer renderer = fogObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial(new Color(0.022f, 0.010f, 0.046f, 0.34f));
        particles.Play();
    }

    private void BuildPlayer(Transform parent)
    {
        Transform frontView = CreateViewPoint("FrontView", parent, new Vector3(0f, 1.78f, -1.82f), Quaternion.Euler(18f, 0f, 0f));
        Transform backView = CreateViewPoint("BackView", parent, new Vector3(0f, 1.72f, -1.82f), Quaternion.Euler(8f, 180f, 0f));

        BoothPlayerController existingController = Object.FindFirstObjectByType<BoothPlayerController>();
        if (existingController != null)
        {
            existingController.transform.position = new Vector3(0f, 0f, -1.82f);
            Camera existingCamera = Camera.main;
            if (existingCamera != null)
            {
                existingCamera.fieldOfView = 56f;
                existingController.Configure(existingCamera);
                ConfigureViewSwitcher(frontView, backView, existingCamera);
            }
            return;
        }

        GameObject player = new GameObject("Inspection Player");
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(0f, 0f, -1.82f);
        player.AddComponent<CapsuleCollider>();
        player.AddComponent<Rigidbody>();
        var controller = player.AddComponent<BoothPlayerController>();
        player.AddComponent<PlayerInteraction>();

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = new GameObject("Main Camera").AddComponent<Camera>();
            playerCamera.tag = "MainCamera";
        }

        playerCamera.fieldOfView = 56f;
        playerCamera.backgroundColor = new Color(0.01f, 0.011f, 0.014f);
        controller.Configure(playerCamera);
        ConfigureViewSwitcher(frontView, backView, playerCamera);
    }

    private void ConfigureViewSwitcher(Transform frontView, Transform backView, Camera playerCamera)
    {
        ViewSwitcher switcher = ViewSwitcher.Instance != null ? ViewSwitcher.Instance : Object.FindFirstObjectByType<ViewSwitcher>();
        if (switcher == null)
        {
            return;
        }

        switcher.SetViews(frontView, backView);
        switcher.SetCamera(playerCamera);
    }

    private Transform CreateViewPoint(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject viewObject = GameObject.Find(name);
        if (viewObject == null)
        {
            viewObject = new GameObject(name);
        }

        if (parent != null && viewObject.transform.parent != parent)
        {
            viewObject.transform.SetParent(parent, false);
        }

        viewObject.transform.position = position;
        viewObject.transform.rotation = rotation;
        return viewObject.transform;
    }

    private Light CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.cullingMask = ~0;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        return light;
    }

    private Light CreateSpotLight(string name, Transform parent, Vector3 position, Quaternion rotation, Color color, float intensity, float range, float spotAngle)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        lightObject.transform.rotation = rotation;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        light.shadows = LightShadows.Soft;
        light.renderMode = LightRenderMode.ForcePixel;
        light.shadowNearPlane = 0.18f;
        light.cullingMask = ~0;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        return light;
    }

    private Transform CreateConveyorPoint(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject pointObject = GameObject.Find(name);
        if (pointObject == null)
        {
            pointObject = new GameObject(name);
        }

        pointObject.transform.SetParent(parent, false);
        pointObject.transform.position = position;
        pointObject.transform.rotation = rotation;
        return pointObject.transform;
    }

    private GameObject CreatePhysicalButton(string name, Transform parent, ButtonType type, Vector3 position)
    {
        GameObject buttonRoot = new GameObject(name);
        buttonRoot.transform.SetParent(parent, false);
        buttonRoot.transform.position = position;
        buttonRoot.tag = "DecisionButton";

        GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonBase.name = "ButtonBase";
        buttonBase.transform.SetParent(buttonRoot.transform, false);
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.localScale = new Vector3(0.205f, 0.044f, 0.135f);
        ApplyMaterial(buttonBase.GetComponent<Renderer>(), new Color(0.015f, 0.013f, 0.024f));

        GameObject buttonTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonTop.name = "ButtonTop";
        buttonTop.transform.SetParent(buttonRoot.transform, false);
        buttonTop.transform.localPosition = new Vector3(0f, 0.038f, 0f);
        buttonTop.transform.localScale = new Vector3(0.152f, 0.030f, 0.095f);
        ApplyMaterial(buttonTop.GetComponent<Renderer>(), GetButtonAccentColor(type));

        GameObject accentLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        accentLine.name = "ButtonAccentGlow";
        accentLine.transform.SetParent(buttonRoot.transform, false);
        accentLine.transform.localPosition = new Vector3(0f, 0.058f, -0.061f);
        accentLine.transform.localScale = new Vector3(0.165f, 0.010f, 0.010f);
        ApplyMaterial(accentLine.GetComponent<Renderer>(), GetButtonAccentColor(type));
        ApplyEmissiveColor(accentLine.GetComponent<Renderer>(), GetButtonAccentColor(type), 0.65f);
        Destroy(accentLine.GetComponent<Collider>());

        BoxCollider buttonCollider = buttonRoot.AddComponent<BoxCollider>();
        buttonCollider.center = new Vector3(0f, 0.045f, 0f);
        buttonCollider.size = new Vector3(0.245f, 0.135f, 0.190f);

        PhysicalButton button = buttonRoot.AddComponent<PhysicalButton>();
        if (button != null)
        {
            button.Configure(type, Object.FindFirstObjectByType<InspectionStation>());
        }

        return buttonRoot;
    }

    private void CreateButtonTextLabel(Transform parent, string label)
    {
        GameObject labelObject = new GameObject("ButtonLabel_" + label);
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.075f, -0.006f);
        labelObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        TextMeshPro textMesh = labelObject.AddComponent<TextMeshPro>();
        textMesh.text = label;
        textMesh.fontSize = 0.122f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = new Color(0.965f, 0.935f, 1f);
        textMesh.outlineWidth = 0.20f;
        textMesh.outlineColor = Color.black;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.fontStyle = FontStyles.Bold;

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(0.52f, 0.155f);
        }
    }

    private Color GetButtonAccentColor(ButtonType type)
    {
        switch (type)
        {
            case ButtonType.Accept:
                return new Color(0.055f, 0.305f, 0.160f);
            case ButtonType.Reject:
                return new Color(0.320f, 0.040f, 0.075f);
            case ButtonType.RotateLeft:
            case ButtonType.RotateRight:
                return new Color(0.105f, 0.130f, 0.360f);
            case ButtonType.ToggleReport:
                return new Color(0.245f, 0.135f, 0.460f);
            default:
                return new Color(0.120f, 0.100f, 0.160f);
        }
    }

    private string GetButtonLabel(ButtonType type)
    {
        switch (type)
        {
            case ButtonType.Accept:
                return "ACCEPT";
            case ButtonType.Reject:
                return "REJECT";
            case ButtonType.RotateLeft:
                return "LEFT";
            case ButtonType.RotateRight:
                return "RIGHT";
            case ButtonType.ToggleReport:
                return "REPORT";
            default:
                return "USE";
        }
    }

    private void ConfigurePhysicalButton(GameObject buttonObject, ButtonType type)
    {
        PhysicalButton button = buttonObject.GetComponent<PhysicalButton>();
        if (button == null)
        {
            button = buttonObject.AddComponent<PhysicalButton>();
        }

        button.Configure(type, Object.FindFirstObjectByType<InspectionStation>());
    }

    private GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Color color, PrimitiveType primitiveType = PrimitiveType.Cube, string editorMaterialPath = null)
    {
        GameObject box = GameObject.CreatePrimitive(primitiveType);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.localScale = scale;

        ApplyMaterial(box.GetComponent<Renderer>(), color, editorMaterialPath);
        return box;
    }

    private GameObject CreateDetailBox(string name, Transform parent, Vector3 position, Vector3 scale, Color color, string editorMaterialPath = null)
    {
        GameObject detail = CreateBox(name, parent, position, scale, color, PrimitiveType.Cube, editorMaterialPath);
        Collider collider = detail.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        return detail;
    }

    private GameObject CreatePanelScrew(string name, Transform parent, Vector3 position, string editorMaterialPath = null)
    {
        GameObject screw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        screw.name = name;
        screw.transform.SetParent(parent, false);
        screw.transform.position = position;
        screw.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        screw.transform.localScale = new Vector3(0.055f, 0.018f, 0.055f);
        ApplyMaterial(screw.GetComponent<Renderer>(), new Color(0.105f, 0.095f, 0.120f), editorMaterialPath);

        Collider collider = screw.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        return screw;
    }

    private void CreateCorridorRivetRow(Transform parent, float x, float minY, float maxY, float z, string editorMaterialPath)
    {
        const int count = 5;
        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0f : i / (float)(count - 1);
            float y = Mathf.Lerp(minY, maxY, t);
            CreatePanelScrew("BackCorridor_EntranceRivet_" + (x < 0f ? "Left_" : "Right_") + i.ToString("00"), parent, new Vector3(x, y, z), editorMaterialPath);
        }
    }

    private GameObject CreateTexturedQuad(string name, Transform parent, Vector3 position, Vector3 scale, string textureResourcePath)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.position = position;
        quad.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        quad.transform.localScale = scale;
        Destroy(quad.GetComponent<Collider>());

        Texture texture = Resources.Load<Texture2D>(textureResourcePath);
        Renderer renderer = quad.GetComponent<Renderer>();
        Material material = CreateTextureMaterial(texture);
        renderer.sharedMaterial = material;
        return quad;
    }

    private TextMeshPro CreateWorldText(string name, Transform parent, string text, Vector3 position, Quaternion rotation, float fontSize, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = position;
        textObject.transform.rotation = rotation;

        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.outlineWidth = 0.12f;
        textMesh.outlineColor = Color.black;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;

        var rect = textObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(2f, 0.55f);
        }

        return textMesh;
    }

    private void ConfigureTextBox(TextMeshPro textMesh, Vector2 size, TextAlignmentOptions alignment)
    {
        if (textMesh == null)
        {
            return;
        }

        textMesh.alignment = alignment;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;

        var rect = textMesh.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = size;
        }
    }

    private void ApplyMaterial(Renderer targetRenderer, Color color, string editorMaterialPath = null)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        material.name = "Runtime_DarkInspection_" + targetRenderer.gameObject.name;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.12f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        targetRenderer.sharedMaterial = material;
    }

    private void ApplyEmissiveColor(Renderer targetRenderer, Color color, float intensity)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Material material = targetRenderer.sharedMaterial;
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            targetRenderer.sharedMaterial = material;
        }
        else
        {
            material = new Material(material);
            targetRenderer.sharedMaterial = material;
        }

        Color finalColor = color * Mathf.Max(0f, intensity);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", finalColor);
            material.EnableKeyword("_EMISSION");
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private Material CreateParticleMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.renderQueue = 3000;
        return material;
    }

    private Material CreateTextureMaterial(Texture texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        material.mainTexture = texture;
        material.renderQueue = 3000;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }
}
