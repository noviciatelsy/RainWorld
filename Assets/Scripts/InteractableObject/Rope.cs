using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player=collision.GetComponent<Player>();
        if (player == null )
        {
            return;
        }
        player.GetComponent<PlayerControl>().SetInRopeArea(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player == null)
        {
            return;
        }
        player.GetComponent<PlayerControl>().SetInRopeArea(false);
    }
}
