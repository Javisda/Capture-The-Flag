using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    [SerializeField] Sprite[] hearts = new Sprite[3];

    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];
    [SerializeField] Text killsUI;
    [SerializeField] Text DeathsUI;
    [SerializeField] Text RespawningText;
    [SerializeField] Text timeUI;
    public Text playerName;

    // Ranking
    private string[] leaderboard;
    [SerializeField] private GameObject inGameRanking;
    [SerializeField] Text[] ranking;

    // Start is called before the first frame update
    void Start()
    {
        ActivateInGameHUD();
        leaderboard = new string[8] { " ", " ", " ", " ", " ", " ", " ", " "};
    }


    private void ActivateInGameHUD()
    {
        inGameHUD.SetActive(true);
        UpdateLifeUI(6);
        UpdateKillsUI(0);
        SetRespawningText(false);
    }

    public void UpdateLifeUI(int hitpoints)
    {
        // Lives
        switch (hitpoints)
        {
            case 6:
                heartsUI[0].texture = hearts[2].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 5:
                heartsUI[0].texture = hearts[1].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 4:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 3:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[1].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 2:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 1:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[1].texture;
                break;
            case 0:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[0].texture;
                break;
        }
    }


    public void UpdateKillsUI(int numKills)
    {
        killsUI.text = "Kills: " + numKills.ToString();
    }

    public void UpdateDeathsUI(int deaths)
    {
        DeathsUI.text = "Deaths: " + deaths.ToString();
    }

    public void SetRespawningText(bool option) {
        RespawningText.enabled = option;

        if(option) { 
            StartCoroutine(DisplaySeconds());
        }
    }

    // Función que muestra el texto cuando un personaje reaparece
    IEnumerator DisplaySeconds()
    {
        RespawningText.color = Color.red;
        RespawningText.text = "Respawning in 3";
        yield return new WaitForSeconds(1);
        RespawningText.color = Color.yellow;
        RespawningText.text = "Respawning in 2";
        yield return new WaitForSeconds(1);
        RespawningText.color = Color.green;
        RespawningText.text = "Respawning in 1";
    }


    // Funcion para mostrar el tiempo correcto
    public void UpdateTimer(int time)
    {
        int min = time / 60;
        int segundos = (time - (min * 60));
        if (time < 10)
            timeUI.text = " " + min + " : 0" + segundos + " ";
        else
            timeUI.text = " " + min + " : " + segundos + " ";
    }


    // Función que recibe cadenas de texto ya montadas del nombre de los jugadores y sus kills y deaths, de forma ordenada para colocarlas en el ranking
    public void AddToLadderBoard(string s) {

        for (int i = leaderboard.Length - 1; i > 0; i--)
        {
            leaderboard[i] = leaderboard[i - 1];
        }
        leaderboard[0] = s;
    }

   
    // Muestra el ranking
    public void ShowLadderboard() {
        inGameHUD.SetActive(false);
        inGameRanking.SetActive(true);

        for (int i = 0; i < leaderboard.Length; i++)
        {
            ranking[i].text = (i+1)+"º Puesto: " + leaderboard[i];
        }
    }
}
