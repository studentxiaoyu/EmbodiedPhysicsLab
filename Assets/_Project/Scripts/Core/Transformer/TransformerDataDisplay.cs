using System.Linq;
using TMPro;
using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Transformer
{
    public sealed class TransformerDataDisplay : MonoBehaviour
    {
        [SerializeField] private TransformerExperimentController controller; [SerializeField] private TMP_Text valuesText,tableText,statusText; [SerializeField] private LineRenderer dataLine; [SerializeField] private Transform markers; [SerializeField] private Material pointMaterial;
        private int drawn=-1;
        public void Configure(TransformerExperimentController c,TMP_Text values,TMP_Text table,TMP_Text status,LineRenderer line,Transform markerRoot,Material dots){if(controller!=null)controller.Changed-=Refresh;controller=c;valuesText=values;tableText=table;statusText=status;dataLine=line;markers=markerRoot;pointMaterial=dots;if(isActiveAndEnabled)c.Changed+=Refresh;Refresh(c.State);}
        private void OnEnable(){if(controller!=null)controller.Changed+=Refresh;}
        private void OnDisable(){if(controller!=null)controller.Changed-=Refresh;}
        public void Refresh(TransformerExperimentState s)
        {
            if(controller==null)return;
            if(valuesText!=null)valuesText.text=$"电源 {(s.PowerOn?"已接通":"已关闭")}    设定 U1 {s.SelectedInputVoltage:F1} V\n有效输入 U1 {s.InputVoltage:F1} V    输出 U2 {s.OutputVoltage:F2} V\n原线圈 N1 {(s.PrimaryInstalled?s.PrimaryTurns+" 匝":"未安装")}    副线圈 N2 {(s.SecondaryInstalled?s.SecondaryTurns+" 匝":"未安装")}\n上铁轭 {(s.CoreClosed?"已合上":"已移开")}    匝数比 {s.TurnsRatio:F3}    电压比 {s.VoltageRatio:F3}\n测量误差 {(s.PowerOn?s.Error.ToString("F5"):"—")}    已记录 {s.RecordCount}/{controller.Settings.minimumDataGroups} 组";
            if(tableText!=null){var rows=controller.Recorder.Points.Select(p=>$"{p.Index,2}  {p.PrimaryTurns,3}/{p.SecondaryTurns,-3}  {p.InputVoltage,5:F1}V  {p.OutputVoltage,6:F2}V  {p.TurnsRatio,5:F3}  {p.VoltageRatio,5:F3}  {p.Error:F4}");tableText.text="序号  N1/N2   U1      U2     N2/N1  U2/U1  误差\n"+string.Join("\n",rows);}
            if(statusText!=null)statusText.text=$"状态：{(s.Phase==TransformerPhase.Ready?"就绪":s.Phase==TransformerPhase.Adjusting?"实验中":"数据达标")}  ·  {s.Message}";
            if(drawn!=controller.Recorder.Points.Count)DrawGraph();
        }
        private void DrawGraph()
        {
            drawn=controller.Recorder.Points.Count;if(markers!=null)for(int index=markers.childCount-1;index>=0;index--){var child=markers.GetChild(index);child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}
            if(dataLine==null)return;var points=controller.Recorder.Points.OrderBy(p=>p.TurnsRatio).ToArray();dataLine.positionCount=points.Length;
            for(int i=0;i<points.Length;i++){var pos=Plot(points[i].TurnsRatio,points[i].VoltageRatio);dataLine.SetPosition(i,pos);if(markers!=null){var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="RatioPoint_"+points[i].Index;dot.transform.SetParent(markers,false);dot.transform.localPosition=pos;dot.transform.localScale=Vector3.one*.07f;dot.GetComponent<Renderer>().sharedMaterial=pointMaterial;if(Application.isPlaying)Destroy(dot.GetComponent<Collider>());else DestroyImmediate(dot.GetComponent<Collider>());}}
        }
        public static Vector3 Plot(double x,double y)=>new Vector3(Mathf.Lerp(-.68f,.68f,(float)(x/4.0)),Mathf.Lerp(-.68f,.68f,(float)(y/4.0)),0);
    }
}
