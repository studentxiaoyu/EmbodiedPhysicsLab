using System;
namespace EmbodiedPhysicsLab.Core.Gas
{
    public static class GasPhysicsModel
    {
        public static double Pressure(double n,double r,double temperatureK,double volumeM3)
        {
            if(!Finite(n)||!Finite(r)||!Finite(temperatureK)||!Finite(volumeM3)||n<=0||r<=0||temperatureK<=0||volumeM3<=0)throw new ArgumentOutOfRangeException("Ideal gas inputs must be finite and positive.");
            return n*r*temperatureK/volumeM3;
        }
        public static double PV(double pressurePa,double volumeM3)=>pressurePa*volumeM3;
        public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }
}
