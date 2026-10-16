using UnityEngine;
namespace EmbodiedPhysicsLab.Core.Acceleration
{
    /// <summary>Observer only: analytical state drives all apparatus transforms and weight models.</summary>
    public sealed class AccelerationExperimentVisual : MonoBehaviour
    {
        [SerializeField] private AccelerationExperimentController experiment;
        [SerializeField] private Transform track,cart,pulley,bucket,cartWeights,bucketWeights;
        [SerializeField] private LineRenderer rope;
        [SerializeField] private Material weightMaterial;
        [SerializeField] private bool usePhysicalWeights;
        private int shownCart=-1,shownBucket=-1;
        public Transform Cart=>cart;
        public Transform Bucket=>bucket;
        public Transform Track=>track;
        public void UsePhysicalWeights() { usePhysicalWeights=true; }
        public void Configure(AccelerationExperimentController controller,Transform board,Transform car,Transform wheel,Transform hanging,Transform cartMasses,Transform bucketMasses,LineRenderer cord,Material massMaterial)
        {if(experiment!=null)experiment.Changed-=Refresh;experiment=controller;track=board;cart=car;pulley=wheel;bucket=hanging;cartWeights=cartMasses;bucketWeights=bucketMasses;rope=cord;weightMaterial=massMaterial;if(isActiveAndEnabled)experiment.Changed+=Refresh;Refresh(controller.State);}
        private void OnEnable(){if(experiment!=null){experiment.Changed+=Refresh;Refresh(experiment.State);}}
        private void OnDisable(){if(experiment!=null)experiment.Changed-=Refresh;}
        public void Refresh(AccelerationExperimentState state)
        {
            if(track==null)return;var config=experiment.Settings;
            track.localRotation=Quaternion.Euler(0,0,(float)state.Angle);
            cart.localPosition=new Vector3(config.cartStartDistance+(float)state.Position,.18f,0);
            var p=pulley.position;
            bucket.position=new Vector3(p.x+.10f,p.y-(float)config.bucketSuspensionGap-(float)state.Position,p.z);
            bucket.rotation=Quaternion.identity;
            var points=new Vector3[12];points[0]=cart.TransformPoint(new Vector3(.19f,.085f,0));
            for(int i=1;i<=10;i++){float a=Mathf.Lerp(150,0,(i-1)/9f)*Mathf.Deg2Rad;points[i]=p+new Vector3(Mathf.Cos(a)*.10f,Mathf.Sin(a)*.10f,0);}
            points[11]=bucket.position+Vector3.up*.15f;rope.positionCount=points.Length;rope.SetPositions(points);
            if(!usePhysicalWeights)
            {
                if(shownCart!=state.CartWeights){Rebuild(cartWeights,state.CartWeights,AccelerationCommand.RemoveCartWeight,false);shownCart=state.CartWeights;}
                if(shownBucket!=state.BucketWeights){Rebuild(bucketWeights,state.BucketWeights,AccelerationCommand.RemoveBucketWeight,true);shownBucket=state.BucketWeights;}
            }
        }
        private void Rebuild(Transform root,int count,AccelerationCommand command,bool isBucket)
        {
            for(int i=root.childCount-1;i>=0;i--){var child=root.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
            for(int i=0;i<count;i++)
            {
                var mass=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mass.name="AddedWeight_"+(i+1);mass.transform.SetParent(root,false);
                mass.transform.localScale=isBucket?new Vector3(.13f,.025f,.13f):new Vector3(.12f,.035f,.12f);
                mass.transform.localPosition=isBucket?new Vector3(0,.05f*i,0):new Vector3((i%3-1)*.13f,.07f*(i/3),0);
                mass.GetComponent<Renderer>().sharedMaterial=weightMaterial;mass.AddComponent<AccelerationInteractable>().Configure(experiment,command);
            }
        }
    }
}
