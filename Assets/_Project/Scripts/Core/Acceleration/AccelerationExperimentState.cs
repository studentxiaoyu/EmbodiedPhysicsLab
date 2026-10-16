namespace EmbodiedPhysicsLab.Core.Acceleration
{
    public enum AccelerationPhase {Ready,Running,Completed}
    /// <summary>Read-only authoritative snapshot; observers never mutate experiment state.</summary>
    public readonly struct AccelerationExperimentState
    {
        public readonly AccelerationPhase Phase;
        public readonly int CartWeights,BucketWeights,RunNumber;
        public readonly double CartMass,BucketMass,Angle,Friction,Force,Acceleration,Time,Position,Velocity,TravelLimit;
        public readonly string Message;
        public AccelerationExperimentState(AccelerationPhase phase,int cart,int bucket,int run,double M,double m,double angle,double friction,double force,double acceleration,double time,double position,double velocity,double limit,string message)
        {Phase=phase;CartWeights=cart;BucketWeights=bucket;RunNumber=run;CartMass=M;BucketMass=m;Angle=angle;Friction=friction;Force=force;Acceleration=acceleration;Time=time;Position=position;Velocity=velocity;TravelLimit=limit;Message=message;}
    }
}
