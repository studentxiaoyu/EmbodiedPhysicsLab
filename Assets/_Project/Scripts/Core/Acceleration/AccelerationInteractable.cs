using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    public enum AccelerationCommand {CycleAngle,CycleFriction,AddCartWeight,RemoveCartWeight,AddBucketWeight,RemoveBucketWeight,Start,Reset,Record}
    public sealed class AccelerationInteractable : MonoBehaviour,IInteractable
    {
        [SerializeField] private AccelerationExperimentController experiment;
        [SerializeField] private AccelerationCommand command;
        public AccelerationCommand Command=>command;
        public void Configure(AccelerationExperimentController controller,AccelerationCommand action){experiment=controller;command=action;}
        public string InteractionPrompt
        {
            get
            {
                if(experiment==null)return "控制未连接";
                if(command!=AccelerationCommand.Reset&&command!=AccelerationCommand.Record&&experiment.State.Phase!=AccelerationPhase.Ready)return "参数已锁定，请 Reset";
                switch(command)
                {
                    case AccelerationCommand.CycleAngle:return "调节木板倾角";
                    case AccelerationCommand.CycleFriction:return "调节摩擦系数";
                    case AccelerationCommand.AddCartWeight:return $"添加 {experiment.Settings.cartWeightMass:F2} kg 小车砝码";
                    case AccelerationCommand.RemoveCartWeight:return $"移除 {experiment.Settings.cartWeightMass:F2} kg 小车砝码";
                    case AccelerationCommand.AddBucketWeight:return $"添加 {experiment.Settings.bucketWeightMass:F2} kg 沙桶配重";
                    case AccelerationCommand.RemoveBucketWeight:return $"移除 {experiment.Settings.bucketWeightMass:F2} kg 沙桶配重";
                    case AccelerationCommand.Start:return "开始实验";
                    case AccelerationCommand.Reset:return "Reset：恢复默认参数与初始位置";
                    default:return "记录本轮最终结果";
                }
            }
        }
        public void Interact(InteractionContext context)
        {
            switch(command)
            {
                case AccelerationCommand.CycleAngle:experiment.CycleAngle();break;
                case AccelerationCommand.CycleFriction:experiment.CycleFriction();break;
                case AccelerationCommand.AddCartWeight:experiment.ChangeCartWeights(1);break;
                case AccelerationCommand.RemoveCartWeight:experiment.ChangeCartWeights(-1);break;
                case AccelerationCommand.AddBucketWeight:experiment.ChangeBucketWeights(1);break;
                case AccelerationCommand.RemoveBucketWeight:experiment.ChangeBucketWeights(-1);break;
                case AccelerationCommand.Start:experiment.StartExperiment();break;
                case AccelerationCommand.Reset:experiment.ResetExperiment();break;
                case AccelerationCommand.Record:experiment.RecordResult();break;
            }
        }
    }
}
