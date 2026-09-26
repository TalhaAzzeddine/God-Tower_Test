using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClimbProgressUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClimberController climber;
    [SerializeField] private Slider progressSlider;

    [Header("Labels")]
    [SerializeField] private TMP_Text currentHeightText;
    [SerializeField] private TMP_Text targetHeightText;

    [Header("Display")]
    [SerializeField] private bool displayRelativeHeight = true;
    [SerializeField] private string heightFormat = "0";

    private void Start()
    {
        if (climber == null)
            climber = FindAnyObjectByType<ClimberController>();

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (climber == null)
            return;

        // Progress from 0 to 1.
        if (progressSlider != null)
        {
            progressSlider.value = climber.HeightProgress;
        }

        // Current height.
        float currentHeight = displayRelativeHeight
            ? climber.CurrentHeight - climber.MinHeight
            : climber.CurrentHeight;

        // Target height.
        float targetHeight = displayRelativeHeight
            ? climber.MaxHeight - climber.MinHeight
            : climber.MaxHeight;

        if (currentHeightText != null)
        {
            currentHeightText.text =
                currentHeight.ToString(heightFormat);
        }

        if (targetHeightText != null)
        {
            targetHeightText.text =
                targetHeight.ToString(heightFormat);
        }
    }
}