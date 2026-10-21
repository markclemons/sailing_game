using UnityEngine;

namespace Sailing
{
    public class ResourceNode : MonoBehaviour
    {
        public ResourceKind kind;
        public int amount = 4;
        public float respawnSeconds = 20f;
        public bool Ready = true;

        float timer;
        Renderer[] renderers;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            if (Ready) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            Ready = true;
            SetVisible(true);
        }

        public bool TryTake()
        {
            if (!Ready) return false;
            Ready = false;
            timer = respawnSeconds;
            SetVisible(false);
            return true;
        }

        void SetVisible(bool visible)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
        }
    }
}
