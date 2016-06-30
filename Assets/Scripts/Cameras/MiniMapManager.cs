using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Cameras
{
    public class MiniMapManager : MonoBehaviour 
    {        
        public Terrain GroundTerrain;
        public RectTransform MinimapPosition;

        ////////public RectTransform MiniMapPanelArea;

        public Camera CameraMiniMap;
        public Camera CameraRTS;
        /////private Rect backgroundArea;
        //////private float MapLeftBorder = 0.032f;
        /////private float MapTopBorder = 0.01f;
        ///////private float MapSize = 0.25f;


    	void Start() 
        {
            
            //////backgroundArea = GetComponentInParent<Canvas>().pixelRect;           
    	}


        void Update()
        {
            SetMiniMapSettingsForPanel();
        }


        // Function called only when a click is detected on the minimap
        // Gets the requested position and moves the carema to the corresponding terrain position
        // Interpolation is done using relative coordinates, so moving/resizing the minimap would not affect the process
        public void OnMiniMapClick()
        {            
//            Debug.Log(GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect);
//            Debug.Log(MinimapPosition.position);
//            Debug.Log(Input.mousePosition);
//
//            var corners = new Vector3[4];
//            MinimapPosition.GetWorldCorners(corners);
//            Debug.Log(string.Format("C1: ({0}), C2: ({1}), C3: ({2}), C4: ({3})",
//                corners[0], corners[1], corners[2], corners[3]));
//
//
//
            // Get pointed mouse position in pixels, (0,0) left-bottom of visible screen
            var minimapPointed = Input.mousePosition;

            // Get the minimap corner positions in pixels, relative to (0,0) left-bottom of visible screen
            var minimapCorners = new Vector3[4];
            MinimapPosition.GetWorldCorners(minimapCorners);           

            // Obtain key positions of minimap
            var xMinMiniMap = minimapCorners[0].x;
            var xMaxMiniMap = minimapCorners[2].x;
            var yMinMiniMap = minimapCorners[0].y;
            var yMaxMiniMap = minimapCorners[2].y;

            // Obtain terrain dimensions as 2D plane
            var wTerrain = GroundTerrain.terrainData.size.x;
            var hTerrain = GroundTerrain.terrainData.size.z;

            // Transfer pixel positions to terrain positions, centered at (0,0,0)
            var xTerrain = (minimapPointed.x - xMinMiniMap) / (xMaxMiniMap - xMinMiniMap) * wTerrain - wTerrain / 2;
            var yTerrain = CameraRTS.transform.position.y;
            var zTerrain = (minimapPointed.y - yMinMiniMap) / (yMaxMiniMap - yMinMiniMap) * hTerrain - hTerrain / 2;

            // Move to new terrain position (camera movement relative to centered terrain, terrain doesn't move)
            CameraRTS.transform.position = new Vector3(xTerrain, yTerrain, zTerrain);


            Debug.Log(minimapPointed);
            Debug.Log(xMinMiniMap);
            Debug.Log(yMinMiniMap);
            Debug.Log(xMaxMiniMap);
            Debug.Log(yMaxMiniMap);
            Debug.Log(wTerrain);
            Debug.Log(hTerrain);
            Debug.Log(xTerrain);
            Debug.Log(yTerrain);
            Debug.Log(zTerrain);
        }

//            // Get pointed mouse position normalized for MiniMap area
//            var minimapPointed = Input.mousePosition.normalized;
//
//            // Get the central position of the minimap that corresponds to terrain position XZ(0,0)
//            // (true since the pivot position is set at (0.5,0.5) of the whole minimap anchor points)
//            var minimapCenter = MinimapPosition.position.normalized;
//
//            // Get the new position relatively to the center (±offset)
//            var minimapRelativePosition = minimapPointed - minimapCenter;
//
//            // Convert relative position to terrain centered dimensions (2D -> 3D, using camera height)
//            var terrainRelativePosition = new Vector3(minimapRelativePosition.x * GroundTerrain.terrainData.size.x / 2, 
//                                                      CameraRTS.transform.position.y,
//                                                      minimapRelativePosition.y * GroundTerrain.terrainData.size.z / 2);

//            Debug.Log(string.Format("Minimap Point: ({0},{1}), Minimap Center: ({2},{3}), Minimap Rel: ({4},{5}), Terrain: ({6},{7},{8})",
//                minimapPointed.x, minimapPointed.y, minimapCenter.x, minimapCenter.y,
//                minimapRelativePosition.x, minimapRelativePosition.y, 
//                terrainRelativePosition.x, terrainRelativePosition.y, terrainRelativePosition.z));


//            // Move to new terrain position (camera movement relative to centered terrain, terrain doesn't move)
//            CameraRTS.transform.position = terrainRelativePosition;


//            Debug.Log(string.Format("Pos: ({0},{1}), Anchor Pos: ({2},{3}), min: ({4},{5}), max: ({6},{7}), W/H: ({8},{9})",
//                MinimapPosition.position.x, MinimapPosition.position.y, 
//                MinimapPosition.anchoredPosition.normalized.x, MinimapPosition.anchoredPosition.normalized.y, 
//                MinimapPosition.rect.xMin, MinimapPosition.rect.yMin, 
//                MinimapPosition.rect.xMax, MinimapPosition.rect.yMax,
//                MinimapPosition.rect.xMax - MinimapPosition.rect.xMin, 
//                MinimapPosition.rect.yMax - MinimapPosition.rect.yMin));
//
//            var corners = new Vector3[4];
//            MinimapPosition.GetWorldCorners(corners);
//            Debug.Log(string.Format("C1: ({0}), C2: ({1}), C3: ({2}), C4: ({3})",
//                corners[0], corners[1], corners[2], corners[3]));


            // SEE ||||||| Rect.PointToNomalized  ||   Rect.NormalizedToPoint

       // }


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
            if (CameraMiniMap != null)
            {
                CameraMiniMap.orthographicSize = Mathf.Max(GroundTerrain.terrainData.size.x, GroundTerrain.terrainData.size.z) / 2;
            }
        }


//        // Use event call to render the MiniMap camera on top of all GUI elements
//        private void OnGUI()
//        {
//            minimap.Render();
//        }           
    }
}