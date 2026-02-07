using UnityEngine;

namespace AH2714
{
    public class Bottle : MonoBehaviour
    {
        [Header("Bottle Properties")]
        [SerializeField]
        public string bottleName;
        [SerializeField]
        public Material bottleMaterial;
        [SerializeField]
        public Color color;
        [SerializeField]
        public float volume;

        private bool capped;
        private float currentFill;
        private bool overflowing;
        private MeshRenderer mesh;
        public void CapToggle()
        {
            capped = !capped;
            UpdateBottleAppearance();
        }

        public void UpdateBottleAppearance()
        {
            //empty for now
        }

        public void UpdateContents(float amount)
        {
            currentFill += amount;

            if (currentFill > volume)
            {
                overflowing = true;
                Debug.Log("The bottle is overflowing!");
                currentFill = volume;
            }
            else
            {
                overflowing = false;
            }
        }

        public void Awake()
        {
            currentFill = 0f;
            capped = true;
            overflowing = false;
            
            mesh = GetComponent<MeshRenderer>();
            mesh.material = bottleMaterial;
        }
    }

}