using System.Collections.Generic;
using UnityEngine;

/// <summary>Pose articulada de espalda: las manos y pies alternan apoyos al trepar.</summary>
public sealed class LevelFourClimbRig : MonoBehaviour
{
    readonly List<Sprite> pieces = new List<Sprite>();
    Transform leftArm, rightArm, leftLeg, rightLeg, torso;

    public void Build(SpriteRenderer actor, Sprite back)
    {
        // Los recortes reutilizan el propio personaje, sin sustituirlo por otro alumno.
        torso = Part(actor, back, "Torso", new Rect(5, 4, 8, 9), new Vector2(.5f, .5f), new Vector2(0, -.2f));
        Part(actor, back, "Cabeza", new Rect(1, 12, 16, 12), new Vector2(.5f, .5f), new Vector2(0, .35f));
        leftArm = Part(actor, back, "Brazo izquierdo", new Rect(1, 4, 4, 8), new Vector2(.5f, .75f), new Vector2(-.38f, .05f));
        rightArm = Part(actor, back, "Brazo derecho", new Rect(13, 4, 4, 8), new Vector2(.5f, .75f), new Vector2(.38f, .05f));
        leftLeg = Part(actor, back, "Pierna izquierda", new Rect(5, 0, 4, 6), new Vector2(.5f, 1), new Vector2(-.13f, -.38f));
        rightLeg = Part(actor, back, "Pierna derecha", new Rect(9, 0, 4, 6), new Vector2(.5f, 1), new Vector2(.13f, -.38f));
        Palm(actor, leftArm);
        Palm(actor, rightArm);
        Pose(0);
    }

    void Palm(SpriteRenderer actor, Transform arm)
    {
        var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        pieces.Add(sprite);
        var go = new GameObject("Mano agarrada a la malla"); go.transform.SetParent(arm, false);
        go.transform.localPosition = new Vector3(0, -.34f, 0);
        go.transform.localScale = new Vector3(.14f, .16f, 1);
        var render = go.AddComponent<SpriteRenderer>(); render.sprite = sprite; render.sharedMaterial = actor.sharedMaterial;
        render.color = new Color32(219, 160, 111, 255); render.sortingOrder = 5003;
    }

    Transform Part(SpriteRenderer actor, Sprite source, string name, Rect relative, Vector2 pivot, Vector2 position)
    {
        var crop = new Rect(source.rect.x + relative.x, source.rect.y + relative.y, relative.width, relative.height);
        var sprite = Sprite.Create(source.texture, crop, pivot, source.pixelsPerUnit);
        pieces.Add(sprite);
        var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = position;
        var render = go.AddComponent<SpriteRenderer>(); render.sprite = sprite; render.sharedMaterial = actor.sharedMaterial;
        render.color = actor.color; render.sortingOrder = 5001;
        return go.transform;
    }

    public void Pose(float cycle)
    {
        float left = Mathf.Sin(cycle * Mathf.PI * 2), right = -left;
        leftArm.localRotation = Quaternion.Euler(0, 0, 155 + left * 18);
        rightArm.localRotation = Quaternion.Euler(0, 0, -155 + right * 18);
        leftArm.localPosition = new Vector3(-.36f, .22f + .12f * left, 0);
        rightArm.localPosition = new Vector3(.36f, .22f + .12f * right, 0);
        leftLeg.localRotation = Quaternion.Euler(0, 0, -18 + left * 22);
        rightLeg.localRotation = Quaternion.Euler(0, 0, 18 + right * 22);
        leftLeg.localPosition = new Vector3(-.14f, -.34f + .12f * Mathf.Max(0, left), 0);
        rightLeg.localPosition = new Vector3(.14f, -.34f + .12f * Mathf.Max(0, right), 0);
        torso.localRotation = Quaternion.Euler(0, 0, left * 3);
    }

    void OnDestroy() { foreach (var piece in pieces) if (piece != null) Destroy(piece); }
}
