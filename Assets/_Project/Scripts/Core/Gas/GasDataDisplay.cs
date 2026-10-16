using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
namespace EmbodiedPhysicsLab.Core.Gas
{
    public sealed class GasDataDisplay : MonoBehaviour
    {
        [SerializeField] private GasExperimentController controller; [SerializeField] private TMP_Text pressureText,temperatureText,volumeText,observationText,recordListText; [SerializeField] private LineRenderer curve; [SerializeField] private Transform markerRoot; [SerializeField] private Material markerMaterial;
        private int shown=-1;
        public void Configure(GasExperimentController c,TMP_Text p,TMP_Text t,TMP_Text v,TMP_Text info,TMP_Text list,LineRenderer graph,Transform markers,Material dots){controller=c;pressureText=p;temperatureText=t;volumeText=v;observationText=info;recordListText=list;curve=graph;markerRoot=markers;markerMaterial=dots;Refresh(c.State);}
        private void OnEnable(){if(controller!=null)controller.Changed+=Refresh;}
        private void OnDisable(){if(controller!=null)controller.Changed-=Refresh;}
        public void Refresh(GasExperimentState s)
        {
            if(controller==null)return;var set=controller.Settings;
            if(pressureText!=null)pressureText.text=$"压强 P\n{s.PressurePa/1000.0:F2} kPa";
            if(temperatureText!=null)temperatureText.text=$"温度 T\n{s.TemperatureK:F1} K";
            if(volumeText!=null)volumeText.text=$"体积 V\n{s.VolumeM3*1000:F2} L";
            if(observationText!=null)observationText.text=$"阶段：{PhaseName(s.Phase)}  |  温控：{ModeName(s.Mode)}\npV = {s.PTimesV:F4} J   目标 T = {s.TargetTemperatureK:F1} K\n{s.DistinctVolumeCount}/{set.minimumDistinctVolumes} 个不同体积数据\n{(s.IsothermalReady?"满足等温条件，可记录":"温度尚未稳定，暂不能记录")}\n{s.Message}";
            if(recordListText!=null)
            {
                var rows=controller.Recorder.Points.OrderBy(x=>x.VolumeM3).Select(x=>$"{x.Index,2}  {x.VolumeM3*1000,4:F1} L   {x.PressurePa/1000,7:F2} kPa   {x.TemperatureK,6:F1} K   pV {x.PTimesV,6:F3} J");
                recordListText.text="序号    体积       压强        温度       pV\n"+string.Join("\n",rows);
            }
            if(shown!=controller.Recorder.Points.Count)DrawCurve();
        }
        private void DrawCurve()
        {
            if(controller==null||curve==null||markerRoot==null)return;shown=controller.Recorder.Points.Count;
            foreach(Transform t in markerRoot)Destroy(t.gameObject);
            var points=controller.Recorder.Points.OrderBy(x=>x.VolumeM3).ToArray();curve.positionCount=points.Length;
            if(points.Length==0)return;
            double minV=controller.Settings.VolumeM3(0),maxV=controller.Settings.VolumeM3(controller.Settings.VolumeCount-1),maxP=points.Max(p=>p.PressurePa)*1.1;
            for(int i=0;i<points.Length;i++)
            {
                float x=Mathf.Lerp(-.8f,.8f,(float)((points[i].VolumeM3-minV)/(maxV-minV)));float y=Mathf.Lerp(-.45f,.45f,(float)(points[i].PressurePa/maxP));var pos=new Vector3(x,y,0);curve.SetPosition(i,pos);
                var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="PVPoint_"+points[i].Index;dot.transform.SetParent(markerRoot,false);dot.transform.localPosition=pos;dot.transform.localScale=Vector3.one*.075f;dot.GetComponent<Renderer>().sharedMaterial=markerMaterial;Destroy(dot.GetComponent<Collider>());
            }
        }
        private static string PhaseName(GasPhase p)=>p switch{GasPhase.Ready=>"就绪",GasPhase.Adjusting=>"实验中",_=>"数据达标"};
        private static string ModeName(ThermalMode m)=>m switch{ThermalMode.Heating=>"加热",ThermalMode.Cooling=>"冷却",_=>"恒温维持"};
    }
}
