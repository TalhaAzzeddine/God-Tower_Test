using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform level;
    [SerializeField] private GameObject towerColumnPrefab;
    [SerializeField] private GameObject karenTowerPrefab;
    [SerializeField] private ClimberController climber;

    [Header("Tower Settings")]
    [SerializeField] private int columnCount = 1;
    [SerializeField] private float columnSpacing = 67.7f;

    [Header("Column Position")]
    [SerializeField] private float columnX = 0.88f;
    [SerializeField] private float columnStartY = 0f;
    [SerializeField] private float columnZ = 14.4f;

    [Header("Karen Tower Position")]
    [SerializeField] private float topX = 2.1f;
    [SerializeField] private float topStartY = 91.7f;
    [SerializeField] private float topZ = 75f;

    [Header("Player")]
    [SerializeField] private float playerMaxHeightStart = 110f;

    public float PlayerMinHeight { get; private set; }
    public float PlayerMaxHeight { get; private set; }

    public Transform TopPoint { get; private set; }

    private void Start()
    {
        BuildTower();
    }

    public void BuildTower()
    {
        ClearTower();

        columnCount = Mathf.Max(1, columnCount);

        SpawnColumns();
        SpawnTop();
        SetupPlayerLimits();
    }

    private void SpawnColumns()
    {
        for (int i = 0; i < columnCount; i++)
        {
            float y = columnStartY + i * columnSpacing;

            Vector3 position = new Vector3(
                columnX,
                y,
                columnZ
            );

            Instantiate(
                towerColumnPrefab,
                position,
                towerColumnPrefab.transform.rotation,
                level
            );
        }
    }

    private void SpawnTop()
    {
        float y = topStartY + (columnCount - 1) * columnSpacing;

        Vector3 position = new Vector3(
            topX,
            y,
            topZ
        );

        GameObject top = Instantiate(
            karenTowerPrefab,
            position,
            karenTowerPrefab.transform.rotation,
            level
        );

        top.name = "Karen_Tower";

        GameObject topPointObject = new GameObject("TopPoint");
        TopPoint = topPointObject.transform;

        TopPoint.SetParent(level);
        TopPoint.position = position;
    }

    private void SetupPlayerLimits()
    {
        PlayerMinHeight = columnStartY;

        PlayerMaxHeight =
            playerMaxHeightStart +
            (columnCount - 1) * columnSpacing;

        if (climber != null)
        {
            climber.SetClimbLimits(
                PlayerMinHeight,
                PlayerMaxHeight
            );
        }
    }

    private void ClearTower()
    {
        if (level == null)
        {
            Debug.LogError("TowerSpawner: Level reference is missing.", this);
            return;
        }

        for (int i = level.childCount - 1; i >= 0; i--)
        {
            Destroy(level.GetChild(i).gameObject);
        }

        TopPoint = null;
    }
}