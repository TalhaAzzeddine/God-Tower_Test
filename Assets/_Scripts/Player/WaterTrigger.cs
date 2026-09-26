using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    [SerializeField]
    private WaterWavyMotion waterMotion;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerVisual"))
            waterMotion.SetInWater(true);




    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerVisual"))
            waterMotion.SetInWater(false);


    }
}