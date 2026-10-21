using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Sailing
{
    [DefaultExecutionOrder(-50)]
    public class SailInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public ShipController ship;
        public IsoCamera iso;
        public ShipGrid grid;
        public Cargo cargo;
        public Voyage voyage;
        public TouchHelm touch;
        public float gatherRange = 7f;

        public bool Building { get; private set; }

        InputAction helmAction;
        InputAction cameraLeft;
        InputAction cameraRight;
        InputAction interact;
        InputAction build;
        InputAction remove;
        InputAction cycle;
        InputAction cycleBack;
        InputAction cycleScroll;
        float cursorTimer;
        float scrollTimer;
        int interactFrame = -1;
        int removeFrame = -1;
        int buildFrame = -1;

        public static Vector2 CombineHelm(Vector2 action, Vector2 touch)
        {
            return touch.sqrMagnitude > action.sqrMagnitude ? touch : action;
        }

        void Awake()
        {
            InputActionMap map = actions.FindActionMap("Sail", true);
            helmAction = map.FindAction("Helm", true);
            cameraLeft = map.FindAction("CameraLeft", true);
            cameraRight = map.FindAction("CameraRight", true);
            interact = map.FindAction("Interact", true);
            build = map.FindAction("Build", true);
            remove = map.FindAction("Remove", true);
            cycle = map.FindAction("Cycle", true);
            cycleBack = map.FindAction("CycleBack", true);
            cycleScroll = map.FindAction("CycleScroll", true);
            map.Enable();
        }

        void OnDisable()
        {
            if (actions != null)
            {
                InputActionMap map = actions.FindActionMap("Sail");
                if (map != null) map.Disable();
            }
            Time.timeScale = 1f;
            if (ship != null) ship.Hold = false;
        }

        void Update()
        {
            Vector2 touchValue = touch != null ? touch.Value : Vector2.zero;
            Vector2 helm = CombineHelm(helmAction.ReadValue<Vector2>(), touchValue);
            if (cameraLeft.WasPressedThisFrame()) CameraLeft();
            if (cameraRight.WasPressedThisFrame()) CameraRight();
            if (build.WasPressedThisFrame()) ToggleBuild();
            if (interact.WasPressedThisFrame() && !MouseBlockedByUi()) Interact();
            if (remove.WasPressedThisFrame()) Remove();
            if (cycle.WasPressedThisFrame()) grid.Cycle(1);
            if (cycleBack.WasPressedThisFrame()) grid.Cycle(-1);
            scrollTimer -= Time.unscaledDeltaTime;
            float scroll = cycleScroll.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) > 0.05f && scrollTimer <= 0f)
            {
                grid.Cycle(scroll > 0f ? 1 : -1);
                scrollTimer = 0.2f;
            }

            if (Building)
            {
                ship.Helm = 0f;
                ship.Hold = true;
                StepCursor(helm);
            }
            else
            {
                ship.Hold = false;
                ship.Helm = helm.x;
                if (helm.y > 0.5f) ship.Sail = Mathf.MoveTowards(ship.Sail, 1f, Time.deltaTime * 0.85f);
                else if (helm.y < -0.5f) ship.Sail = Mathf.MoveTowards(ship.Sail, 0.35f, Time.deltaTime * 0.85f);
            }
        }

        public void CameraLeft()
        {
            if (iso != null) iso.Turn(-1);
        }

        public void CameraRight()
        {
            if (iso != null) iso.Turn(1);
        }

        public void ToggleBuild()
        {
            if (buildFrame == Time.frameCount) return;
            buildFrame = Time.frameCount;
            Building = !Building;
            grid.ShowGhost = Building;
            grid.RefreshGhost();
            Time.timeScale = Building ? 0.2f : 1f;
            ship.Hold = Building;
            voyage.Flash(Building ? "Build mode. Place with interact, remove with the other button." : "Back to sailing.");
        }

        public void Interact()
        {
            if (interactFrame == Time.frameCount) return;
            interactFrame = Time.frameCount;
            if (Building)
            {
                int wood = cargo.wood;
                int salvage = cargo.salvage;
                if (!grid.Place(ref wood, ref salvage, out string reason))
                {
                    voyage.Flash(reason);
                    return;
                }
                cargo.wood = wood;
                cargo.salvage = salvage;
                voyage.Flash("Placed a " + grid.GhostKind.ToString().ToLower() + " block.");
                return;
            }

            ResourceNode[] nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            ResourceNode best = null;
            float bestDistance = gatherRange;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!nodes[i].Ready) continue;
                float distance = Vector3.Distance(ship.LogicalPosition, nodes[i].transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = nodes[i];
                }
            }
            if (best == null)
            {
                voyage.Flash("Nothing in reach.");
                return;
            }
            if (!best.TryTake()) return;
            if (best.kind == ResourceKind.Wood) cargo.wood += best.amount;
            else cargo.salvage += best.amount;
            voyage.Flash("Picked up " + best.amount + " " + best.kind.ToString().ToLower() + ".");
        }

        public void Remove()
        {
            if (!Building) return;
            if (removeFrame == Time.frameCount) return;
            removeFrame = Time.frameCount;
            int wood = cargo.wood;
            int salvage = cargo.salvage;
            if (!grid.Remove(ref wood, ref salvage, out string reason))
            {
                voyage.Flash(reason);
                return;
            }
            cargo.wood = wood;
            cargo.salvage = salvage;
            voyage.Flash("Block removed.");
        }

        public void CycleType()
        {
            grid.Cycle(1);
        }

        void StepCursor(Vector2 helm)
        {
            cursorTimer -= Time.unscaledDeltaTime;
            if (helm.sqrMagnitude < 0.36f)
            {
                cursorTimer = 0f;
                return;
            }
            if (cursorTimer > 0f) return;
            int dx = Mathf.Abs(helm.x) >= 0.5f ? (int)Mathf.Sign(helm.x) : 0;
            int dz = Mathf.Abs(helm.y) >= 0.5f ? (int)Mathf.Sign(helm.y) : 0;
            grid.MoveCursor(dx, dz);
            cursorTimer = 0.16f;
        }

        static bool MouseBlockedByUi()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;
            EventSystem system = EventSystem.current;
            if (system == null) return false;
            if (system.IsPointerOverGameObject()) return true;
            Pointer pointer = Pointer.current;
            return pointer != null && system.IsPointerOverGameObject(pointer.deviceId);
        }
    }
}
