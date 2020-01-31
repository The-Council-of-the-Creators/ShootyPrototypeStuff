using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPopUp : MonoBehaviour
{
    private Image renderer;
    private float timeUntilFade = 2.0f;
    private float fadeRate = 0.01f;
    private Coroutine fade;

    public void Display(Sprite image)
    {
        if(fade != null)
        {
            StopCoroutine(fade);
        }
        if(renderer == null)
            renderer = GetComponent<Image>();
        if (image == null)
            return;
        renderer.sprite = image;
        renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1.0f);
        gameObject.SetActive(true);
        fade = StartCoroutine(FadeAfterTime());
    }

    private IEnumerator FadeAfterTime()
    {
        yield return new WaitForSeconds(timeUntilFade);

        while(renderer.color.a > 0)
        {
            renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, renderer.color.a - 0.01f);
            yield return new WaitForSeconds(fadeRate);
        }

        gameObject.SetActive(false);
    }
}
