using UnityEngine;

public class WaterWavyMotion : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private float verticalAmplitude = 0.08f;
    [SerializeField] private float horizontalAmplitude = 0.025f;
    [SerializeField] private float waveSpeed = 2.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationAmount = 2f;
    [SerializeField] private float rotationSpeed = 2f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private bool isInWater;
    private float time;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!isInWater)
            return;

        time += Time.deltaTime * waveSpeed;

        float verticalWave =
            Mathf.Sin(time) * verticalAmplitude;

        float horizontalWave =
            Mathf.Sin(time * 0.8f + 1f) * horizontalAmplitude;

        float rotationWave =
            Mathf.Sin(time * rotationSpeed + 0.5f) * rotationAmount;

        transform.localPosition =
            initialLocalPosition +
            new Vector3(
                horizontalWave,
                verticalWave,
                0f
            );

        transform.localRotation =
            initialLocalRotation *
            Quaternion.Euler(
                0f,
                0f,
                rotationWave
            );
    }

    public void SetInWater(bool value)
    {
        isInWater = value;

        if (!isInWater)
        {
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;

            time = 0f;
        }
    }
}