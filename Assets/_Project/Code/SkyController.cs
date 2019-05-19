using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

[System.Serializable]
public struct SunStageFloat
{
    public float AtZenith;
    public float AtHorizon;
    public float AtOpposedZenith;

    public float Evaluate(float zenithCorrelation)
    {
        if (zenithCorrelation >= 0f)
        {
            return Mathf.Lerp(AtHorizon, AtZenith, zenithCorrelation);
        }
        else
        {
            return Mathf.Lerp(AtHorizon, AtOpposedZenith, -zenithCorrelation);
        }
    }
}

[System.Serializable]
public struct SunStageColor
{
    public Color AtZenith;
    public Color AtHorizon;
    public Color AtDown;

    public Color Evaluate(float zenithCorrelation)
    {
        if (zenithCorrelation >= 0f)
        {
            return Color.Lerp(AtHorizon, AtZenith, zenithCorrelation);
        }
        else
        {
            return Color.Lerp(AtHorizon, AtDown, -zenithCorrelation);
        }
    }
}

[ExecuteInEditMode]
public class SkyController : MonoBehaviour
{
    [Header("Components")]
    public Volume SkyVolume;
    public Light Sun;

    [Header("Sun")]
    public float SunStrength;
    [Range(0f, 360f)]
    public float SunAngle;
    public Color SunColor;
    public Vector3 SunAxis;

    [Header("Sky")]
    public SunStageColor TopGradient;
    public SunStageColor MiddleGradient;
    public SunStageColor BottomGradient;
    public SunStageFloat Exposure;
    public SunStageFloat Multiplier;

    private Transform _sunTransform;
    private GradientSky _gradientSky;

    void OnEnable()
    {
        _sunTransform = Sun.transform;
        SkyVolume.sharedProfile.TryGet<GradientSky>(out _gradientSky);
    }

    void Update()
    {
        Sun.intensity = SunStrength;
        Sun.color = SunColor;
        _sunTransform.position = Vector3.zero;
        _sunTransform.rotation = Quaternion.AngleAxis(SunAngle, SunAxis.normalized);

        float zenithCorrelation = Vector3.Dot(_sunTransform.forward, Vector3.down);

        _gradientSky.top.Override(TopGradient.Evaluate(zenithCorrelation));
        _gradientSky.middle.Override(MiddleGradient.Evaluate(zenithCorrelation));
        _gradientSky.bottom.Override(BottomGradient.Evaluate(zenithCorrelation));
        _gradientSky.exposure.Override(Exposure.Evaluate(zenithCorrelation));
        _gradientSky.multiplier.Override(Multiplier.Evaluate(zenithCorrelation));
    }
}
