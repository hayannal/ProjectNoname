Shader "FrameworkPV/OutlineStencil"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+50" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
			Cull Back
			ZTest Always
			ZWrite Off
			ColorMask 0

			Stencil
			{
				Ref 1
				Pass Replace
			}
        }
    }
}
