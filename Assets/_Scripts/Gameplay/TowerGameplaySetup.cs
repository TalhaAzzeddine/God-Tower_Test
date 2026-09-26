using UnityEngine;

public class TowerGameplaySetup : MonoBehaviour
{
    [SerializeField] private TowerSpawner towerSpawner;
    [SerializeField] private ClimberController climberController;

    private void Start()
    {
        towerSpawner.BuildTower();

        climberController.SetClimbLimits(
            towerSpawner.PlayerMinHeight,
            towerSpawner.PlayerMaxHeight
        );
    }
}