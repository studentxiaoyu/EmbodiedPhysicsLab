using TMPro;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    public sealed class TransformerMeterDisplay : MonoBehaviour
    {
        [SerializeField] private TransformerExperimentController controller;
        [SerializeField] private TMP_Text inputText,outputText,selectedVoltageText;
        public void Configure(TransformerExperimentController c,TMP_Text input,TMP_Text output,TMP_Text selected){if(controller!=null)controller.Changed-=Refresh;controller=c;inputText=input;outputText=output;selectedVoltageText=selected;if(isActiveAndEnabled)c.Changed+=Refresh;Refresh(c.State);}
        private void OnEnable(){if(controller!=null)controller.Changed+=Refresh;}
        private void OnDisable(){if(controller!=null)controller.Changed-=Refresh;}
        private void Refresh(TransformerExperimentState s){if(inputText!=null)inputText.text=$"U1\n{s.InputVoltage:F1} V";if(outputText!=null)outputText.text=$"U2\n{s.OutputVoltage:F2} V";if(selectedVoltageText!=null)selectedVoltageText.text=$"{s.SelectedInputVoltage:F1} V";}
    }
}
