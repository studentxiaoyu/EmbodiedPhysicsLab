using EmbodiedPhysicsLab.Core;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Gas
{
    public enum GasCommand { VolumeDown,VolumeUp,Heat,Cool,Hold,Record,Reset }
    public sealed class GasInteractable : MonoBehaviour,IInteractable
    {
        [SerializeField] private GasExperimentController controller; [SerializeField] private GasCommand command;
        public GasCommand Command=>command;
        public void Configure(GasExperimentController c,GasCommand action){controller=c;command=action;}
        public string InteractionPrompt=>command switch
        {
            GasCommand.VolumeDown=>"减小气体体积一档",GasCommand.VolumeUp=>"增大气体体积一档",GasCommand.Heat=>"开启加热",GasCommand.Cool=>"开启冷却",GasCommand.Hold=>"恒温维持",GasCommand.Record=>controller!=null&&controller.State.IsothermalReady?"记录当前 P-V 数据":"当前温度未稳定，暂不能记录",_=>"重置热学实验"
        };
        public void Interact(InteractionContext context)
        {
            if(controller==null)return;
            switch(command){case GasCommand.VolumeDown:controller.ChangeVolume(-1);break;case GasCommand.VolumeUp:controller.ChangeVolume(1);break;case GasCommand.Heat:controller.SetMode(ThermalMode.Heating);break;case GasCommand.Cool:controller.SetMode(ThermalMode.Cooling);break;case GasCommand.Hold:controller.SetMode(ThermalMode.HoldConstant);break;case GasCommand.Record:controller.Record();break;case GasCommand.Reset:controller.ResetExperiment();break;}
        }
    }
}
