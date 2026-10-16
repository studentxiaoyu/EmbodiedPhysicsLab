using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Gas
{
    public sealed class GasExperimentVisual : MonoBehaviour
    {
        [SerializeField] private GasExperimentController controller; [SerializeField] private Transform piston,gasVolume,pistonRod; [SerializeField] private Renderer gasRenderer;
        private MaterialPropertyBlock gasProperties;
        public Transform Piston=>piston; public Transform GasVolume=>gasVolume;
        public void Configure(GasExperimentController c,Transform p,Transform g,Transform rod,Renderer r){if(controller!=null)controller.Changed-=Render;controller=c;piston=p;gasVolume=g;pistonRod=rod;gasRenderer=r;if(isActiveAndEnabled)controller.Changed+=Render;Render(c.State);}
        private void OnEnable(){if(controller!=null)controller.Changed+=Render;}
        private void OnDisable(){if(controller!=null)controller.Changed-=Render;}
        public void Render(GasExperimentState state)
        {
            if(controller==null)return;var s=controller.Settings;float ratio=(float)(state.VolumeM3/s.VolumeM3(s.VolumeCount-1));
            float y=Mathf.Lerp(s.pistonMinY,s.pistonMaxY,ratio);
            if(piston!=null)piston.localPosition=new Vector3(0,y,0);
            float bottom=s.cylinderBottomY+.04f;float height=Mathf.Max(.08f,y-bottom-.08f);
            if(gasVolume!=null){gasVolume.localPosition=new Vector3(0,bottom+height*.5f,0);gasVolume.localScale=new Vector3(s.cylinderRadius*1.7f,height*.5f,s.cylinderRadius*1.7f);}
            if(pistonRod!=null){float rodHeight=Mathf.Max(.05f,s.pistonMaxY-y+.12f);pistonRod.localPosition=new Vector3(0,y+rodHeight*.5f+.04f,0);pistonRod.localScale=new Vector3(.07f,rodHeight*.5f,.07f);}
            if(gasRenderer!=null){var c=Color.Lerp(new Color(.25f,.78f,.92f,.36f),new Color(1f,.36f,.12f,.48f),Mathf.InverseLerp(285f,320f,(float)state.TemperatureK));gasProperties ??= new MaterialPropertyBlock();gasRenderer.GetPropertyBlock(gasProperties);gasProperties.SetColor("_BaseColor",c);gasRenderer.SetPropertyBlock(gasProperties);}
        }
    }
}
