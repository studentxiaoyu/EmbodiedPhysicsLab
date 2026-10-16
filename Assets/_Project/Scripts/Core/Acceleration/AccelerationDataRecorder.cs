using System.Collections.Generic;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    public sealed class AccelerationDataRecorder : MonoBehaviour
    {
        public readonly struct Sample
        {
            public readonly double Time,Position,Velocity,Acceleration;
            public Sample(double t,double x,double v,double a){Time=t;Position=x;Velocity=v;Acceleration=a;}
        }
        private readonly List<Sample> samples=new List<Sample>();
        private readonly List<AccelerationExperimentState> results=new List<AccelerationExperimentState>();
        public IReadOnlyList<Sample> Samples=>samples.AsReadOnly();
        public IReadOnlyList<AccelerationExperimentState> SavedResults=>results.AsReadOnly();
        public void ClearSamples()=>samples.Clear();
        public void Append(double t,double a)=>samples.Add(new Sample(t,AccelerationPhysicsModel.Position(a,t),AccelerationPhysicsModel.Velocity(a,t),a));
        public bool SaveResult(AccelerationExperimentState result)
        {
            if(result.Phase!=AccelerationPhase.Completed)return false;
            if(results.Count>0&&results[results.Count-1].RunNumber==result.RunNumber)return false;
            results.Add(result);return true;
        }
    }
}
