using UnityEngine;
using UnityEngine.EventSystems;

namespace ArcherBattle
{
    /// <summary>Hover grow + press squash + click sound for menu buttons (works while the game is paused).</summary>
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        float target = 1f, current = 1f;
        bool hover;

        public void OnPointerEnter(PointerEventData e) { hover = true; target = 1.07f; Sfx.Play(Sfx.Click, 0.15f, 0.05f, 1.5f); }
        public void OnPointerExit(PointerEventData e) { hover = false; target = 1f; }
        public void OnPointerDown(PointerEventData e) { target = 0.94f; Sfx.Play(Sfx.Click, 0.5f); }
        public void OnPointerUp(PointerEventData e) { target = hover ? 1.07f : 1f; }

        void OnDisable() { hover = false; target = current = 1f; transform.localScale = Vector3.one; }

        void Update()
        {
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            transform.localScale = Vector3.one * current;
        }
    }
}
