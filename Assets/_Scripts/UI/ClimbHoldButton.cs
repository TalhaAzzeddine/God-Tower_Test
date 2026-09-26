using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClimbHoldButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    public enum Direction
    {
        Up,
        Down
    }

    [Header("References")]
    [SerializeField] private Direction direction;
    [SerializeField] private ClimberInput climberInput;
    [SerializeField] private ClimberController climberController;
    [SerializeField] private Image buttonImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField]
    private Color holdingColor =
        new Color(0.75f, 0.9f, 1f);

    [SerializeField]
    private Color disabledColor =
        new Color(0.5f, 0.5f, 0.5f);

    private bool isHeld;

    private void Awake()
    {
        if (climberInput == null)
            climberInput = GetComponentInParent<ClimberInput>();

        if (climberController == null)
            climberController = GetComponentInParent<ClimberController>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        UpdateVisualState();
    }

    private void Update()
    {
        UpdateVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanMove())
        {
            isHeld = false;
            SetInput(false);
            return;
        }

        isHeld = true;
        SetInput(true);

        UpdateVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        isHeld = false;
        SetInput(false);

        UpdateVisualState();
    }

    private void SetInput(bool held)
    {
        if (climberInput == null)
            return;

        switch (direction)
        {
            case Direction.Up:
                climberInput.SetUpHeld(held);
                break;

            case Direction.Down:
                climberInput.SetDownHeld(held);
                break;
        }
    }

    private bool CanMove()
    {
        if (climberController == null)
            return true;

        return direction == Direction.Up
            ? climberController.CanMoveUp
            : climberController.CanMoveDown;
    }

    private void UpdateVisualState()
    {
        if (buttonImage == null)
            return;

        // Highest priority: movement is not possible.
        if (!CanMove())
        {
            buttonImage.color = disabledColor;
            return;
        }

        // Player is currently holding the button.
        if (isHeld)
        {
            buttonImage.color = holdingColor;
            return;
        }

        // Available normally.
        buttonImage.color = normalColor;
    }

    private void OnDisable()
    {
        Release();
    }
}