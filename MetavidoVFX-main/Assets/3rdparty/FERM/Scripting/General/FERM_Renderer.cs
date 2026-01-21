using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

[ExecuteInEditMode]
public class FERM_Renderer : MonoBehaviour {
    //camera/rendering references
    private Dictionary<Camera, CommandBuffer> cameras_ = new Dictionary<Camera, CommandBuffer>();
    private Mesh fullScreenQuad;
    private const CameraEvent raymarchRenderPass = CameraEvent.AfterHaloAndLensFlares;

    //shader/material refrences
    public FERM_Shader shader;
    private FERM_Caster caster;
    public Material material;
    public GameObject selectionRef;

    //player accessible options
    public bool autoCompile = true;
    public bool showTips = true;
	
    public Mode renderingMode;
    public enum Mode {
        Normal, Depthless, Skybox, Equirect360
    }

    public SuperSampling superSampling;
	public enum SuperSampling{
		None, x2, x4, x9, x16
	}


    /*
    case RenderPriority.Skybox: return CameraEvent.BeforeForwardOpaque;
    case RenderPriority.CrystalBall: return CameraEvent.BeforeSkybox;
    case RenderPriority.OverOpaque: return CameraEvent.BeforeImageEffectsOpaque;
    case RenderPriority.OverEverything: return CameraEvent.raymarchRenderPass;
    */

    private Mesh GenerateFullScreenQuad() {
        var mesh = new Mesh();

        mesh.vertices = new Vector3[4] {
            new Vector3( 1f,  1f,  0f),
            new Vector3(-1f,  1f,  0f),
            new Vector3(-1f, -1f,  0f),
            new Vector3( 1f, -1f,  0f),
        };

        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    [ContextMenu("CleanUp")]
    public void CleanUpCameraSettings() {
        foreach (var pair in cameras_) {
            var camera = pair.Key;
            var buffer = pair.Value;
            if (camera) {
                camera.RemoveCommandBuffer(raymarchRenderPass, buffer);
            }
        }
        cameras_.Clear();
    }

    private void OnEnable() {
        CleanUpCameraSettings();
    }

    private void OnDisable() {
        CleanUpCameraSettings();
    }

    private void Awake() {
        CheckForShaderChanges();
    }

    private void Update() {
        UpdateTransform();
        UpdateCameraSetup();
        if(autoCompile)
            CheckForShaderChanges();
    }

    private void UpdateCameraSetup() {
        if(!gameObject.activeInHierarchy || !enabled || shader == null) {
            CleanUpCameraSettings();
            return;
        }

        foreach(var camera in Camera.allCameras)
            UpdateCommandBuffer(camera);

#if UNITY_EDITOR
        foreach (SceneView view in SceneView.sceneViews) {
            if (view != null) {
                UpdateCommandBuffer(view.camera);
            }
        }
#endif
    }

    private void UpdateCommandBuffer(Camera camera) {
        if (!camera || cameras_.ContainsKey(camera) || shader.material == null) return;

        if (!fullScreenQuad)
            fullScreenQuad = GenerateFullScreenQuad();

        var buffer = new CommandBuffer();
        buffer.name = "Raymarching";
        buffer.DrawMesh(fullScreenQuad, Matrix4x4.identity, shader.material, 0, 0);
        camera.AddCommandBuffer(raymarchRenderPass, buffer);
        cameras_.Add(camera, buffer);
    }

    /// <summary>
    /// Generates a new shader based on the RaymarchComponent structure.
    /// This is only possible in the editor!
    /// </summary>
    [ContextMenu("RebuildShader")]
    public void BuildShader() {
#if UNITY_EDITOR
        this.shader.Generate(this.transform, renderingMode, superSampling);
        UpdateKick();
        CleanUpCameraSettings();
        caster = new FERM_Caster(this, shader.GetBaseComponents(this.transform));
#endif
    }

    /// <summary>
    /// Generate a new shader, similar to Build, but only
    /// if there have been changes since the last check.
    /// </summary>
    private void CheckForShaderChanges() {
#if UNITY_EDITOR
        if(this.shader.CheckForStructureChanges(this.transform))
            BuildShader();
#endif
    }

    /// <summary>
    /// Force the Renderer to update and redraw the scene.
    /// This sometimes needs to be done in the editor after 
    /// rebuilding the scene or if the editor has lost focus 
    /// for a while.
    /// </summary>
    public void UpdateKick() {
#if UNITY_EDITOR
        selectionRef = UnityEditor.Selection.activeGameObject;
        UnityEditor.Selection.activeGameObject = gameObject;
#endif
    }

    private FERM_Parameter[] GetParameters() {
        return null;
    }

    /// <summary>
    /// Apply the renderer's own transform to the Raymarch shader.
    /// Note that this own transform is handeld seperately, it changes 
    /// the parameters of the virtual camera that is used to render,
    /// in doing so the operations are more stable at enormous positions
    /// and scales.
    /// This function will also equalize the scale of this transform.
    /// </summary>
    private void UpdateTransform() {
        Material mat = shader.material;
        if(mat == null)
            return;

        mat.SetVector("_t_position", transform.position);
        mat.SetVector("_t_rotation", FERM_Util.ToVector(transform.rotation));

        Vector3 scale = transform.localScale;
        scale = FERM_Util.EqualizeScale(scale);
        transform.localScale = scale;
        mat.SetFloat("_t_scale", scale.x);

        if(caster != null)
            caster.UpdateTransform(transform.position, transform.rotation, scale.x);
    }

    public void ChangeTrigger() {
#if UNITY_EDITOR
        if(autoCompile && shader.CheckForStructureChanges(transform)) 
            BuildShader();
#endif
    }

    public void SetShader(FERM_Shader shader) {
        this.shader = shader;
        this.material = shader == null ? null : shader.material;
    }

    /// <summary>
    /// Returns object that can probe raymarching distance function of this renderer.
    /// Note that if the renderer recompiles, old casters will no longer be valid.
    /// Changes of parameters and animations on the other hand are taken into account 
    /// correctly and do not require a new caster.
    /// </summary>
    public FERM_Caster GetCaster() {
        if(caster == null)
            caster = new FERM_Caster(this, shader.GetBaseComponents(this.transform));
        return caster; 
    }
}
