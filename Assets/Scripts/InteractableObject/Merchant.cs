using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Merchant : PlayerSensorTarget
{
    public override void Interact()
    {
        base.Interact();
        if(InGameUI.Instance!=null)
        {
            InGameUI.Instance.ToggleMerchantUI();
        }
    }
}
