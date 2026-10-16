using System;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    /// <summary>Ideal massless rope/pulley, constant kinetic friction; x is distance up the board.</summary>
    public static class AccelerationPhysicsModel
    {
        public readonly struct Solution
        {
            public readonly double Force, Acceleration;
            public Solution(double force,double acceleration){Force=force;Acceleration=acceleration;}
        }
        public static Solution Evaluate(double M,double m,double thetaDegrees,double mu,double g)
        {
            if(!Finite(M)||!Finite(m)||!Finite(thetaDegrees)||!Finite(mu)||!Finite(g)||M<=0||m<=0||g<=0||mu<0||thetaDegrees<0||thetaDegrees>=90)
                throw new ArgumentOutOfRangeException("Physical parameters must be finite and in their valid ranges.");
            double theta=thetaDegrees*Math.PI/180;
            double F=m*g-M*g*Math.Sin(theta)-mu*M*g*Math.Cos(theta);
            return new Solution(F,F/(M+m));
        }
        public static double Position(double acceleration,double time)=>.5*acceleration*time*time;
        public static double Velocity(double acceleration,double time)=>acceleration*time;
        public static double FinishTime(double acceleration,double travel)
        {
            if(acceleration<=0||travel<=0)throw new ArgumentOutOfRangeException("A positive acceleration and travel are required.");
            return Math.Sqrt(2*travel/acceleration);
        }
        public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }
}
