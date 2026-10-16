using TMPro;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    public sealed class AccelerationDataDisplay : MonoBehaviour
    {
        [SerializeField] private AccelerationExperimentController experiment;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private LineRenderer velocityPlot;
        public void Configure(AccelerationExperimentController controller,TMP_Text text,LineRenderer plot){experiment=controller;readout=text;velocityPlot=plot;Refresh(controller.State);}
        private void OnEnable(){if(experiment!=null){experiment.Changed+=Refresh;Refresh(experiment.State);}}
        private void OnDisable(){if(experiment!=null)experiment.Changed-=Refresh;}
        public void Refresh(AccelerationExperimentState s)
        {
            if(readout==null)return;
            string phase=s.Phase==AccelerationPhase.Ready?"Ready / 就绪":s.Phase==AccelerationPhase.Running?"Running / 运行中":"Completed / 完成";
            readout.text=$"探究加速度与力、质量的关系\n状态：{phase}   第 {s.RunNumber} 轮\nM = {s.CartMass:F2} kg    m = {s.BucketMass:F2} kg\nθ = {s.Angle:F1}°    μ = {s.Friction:F2}\nF = {s.Force:F4} N    a = {s.Acceleration:F4} m/s²\nt = {s.Time:F3} s    x = {s.Position:F4} m\nv = {s.Velocity:F4} m/s\n50 Hz 样本：{experiment.Recorder.Samples.Count}    已保存轮数：{experiment.Recorder.SavedResults.Count}\n{s.Message}";
            if(velocityPlot==null)return;
            var samples=experiment.Recorder.Samples;velocityPlot.positionCount=samples.Count;
            double duration=s.Acceleration>0?AccelerationPhysicsModel.FinishTime(s.Acceleration,s.TravelLimit):1;
            double vmax=System.Math.Max(.1,s.Acceleration*duration);
            for(int i=0;i<samples.Count;i++)velocityPlot.SetPosition(i,new Vector3((float)(samples[i].Time/duration)*.95f,(float)(samples[i].Velocity/vmax)*.38f,0));
        }
    }
}
