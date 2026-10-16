using System;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Gas
{
    public sealed class GasExperimentController : MonoBehaviour
    {
        [SerializeField] private GasExperimentSettings settings;
        private readonly GasDataRecorder recorder=new GasDataRecorder();
        private int volumeIndex; private double temperature,stableTimer; private ThermalMode mode; private bool modified; private string message;
        public event Action<GasExperimentState> Changed;
        public event Action ResetPerformed;
        public GasExperimentSettings Settings=>settings; public GasDataRecorder Recorder=>recorder; public int VolumeIndex=>volumeIndex;
        public GasExperimentState State
        {
            get
            {
                double v=settings.VolumeM3(volumeIndex),p=GasPhysicsModel.Pressure(settings.amountOfSubstance,settings.gasConstant,temperature,v);
                bool ready=mode==ThermalMode.HoldConstant&&Math.Abs(temperature-settings.targetTemperature)<=settings.recordToleranceKelvin&&stableTimer>=settings.stableDwellSeconds;
                var phase=recorder.DistinctVolumeCount>=settings.minimumDistinctVolumes?GasPhase.EnoughData:modified||recorder.Points.Count>0?GasPhase.Adjusting:GasPhase.Ready;
                return new GasExperimentState(phase,mode,volumeIndex,recorder.Points.Count,recorder.DistinctVolumeCount,p,v,temperature,settings.targetTemperature,stableTimer,ready,message);
            }
        }
        public void Configure(GasExperimentSettings config){settings=config;ResetExperiment();}
        private void Awake(){if(settings==null)throw new InvalidOperationException("Gas settings are missing.");ResetExperiment();}
        private void Update()=>Advance(Time.deltaTime);
        private void Publish()=>Changed?.Invoke(State);
        public void Advance(double dt)
        {
            if(!GasPhysicsModel.Finite(dt)||dt<0)throw new ArgumentOutOfRangeException(nameof(dt));
            switch(mode)
            {
                case ThermalMode.Heating:temperature+=settings.heatingRate*dt;stableTimer=0;break;
                case ThermalMode.Cooling:temperature=Math.Max(1,temperature-settings.coolingRate*dt);stableTimer=0;break;
                case ThermalMode.HoldConstant:
                    temperature+=(settings.targetTemperature-temperature)*(1-Math.Exp(-settings.holdConvergenceRate*dt));
                    stableTimer=Math.Abs(temperature-settings.targetTemperature)<=settings.recordToleranceKelvin?stableTimer+dt:0;break;
            }
            if(dt>0)Publish();
        }
        public bool ChangeVolume(int direction)
        {
            if(settings.VolumeCount<2||direction==0)return false;
            int next=Mathf.Clamp(volumeIndex+Math.Sign(direction),0,settings.VolumeCount-1);if(next==volumeIndex){message="已到达体积调节范围端点。";Publish();return false;}
            return SetVolumeIndex(next);
        }
        public bool SetVolumeIndex(int next)
        {
            next=Mathf.Clamp(next,0,settings.VolumeCount-1);if(next==volumeIndex)return false;
            double oldV=settings.VolumeM3(volumeIndex);volumeIndex=next;double newV=settings.VolumeM3(volumeIndex);
            temperature*=Math.Pow(oldV/newV,settings.volumeDisturbanceExponent);mode=ThermalMode.HoldConstant;stableTimer=0;modified=true;message="体积已改变，气体温度出现扰动；请等待恒温稳定后记录。";Publish();return true;
        }
        public void SetMode(ThermalMode next)
        {
            mode=next;stableTimer=0;modified=true;
            message=next==ThermalMode.Heating?"加热中：温度持续升高。":next==ThermalMode.Cooling?"冷却中：温度持续降低。":"恒温控制已开启：温度正逐渐趋近目标值。";Publish();
        }
        public bool Record()
        {
            var s=State;
            if(!s.IsothermalReady){message="当前温度未稳定，暂不能记录；请开启恒温并等待稳定。";Publish();return false;}
            if(recorder.HasVolume(s.VolumeM3)){message="该体积已记录，请选择新的体积档位。";Publish();return false;}
            recorder.Add(s.PressurePa,s.VolumeM3,s.TemperatureK);modified=true;
            message=recorder.DistinctVolumeCount>=settings.minimumDistinctVolumes?"已记录：数据组数达标，可查看 pV 是否保持稳定。":"已记录当前等温数据。";Publish();return true;
        }
        public void ResetExperiment()
        {
            if(settings==null)return;
            if(settings.VolumeCount<2||settings.amountOfSubstance<=0||settings.gasConstant<=0||settings.targetTemperature<=0)throw new InvalidOperationException("Invalid gas settings.");
            volumeIndex=0;temperature=settings.defaultTemperature;mode=ThermalMode.HoldConstant;stableTimer=0;modified=false;recorder.Clear();message="就绪：拖动活塞并拨动温控开关，稳定后记录。";Publish();ResetPerformed?.Invoke();
        }
    }
}
