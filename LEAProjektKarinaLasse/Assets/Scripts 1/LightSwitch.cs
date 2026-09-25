using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [Header("Lights")]
    [SerializeField] private GameObject[] lights;

    [Header("Sound")]
    [SerializeField] private AudioSource lightSwitchAudioSource;
    [SerializeField] private AudioClip lightOnSound;

    private bool lightsOn = false;


    public void Interact()
    {
        // ==========================================
        // LICHT IST AUS → EINSCHALTEN
        // ==========================================

        if (!lightsOn)
        {
            lightsOn = true;

            SetLights(true);


            // Lichtschalter-Sound
            if (lightSwitchAudioSource != null &&
                lightOnSound != null)
            {
                lightSwitchAudioSource.PlayOneShot(
                    lightOnSound
                );
            }


            if (StoryManagerStore.Instance != null)
            {
                StoryManagerStore.Instance.LightTurnedOn();
            }

            return;
        }


        // ==========================================
        // LICHT IST AN → AUSSCHALTEN
        // ==========================================

        if (StoryManagerStore.Instance != null)
        {
            if (!StoryManagerStore.Instance.CanTurnLightOff())
            {
                return;
            }
        }


        lightsOn = false;

        SetLights(false);


        if (StoryManagerStore.Instance != null)
        {
            StoryManagerStore.Instance.LightTurnedOff();
        }
    }


    private void SetLights(bool state)
    {
        foreach (GameObject lightObject in lights)
        {
            if (lightObject != null)
            {
                lightObject.SetActive(state);
            }
        }
    }
}