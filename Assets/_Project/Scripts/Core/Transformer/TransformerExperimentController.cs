using System;
using System.Linq;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    /// <summary>Single authority for source, turns, measurements, phase and saved data.</summary>
    public sealed class TransformerExperimentController : MonoBehaviour
    {
        [SerializeField] private TransformerExperimentSettings settings;
        private readonly TransformerDataRecorder recorder=new TransformerDataRecorder();
        private int inputIndex,primaryIndex,secondaryIndex; private bool powerOn,modified; private string message;
        private double selectedInputVoltage;
        private bool primaryInstalled=true,secondaryInstalled=true,coreClosed=true;
        public event Action<TransformerExperimentState> Changed;
        public event Action ResetPerformed;
        public TransformerExperimentSettings Settings=>settings; public TransformerDataRecorder Recorder=>recorder;
        public TransformerExperimentState State
        {
            get
            {
                int n1=settings.TurnsAt(primaryIndex),n2=settings.TurnsAt(secondaryIndex);double selected=selectedInputVoltage,u1=powerOn?selected:0;
                double u2=TransformerPhysicsModel.SecondaryVoltage(u1,n1,n2,powerOn);var phase=recorder.Points.Count>=settings.minimumDataGroups?TransformerPhase.EnoughData:modified||recorder.Points.Count>0?TransformerPhase.Adjusting:TransformerPhase.Ready;
                return new TransformerExperimentState(phase,powerOn,inputIndex,primaryIndex,secondaryIndex,n1,n2,recorder.Points.Count,selected,u1,u2,message,primaryInstalled,secondaryInstalled,coreClosed);
            }
        }
        public void Configure(TransformerExperimentSettings config){settings=config;ResetExperiment();}
        private void Awake(){if(settings==null)throw new InvalidOperationException("Transformer settings are missing.");ResetExperiment();}
        private void Publish()=>Changed?.Invoke(State);
        private bool Cycle(ref int index,int count,string label)
        {
            if(count<1)return false;index=(index+1)%count;modified=true;message=label+"已调整。";Publish();return true;
        }
        public bool CycleInputVoltage()
        {
            inputIndex=(inputIndex+1)%settings.InputCount;selectedInputVoltage=settings.InputAt(inputIndex);
            modified=true;message="输入电压已调整。";Publish();return true;
        }
        public bool SetInputVoltage(double value)
        {
            if(!TransformerPhysicsModel.Finite(value))return false;
            selectedInputVoltage=Math.Max(settings.inputVoltageOptions.Min(),Math.Min(settings.inputVoltageOptions.Max(),value));
            modified=true;message="调压旋钮已更新交流输入电压。";Publish();return true;
        }
        public bool CyclePrimaryTurns()=>Cycle(ref primaryIndex,settings.TurnsCount,"原线圈匝数");
        public bool CycleSecondaryTurns()=>Cycle(ref secondaryIndex,settings.TurnsCount,"副线圈匝数");
        public void TogglePower()=>SetPower(!powerOn);
        public bool SetPower(bool value)
        {
            if(value&&(!primaryInstalled||!secondaryInstalled||!coreClosed))
            {message="请装好原、副线圈并合上上铁轭，再接通交流电源。";Publish();return false;}
            powerOn=value;modified=true;message=powerOn?"交流电源已接通；线圈与上铁轭已锁定，请断电后换装。":"交流电源已关闭，输出电压归零。";Publish();return true;
        }
        public void SetAssembly(int primaryOption,int secondaryOption,bool closed)
        {
            bool hasPrimary=primaryOption>=0,hasSecondary=secondaryOption>=0;
            int pri=hasPrimary?Mathf.Clamp(primaryOption,0,settings.TurnsCount-1):primaryIndex;
            int sec=hasSecondary?Mathf.Clamp(secondaryOption,0,settings.TurnsCount-1):secondaryIndex;
            if(pri==primaryIndex&&sec==secondaryIndex&&hasPrimary==primaryInstalled&&hasSecondary==secondaryInstalled&&closed==coreClosed)return;
            primaryIndex=pri;secondaryIndex=sec;primaryInstalled=hasPrimary;secondaryInstalled=hasSecondary;coreClosed=closed;
            if(!hasPrimary||!hasSecondary||!closed)powerOn=false;
            modified=true;message=hasPrimary&&hasSecondary&&closed?"线圈与铁芯已装配，可合闸测量。":"换装中：断电，移开上铁轭后更换线圈。";Publish();
        }
        public bool Record()
        {
            var s=State;if(!s.PowerOn||!s.AssemblyReady||s.InputVoltage<=0){message="交流电源未接通或器材未装配完整，不能记录。";Publish();return false;}
            if(recorder.HasTurns(s.PrimaryTurns,s.SecondaryTurns)){message="该匝数组合已记录，请调整 N1 或 N2。";Publish();return false;}
            recorder.Add(s.InputVoltage,s.OutputVoltage,s.PrimaryTurns,s.SecondaryTurns);modified=true;
            message=recorder.Points.Count>=settings.minimumDataGroups?"数据组数已达标，可比较 N2/N1 与 U2/U1。":"已记录当前匝数组合。";Publish();return true;
        }
        public void ResetExperiment()
        {
            if(settings==null)return;if(settings.InputCount<1||settings.TurnsCount<1)throw new InvalidOperationException("Transformer settings contain no valid options.");
            inputIndex=Mathf.Clamp(settings.defaultInputIndex,0,settings.InputCount-1);primaryIndex=Mathf.Clamp(settings.defaultPrimaryTurnsIndex,0,settings.TurnsCount-1);secondaryIndex=Mathf.Clamp(settings.defaultSecondaryTurnsIndex,0,settings.TurnsCount-1);
            selectedInputVoltage=settings.InputAt(inputIndex);primaryInstalled=secondaryInstalled=coreClosed=true;
            powerOn=false;modified=false;recorder.Clear();message="就绪：转动调压旋钮，断电换装线圈，合上铁轭后接通电源。";Publish();ResetPerformed?.Invoke();
        }
    }
}
