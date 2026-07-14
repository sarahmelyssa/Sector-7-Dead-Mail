using System.Collections;
using UnityEngine;

/// <summary>
/// Permite rodar a caixa em passos de 90 graus para o jogador conseguir ler
/// etiquetas que chegam viradas de lado.
/// </summary>
public class PackageRotator : MonoBehaviour
{
    [SerializeField] private PackageConveyor packageConveyor = null;
    [SerializeField] private Transform currentPackage = null;
    [SerializeField] private float rotationAngle = 90f;
    [SerializeField] private float rotationDuration = 0.25f;

    private bool isRotating;

    private void Awake()
    {
        if (packageConveyor == null)
        {
            packageConveyor = Object.FindFirstObjectByType<PackageConveyor>();
        }
    }

    private void OnEnable()
    {
        PackageConveyor.PackageArrivedAtInspection += SetCurrentPackage;
        PackageConveyor.PackageLeftInspection += ClearCurrentPackage;
    }

    private void OnDisable()
    {
        PackageConveyor.PackageArrivedAtInspection -= SetCurrentPackage;
        PackageConveyor.PackageLeftInspection -= ClearCurrentPackage;
    }

    public void SetCurrentPackage(GameObject packageObject)
    {
        currentPackage = packageObject != null ? packageObject.transform : null;
    }

    public void RotateLeft()
    {
        TryRotate(rotationAngle);
    }

    public void RotateRight()
    {
        TryRotate(-rotationAngle);
    }

    private void TryRotate(float angle)
    {
        if (isRotating || !CanRotateCurrentPackage())
        {
            return;
        }

        StartCoroutine(RotatePackage(angle));
    }

    private bool CanRotateCurrentPackage()
    {
        if (packageConveyor == null)
        {
            packageConveyor = Object.FindFirstObjectByType<PackageConveyor>();
        }

        if (packageConveyor == null || !packageConveyor.IsPackageReadyForInspection)
        {
            return false;
        }

        if (currentPackage == null && packageConveyor.CurrentPackage != null)
        {
            currentPackage = packageConveyor.CurrentPackage.transform;
        }

        return currentPackage != null;
    }

    private IEnumerator RotatePackage(float angle)
    {
        // Rotacao interpolada para nao dar salto visual.
        isRotating = true;
        Quaternion startRotation = currentPackage.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, angle, 0f);
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            if (currentPackage == null)
            {
                isRotating = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            currentPackage.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / rotationDuration);
            yield return null;
        }

        currentPackage.rotation = targetRotation;
        isRotating = false;
    }

    private void ClearCurrentPackage(GameObject packageObject)
    {
        if (currentPackage != null && packageObject != null && currentPackage.gameObject != packageObject)
        {
            return;
        }

        currentPackage = null;
        isRotating = false;
    }
}
