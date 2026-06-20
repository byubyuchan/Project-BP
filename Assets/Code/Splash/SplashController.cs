using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SplashController : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup splashCanvasGroup;

    [Header("Timings")]
    public float fadeDuration = 1.5f;
    public float displayTime = 2.0f;

    [Header("Next Scene")]
    public string nextSceneName = "LobbyScene";

    private Coroutine splashCoroutine;
    private bool isSkipping = false;

    private void Start()
    {
        splashCanvasGroup.alpha = 0f;
        splashCoroutine = StartCoroutine(ShowSplashScreen());
    }

    private void Update()
    {
        if (!isSkipping)
        {
            bool isAnyButtonPressed = false;

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                isAnyButtonPressed = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isAnyButtonPressed = true;
            }

            else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                isAnyButtonPressed = true;
            }

            if (isAnyButtonPressed)
            {
                isSkipping = true;
                SkipSplash();
            }
        }
    }

    private IEnumerator ShowSplashScreen()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayTime);

        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            yield return null;
        }

        LoadNextScene();
    }

    private void SkipSplash()
    {
        if (splashCoroutine != null)
        {
            StopCoroutine(splashCoroutine);
        }
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}