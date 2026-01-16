using System.Collections.Generic;
using UnityEngine;

namespace H3M.Painting
{
    [CreateAssetMenu(fileName = "BrushCatalog", menuName = "H3M/Painting/Brush Catalog")]
    public class H3MBrushCatalog : ScriptableObject
    {
        [Header("Brush Library")]
        public List<H3MBrushDescriptor> allBrushes = new List<H3MBrushDescriptor>();

        [Header("Categories")]
        public List<string> categories = new List<string>
        {
            "Fire", "Plasma", "Smoke", "Magic", "Nature", "Geometric", "Abstract"
        };

        // Get brushes by category
        public List<H3MBrushDescriptor> GetBrushesByCategory(string category)
        {
            return allBrushes.FindAll(b => b.category == category);
        }

        // Get brush by name
        public H3MBrushDescriptor GetBrush(string name)
        {
            return allBrushes.Find(b => b.brushName == name);
        }
    }
}
