using UnityEngine;
using UnityEditor;
using RayFire;

namespace RayFireEditor
{
    
        public class RFUvRegionEditor : EditorWindow
    {
        static Texture2D texture;
        static RFSurface surface;
        Vector2          regMin;
        Vector2          regMax;
        Rect             rect;
        Vector2[]        resizeHandles;
        const string     minStr = "Min: ";
        const string     maxStr = "Max: ";
        
        Vector2 posMin;
        Vector2 posMax;
        bool    isDragging;
        float   dotRadius = 4f;
        
        // Show window
        public static EditorWindow ShowWindow(Texture2D tex, RFSurface surf, string nm)
        {
            texture = tex;
            surface = surf;
            
            EditorWindow win = GetWindow<RFUvRegionEditor>(nm + ": UV Region");
            if (texture != null)
            {
                win.maxSize = new Vector2 (texture.width, texture.height);
                win.minSize = new Vector2 (texture.width, texture.height) / 10;
            }
            else
            {
                win.maxSize = new Vector2 (400, 400);
                win.minSize = new Vector2 (50, 50);
            }
            return win;
        }

        private void OnEnable()
        {
            if (surface == null)
                return;
            
            // Get coords
            regMin   = surface.UvRegionMin;
            regMax   = surface.UvRegionMax;
            regMin.y = 1f - regMin.y;
            regMax.y = 1f - regMax.y;
            
            rect = new Rect(regMin.x * maxSize.x, regMin.y * maxSize.y, regMax.x*maxSize.x, regMax.x*maxSize.y);
            
            UpdateResizeHandles();
        }

        private void OnDisable()
        {
            texture = null;
            surface = null;
        }

        private void OnGUI()
        {
            if (surface == null)
                return;
            
            // Draw texture
            if (texture != null)
                GUI.DrawTexture(new Rect(0, 0, position.width, position.height), texture, ScaleMode.StretchToFill);
            
            // Region coords
            regMin   = surface.UvRegionMin;
            regMax   = surface.UvRegionMax;
            regMin.y = 1f - regMin.y;
            regMax.y = 1f - regMax.y;
            
            // Corner position
            posMin.x = Mathf.Lerp (0, position.width,  regMin.x);
            posMin.y = Mathf.Lerp (0, position.height, regMin.y);
            posMax.x = Mathf.Lerp (0, position.width,  regMax.x);
            posMax.y = Mathf.Lerp (0, position.height, regMax.y);
            
            // Green rectangle
            rect     = new Rect(posMin.x, posMin.y, posMax.x - posMin.x - 0.01f, posMax.y - posMin.y - 0.001f);
            
            // Draw rect
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0.1f, 0.1f, 0.1f, 0.1f), Color.green);
            Handles.EndGUI();
            
            // Draw labels
            Handles.Label (new Vector3 (posMin.x + 2,   posMin.y + 10f, 0), minStr + surface.UvRegionMin.ToString());
            Handles.Label (new Vector3 (posMax.x - 100, posMax.y -10f,  0), maxStr + surface.UvRegionMax.ToString());
            
            // Corner dots
            Handles.color = RFUI.color_orange;
            Handles.DrawSolidDisc(posMin, Vector3.forward, dotRadius);
            Handles.DrawSolidDisc(posMax, Vector3.forward, dotRadius);
            
            /*
            
            // Handle mouse events
            Event   currentEvent  = Event.current;
            Vector2 mousePosition = currentEvent.mousePosition;
            
            // Check if mouse is over the dot
            bool isMouseOverDotMin = Vector2.Distance(mousePosition, posMin) <= dotRadius;
            bool isMouseOverDotMax = Vector2.Distance(mousePosition, posMax) <= dotRadius;
            
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0)
                    {
                        if (isMouseOverDotMin == true || isMouseOverDotMax == true)
                        {
                            isDragging = true;
                            currentEvent.Use();
                        }
                    }
                    break;
                
                case EventType.MouseDrag:
                    if (isDragging == true && currentEvent.button == 0)
                    {
                        if (isMouseOverDotMin == true)
                        {
                            posMin = mousePosition;
                        }
                        if (isMouseOverDotMin == true)
                        {
                            posMax = mousePosition;
                        }
                        currentEvent.Use();
                        Repaint();
                    }
                    break;
                
                case EventType.MouseUp:
                    if (currentEvent.button == 0)
                    {
                        isDragging = false;
                        currentEvent.Use();
                    }
                    break;
            }
            
            // Add visual
            if (isMouseOverDotMin == true || isMouseOverDotMax == true)
                EditorGUIUtility.AddCursorRect(new Rect(mousePosition.x - 20, mousePosition.y - 20, 40, 40), MouseCursor.MoveArrow);
                
            */
        }

        private void HandleResize()
        {
            for (int i = 0; i < resizeHandles.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 pos               = new Vector3 (resizeHandles[i].x, resizeHandles[i].y, 0);
                var fmh_165_73_638874995087188649 = Quaternion.identity; Vector3 newHandlePosition = Handles.FreeMoveHandle(pos, 10f, Vector2.zero, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateRectBasedOnHandle(i, new Vector2 (newHandlePosition.x, newHandlePosition.y));
                }
            }
        }

        private void UpdateRectBasedOnHandle(int handleIndex, Vector2 newPosition)
        {
            switch (handleIndex)
            {
                case 0: 
                    rect.xMin = newPosition.x;
                    rect.yMin = newPosition.y;
                    break;
                case 1: 
                    rect.xMax = newPosition.x;
                    rect.yMin = newPosition.y;
                    break;
                case 2: 
                    rect.xMin = newPosition.x;
                    rect.yMax = newPosition.y;
                    break;
                case 3: 
                    rect.xMax = newPosition.x;
                    rect.yMax = newPosition.y;
                    break;
            }
            
            rect.width = Mathf.Max(rect.width, 50);
            rect.height = Mathf.Max(rect.height, 50);
        }

        private void UpdateResizeHandles()
        {
            resizeHandles = new Vector2[]
            {
                new Vector2(rect.xMin, rect.yMin), 
                new Vector2(rect.xMax, rect.yMin), 
                new Vector2(rect.xMin, rect.yMax), 
                new Vector2(rect.xMax, rect.yMax)  
            };
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}



                   
