using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace YaCasi.ActividadMenu
{
    public sealed class ActivityMenuReturn : MonoBehaviour
    {
        public Button backButton;
        void Awake() { backButton.onClick.AddListener(() => { Time.timeScale = 1f; SceneManager.LoadScene("MenuActividadCompleta"); }); }
    }
}
