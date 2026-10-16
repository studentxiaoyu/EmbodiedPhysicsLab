using System;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Gas
{
    [CreateAssetMenu(menuName="Embodied Physics Lab/Gas Experiment Settings")]
    public sealed class GasExperimentSettings : ScriptableObject
    {
        public float[] volumeLiters={1f,1.2f,1.5f,1.8f,2f};
        [Min(.000001f)] public float amountOfSubstance=.0001f;
        [Min(0.01f)] public float gasConstant=8.314f;
        [Min(1f)] public float defaultTemperature=300f;
        [Min(1f)] public float targetTemperature=300f;
        [Min(.01f)] public float heatingRate=12f;
        [Min(.01f)] public float coolingRate=12f;
        [Min(.01f)] public float holdConvergenceRate=1.8f;
        [Min(.001f)] public float volumeDisturbanceExponent=.12f;
        [Min(.01f)] public float recordToleranceKelvin=1f;
        [Min(0f)] public float stableDwellSeconds=.7f;
        public int minimumDistinctVolumes=4;
        public float pistonMinY=.30f;
        public float pistonMaxY=1.34f;
        public float cylinderBottomY=.20f;
        public float cylinderRadius=.48f;
        public float CylinderHeight=>pistonMaxY-cylinderBottomY;
        public int VolumeCount=>volumeLiters==null?0:volumeLiters.Length;
        public float VolumeM3(int index)=>volumeLiters[Mathf.Clamp(index,0,VolumeCount-1)]*.001f;
        public double PressurePa(float volumeM3,float temperatureK)=>GasPhysicsModel.Pressure(amountOfSubstance,gasConstant,temperatureK,volumeM3);
    }
}

