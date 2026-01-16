using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(HU_Star))]
public class StarEditor : Editor
{
    string path = "Assets/";

    bool materialsFold = true;
    bool starFold = true;
    bool coronaFold = true;
    bool prominenceFold = true;
    bool shadersFold = false;
    bool spritesFold = true;
    bool particlesFold = true;
    bool flareFold = true;
    bool jetsFold = true;
    bool accretionFold = true;
    bool debug = false;
    public override void OnInspectorGUI()
    {
        

        HU_Star star = (HU_Star)target;
        debug= GUILayout.Toggle(debug,"Show debug values");
        if(debug) DrawDefaultInspector();
        if (GUILayout.Button("Force Update")) star.ForceUpdate();
        materialsFold = EditorGUILayout.Foldout(materialsFold, "Materials", true);
        if (materialsFold)
        {
            star._starMaterial = EditorGUILayout.ObjectField(" Star Material:", star._starMaterial, typeof(Material), true) as Material;
            star._coronaMateral = EditorGUILayout.ObjectField(" Corona Material:", star._coronaMateral, typeof(Material), true) as Material;
            if (GUILayout.Button("Make Unique"))
            {
                path = EditorUtility.SaveFilePanel("Choose folder to save materials", path, "Star Name", "mat");
                if (path.Length != 0)
                {
                    if (path.Substring(path.Length - 4) == ".mat")
                    {
                        path = path.Remove(path.Length - 4);
                    }
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.CreateAsset(new Material(star._starMaterial), path + "_Star.mat");
                    AssetDatabase.CreateAsset(new Material(star._coronaMateral), path + "_Corona.mat");
//                    AssetDatabase.CreateAsset(new Material(star._particleCoronaMaterial), path + "_CoronaParticle.mat");

                    star._starMaterial = AssetDatabase.LoadAssetAtPath<Material>(path + "_Star.mat");
                    star._coronaMateral = AssetDatabase.LoadAssetAtPath<Material>(path + "_Corona.mat");
//                    star._particleCoronaMaterial = AssetDatabase.LoadAssetAtPath<Material>(path + "_CoronaParticle.mat");
                    
                }
            }
        }
        starFold = EditorGUILayout.Foldout(starFold, "Star", true);
        if (starFold)
        {
            star._size = EditorGUILayout.Slider(" Size:", star._size, 0, 5);
            star._singleColor = EditorGUILayout.Toggle(" Single Color:", star._singleColor);
            star._color = EditorGUILayout.ColorField(new GUIContent(" Color:"), star._color, false, false, true, new ColorPickerHDRConfig(0, 5, 0, 5));
            if (!star._singleColor)
            {
                star._color2 = EditorGUILayout.ColorField(new GUIContent(" Color:"), star._color2, false, false, true, new ColorPickerHDRConfig(0, 5, 0, 5));
            }
            star._rim = EditorGUILayout.Slider(" Rim:", star._rim, 0, 1);
            star._flowSpeed = EditorGUILayout.Slider(" Flow Speed:", star._flowSpeed, 0, 1);
            star._spots = EditorGUILayout.Slider(" Spots:", star._spots, 0, 1);
        }
        coronaFold = EditorGUILayout.Foldout(coronaFold, "Corona", true);
        if (coronaFold)
        {
            star._coronaBrightness = EditorGUILayout.Slider(" Brightness:", star._coronaBrightness, 0.5f, 5);
            star._coronaSpeed = EditorGUILayout.Slider(" Speed:", star._coronaSpeed, 0, 1);

            flareFold = EditorGUILayout.Foldout(flareFold, " Flare");
            if (flareFold)
            {
                star._flare = EditorGUILayout.Toggle(" Enable:", star._flare);
                if (star._flare)
                {
                    star._flareSize = EditorGUILayout.Slider(" Size:", star._flareSize, 0, 1);
                    star._flareContrast = EditorGUILayout.Slider(" Contrast:", star._flareContrast, 0, 1);

                }
            }

                spritesFold = EditorGUILayout.Foldout(spritesFold, " Sprites");
            if (spritesFold)
            {
                star._coronaSprites = EditorGUILayout.Toggle("  Enable:", star._coronaSprites);
                if (star._coronaSprites)
                {
                    star._simpleCoronaSprites = EditorGUILayout.Toggle("  Simple:", star._simpleCoronaSprites);
                    star._coronaDensity = Mathf.RoundToInt(EditorGUILayout.Slider("  Density:", (float)star._coronaDensity / 50f, 0, 1) * 50);
                    star._coronaSize = EditorGUILayout.Slider("  Size:", star._coronaSize, 0, 1);
                    star._coronaWaves = EditorGUILayout.Slider("  Waves:", star._coronaWaves, 0, 1);
                    star._coronaRipple = EditorGUILayout.Slider("  Ripple:", star._coronaRipple, 0, 1);

                }
            }


            {
                particlesFold = EditorGUILayout.Foldout(particlesFold, " Particles");
                if (particlesFold)
                {
                    star._coronaParticles = EditorGUILayout.Toggle("  Enable:", star._coronaParticles);
                    if (star._coronaParticles)
                    {
                        if (GUILayout.Button("Select Emiter")) Selection.activeGameObject = star.coronaParticlesComp.gameObject;
                        star._coronaTrails = EditorGUILayout.Toggle("  Trails:", star._coronaTrails);
                        if (star._coronaTrails)
                            star._trailsResolution = EditorGUILayout.Slider("   Trails Resolution:", star._trailsResolution, 0, 1);
                        star._coronaParticlesSize = EditorGUILayout.Slider("  Size:", star._coronaParticlesSize, 0, 1);
                        star._particleDensity = EditorGUILayout.Slider("  Density:", star._particleDensity, 0, 1);
                        star._particleSpread = EditorGUILayout.Slider("  Spread:", star._particleSpread, 0, 1);
                        star._particleWavesAmplitude = EditorGUILayout.Slider("  Waves Amplitude:", star._particleWavesAmplitude, 0, 1);
                        star._particleWavesFreq = EditorGUILayout.Slider("  Waves Frequency", star._particleWavesFreq, 0, 1);
                    }
                    
                }
                
            }
            prominenceFold = EditorGUILayout.Foldout(prominenceFold, " Prominence", true);
            if (prominenceFold)
            {
                star.prominenceAmount = EditorGUILayout.Slider("  Amount:", star.prominenceAmount, 0, 1);
                star.prominenceSize = EditorGUILayout.Slider("  Size:", star.prominenceSize, 0, 1);
            }



        }
        jetsFold = EditorGUILayout.Foldout(jetsFold, "Jets", true);
        {
            if (jetsFold)
            {
                star._jets = EditorGUILayout.Toggle(" Enable:", star._jets);
                if (star._jets)
                {
                    star._jetDensity = EditorGUILayout.IntSlider(" Density:", star._jetDensity, 0, 20);
                    star._jetHeight = EditorGUILayout.Slider(" Height:", star._jetHeight, 0, 30);
                    star._jetWidth = EditorGUILayout.Slider(" Width:", star._jetWidth, 0, 1);
                    star._jetSmear = EditorGUILayout.Slider(" Smear:", star._jetSmear, 0, 1);
                    star._jetSpeed = EditorGUILayout.Slider(" Speed:", star._jetSpeed, 0, 1);
                    star._jetSpread = EditorGUILayout.Slider(" Spread:", star._jetSpread, 0, 1);
                }
            }
        }
        //accretionFold = EditorGUILayout.Foldout(accretionFold, "Accretion Disk", true);
        //if (accretionFold)
        //{
        //    star._accretionDisk = EditorGUILayout.Toggle(" Enable:", star._accretionDisk);
        //    if (star._accretionDisk)
        //    {
        //        star._accretionColor1 = EditorGUILayout.ColorField(new GUIContent(" Color:"), star._accretionColor1, false, false, true, new ColorPickerHDRConfig(0, 5, 0, 5));
        //        star._accretionColor2 = EditorGUILayout.ColorField(new GUIContent(" Color 2:"), star._accretionColor2, false, false, true, new ColorPickerHDRConfig(0, 5, 0, 5));
        //        star._accretionSegments = EditorGUILayout.IntSlider(" Segments", star._accretionSegments, 1, 50);
        //        star._accretionSize = EditorGUILayout.Slider(" Size:", star._accretionSize, 0, 1);
        //        star._accretionInnerRadius = EditorGUILayout.Slider(" Inner Radius:", star._accretionInnerRadius, 0, 1);
        //        star._accretionSpeed = EditorGUILayout.Slider(" Speed:", star._accretionSpeed, 0, 1);
        //        star._accretionTwist = EditorGUILayout.Slider(" Twist:", star._accretionTwist, 0, 1);
        //    }
        //}

        shadersFold = EditorGUILayout.Foldout(prominenceFold, "Shaders", true);
        if (shadersFold)
        {
            star.starShader = EditorGUILayout.ObjectField(" Star shader:", star.starShader, typeof(Shader),true) as Shader;
            star.coronaShader = EditorGUILayout.ObjectField(" Corona Segments shader:", star.coronaShader, typeof(Shader), true) as Shader;
            star.simpleCoronaShader = EditorGUILayout.ObjectField(" Corona Sprite shader:", star.simpleCoronaShader, typeof(Shader), true) as Shader;
            star.flareShader = EditorGUILayout.ObjectField(" Flare Shader:",star.flareShader, typeof(Shader), true) as Shader;
        }
       // DrawDefaultInspector();
    }
    
}

