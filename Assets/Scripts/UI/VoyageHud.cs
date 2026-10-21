using UnityEngine;
using UnityEngine.UI;

namespace Sailing
{
    public class VoyageHud : MonoBehaviour
    {
        public Text label;

        public void Show(string text)
        {
            if (label != null) label.text = text;
        }
    }
}
