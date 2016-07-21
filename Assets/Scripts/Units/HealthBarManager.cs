using UnityEngine;
using System.Collections;

namespace Units
{
    public class HealthBarManager : MonoBehaviour 
    {
        // Reference to the GameObject with the material using the alpha shader to control the bar fill percentage
        public GameObject BarFill;
        private Material barFillMaterial;       

        // Reference to the unit using the health bar
        public GameObject Unit = null;

        // Applied offset to have the health bar above the unit
        public float OffsetAboveUnit = 20;

        // Desired size of the bar to be displayed on screen (constant size independantly of camera zoom)
        public float SizeOnScreen = 1;

        // Visibility status and enforcing
        public bool ForceVisible = false;
        private bool visible = true;


        void Start()
        {            
            barFillMaterial = BarFill.GetComponent<Renderer>().material;
        }


        void Update()
        {            
            // Asjust visibility according to current health value
            if (Health == 1) Visible = false;
            else if (Health <= 0)
            { 
                ForceVisible = false;
                Visible = false;
            }
            else Visible = true;

            // Display the updated position/rotation/scale of the health bar so it can be visible on screen
            if (Unit != null && visible)
            {                                
                var unitPosition = Unit.transform.position;
                unitPosition.y += OffsetAboveUnit;
                transform.position = unitPosition;
                transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                UpdateLockedSizeOnScreen();
            }
        }


        public float Health
        {            
            set { if (barFillMaterial != null) barFillMaterial.SetFloat("_Progress", Mathf.Clamp01(value)); }
            get { return barFillMaterial != null ? barFillMaterial.GetFloat("_Progress") : -1.0f; }
        }


        public bool Visible
        {
            set 
            { 
                visible = value || ForceVisible;
                foreach (Transform child in transform) child.gameObject.SetActive(visible); 
            }
            get { return visible; }
        }


        // Dynamically adjusts the scale of the health bar to ensure it keeps the same size on screen
        private void UpdateLockedSizeOnScreen()
        {
            var a = Camera.main.WorldToScreenPoint(transform.position);
            var b = new Vector3(a.x, a.y + SizeOnScreen, a.z);

            var aa = Camera.main.ScreenToWorldPoint(a);
            var bb = Camera.main.ScreenToWorldPoint(b);

            transform.localScale = Vector3.one * (aa - bb).magnitude;
        }
    }
}