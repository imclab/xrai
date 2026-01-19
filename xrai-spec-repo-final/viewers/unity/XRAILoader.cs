using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class XRAINode {
    public string id;
    public float[] transform;
    public XRAIComponents components;
}

[System.Serializable]
public class XRAIComponents {
    public XRAIGeometry geometry;
    public XRAIAgent aiAgent;
    public XRAIAudio audio;
}

[System.Serializable]
public class XRAIGeometry {
    public string refId;
    public string type;
}

[System.Serializable]
public class XRAIAgent {
    public string model;
    public string prompt;
    public string memoryRef;
}

[System.Serializable]
public class XRAIAudio {
    public string refId;
    public string mode;
}

[System.Serializable]
public class XRAIScene {
    public List<XRAINode> nodes;
}

[System.Serializable]
public class XRAIData {
    public string format;
    public string version;
    public XRAIScene scene;
}

public class XRAILoader : MonoBehaviour {
    public string jsonFilePath = "Assets/sample.xrai.json";

    void Start() {
        string json = File.ReadAllText(jsonFilePath);
        XRAIData data = JsonUtility.FromJson<XRAIData>(json);
        foreach (var node in data.scene.nodes) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = node.id;
            go.transform.position = new Vector3(node.transform[0], node.transform[1], node.transform[2]);
            Debug.Log($"Loaded XRAI node: {node.id}");
        }
    }
}