using System;
using System.Linq;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    /// <summary>Single authority for parameters, phase, simulation clock and record commands.</summary>
    public sealed class AccelerationExperimentController : MonoBehaviour
    {
        [SerializeField] private AccelerationExperimentSettings settings;
        [SerializeField] private AccelerationDataRecorder recorder;
        private int cartWeights,bucketWeights,angleIndex,frictionIndex,runNumber,nextSample;
        private double elapsed,finishTime;
        private double angleDegrees,frictionCoefficient;
        private AccelerationPhase phase;
        private string message;
        public event Action<AccelerationExperimentState> Changed;
        public event Action ResetPerformed;
        public Func<bool> StartAllowed { get; set; }
        public AccelerationExperimentSettings Settings=>settings;
        public AccelerationDataRecorder Recorder=>recorder;
        public AccelerationExperimentState State
        {
            get
            {
                double M=settings.cartBaseMass+cartWeights*settings.cartWeightMass,m=settings.bucketBaseMass+bucketWeights*settings.bucketWeightMass;
                double angle=angleDegrees,mu=frictionCoefficient;
                var solution=AccelerationPhysicsModel.Evaluate(M,m,angle,mu,settings.gravity);
                return new AccelerationExperimentState(phase,cartWeights,bucketWeights,runNumber,M,m,angle,mu,solution.Force,solution.Acceleration,elapsed,
                    elapsed==0?0:AccelerationPhysicsModel.Position(solution.Acceleration,elapsed),elapsed==0?0:AccelerationPhysicsModel.Velocity(solution.Acceleration,elapsed),settings.TravelLimit(angle),message);
            }
        }
        public void Configure(AccelerationExperimentSettings config,AccelerationDataRecorder data){settings=config;recorder=data;ResetExperiment();}
        private void Awake(){if(settings==null||recorder==null)throw new InvalidOperationException("Acceleration experiment references missing.");ResetExperiment();}
        private void Update(){if(phase==AccelerationPhase.Running)Advance(Time.deltaTime);}
        private void Publish()=>Changed?.Invoke(State);
        private bool Editable()
        {
            if(phase==AccelerationPhase.Ready)return true;
            message="运行或完成后参数已锁定，请 Reset 后调整。";Publish();return false;
        }
        public bool CycleAngle(){if(!Editable())return false;angleIndex=(angleIndex+1)%settings.anglesDegrees.Length;angleDegrees=settings.anglesDegrees[angleIndex];message="已调整木板倾角。";Publish();return true;}
        public bool CycleFriction(){if(!Editable())return false;frictionIndex=(frictionIndex+1)%settings.frictionCoefficients.Length;frictionCoefficient=settings.frictionCoefficients[frictionIndex];message="已调整摩擦条件。";Publish();return true;}
        public bool SetAngleDegrees(double value)
        {
            if(!Editable()||!AccelerationPhysicsModel.Finite(value))return false;
            angleDegrees=Math.Max(settings.anglesDegrees.Min(),Math.Min(settings.anglesDegrees.Max(),value));
            message="拖动调角滑块：木板倾角已更新。";Publish();return true;
        }
        public bool SetFrictionCoefficient(double value)
        {
            if(!Editable()||!AccelerationPhysicsModel.Finite(value))return false;
            frictionCoefficient=Math.Max(settings.frictionCoefficients.Min(),Math.Min(settings.frictionCoefficients.Max(),value));
            message="摩擦调节旋钮已更新。";Publish();return true;
        }
        public bool ChangeCartWeights(int delta)
        {
            if(!Editable())return false;int next=Mathf.Clamp(cartWeights+delta,0,settings.maxCartWeights);
            if(next==cartWeights){message="小车砝码已达到上限或下限。";Publish();return false;}
            cartWeights=next;message="已更新小车质量。";Publish();return true;
        }
        public bool ChangeBucketWeights(int delta)
        {
            if(!Editable())return false;int next=Mathf.Clamp(bucketWeights+delta,0,settings.maxBucketWeights);
            if(next==bucketWeights){message="沙桶配重已达到上限或下限。";Publish();return false;}
            bucketWeights=next;message="已更新沙桶质量。";Publish();return true;
        }
        public bool StartExperiment()
        {
            if(!Editable())return false;
            if(StartAllowed!=null&&!StartAllowed()){message="请先放稳砝码并释放调节手柄，再开始实验。";Publish();return false;}
            var state=State;
            if(state.Force<=0||state.TravelLimit<=0){message="当前牵引力不足以使小车沿设定方向运动（F ≤ 0），或行程不足。";Publish();return false;}
            elapsed=0;finishTime=AccelerationPhysicsModel.FinishTime(state.Acceleration,state.TravelLimit);nextSample=1;
            recorder.ClearSamples();recorder.Append(0,state.Acceleration);phase=AccelerationPhase.Running;runNumber++;
            message="实验运行中，参数已锁定。";Publish();return true;
        }
        /// <summary>Injectable clock step, independent of rendering cadence; future synchronization can use this boundary.</summary>
        public void Advance(double deltaTime)
        {
            if(phase!=AccelerationPhase.Running)return;
            if(!AccelerationPhysicsModel.Finite(deltaTime)||deltaTime<0)throw new ArgumentOutOfRangeException(nameof(deltaTime));
            elapsed=Math.Min(elapsed+deltaTime,finishTime);double a=State.Acceleration;
            while(nextSample*settings.sampleInterval<=elapsed+1e-10)
            {recorder.Append(nextSample*settings.sampleInterval,a);nextSample++;}
            if(elapsed>=finishTime){phase=AccelerationPhase.Completed;message="本轮完成：已到达允许行程终点。可记录结果或 Reset。";}
            Publish();
        }
        public bool RecordResult()
        {
            bool saved=recorder.SaveResult(State);message=saved?"已保存本轮结果（本次进入实验室期间保留）。":"请先完成实验；同一轮结果不会重复保存。";Publish();return saved;
        }
        public void ResetExperiment()
        {
            if(settings==null||recorder==null)return;
            if(settings.sampleInterval<=0||settings.anglesDegrees.Length==0||settings.frictionCoefficients.Length==0)throw new InvalidOperationException("Invalid experiment settings.");
            cartWeights=Mathf.Clamp(settings.defaultCartWeights,0,settings.maxCartWeights);bucketWeights=Mathf.Clamp(settings.defaultBucketWeights,0,settings.maxBucketWeights);
            angleIndex=Mathf.Clamp(settings.defaultAngleIndex,0,settings.anglesDegrees.Length-1);frictionIndex=Mathf.Clamp(settings.defaultFrictionIndex,0,settings.frictionCoefficients.Length-1);
            angleDegrees=settings.anglesDegrees[angleIndex];frictionCoefficient=settings.frictionCoefficients[frictionIndex];
            elapsed=finishTime=0;nextSample=0;phase=AccelerationPhase.Ready;recorder.ClearSamples();message="就绪：调节参数后开始实验。";Publish();ResetPerformed?.Invoke();
        }
    }
}
