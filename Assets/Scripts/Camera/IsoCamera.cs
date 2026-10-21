using UnityEngine;

namespace Sailing
{
    public class IsoCamera : MonoBehaviour
    {
        public Transform target;
        public float pitch = 40f;
        public float distance = 36f;
        public int quarterTurns;

        float yaw;
        Vector3 focus;
        Vector3 focusVelocity;
        bool hasFocus;

        void LateUpdate()
        {
            if (target == null) return;
            if (!hasFocus)
            {
                focus = target.position;
                hasFocus = true;
            }
            float targetYaw = quarterTurns * 90f;
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-6f * Time.deltaTime));
            focus = Vector3.SmoothDamp(focus, target.position, ref focusVelocity, 0.18f);
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus + orbit * new Vector3(0f, 0f, -distance), orbit);
        }

        public void Turn(int direction)
        {
            quarterTurns += direction;
        }
    }
}
