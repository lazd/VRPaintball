Shader "Decal/Eraser/Write"
{
	Properties
	{
		_Occlusion("Occlusion", 2D) = "white" {}

		_Multiplier("Alpha Multiplier", Range(0.0, 1.0)) = 0.5
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
	}
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" }

		ZWrite Off
		ZTest Always
		Cull Front
		
		GrabPass
		{
			"_Eraser"
		}
	}
}