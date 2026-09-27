using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class TowerGameplaySetup : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private ClimberController climber;
    [SerializeField] private BumpEventController bumpEventController;
    [SerializeField] private TowerSpawner towerSpawner;

    [Header("Victory")]
    [SerializeField] private float victoryDuration = 3f;
    [SerializeField] private float victoryBumpInterval = 0.35f;
    [SerializeField] private GameObject gameOverUI;


    private bool levelCompleted;

    private void Update()
    {
        if (levelCompleted)
            return;

        if (climber.CurrentHeight >= climber.MaxHeight)
        {
            levelCompleted = true;

            StartCoroutine(
                CompleteLevelRoutine()
            );
        }
    }

    private void Start()
    {
        towerSpawner.BuildTower();

        climber.SetClimbLimits(
            towerSpawner.PlayerMinHeight,
            towerSpawner.PlayerMaxHeight
        );
    }

    private IEnumerator CompleteLevelRoutine()
    {
        Debug.Log("[Tower] Player reached the top!");

        climber.SetGameplayEnabled(false);


        if (bumpEventController != null)
        {
            yield return StartCoroutine(
                bumpEventController.PlayVictoryBumpSequence(
                    victoryDuration,
                    victoryBumpInterval
                )
            );
        }

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        Debug.Log("[Tower] Game Over UI shown.");
    }
}