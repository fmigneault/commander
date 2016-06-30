using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Cameras
{
    /////////[RequireComponent(typeof(Camera))]
    public class MiniMapManager : MonoBehaviour 
    {        
        public Terrain GroundTerrain;

        ////////public RectTransform MiniMapPanelArea;

        private Camera minimap;
        /////private Rect backgroundArea;
        //////private float MapLeftBorder = 0.032f;
        /////private float MapTopBorder = 0.01f;
        ///////private float MapSize = 0.25f;


    	void Start() 
        {
            minimap = GetComponent<Camera>();
            //////backgroundArea = GetComponentInParent<Canvas>().pixelRect;           
    	}


        void Update()
        {
            SetMiniMapSettingsForPanel();
        }


        public void OnMiniMapClick()
        {
            Debug.Log("OnMiniMapClick FIRED!");

            // Get pointed mouse position normalized for MiniMap area
            var pointed = Input.mousePosition;
            Debug.Log(string.Format("Pointed {0}, Norm: {1}", pointed, pointed.normalized));


            // SEE ||||||| Rect.PointToNomalized  ||   Rect.NormalizedToPoint

        }


        // Adjusts the camera dimension settings to properly position it where the MiniMapPanelArea is specified
        // Also adjust the camera settings to ensure that the terrain is completely visible within the mini-map area
        private void SetMiniMapSettingsForPanel()
        {
//            float anchorWidth = MiniMapPanelArea.anchorMax.x - MiniMapPanelArea.anchorMin.x;
//            float anchorHeight = MiniMapPanelArea.anchorMax.y - MiniMapPanelArea.anchorMin.y;
//
//            Debug.Log(minimap.aspect);
//
//            // Apply normalized map rectangle corresponding to Mini-Map Panel
//            minimap.rect = new Rect(MiniMapPanelArea.anchorMin.x, MiniMapPanelArea.anchorMin.y, anchorWidth, anchorHeight);

            // Apply terrain dimension to orthographic half-size to capture all of it on the map 
            if (minimap != null)
            {
                minimap.orthographicSize = Mathf.Max(GroundTerrain.terrainData.size.x, GroundTerrain.terrainData.size.z) / 2;
            }
        }


//        // Use event call to render the MiniMap camera on top of all GUI elements
//        private void OnGUI()
//        {
//            minimap.Render();
//        }           
    }
}