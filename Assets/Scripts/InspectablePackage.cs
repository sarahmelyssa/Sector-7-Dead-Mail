using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InspectablePackage : MonoBehaviour
{
    [SerializeField] private PackageData data;

    public PackageData Data => data;

    public void SetData(PackageData packageData)
    {
        data = packageData;
        name = "Package_" + data.remetente.Replace(" ", "_") + "_to_" + data.destino.Replace(" ", "_");
    }
}
