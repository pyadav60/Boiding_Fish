using UnityEngine;

public class FishAnimation : MonoBehaviour
{
    public Transform body;        
    public Transform tailBase;    
    public Transform tailMid;     
    public Transform tailTip;     

    public float tailSwingAmplitude = 30f;   // Maximum swing angle
    public float tailSwingFrequency = 2f;    // Tail swings per second
    public float bodySwingMultiplier = 0.1f; // Multiplier for body counter-rotation

    private float swingTime;
    private float phaseOffset;

    void Start()
    {
        // Assign a random phase offset between 0 and 2pi (so all the fish don't look the same lol)
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        // Update swing time
        swingTime += Time.deltaTime * tailSwingFrequency * Mathf.PI * 2f;

        // Calculate swing angle using sine wave
        float swingAngle = Mathf.Sin(swingTime + phaseOffset) * tailSwingAmplitude;

        // Rotate tail segments for waving motion
        if (tailBase != null)
            tailBase.localRotation = Quaternion.Euler(0f, swingAngle, 0f);

        if (tailMid != null)
            tailMid.localRotation = Quaternion.Euler(0f, swingAngle * 1.5f, 0f);

        if (tailTip != null)
            tailTip.localRotation = Quaternion.Euler(0f, swingAngle * 2f, 0f);

        // Slightly rotate the body
        if (body != null)
            body.localRotation = Quaternion.Euler(0f, -swingAngle * bodySwingMultiplier, 0f);
    }
}
