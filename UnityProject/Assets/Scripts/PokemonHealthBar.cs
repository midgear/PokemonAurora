using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PokemonHealthBar : MonoBehaviour
{
    // NOTE(BluCloos): These inspector varaibles are always initialized during runtime.
    #region InspectorVariables
    [SerializeField] private Slider slider;
    [SerializeField] private Image sliderImg;
    [SerializeField] private Image backgroundImg;
    #endregion

    #region PrivateVariables
    private Material masterMat = null;
    private float targetSliderVal = 1.0f;
    #endregion

    #region PublicInterface
    /// <summary>
    /// Updates the health bar by setting the percentage and color. 
    /// </summary>
    public void Updated(float newVal)
    {
        targetSliderVal = newVal;
    }
    
    /// <summary>
    /// Make the health bar dissapear with a "bang".
    /// </summary>
    public void BeginDeath()
    {
        StartCoroutine(DeathOverTime());
    }
    #endregion

    #region Functions
    IEnumerator DeathOverTime()
    {
        for (int i = 0; i < 60; i++)
        {
            yield return new WaitForSeconds(0.01f);
            if (masterMat != null)
                masterMat.SetFloat("_MasterAlpha", (60.0f - i) / 60.0f);
        }

        // Of course, once the pokemon is absolutely dead, this component needs to die as well
        Destroy(gameObject, 0.1f);

        yield return null;
    }
    #endregion

    #region UnityCallbacks
    void Awake()
    {
        bool bad = false;

        bad = (bad || (sliderImg == null));
        bad = (bad || (backgroundImg == null));
        bad = (bad || (slider == null));

        if (bad)
        {
            Debug.LogError("Inspector variables not set! Deleting health bar.");
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        // NOTE(BluCloos): I don't know if we could ever get a null material, but I do these checks just to be extra verbose.
        // Clone the existing material to prevent material mods from affecting all pokemon will health bars
        masterMat = new Material(sliderImg.material);
        if (masterMat != null)
        {
            sliderImg.material = masterMat;
            backgroundImg.material = masterMat;
        }
    }

    void Update()
    {
        // Push the hp towards the target by the constant delta!
        // once the hp is set, we gotta update that color mans!
        bool down = true;
        float cVal = slider.value;
        float newVal = cVal - GameManager.GetPokemonHealthVelocity() * Time.deltaTime;

        if (cVal < targetSliderVal)
            down = false;

        if (down)
        {
            if (newVal < targetSliderVal)
                newVal = targetSliderVal;
        }
        else
        {
            if (newVal > targetSliderVal)
                newVal = targetSliderVal;
        }

        slider.value = newVal; // Commit the new value

        if (newVal >= 0.5f)
        {
            //green
            sliderImg.color = new Color(99.0f / 255.0f, 255.0f / 255.0f, 99.0f / 255.0f);
        }
        else if (newVal >= 0.2f)
        {
            // yellow
            sliderImg.color = new Color(255.0f / 255.0f, 221.0f / 255.0f, 0.0f);
        }
        else
        {
            // red
            sliderImg.color = new Color(222.0f / 255.0f, 98.0f / 255.0f, 70.0f / 255.0f);
        }
    }
    #endregion
}
