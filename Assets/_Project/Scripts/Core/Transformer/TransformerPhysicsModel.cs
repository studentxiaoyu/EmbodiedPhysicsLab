using System;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    /// <summary>Ideal transformer equations, independent of Unity scene and interaction code.</summary>
    public static class TransformerPhysicsModel
    {
        public static double SecondaryVoltage(double inputVoltage,int primaryTurns,int secondaryTurns,bool energized)
        {
            if(!Finite(inputVoltage)||inputVoltage<0)throw new ArgumentOutOfRangeException(nameof(inputVoltage));
            if(primaryTurns<=0)throw new ArgumentOutOfRangeException(nameof(primaryTurns));
            if(secondaryTurns<0)throw new ArgumentOutOfRangeException(nameof(secondaryTurns));
            return energized?inputVoltage*((double)secondaryTurns/primaryTurns):0.0;
        }
        public static double TurnsRatio(int primaryTurns,int secondaryTurns)
        {
            if(primaryTurns<=0)throw new ArgumentOutOfRangeException(nameof(primaryTurns));
            if(secondaryTurns<0)throw new ArgumentOutOfRangeException(nameof(secondaryTurns));
            return (double)secondaryTurns/primaryTurns;
        }
        public static double VoltageRatio(double inputVoltage,double outputVoltage)=>inputVoltage>0?outputVoltage/inputVoltage:0;
        public static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    }
}
