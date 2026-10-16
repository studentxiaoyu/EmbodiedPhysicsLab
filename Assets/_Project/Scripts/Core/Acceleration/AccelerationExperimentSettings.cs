using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    [CreateAssetMenu(menuName="Embodied Physics Lab/Acceleration settings")]
    public sealed class AccelerationExperimentSettings : ScriptableObject
    {
        [Header("Masses (kg)")]
        public double cartBaseMass=.5, cartWeightMass=.1, bucketBaseMass=.05, bucketWeightMass=.05;
        public int maxCartWeights=6, maxBucketWeights=6, defaultCartWeights=0, defaultBucketWeights=0;
        [Header("Environment")]
        public double[] anglesDegrees={0,2,4};
        public double[] frictionCoefficients={0,.02,.05};
        public int defaultAngleIndex=0, defaultFrictionIndex=0;
        public double gravity=9.81, sampleInterval=.02;
        [Header("Apparatus geometry (metres)")]
        public float trackPivotHeight=1.18f, pulleyDistance=3.35f, pulleyHeight=.22f, cartStartDistance=.35f;
        public double maximumCartTravel=2.5, maximumBucketDrop=.8, minimumBucketCenterHeight=.25, bucketSuspensionGap=.26;
        public double TravelLimit(double angle)
        {
            double rad=angle*System.Math.PI/180;
            double bucketStart=trackPivotHeight+pulleyDistance*System.Math.Sin(rad)+pulleyHeight*System.Math.Cos(rad)-bucketSuspensionGap;
            return System.Math.Min(maximumCartTravel,System.Math.Min(maximumBucketDrop,bucketStart-minimumBucketCenterHeight));
        }
    }
}
