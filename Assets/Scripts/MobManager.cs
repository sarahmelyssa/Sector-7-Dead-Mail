using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MobManager : MonoBehaviour
{
    private const string EditorMobPrefabPath = "Assets/Prefabs/MobPrefab.prefab";

    [SerializeField] private GameObject mobPrefab = null;
    [SerializeField] private Vector3 spawnPoint = new Vector3(0f, 1.25f, 4.45f);
    [SerializeField] private List<Vector3> spawnPoints = new List<Vector3>();
    [SerializeField] private float timeBeforeDamage = 5f;
    [SerializeField] private float lookDotThreshold = 0.92f;
    [SerializeField] private bool mobEnabled = true;
    [SerializeField] private Vector2 tensionPauseBeforeSpawn = new Vector2(0.55f, 1.35f);

    private Camera playerCamera;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    private GameObject activeMob;
    private float timeNotLooking;
    private Coroutine pendingSpawnRoutine;
    private bool forcePendingSpawn;

    private void Awake()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
        playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        ResolveMobPrefabIfNeeded();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            CancelPendingSpawn();
            DespawnMob();
            return;
        }

        if (activeMob == null)
        {
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }
        }

        if (IsPlayerLookingAtMob())
        {
            DespawnMob();
            return;
        }

        timeNotLooking += Time.deltaTime;
        if (timeNotLooking >= timeBeforeDamage)
        {
            if (playerHealth == null)
            {
                playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
            }

            playerHealth?.TakeDamage(1);
            DespawnMob();
        }
    }

    public void SetSpawnPoint(Vector3 point)
    {
        spawnPoint = point;
        spawnPoints.Clear();
        spawnPoints.Add(point);
    }

    public void SetSpawnPoints(List<Vector3> points)
    {
        spawnPoints = points ?? new List<Vector3>();

        if (spawnPoints.Count > 0)
        {
            spawnPoint = spawnPoints[0];
        }
    }

    public void SetMobEnabled(bool enabled)
    {
        mobEnabled = enabled;

        if (!mobEnabled)
        {
            CancelPendingSpawn();
            DespawnMob();
        }
    }

    public void SetTimeBeforeDamage(float seconds)
    {
        timeBeforeDamage = Mathf.Max(0.5f, seconds);
    }

    public void ConsiderSpawnFromDanger(int dangerLevel)
    {
        if (!mobEnabled || activeMob != null || pendingSpawnRoutine != null)
        {
            return;
        }

        int night = NightManager.Instance != null ? NightManager.Instance.CurrentNight : 1;
        if (night <= 1)
        {
            return;
        }

        float chance = night >= 3
            ? 0.32f + Mathf.Max(0, dangerLevel) * 0.085f
            : 0.16f + Mathf.Max(0, dangerLevel) * 0.055f;

        if (Random.value <= Mathf.Clamp01(chance))
        {
            SpawnMob();
        }
    }

    public void SpawnMob()
    {
        SpawnMobInternal(false);
    }

    public void ForceSpawnMob()
    {
        SpawnMobInternal(true);
    }

    private void SpawnMobInternal(bool force)
    {
        if (!mobEnabled)
        {
            if (!force)
            {
                return;
            }
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        if (activeMob != null || pendingSpawnRoutine != null)
        {
            return;
        }

        ResolveMobPrefabIfNeeded();
        forcePendingSpawn = force;
        pendingSpawnRoutine = StartCoroutine(SpawnMobAfterTensionPause());
    }

    public void StopMobs()
    {
        CancelPendingSpawn();
        DespawnMob();
    }

    private IEnumerator SpawnMobAfterTensionPause()
    {
        HorrorEffectsManager.Instance?.MaybeShowStrangeText(0.8f);
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.42f, 0.20f);
        AudioManager.Instance?.PlayBreathingBehind();

        float pause = Random.Range(tensionPauseBeforeSpawn.x, tensionPauseBeforeSpawn.y);
        yield return new WaitForSeconds(pause);

        pendingSpawnRoutine = null;

        bool forceSpawn = forcePendingSpawn;
        forcePendingSpawn = false;

        if (!mobEnabled && !forceSpawn)
        {
            yield break;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            yield break;
        }

        if (activeMob != null)
        {
            yield break;
        }

        Vector3 selectedSpawnPoint = GetSpawnPoint();
        activeMob = mobPrefab != null
            ? Instantiate(mobPrefab, selectedSpawnPoint, Quaternion.identity)
            : CreatePlaceholderMob();

        Transform mobRoot = GetRuntimeMobRoot();
        if (mobRoot != null)
        {
            activeMob.transform.SetParent(mobRoot, true);
        }

        activeMob.transform.position = selectedSpawnPoint;
        timeNotLooking = 0f;
        AudioManager.Instance?.PlayMobAppears();
        HorrorEffectsManager.Instance?.OnMobAppeared();
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Count)];
        }

        return spawnPoint;
    }

    private bool IsPlayerLookingAtMob()
    {
        Vector3 toMob = activeMob.transform.position - playerCamera.transform.position;
        float dot = Vector3.Dot(playerCamera.transform.forward, toMob.normalized);
        return dot >= lookDotThreshold;
    }

    private GameObject CreatePlaceholderMob()
    {
        GameObject mob = new GameObject("Generated Shadow Entity");
        CreateMobPart("Shadow Torso", mob.transform, PrimitiveType.Capsule, new Vector3(0f, 0.52f, 0f), new Vector3(0.34f, 1.28f, 0.28f), new Color(0.003f, 0.002f, 0.010f), false);
        CreateMobPart("Bent Neck", mob.transform, PrimitiveType.Cube, new Vector3(0.02f, 1.15f, 0f), new Vector3(0.12f, 0.34f, 0.11f), new Color(0.002f, 0.002f, 0.008f), false);
        CreateMobPart("Too Still Head", mob.transform, PrimitiveType.Sphere, new Vector3(0.04f, 1.38f, 0.02f), new Vector3(0.34f, 0.30f, 0.28f), new Color(0.002f, 0.002f, 0.008f), false);
        CreateMobPart("Left Hanging Arm", mob.transform, PrimitiveType.Cube, new Vector3(-0.36f, 0.42f, 0.02f), new Vector3(0.075f, 1.05f, 0.070f), new Color(0.001f, 0.001f, 0.006f), false);
        CreateMobPart("Right Hanging Arm", mob.transform, PrimitiveType.Cube, new Vector3(0.38f, 0.36f, 0.02f), new Vector3(0.075f, 1.20f, 0.070f), new Color(0.001f, 0.001f, 0.006f), false);
        CreateMobPart("Left Pin Eye", mob.transform, PrimitiveType.Sphere, new Vector3(-0.045f, 1.41f, -0.17f), Vector3.one * 0.055f, new Color(0.760f, 0.015f, 0.070f), true);
        CreateMobPart("Right Pin Eye", mob.transform, PrimitiveType.Sphere, new Vector3(0.105f, 1.41f, -0.17f), Vector3.one * 0.055f, new Color(0.760f, 0.015f, 0.070f), true);

        Light eyeGlow = new GameObject("Entity Eye Glow").AddComponent<Light>();
        eyeGlow.transform.SetParent(mob.transform, false);
        eyeGlow.transform.localPosition = new Vector3(0.03f, 1.40f, -0.22f);
        eyeGlow.type = LightType.Point;
        eyeGlow.color = new Color(0.620f, 0.010f, 0.060f);
        eyeGlow.intensity = 0.55f;
        eyeGlow.range = 1.1f;

        return mob;
    }

    private void DespawnMob()
    {
        if (activeMob != null)
        {
            Destroy(activeMob);
            AudioManager.Instance?.PlayMobDisappears();
        }

        activeMob = null;
        timeNotLooking = 0f;
    }

    private void CancelPendingSpawn()
    {
        forcePendingSpawn = false;
        if (pendingSpawnRoutine != null)
        {
            StopCoroutine(pendingSpawnRoutine);
            pendingSpawnRoutine = null;
        }
    }

    private void ResolveMobPrefabIfNeeded()
    {
        if (mobPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        mobPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorMobPrefabPath);
#endif
    }

    private Transform GetRuntimeMobRoot()
    {
        GameObject root = GameObject.Find(CheckpointBootstrap.MobSystemRootName);
        return root != null ? root.transform : transform;
    }

    private GameObject CreateMobPart(string partName, Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color, bool emissive)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }

        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 1.8f);
            material.EnableKeyword("_EMISSION");
        }

        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }
}
