using UnityEngine;
using UnityEngine.EventSystems;
public sealed class TouchGameControls : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
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
    public void OnPointerExit(PointerEventData e){if(sprint)Sprint=false;else if(!interact&&!pause && Direction==direction)Direction=Vector2.zero;}
    void OnApplicationFocus(bool focused){if(!focused){Direction=Vector2.zero;Sprint=false;}}
    void OnApplicationPause(bool paused){if(paused){Direction=Vector2.zero;Sprint=false;}}
    void OnDisable(){Direction=Vector2.zero;Sprint=false;}
}
