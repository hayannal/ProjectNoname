This package comes with custom made floating health panels so you can get started straight away. They are intended to follow your gameobjects on screen and work in world space or screen space.

** Quickstart Worldspace ********************************************************************************************************

1. Under MOBAEnergyBars/Prefabs/WorldSpace drag a prefab from one of the folders onto a gameobject in the active scene.
2. Move the prefab instance in the scene to the desired offset.

That's it! When you hit play the health panel will follow your gameobject from the chosen offset and automatically turn to face the camera. To control the health bar in code from the parent gameobject you will need something like this:

void Start()
{
    healthBar = GetComponentInChildren<MOBAHealthBarPanel>().HealthBar;
    healthBar.MaxValue = 100f;
    healthBar.SetValueNoBurn(100f); // Won't show burn animation
    // healthBar.Value = 100f;      // Will show burn animation when decreasing value
}

void Update()
{
    healthBar.Value = MyHealth;	
}

** Quickstart Screenspace *********************************************************************************************************

1. Under MOBAEnergyBars/Prefabs/ScreenSpace drag the ScreenSpacePanel prefab onto a gameobject in the active scene.
2. Move the ScreenSpacePanel instance to the desired offset.
3. In the inspector link the instances 'UI Element Prefab' to any prefab in EvilPanel, MobaStylePanel or SimplePanel under the ScreenSpace directory.
4. Optionally link the 'Show On Canvas' property in the inspector to an existing canvas in the active scene. If you leave this blank it will create its own canvas.

That's it! Hit play and you should see a floating health panel over your gameobject. These panels are projected onto screenspace and they will stay a fixed size. The ScreenSpacePanel component is responsible for moving the UI elements to the correct position on screen. It will disable them when they are offscreen to avoid the overhead of moving them on the canvas.

To control the health bar from the parent gameobject you will need code like this:

// Getting the health bar must happen in Start, because ScreenSpacePanel instantiates it in Awake
void Start()
{
    ScreenSpacePanel ssp = GetComponentInChildren<ScreenSpacePanel>();
    healthBar = ssp.PanelUIElement.GetComponentInChildren<MOBAHealthBarPanel>().HealthBar;
    healthBar.MaxValue = 100f;
    healthBar.SetValueNoBurn(100f); // Won't show burn animation
    // healthBar.Value = 100f;      // Will show burn animation when decreasing value
}

void Update()
{
    healthBar.Value = MyHealth; 
}

** MOBA Energy Bar Tool *************************************************************************************************************

You can add MOBA energy bars into your own UI elements and design them in the editor. To create one right-click in the scene hierarchy and click UI/MOBA Energy Bar. This will need to be created under a canvas to display. The parameters are described below:

Cell Texture - Texture applied to each cell (between gaps) in the energy bar.
Burn Texture - Texture applied to the burn portion of the energy bar.
Full Colour - Colour of energy bar. Final Colour is Texture Colour * Full Colour.
Empty Colour - Background colour of energy bar.
Small Gap Colour - Colour of most frequent gaps.
Big Gap Colour - Colour of least frequent gaps.
Burn Colour - Colour of burn animation.
Max Value - Maximum value of energy bar.
Value - Current value of energy bar.
Burn Rate - Speed of burn animation, specified in bar lengths per second.
Small Gap Intervals - Amount of bar value between each small gap.
Big Gap Intervals - Every n small gaps is a big gap.
Small Gap Size - Apparent size of small gaps. Adapts to distance from camera so it stays relatively the same size on screen.
Big Gap Size - Apparent size of big gaps. Same as above, it adapts to distance from camera and will stay relatively the same size.
Flip - When true the energy bars will deplete from left to right instead of right to left.
__AHB Shader - Better to leave this alone :)

** Note for Cameras updating on LateUpdate *******************************************************************************************

By default the MOBA health bar panels update their state in LateUpdate, so if you're updating your camera in LateUpdate as well this can make the panels appear to lag behind the object they're following. If this is the case then simply add the UpdateMOBAPanelsOnPreRender script to your camera object. The panels will automatically register with this component and be updated in OnPreRender instead of LateUpdate.