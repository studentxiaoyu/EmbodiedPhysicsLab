using System.Collections.Generic;
using System.Linq;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    public enum TransformerPhase { Ready, Adjusting, EnoughData }
    public readonly struct TransformerDataPoint
    {
        public readonly int Index,PrimaryTurns,SecondaryTurns; public readonly double InputVoltage,OutputVoltage,TurnsRatio,VoltageRatio,Error;
        public TransformerDataPoint(int index,double u1,double u2,int n1,int n2){Index=index;InputVoltage=u1;OutputVoltage=u2;PrimaryTurns=n1;SecondaryTurns=n2;TurnsRatio=TransformerPhysicsModel.TurnsRatio(n1,n2);VoltageRatio=TransformerPhysicsModel.VoltageRatio(u1,u2);Error=System.Math.Abs(VoltageRatio-TurnsRatio);}
    }
    public readonly struct TransformerExperimentState
    {
        public readonly TransformerPhase Phase; public readonly bool PowerOn; public readonly int InputIndex,PrimaryIndex,SecondaryIndex,PrimaryTurns,SecondaryTurns,RecordCount;
        public readonly double SelectedInputVoltage,InputVoltage,OutputVoltage,TurnsRatio,VoltageRatio,Error; public readonly string Message;
        public readonly bool PrimaryInstalled,SecondaryInstalled,CoreClosed;
        public bool AssemblyReady=>PrimaryInstalled&&SecondaryInstalled&&CoreClosed;
        public TransformerExperimentState(TransformerPhase phase,bool on,int vi,int pi,int si,int n1,int n2,int count,double selected,double u1,double u2,string message,bool primaryInstalled=true,bool secondaryInstalled=true,bool coreClosed=true)
        {Phase=phase;PowerOn=on;InputIndex=vi;PrimaryIndex=pi;SecondaryIndex=si;PrimaryTurns=n1;SecondaryTurns=n2;RecordCount=count;SelectedInputVoltage=selected;InputVoltage=u1;OutputVoltage=u2;PrimaryInstalled=primaryInstalled;SecondaryInstalled=secondaryInstalled;CoreClosed=coreClosed;TurnsRatio=primaryInstalled&&secondaryInstalled?TransformerPhysicsModel.TurnsRatio(n1,n2):0;VoltageRatio=TransformerPhysicsModel.VoltageRatio(u1,u2);Error=System.Math.Abs(VoltageRatio-TurnsRatio);Message=message;}
    }
    public sealed class TransformerDataRecorder
    {
        private readonly List<TransformerDataPoint> points=new List<TransformerDataPoint>();
        public IReadOnlyList<TransformerDataPoint> Points=>points;
        public bool HasTurns(int n1,int n2)=>points.Any(p=>p.PrimaryTurns==n1&&p.SecondaryTurns==n2);
        public void Add(double u1,double u2,int n1,int n2)=>points.Add(new TransformerDataPoint(points.Count+1,u1,u2,n1,n2));
        public void Clear()=>points.Clear();
    }
}
