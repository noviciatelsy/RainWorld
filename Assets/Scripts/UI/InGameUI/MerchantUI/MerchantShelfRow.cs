using UnityEngine;

public class MerchantShelfRow : MonoBehaviour
{
    [SerializeField] private Transform merchandiseRoot;

    public Transform MerchandiseRoot
    {
        get
        {
            return merchandiseRoot;
        }
    }

    public void Clear()
    {
        if (merchandiseRoot == null)
        {
            return;
        }

        for (int i = merchandiseRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(merchandiseRoot.GetChild(i).gameObject);
        }
    }
}