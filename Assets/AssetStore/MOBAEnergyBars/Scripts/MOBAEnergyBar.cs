#define USE_LERP_DAMAGE
#define USE_ADD_RENDER_QUEUE

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.Collections;

[ExecuteInEditMode]
[RequireComponent (typeof(Image))]
public class MOBAEnergyBar : MonoBehaviour {
    public Texture CellTexture;
    public Texture BurnTexture;
    public Color FullColour = new Color(0.44f, 0.96f, 0.44f);
    public Color EmptyColour = Color.clear;
    public Color SmallGapColour = new Color(0.2f, 0.29f, 0.19f);
    public Color BigGapColour = Color.black;
    public Color BurnColour = new Color(1.0f, 0.43f, 0.43f);

    public float MaxValue = 1000f;
    public float Value = 1000f;
    public float BurnRate = 0.1f;

    public float SmallGapInterval = 100f;
    public float BigGapInterval = 5f;
    [Range(0f, 0.1f)]
    public float SmallGapSize = 0.025f;
    [Range(0f, 0.1f)]
    public float BigGapSize = 0.05f;

    public bool Flip = false;

    public Shader ___AHBShader;

    Image img;
    Material m;
    float lastValue;
    float damage;

    Texture oldCellTexture;
    Texture oldBurnTexture;
    Color oldFullColour;
    Color oldEmptyColour;
    Color oldSmallGapColour;
    Color oldBigGapColour;
    Color oldBurnColour;

    float oldSmallGapInterval;
    float oldBigGapInterval;
    float oldSmallGapSize;
    float oldBigGapSize;

    float oldMaxValue;
    float oldValue;
    float oldDamage;

    bool oldFlipValue;

    public void SetValueNoBurn(float value)
    {
        Value = value;
        lastValue = value;
    }

    void Start()
    {
        setProperties();

        damage = 0.0f;
        lastValue = Value;
    }

    void Update () {
        if (Value > MaxValue)
        {
            SetValueNoBurn(MaxValue);
        }
#if USE_LERP_DAMAGE
#else
		damage = Mathf.Max(0, damage - BurnRate * MaxValue * Time.deltaTime);
#endif
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
			if (lastValue > Value)
			{
				damage += lastValue - Value;
				damage = Mathf.Clamp(damage, 0f, MaxValue - Value);
				lastValue = Value;
			}
			else if (lastValue < Value)
			{
				lastValue = Value;
			}
        }
        else if (!EditorApplication.isPlaying)
        {
            damage = 0;
        }
#else
        if (lastValue > Value)
        {
            damage += lastValue - Value;
            damage = Mathf.Clamp(damage, 0f, MaxValue - Value);
            lastValue = Value;
        }
		else if (lastValue < Value)
		{
			lastValue = Value;
		}
#endif
		updatePropertiesIfChanged();
    }

#if USE_LERP_DAMAGE
	public bool UpdateLerpDamage()
	{
		damage = Mathf.Lerp(damage, 0.0f, Time.deltaTime * 4.0f);

		if (Mathf.Abs(damage) < MaxValue * 0.01f)
		{
			damage = 0.0f;
			return true;
		}
		return false;
	}
#endif

	void OnValidate()
    {
        if (enabled)
            setProperties();
    }
    
    bool isDirty()
    {
        if (img == null || img.material != m || m == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void fix()
    {
        img = GetComponent<Image>();
        m = new Material(___AHBShader);
#if USE_ADD_RENDER_QUEUE
		m.renderQueue += 10;
#endif
		m.SetFloat("_AAF", 1.0f);
        img.material = m;
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			m.shader = Shader.Find(m.shader.name);
#endif
	}

	void setProperties()
    {
        if (isDirty()) fix();
        m.SetColor("_FullColour", FullColour);          oldFullColour = FullColour;
        m.SetColor("_EmptyColour", EmptyColour);        oldEmptyColour = EmptyColour;
        m.SetColor("_GapColour", SmallGapColour);       oldSmallGapColour = SmallGapColour;
        m.SetColor("_BigGapColour", BigGapColour);      oldBigGapColour = BigGapColour;
        m.SetColor("_DamageColour", BurnColour);        oldBurnColour = BurnColour;
        m.SetTexture("_CellTex", CellTexture);          oldCellTexture = CellTexture;
        m.SetTexture("_BurnTex", BurnTexture);          oldBurnTexture = BurnTexture;
        m.SetFloat("_GapInterval", SmallGapInterval);   oldSmallGapInterval = SmallGapInterval;
        m.SetFloat("_GapSize", SmallGapSize);           oldSmallGapSize = SmallGapSize;
        m.SetFloat("_BigGapInterval", BigGapInterval);  oldBigGapInterval = BigGapInterval;
        m.SetFloat("_BigGapSize", BigGapSize);          oldBigGapSize = BigGapSize;
        m.SetFloat("_Value", Value);                    oldValue = Value;
        m.SetFloat("_MaxValue", MaxValue);              oldMaxValue = MaxValue;
        m.SetFloat("_DamageValue", damage);             oldDamage = damage;
        m.SetFloat("_Flip", Flip ? 1f : 0f);            oldFlipValue = Flip;
        img.SetAllDirty();
    }

    void updatePropertiesIfChanged()
    {
        if (isDirty()) fix();

        if (FullColour != oldFullColour) {
            m.SetColor("_FullColour", FullColour);
            oldFullColour = FullColour;
        }
        if (EmptyColour != oldEmptyColour) {
            m.SetColor("_EmptyColour", EmptyColour);
            oldEmptyColour = EmptyColour;
        }
        if (SmallGapColour != oldSmallGapColour) {
            m.SetColor("_GapColour", SmallGapColour);
            oldSmallGapColour = SmallGapColour;
        }
        if (BigGapColour != oldBigGapColour) {
            m.SetColor("_BigGapColour", BigGapColour);
            oldBigGapColour = BigGapColour;
        }
        if (BurnColour != oldBurnColour) {
            m.SetColor("_DamageColour", BurnColour);
            oldBurnColour = BurnColour;
        }
        if (CellTexture != oldCellTexture) {
            m.SetTexture("_CellTex", CellTexture);
            oldCellTexture = CellTexture;
            img.SetAllDirty();
        }
        if (BurnTexture != oldBurnTexture) {
            m.SetTexture("_BurnTex", BurnTexture);
            oldBurnTexture = BurnTexture;
            img.SetAllDirty();
        }
        if (SmallGapInterval != oldSmallGapInterval) {
            m.SetFloat("_GapInterval", SmallGapInterval);
            oldSmallGapInterval = SmallGapInterval;
        }
        if (SmallGapSize != oldSmallGapSize) {
            m.SetFloat("_GapSize", SmallGapSize);
            oldSmallGapSize = SmallGapSize;
        }
        if (BigGapInterval != oldBigGapInterval) {
            m.SetFloat("_BigGapInterval", BigGapInterval);
            oldBigGapInterval = BigGapInterval;
        }
        if (BigGapSize != oldBigGapSize) {
            m.SetFloat("_BigGapSize", BigGapSize);
            oldBigGapSize = BigGapSize;
        }
        if (oldValue != Value) {
            m.SetFloat("_Value", Value);
            oldValue = Value;
        }
        if (oldMaxValue != MaxValue) {
            m.SetFloat("_MaxValue", MaxValue);
            oldMaxValue = Value;
        }
        if (oldDamage != damage) {
            m.SetFloat("_DamageValue", damage);
            oldDamage = damage;
        }
        if (oldFlipValue != Flip) {
            m.SetFloat("_Flip", Flip ? 1f : 0f);
            oldFlipValue = Flip;
        }
    }
}
