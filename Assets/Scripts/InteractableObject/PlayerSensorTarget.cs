using TMPro;
using UnityEngine;

public abstract class PlayerSensorTarget : MonoBehaviour
{

    [SerializeField] protected TextMeshPro displayText;

    protected virtual void Awake()
    {
        if(displayText!=null)
        {
            displayText.gameObject.SetActive(false);
        }
    }
    public virtual void Interact()
    {

    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (displayText != null)
            displayText.gameObject.SetActive(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (displayText != null)
            displayText.gameObject.SetActive(false);
    }
}
