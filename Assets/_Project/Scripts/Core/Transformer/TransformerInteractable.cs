using EmbodiedPhysicsLab.Core;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    public enum TransformerCommand { CycleInputVoltage,TogglePower,CyclePrimaryTurns,CycleSecondaryTurns,Record,Reset }
    public sealed class TransformerInteractable : MonoBehaviour,IInteractable
    {
        [SerializeField] private TransformerExperimentController controller; [SerializeField] private TransformerCommand command;
        public TransformerCommand Command=>command;
        public void Configure(TransformerExperimentController c,TransformerCommand action){controller=c;command=action;}
        public string InteractionPrompt=>command switch
        {
            TransformerCommand.CycleInputVoltage=>"调节交流输入电压 U1",TransformerCommand.TogglePower=>controller!=null&&controller.State.PowerOn?"断开交流电源":"接通交流电源",
            TransformerCommand.CyclePrimaryTurns=>"调节原线圈匝数 N1",TransformerCommand.CycleSecondaryTurns=>"调节副线圈匝数 N2",
            TransformerCommand.Record=>controller!=null&&controller.State.PowerOn?"记录 N1、N2、U1、U2":"电源关闭，暂不能记录",_=>"Reset：关闭电源并清除数据"
        };
        public void Interact(InteractionContext context)
        {
            if(controller==null)return;switch(command){case TransformerCommand.CycleInputVoltage:controller.CycleInputVoltage();break;case TransformerCommand.TogglePower:controller.TogglePower();break;case TransformerCommand.CyclePrimaryTurns:controller.CyclePrimaryTurns();break;case TransformerCommand.CycleSecondaryTurns:controller.CycleSecondaryTurns();break;case TransformerCommand.Record:controller.Record();break;case TransformerCommand.Reset:controller.ResetExperiment();break;}
        }
    }
}
