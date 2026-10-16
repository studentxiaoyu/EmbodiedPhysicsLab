using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    public sealed class TransformerExperimentVisual : MonoBehaviour
    {
        [SerializeField] private TransformerExperimentController controller; [SerializeField] private Transform switchLever,inputNeedle,outputNeedle;
        [SerializeField] private Renderer[] primaryRenderers,secondaryRenderers,coreRenderers,sourceIndicator;
        [SerializeField] private Transform[] primaryTurns,secondaryTurns; [SerializeField] private TMP_Text inputMeterText,outputMeterText;
        private MaterialPropertyBlock block; private static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        public void UsePhysicalEquipment(Renderer[] core)
        {
            switchLever=null;primaryTurns=secondaryTurns=System.Array.Empty<Transform>();coreRenderers=core;
            SetActiveCoils(System.Array.Empty<Renderer>(),System.Array.Empty<Renderer>());
        }
        public void SetActiveCoils(Renderer[] primary,Renderer[] secondary)
        {
            SetEmission(primaryRenderers,Color.black,0);SetEmission(secondaryRenderers,Color.black,0);
            primaryRenderers=primary;secondaryRenderers=secondary;
        }
        public void Configure(TransformerExperimentController c,Transform lever,Transform n1,Transform n2,Renderer[] pri,Renderer[] sec,Renderer[] core,Renderer indicator,Transform[] priTurns,Transform[] secTurns,TMP_Text input,TMP_Text output)
        {if(controller!=null)controller.Changed-=Render;controller=c;switchLever=lever;inputNeedle=n1;outputNeedle=n2;primaryRenderers=pri;secondaryRenderers=sec;coreRenderers=core;sourceIndicator=new[]{indicator};primaryTurns=priTurns;secondaryTurns=secTurns;inputMeterText=input;outputMeterText=output;if(isActiveAndEnabled)c.Changed+=Render;Render(c.State);}
        private void OnEnable(){if(controller!=null)controller.Changed+=Render;}
        private void OnDisable(){if(controller!=null)controller.Changed-=Render;}
        private void Awake(){block=new MaterialPropertyBlock();}
        private void Update(){if(controller==null)return;var s=controller.State;float pulse=s.PowerOn ? .72f+.16f*Mathf.Sin(Time.time*7f) : 0f;SetEmission(primaryRenderers,new Color(1f,.24f,.04f),pulse);SetEmission(secondaryRenderers,new Color(1f,.42f,.08f),s.PowerOn?pulse*.7f:0f);SetEmission(coreRenderers,new Color(.18f,.48f,.95f),s.PowerOn ? .12f+.05f*Mathf.Sin(Time.time*7f) : 0f);SetIndicator(s.PowerOn);}
        public void Render(TransformerExperimentState s)
        {
            if(controller==null)return;var settings=controller.Settings;
            if(switchLever!=null)switchLever.localRotation=Quaternion.Euler(0,0,s.PowerOn?-28f:28f);
            SetTurns(primaryTurns,s.PrimaryIndex,settings.TurnsCount);SetTurns(secondaryTurns,s.SecondaryIndex,settings.TurnsCount);
            if(inputMeterText!=null)inputMeterText.text=$"U1\n{s.InputVoltage:F1} V";
            if(outputMeterText!=null)outputMeterText.text=$"U2\n{s.OutputVoltage:F2} V";
            if(inputNeedle!=null)inputNeedle.localRotation=Quaternion.Euler(0,0,-65f+Mathf.Clamp01((float)(s.InputVoltage/12.0))*130f);
            if(outputNeedle!=null)outputNeedle.localRotation=Quaternion.Euler(0,0,-65f+Mathf.Clamp01((float)(s.OutputVoltage/48.0))*130f);
        }
        private static void SetTurns(Transform[] segments,int index,int optionCount){if(segments==null)return;int visible=Mathf.RoundToInt(Mathf.Lerp(4,segments.Length,(float)index/Mathf.Max(1,optionCount-1)));for(int i=0;i<segments.Length;i++)if(segments[i]!=null)segments[i].gameObject.SetActive(i<visible);}
        private void SetEmission(Renderer[] renderers,Color color,float intensity){if(renderers==null)return;if(block==null)block=new MaterialPropertyBlock();block.Clear();block.SetColor(Emission,color*intensity);foreach(var r in renderers)if(r!=null)r.SetPropertyBlock(block);}
        private void SetIndicator(bool on)
        {
            if(block==null)block=new MaterialPropertyBlock();block.Clear();var color=on?new Color(.08f,.9f,.28f):new Color(.4f,.06f,.04f);
            block.SetColor("_BaseColor",color);block.SetColor(Emission,color*(on ? .65f : .08f));
            if(sourceIndicator!=null)foreach(var indicator in sourceIndicator)if(indicator!=null)indicator.SetPropertyBlock(block);
        }
    }
}

