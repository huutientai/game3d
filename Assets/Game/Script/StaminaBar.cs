using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
public class StaminaBar : MonoBehaviour
{
    public Slider StaminaSlider;
    public Character Character;
    // Start is called before the first frame update
    private void Update()
    {
        if (Character != null && StaminaSlider != null)
        {
            StaminaSlider.value = Character.CurrentStamina / Character.MaxStamina;
        }
    }
}
