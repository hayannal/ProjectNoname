using UnityEngine;


public class FPD_OverridableFloatAttribute : PropertyAttribute
{
    public string BoolVarName;
    public string TargetVarName;

    public FPD_OverridableFloatAttribute(string boolVariableName, string targetVariableName)
    {
        BoolVarName = boolVariableName;
        TargetVarName = targetVariableName;
    }
}


// -------------------------- Next F Property Drawer -------------------------- \\


public class BackgroundColorAttribute : PropertyAttribute
{
    public float r;
    public float g;
    public float b;
    public float a;

    public BackgroundColorAttribute()
    {
        r = g = b = a = 1f;
    }

    public BackgroundColorAttribute(float aR, float aG, float aB, float aA)
    {
        r = aR;
        g = aG;
        b = aB;
        a = aA;
    }

    public Color Color { get { return new Color(r, g, b, a); } }
}


// -------------------------- Next F Property Drawer -------------------------- \\

public class FPD_WidthAttribute : PropertyAttribute
{
    public float LabelWidth;

    public FPD_WidthAttribute(int labelWidth)
    {
        LabelWidth = labelWidth;
    }
}

