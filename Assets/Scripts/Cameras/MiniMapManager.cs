using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Cameras
{
    public class MiniMapManager : MonoBehaviour 
    {        
        public Terrain GroundTerrain;
        public RectTransform MinimapPosition;
        public Camera CameraMiniMap;
        public Camera CameraRTS;


        void Update()
        {
            SetMiniMapSettingsForPanel();
        }


        // Function called only when a click is detected on the minimap
        // Gets the requested position and moves the carema to the corresponding terrain position
        // Interpolation is done using relative coordinates, so moving/resizing the minimap would not affect the process
        public void OnMiniMapClick()
        {
            // Get pointed mouse position in pixels, (0,0) left-bottom of visible screen
            var minimapPointed = Input.mousePosition;

            // Get the minimap corner positions in pixels, relative to (0,0) left-bottom of visible screen
            var minimapCorners = new Vector3[4];
            MinimapPosition.GetWorldCorners(minimapCorners);           

            // Obtain key positions of minimap
            var minimapMinX = minimapCorners[0].x;
            var minimapMaxX = minimapCorners[2].x;
            var minimapMinY = minimapCorners[0].y;
            var minimapMaxY = minimapCorners[2].y;

            // Obtain terrain dimensions as 2D plane
            var terrainW = GroundTerrain.terrainData.size.x;
            var terrainH = GroundTerrain.terrainData.size.z;

            // Transfer pixel positions to terrain positions, centered at (0,0,0)
            var terrainX = (minimapPointed.x - minimapMinX) / (minimapMaxX - minimapMinX) * terrainW - terrainW / 2;
            var terrainY = CameraRTS.transform.position.y;
            var terrainZ = (minimapPointed.y - minimapMinY) / (minimapMaxY - minimapMinY) * terrainH - terrainH / 2;

            // Since the minimap is displayed as a square image with the maximum dimension (after resized by screen),
            // non-sqaured terrain are displayed with filling grey borders in the appropriate sides. The terrain is 
            // displayed with an aspect ratio of 1, but the coordinates will not if w != h. They must therefore be 
            // counter-scaled by the real terrain dimension ratio to move to the right location.
            var terrainAspectRatio = terrainW / terrainH;           
            if (terrainAspectRatio < 1)      terrainX = terrainX / terrainAspectRatio;
            else if (terrainAspectRatio > 1) terrainZ = terrainZ * terrainAspectRatio;

            // Move to new terrain position (camera movement relative to centered terrain, terrain doesn't move)
            CameraRTS.transform.position = new Vector3(terrainX, terrainY, terrainZ);
        }


        // Adjusts the camera dimension settings to properly capture the whole terrain on the minimap
        private void SetMiniMapSettingsForPanel()
        {          
            if (CameraMiniMap != null)
            {
                // Apply terrain dimension to orthographic half-size to capture all of it on the map 
                //    We use the maximum terrain dimension to capture the whole map, a background fills the other
                //    dimension remaining space in the image if the terrain is not square
                CameraMiniMap.orthographicSize = Mathf.Max(GroundTerrain.terrainData.size.x, GroundTerrain.terrainData.size.z) / 2;
            }
        }          
    }
}