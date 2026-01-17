using JetBrains.Annotations;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

public class FrameRequester : MonoBehaviour
{
    public string pythonServerIp = "PYTHON_PC_IP"; // replace with real IP
    public int pythonPort = 6006;
    [SerializeField] Renderer debugRenderer;
    [SerializeField] VisualEffect vfx;
    [SerializeField] Vector4 faceAtlasConfig; // (numX, numY, resX, resY)

    bool showDebugMenu = false;
    string ipInput = "";
    int faceId = 0;
    Texture2D atlasTexture;
    int cellX = 0;
    int cellY = 0;
    float cooldown = 0f;

    void Awake()
    {
        // Load previously saved IP (if exists)
        if (PlayerPrefs.HasKey("PythonServerIP"))
        {
            pythonServerIp = PlayerPrefs.GetString("PythonServerIP");
        }
        ipInput = pythonServerIp;
    }

    private void Start()
    {
        CreateAtlas();
    }

    void Update()
    {
        /*        if (Input.GetKeyDown(KeyCode.F)) // Change 'F' to your desired key
                {
                    HumanBorn(Vector3.zero);
                }*/

        // Toggle debug UI with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            showDebugMenu = !showDebugMenu;
        }

        cooldown += Time.deltaTime;
    }

    public void HumanBorn(Vector3 pos)
    {
        if (cooldown > 1.5f)
        {
            Texture2D receivedTexture = SendFrameRequest();

            if (receivedTexture)
            {
                int cellIndex = 0;
                UpdateAtlas(receivedTexture, faceId, out cellIndex);
                SendVFXEvent(cellIndex, pos);

                faceId++;

                Destroy(receivedTexture);
            }
            else
            {
                SendVFXEvent(0, pos);
                Debug.Log("failed to receive facetexture");
            }
            cooldown = 0f;
        }
    }

    void CreateAtlas()
    {
        int atlasWidth = (int)faceAtlasConfig.x * (int)faceAtlasConfig.z;
        int atlasHeight = (int)faceAtlasConfig.y * (int)faceAtlasConfig.w;

        atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);
        // Initialize with transparent pixels
        Color32[] fillColorArray = atlasTexture.GetPixels32();

        for (int i = 0; i < fillColorArray.Length; i++)
            fillColorArray[i] = new Color32(0, 0, 0, 0);

        atlasTexture.SetPixels32(fillColorArray);
        atlasTexture.Apply();

        // debug: assign to material or shader that will use the atlas
        if(debugRenderer) debugRenderer.material.mainTexture = atlasTexture;
    }

    Texture2D SendFrameRequest()
    {
        try
        {
            using (TcpClient client = new TcpClient(pythonServerIp, pythonPort))
            using (NetworkStream stream = client.GetStream())
            {
                // Simple request - can be "FRAME", or protocol as you want
                string request = "FRAME";
                byte[] requestBytes = Encoding.UTF8.GetBytes(request);
                stream.Write(requestBytes, 0, requestBytes.Length);

                // Read frame data back (e.g., as PNG bytes)
                byte[] lenBuf = new byte[4];
                stream.Read(lenBuf, 0, 4);
                if (System.BitConverter.IsLittleEndian)
                    System.Array.Reverse(lenBuf);
                int imgLength = System.BitConverter.ToInt32(lenBuf, 0);
                byte[] imgBytes = new byte[imgLength];
                int read = 0;
                while (read < imgLength)
                    read += stream.Read(imgBytes, read, imgLength - read);

                // Load the image bytes into a Texture2D
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imgBytes))
                {
                    Debug.Log("Received frame from Python!");
                    tex.Apply();
                    return tex;
                }
                else
                {
                    Debug.LogError("Failed to load image bytes into texture.");
                    return null;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"FrameRequest error: {ex}");
            return null;
        }
    }

    void OnGUI()
    {
        if (!showDebugMenu) return;

        GUILayout.BeginArea(new Rect(20, 20, 300, 150), GUI.skin.window);
        GUILayout.Label("Debug Menu");

        GUILayout.Label("Python Server IP:");
        ipInput = GUILayout.TextField(ipInput);

        GUILayout.Space(10);
        if (GUILayout.Button("Save"))
        {
            pythonServerIp = ipInput;
            PlayerPrefs.SetString("PythonServerIP", pythonServerIp);
            PlayerPrefs.Save();
            Debug.Log("PythonServerIP saved: " + pythonServerIp);
        }

        if (GUILayout.Button("Close"))
        {
            showDebugMenu = false;
        }
        GUILayout.EndArea();
    }

    // Updates the atlas texture by copying newTexture pixels into atlas cell at 'index'
    void UpdateAtlas(Texture2D newTexture, int index, out int cellIndex)
    {
        int totalCells = (int)(faceAtlasConfig.y * faceAtlasConfig.x);
        cellIndex = index % totalCells;  // wrap index if needed

        int xCell = cellIndex % (int)faceAtlasConfig.x;
        int yCell = cellIndex / (int)faceAtlasConfig.x;
        cellX = xCell;
        cellY = yCell;

        int xPos = xCell * (int)faceAtlasConfig.z;
        int yPos = yCell * (int)faceAtlasConfig.w;

        // resize and Safety size check
        Texture2D resizedTex;
        if (newTexture.width != faceAtlasConfig.z || newTexture.height != faceAtlasConfig.w)
        {
            //Debug.LogWarning("New texture size does not match atlas cell size.");
            // resize
            resizedTex = ResizeTexture(newTexture, (int)faceAtlasConfig.z, (int)faceAtlasConfig.w);
        }
        else
        {
            resizedTex = newTexture;
        }

        // Copy pixels from newTexture into atlas at proper location
        Color[] pixels = resizedTex.GetPixels();
        atlasTexture.SetPixels(xPos, yPos, (int)faceAtlasConfig.z, (int)faceAtlasConfig.w, pixels);
        atlasTexture.Apply();

        index = cellIndex;

        Debug.Log($"Updated atlas cell {cellIndex} at position ({xPos},{yPos})");
    }

    // Helper to resize a Texture2D to target width and height using GPU
    Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;

        // Blit the source texture onto the RenderTexture (scales it)
        Graphics.Blit(source, rt);

        // Create a new Texture2D with scaled dimensions
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);

        // Read the RenderTexture pixels into the new texture
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    // Sends VFX event to notify particle system of the new atlas cell to use and trigger spawning
    void SendVFXEvent(int atlasCellIndex, Vector3 pos)
    {
        if (vfx != null)
        {
            vfx.SetInt("CellRow", (int)faceAtlasConfig.x);
            vfx.SetInt("CellColume", (int)faceAtlasConfig.y);
            vfx.SetInt("CellX", cellX);
            vfx.SetInt("CellY", cellY);
            vfx.SetVector3("SpawnPos", pos);
            vfx.SetTexture("FaceAtlas", atlasTexture);
            vfx.SendEvent("Born");
            Debug.Log($"Sent VFX event for AtlasCellIndex {atlasCellIndex}");
        }
        else
        {
            Debug.LogWarning("VisualEffect component is null. Cannot send VFX event.");
        }
    }

}
