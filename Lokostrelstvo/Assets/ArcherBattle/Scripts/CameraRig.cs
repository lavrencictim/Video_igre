using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ArcherBattle
{
    /// <summary>
    /// Orthographic side-view camera that keeps both archers framed, with smooth zoom and screen shake.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        static CameraRig instance;

        public Camera cam;
        public Transform a, b;

        const float GroundMargin = 2.2f;   // how much ground is visible below the archers' feet
        const float SideMargin = 4.5f;
        const float MinSize = 6.8f;

        float shake;
        Vector3 smoothPos;
        float sizeVel;
        Vector3 posVel;

        public static void Shake(float amount)
        {
            if (instance != null) instance.shake = Mathf.Max(instance.shake, amount);
        }

        public static Camera Setup()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            if (Object.FindAnyObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            cam.orthographic = true;
            cam.orthographicSize = MinSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.24f, 0.48f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.transform.SetPositionAndRotation(new Vector3(0, 4, -10), Quaternion.identity);

            // Keep the hand-painted colours exactly as authored (no tonemapping from the template's volume).
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = false;

            instance = cam.GetComponent<CameraRig>();
            if (instance == null) instance = cam.gameObject.AddComponent<CameraRig>();
            instance.cam = cam;
            instance.smoothPos = cam.transform.position;
            return cam;
        }

        void Desired(out Vector3 pos, out float size)
        {
            float ax = a != null ? a.position.x : -8f;
            float bx = b != null ? b.position.x : 8f;
            float halfWidth = Mathf.Abs(ax - bx) * 0.5f + SideMargin;
            size = Mathf.Max(MinSize, halfWidth / Mathf.Max(0.5f, cam.aspect));
            float cx = (ax + bx) * 0.5f;
            // keep the ground near the bottom of the screen and leave sky for arrow arcs
            pos = new Vector3(cx, size - GroundMargin, -10f);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Desired(out var pos, out var size);
            float dt = Time.unscaledDeltaTime;
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVel, 0.6f, Mathf.Infinity, dt);
            smoothPos = Vector3.SmoothDamp(smoothPos, pos, ref posVel, 0.5f, Mathf.Infinity, dt);

            Vector3 offset = Vector3.zero;
            if (shake > 0.001f)
            {
                offset = (Vector3)(Random.insideUnitCircle * shake * 0.5f);
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
            }
            cam.transform.position = smoothPos + offset;
        }
    }
}
