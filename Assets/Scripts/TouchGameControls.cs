using UnityEngine;
using UnityEngine.EventSystems;
public sealed class TouchGameControls : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
{
    public static Vector2 Direction;public static bool Sprint;
    public Vector2 direction;public bool sprint;public bool interact;public bool pause;
    public void OnPointerDown(PointerEventData e)
    {
        if(interact){var p=Object.FindFirstObjectByType<PlayerInteraction>();if(p!=null)p.TryInteract();}
        else if(pause){var u=Object.FindFirstObjectByType<LevelOneUpgrade>();if(u!=null)u.TogglePause();}
        else if(sprint)Sprint=true;else Direction=direction;
    }
    public void OnPointerUp(PointerEventData e){if(sprint)Sprint=false;else if(!interact&&!pause)Direction=Vector2.zero;}
    void OnDisable(){Direction=Vector2.zero;Sprint=false;}
}
