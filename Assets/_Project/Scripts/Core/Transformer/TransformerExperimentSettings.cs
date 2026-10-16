using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    [CreateAssetMenu(menuName="Embodied Physics Lab/Transformer Experiment Settings")]
    public sealed class TransformerExperimentSettings : ScriptableObject
    {
        public float[] inputVoltageOptions={6f,9f,12f};
        public int[] turnsOptions={100,200,300,400};
        public int defaultInputIndex=1,defaultPrimaryTurnsIndex=1,defaultSecondaryTurnsIndex=1;
        [Min(1)] public int minimumDataGroups=4;
        [Min(.01f)] public float displaySmoothing=.16f;
        [Min(0f)] public float leakageCoefficient=0f;
        [Min(0f)] public float windingResistance=0f;
        [Min(0f)] public float loadResistance=0f;
        public int InputCount=>inputVoltageOptions==null?0:inputVoltageOptions.Length;
        public int TurnsCount=>turnsOptions==null?0:turnsOptions.Length;
        public float InputAt(int index)=>inputVoltageOptions[Mathf.Clamp(index,0,InputCount-1)];
        public int TurnsAt(int index)=>turnsOptions[Mathf.Clamp(index,0,TurnsCount-1)];
    }
}
