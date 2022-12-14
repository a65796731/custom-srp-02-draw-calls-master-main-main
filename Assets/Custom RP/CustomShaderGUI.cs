using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{

    MaterialEditor editor;
    Object[] materials;
    MaterialProperty[] properties;
    bool ShowPreset;
    RenderQueue RenderQueue{
      set {
            foreach(Material m in materials)
            {
                m.renderQueue = (int)value; 
            }
        }
    }
    bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    bool PremultuplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }
    BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend",  (float)value);
    }
    BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }
    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }
    bool HasProperty(string name) => FindProperty(name, properties, false) != null;

    bool HasPremultiplyAlpha() => HasProperty("_PremulAlpha");
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
    
        editor =materialEditor;
        materials = editor.targets;
        this.properties = properties;
        EditorGUILayout.Space();
        ShowPreset = EditorGUILayout.Foldout(ShowPreset,"Presets", true);
        if (ShowPreset)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }
    
    }
    private void SetProperty(string name,string keyword,bool vlaue)
    {
      if(  SetProperty(name, vlaue ? 1.0f : 0.0f))
        SetKeyWord(keyword,vlaue);
    }
    bool  SetProperty(string name,float value )
    {
        var findProperty = FindProperty(name, properties,false);
        
        if (findProperty != null)
        {
            findProperty.floatValue = value;
           return true;
        }
        return false;
    }
    private void SetKeyWord(string keyword,bool enabled)
    {
        if(enabled)
        {
            foreach(Material m in materials)
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material m in materials)
            {
                m.DisableKeyword(keyword);
            };
        }
    }
    bool PresetButton(string name)
    {
        if(GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
    void OpaquePreset()
    {
        if(PresetButton("Qpaque"))
        {
            Clipping = false;
            PremultuplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultuplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }
    void  FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultuplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    void TransparentPreset()
    {
        if (HasPremultiplyAlpha()&&PresetButton("Transparent"))
        {
            Clipping = false;
            PremultuplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
}
