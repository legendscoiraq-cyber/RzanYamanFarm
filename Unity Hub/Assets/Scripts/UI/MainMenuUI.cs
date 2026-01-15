using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MainMenuUI : MonoBehaviour
{
    public GameObject mainMenu, charSelect, connectionMenu, waiting;
    public NetworkDiscovery netDiscovery;

    private int selectedChar = 0;

    // Step 1: Start
    public void OnStartClicked() 
    { 
        mainMenu.SetActive(false); 
        charSelect.SetActive(true); 
    }

    // Step 2: Select Character
    public void SelectChar(int c) 
    { 
        selectedChar = c;
        PlayerPrefs.SetInt("SelectedCharacter", c);
        
        charSelect.SetActive(false);
        connectionMenu.SetActive(true);
    }

    // Step 3: Host or Join
    public void StartHost()
    {
        connectionMenu.SetActive(false);
        waiting.SetActive(true);
        netDiscovery.StartHostBroadcast();
    }

    public void JoinGame()
    {
        connectionMenu.SetActive(false);
        waiting.SetActive(true);
        netDiscovery.StartClientListener();
    }
}
