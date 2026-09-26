using UnityEngine;

public class ClimberInput : MonoBehaviour
{
    private bool upHeld;
    private bool downHeld;

    public float VerticalInput
    {
        get
        {
            if (upHeld && !downHeld)
                return 1f;

            if (downHeld && !upHeld)
                return -1f;

            return 0f;
        }
    }

    public void SetUpHeld(bool held)
    {
        upHeld = held;
    }

    public void SetDownHeld(bool held)
    {
        downHeld = held;
    }

    public void ReleaseAll()
    {
        upHeld = false;
        downHeld = false;
    }
}