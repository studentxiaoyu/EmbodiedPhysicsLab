using System.Collections.Generic;
using System.Linq;
namespace EmbodiedPhysicsLab.Core.Gas
{
    public enum GasPhase { Ready, Adjusting, EnoughData }
    public enum ThermalMode { HoldConstant, Heating, Cooling }
    public readonly struct GasDataPoint
    {
        public readonly int Index; public readonly double PressurePa,VolumeM3,TemperatureK,PTimesV;
        public GasDataPoint(int index,double p,double v,double t){Index=index;PressurePa=p;VolumeM3=v;TemperatureK=t;PTimesV=p*v;}
    }
    public readonly struct GasExperimentState
    {
        public readonly GasPhase Phase; public readonly ThermalMode Mode; public readonly int VolumeIndex,RecordCount,DistinctVolumeCount;
        public readonly double PressurePa,VolumeM3,TemperatureK,TargetTemperatureK,PTimesV,StableSeconds;
        public readonly bool IsothermalReady; public readonly string Message;
        public GasExperimentState(GasPhase phase,ThermalMode mode,int vi,int records,int distinct,double p,double v,double t,double target,double stable,bool ready,string message)
        {Phase=phase;Mode=mode;VolumeIndex=vi;RecordCount=records;DistinctVolumeCount=distinct;PressurePa=p;VolumeM3=v;TemperatureK=t;TargetTemperatureK=target;PTimesV=GasPhysicsModel.PV(p,v);StableSeconds=stable;IsothermalReady=ready;Message=message;}
    }
    public sealed class GasDataRecorder
    {
        private readonly List<GasDataPoint> points=new List<GasDataPoint>();
        public IReadOnlyList<GasDataPoint> Points=>points;
        public int DistinctVolumeCount=>points.Select(p=>System.Math.Round(p.VolumeM3,9)).Distinct().Count();
        public bool HasVolume(double v)=>points.Any(p=>System.Math.Abs(p.VolumeM3-v)<1e-9);
        public void Add(double p,double v,double t)=>points.Add(new GasDataPoint(points.Count+1,p,v,t));
        public void Clear()=>points.Clear();
    }
}
