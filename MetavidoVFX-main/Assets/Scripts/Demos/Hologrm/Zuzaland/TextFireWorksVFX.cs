using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;

public class TextFireWorksVFX : MonoBehaviour
{
    public string InputText;

    private VisualEffect _vfx;
    private Vector4 Index1;
    private Vector4 Index2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

   

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetInputText()
    {
        try
        {
            _vfx = GetComponent<VisualEffect>();
            if (_vfx == null)
            {
                Debug.LogError("TextFireWorksVFX: VisualEffect component not found");
                return;
            }

            if (string.IsNullOrEmpty(InputText))
            {
                Debug.LogWarning("TextFireWorksVFX: InputText is null or empty, using default text");
                InputText = "HELLO";
            }

            String2Vector(InputText);

            _vfx.SetVector4("TextIndex1", Index1);
            _vfx.SetVector4("TextIndex2", Index2);
        }
        catch (Exception e)
        {
            Debug.LogError($"TextFireWorksVFX: Error in SetInputText: {e.Message}");
        }
    }

    void String2Vector(string str)
    {
        try
        {
            if (string.IsNullOrEmpty(str))
            {
                str = "HELLO";
            }
            
            str = str.ToUpper();
            Debug.Log("upper:" + str);

            // Ensure the string is exactly 8 characters
            if (str.Length < 8)
            {
                // Pad with backticks to reach 8 characters
                str = str.PadRight(8, '`');
            }
            else if (str.Length > 8)
            {
                // Truncate to 8 characters
                str = str.Substring(0, 8);
            }

            Debug.Log("length:" + str);

            char[] chars = str.ToCharArray();

            // Initialize vectors to avoid null references
            Index1 = new Vector4(0, 0, 0, 0);
            Index2 = new Vector4(0, 0, 0, 0);

            for (int i = 0; i < chars.Length && i < 8; i++)
            {
                float charIndex = 0;
                
                // Map special characters to specific indices
                if (chars[i] == '!')
                {
                    charIndex = 28;
                }
                else if (chars[i] == ',')
                {
                    charIndex = 26;
                }
                else if (chars[i] == '.')
                {
                    charIndex = 27;
                }
                else if (chars[i] == '(')
                {
                    charIndex = 29;
                }
                else if (chars[i] == ')')
                {
                    charIndex = 30;
                }
                else if (chars[i] >= 'A' && chars[i] <= 'Z')
                {
                    // Standard alphabet characters
                    charIndex = chars[i] - 'A';
                }
                else
                {
                    // Default for any other character
                    charIndex = 0;
                }
                
                // First 4 characters go to Index1, next 4 to Index2
                if (i < 4)
                {
                    Index1[i] = charIndex;
                }
                else
                {
                    Index2[i - 4] = charIndex;
                }
            }

            Debug.Log("1:" + Index1);
            Debug.Log("2:" + Index2);
        }
        catch (Exception e)
        {
            Debug.LogError($"TextFireWorksVFX: Error in String2Vector: {e.Message}");
            
            // Set default values in case of error
            Index1 = new Vector4(7, 4, 11, 11); // HELL
            Index2 = new Vector4(14, 0, 0, 0);  // O
        }
    }
}
