using Gameplay.Controller;
using Gameplay.Items;
using Gameplay.PlayerData;
using UnityEngine;

public class PlayerSetUpBridge : MonoBehaviour
{
    public PlayerController PlayerController;
    public PlayerHealth PlayerHealth;
    public PlayerInteract PlayerInteract;
    public PlayerInventory PlayerInventory;
    public PlayerInput PlayerInput;

    public void SetUp(
        PlayerController controller, 
        PlayerHealth playerHealth, 
        PlayerInteract playerInteract, 
        PlayerInventory playerInventory, 
        PlayerInput playerInput)
    {
        PlayerController = controller;
        PlayerHealth = playerHealth;
        PlayerInteract = playerInteract;
        PlayerInventory = playerInventory;
        PlayerInput = playerInput;
    }
}
