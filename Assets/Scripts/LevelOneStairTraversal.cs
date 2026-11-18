using System;
using System.Collections;
using UnityEngine;

/// <summary>El cambio de piso exige recorrer los dos tramos y el descanso de la escalera en U.</summary>
public sealed class LevelOneStairTraversal : MonoBehaviour
{
    enum StairStep { Outside, FirstFlight, Landing, ReturnFlight }

    const float FloorSpacing = 32f;
    const float FadeSeconds = .45f;
    LevelOneNightSchool world;
    Level01Manager manager;
    PlayerMovement player;
    StairStep step;
    int stairFloor = -1;
    float readyAt;
    GameObject fadeCanvas;
    CanvasGroup fade;
    bool restoreMovement;

    public bool InTransit { get; private set; }
    public event Action<string> Message;

    public void Configure(LevelOneNightSchool school, Level01Manager levelManager, PlayerMovement actor)
    {
        world = school;
        manager = levelManager;
        player = actor;
        ResetProgress();
    }

    void Update()
    {
        if (world == null || manager == null || player == null || InTransit || !manager.IsPlaying)
            return;
        if (Time.time < readyAt) return;

        Vector2 position = player.transform.position;
        // Las dos escaleras tienen la misma huella y están separadas espacialmente.
        // FloorAt devuelve -1 en los escalones: aquí se identifica la huella por su altura.
        int floor = position.y >= FloorSpacing * .5f ? 1 : 0;
        float y = position.y - floor * FloorSpacing;
        if (floor != stairFloor || position.x < 74f || position.x > 85.6f || y < -2.6f || y > 11.6f)
        {
            ResetProgress();
            stairFloor = floor;
        }

        if (position.x < 74f || position.x > 85.6f || y < -2.6f || y > 11.6f)
            return;

        switch (step)
        {
            case StairStep.Outside:
                if (Inside(position.x, y, 75.3f, 77.8f, 0, 7.5f))
                    step = StairStep.FirstFlight;
                break;
            case StairStep.FirstFlight:
                if (Inside(position.x, y, 75f, 85f, 8f, 11.3f))
                    step = StairStep.Landing;
                break;
            case StairStep.Landing:
                if (Inside(position.x, y, 82f, 85f, 0, 7.5f))
                    step = StairStep.ReturnFlight;
                break;
            case StairStep.ReturnFlight:
                if (Inside(position.x, y, 82f, 85f, -2f, 0))
                    StartCoroutine(ChangeFloor(1 - floor));
                break;
        }
    }

    static bool Inside(float x, float y, float left, float right, float bottom, float top)
        => x >= left && x <= right && y >= bottom && y <= top;

    IEnumerator ChangeFloor(int destinationFloor)
    {
        InTransit = true;
        restoreMovement = player != null && player.enabled;
        if (player != null)
        {
            player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        fadeCanvas = U2Presentation.NewCanvas("Transición de la escalera en U", 150);
        fade = fadeCanvas.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        fade.blocksRaycasts = true;
        fade.interactable = false;
        var curtain = U2Presentation.Image(fadeCanvas.transform, "Cambio de piso", Color.black);
        U2Presentation.Stretch(curtain.rectTransform);
        curtain.raycastTarget = true;

        var caption=U2Presentation.Text(fadeCanvas.transform,"Dirección",destinationFloor==1?"↑ Subiendo al segundo piso":"↓ Bajando a planta baja",Vector2.zero,new Vector2(850,90),36,Color.white);
        LevelOneTypography.Apply(fadeCanvas);
        yield return FadeTo(1f);
        yield return new WaitForSecondsRealtime(.35f);
        if (world != null && player != null && manager != null && !manager.IsCompleted && !manager.IsFailed)
        {
            world.TeleportPlayer(new Vector2(72.5f, destinationFloor * FloorSpacing - .5f));
            Message?.Invoke(destinationFloor == 1 ? "Segundo piso · La escalera queda detrás de ti." :
                "Planta baja · Vuelve a la salida con la USB.");
        }
        yield return FadeTo(0f);

        ResetProgress();
        readyAt = Time.time + 2f;
        CleanupFade();
        RestoreMovement();
        InTransit = false;
    }

    IEnumerator FadeTo(float target)
    {
        float initial = fade != null ? fade.alpha : target;
        float elapsed = 0;
        while (elapsed < FadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            if (fade != null) fade.alpha = Mathf.Lerp(initial, target, Mathf.Clamp01(elapsed / FadeSeconds));
            yield return null;
        }
        if (fade != null) fade.alpha = target;
    }

    void ResetProgress()
    {
        step = StairStep.Outside;
        stairFloor = -1;
    }

    void RestoreMovement()
    {
        if (restoreMovement && player != null && player.gameObject.activeInHierarchy &&
            manager != null && !manager.IsCompleted && !manager.IsFailed)
            player.enabled = true;
        restoreMovement = false;
    }

    void CleanupFade()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.SetActive(false);
            Destroy(fadeCanvas);
        }
        fadeCanvas = null;
        fade = null;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        CleanupFade();
        RestoreMovement();
        ResetProgress();
        InTransit = false;
    }
}
