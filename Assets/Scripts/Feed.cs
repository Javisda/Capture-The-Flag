using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Feed : MonoBehaviour
{

    // Esta funcion ordena en orden de llegada el feedback de las muertes, hasta un máximo de 3.

    private string[] feed;
    [SerializeField] Text[] holders;

    private void Start()
    {
        feed = new string[3] {"", "", ""};
    }

    public void AddFeed(string kill) {
        for (int i = feed.Length - 1; i > 0; i--) {
            feed[i] = feed[i - 1]; 
        }  
        feed[0] = kill;

        // Update UI
        UpdateUI();
    }

    private void UpdateUI() {
        for (int i = 0; i < holders.Length; i++) {
            holders[i].text = feed[i];
        }
    }
}
