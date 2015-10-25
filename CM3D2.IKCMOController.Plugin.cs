using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityInjector.Attributes;

namespace CM3D2.IKCMOController.Plugin
{
    [PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginFilter("CM3D2VRx64")]
    [PluginName("CM3D2 IKCMOController"), PluginVersion("0.0.0.0")]
    public class IKCMOController : UnityInjector.PluginBase
    {
        #region Constants
        
        public const string PluginName = "IKCMOController";
        public const string Version    = "0.0.0.0";

        private readonly string LogLabel = IKCMOController.PluginName + " : ";

        #endregion



        #region Variables

        private int  sceneLevel;
        private bool isEnabled = false;
        private bool initCompleted = false;
        
        private GameObject goMenuButton = null;

		private Dictionary<string, ControllIKCMO> ctrlIKCMO = new Dictionary<string, ControllIKCMO>();

		private Maid maid;

        #endregion



        #region Nested classes

		private class ControllIKCMO
		{
			private string name;
			
            private GameObject goDrag;
			private Transform[] trans  = new Transform[3];
			private TBody.IKCMO ikcmo;

			public string Name      { get{ return this.name; } }
			public float x          { get{ return this.goDrag.transform.position.x; } }
			public float y          { get{ return this.goDrag.transform.position.y; } }
			public float z          { get{ return this.goDrag.transform.position.z; } }
			public Vector3 Target   { get{ return new Vector3(this.x, this.y, this.z); } }
			public Transform trans0 { get{ return this.trans[0]; } }
			public Transform trans1 { get{ return this.trans[1]; } }
			public Transform trans2 { get{ return this.trans[2]; } }
            public bool Visible 
            {
				get	{ return goDrag.activeSelf; }
				set { goDrag.SetActive(value); }
			}

			public ControllIKCMO(string _name, TBody tbody, Transform trans0, Transform trans1, Transform trans2)
			 : this(_name, initIKCMO(tbody, trans0, trans1, trans2), trans0, trans1, trans2) {} 
			public ControllIKCMO(string _name, TBody.IKCMO _ikcmo, Transform trans0, Transform trans1, Transform trans2)
			{
				this.name     = _name;
				this.ikcmo    = _ikcmo;
				this.trans[0] = trans0;
				this.trans[1] = trans1;
				this.trans[2] = trans2;
				
	            goDrag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
	            goDrag.name = name;
	            goDrag.AddComponent<Draggable>();
	            //Debug.LogWarning(goDrag.renderer.material.shader);
	            //goDrag.renderer.material.shader = Shader.Find( "Specular" );
	            goDrag.renderer.material.color  = new Color(1f, 0.66f, 0.66f, 0.8f);
	            goDrag.transform.position = trans2.position;
	            goDrag.transform.localScale *= 0.125f;
			}
			
			public void Proc() { this.Proc(Target, Vector3.zero); }
			public void Proc(Vector3 offset) { this.Proc(Target, offset); }
			public void Proc(Vector3 target, Vector3 offset)
			{
				ikcmo.Porc(trans[0], trans[1], trans[2], target, offset);
			}
			
			private static TBody.IKCMO initIKCMO(TBody tbody, Transform trans0, Transform trans1, Transform trans2)
			{
				TBody.IKCMO _ikcmo = new TBody.IKCMO();
				_ikcmo.Init(trans0, trans1, trans2, tbody);
				
				return _ikcmo;
			}

			
		}

		// http://believeinyourself.hateblo.jp/entry/2014/05/11/074756
		private class Draggable : MonoBehaviour
		{
		    private Vector3 screenPoint;
		    private Vector3 offset;

		    public void OnMouseDown()
		    {
		        //カメラから見たオブジェクトの現在位置を画面位置座標に変換
		        screenPoint = Camera.main.WorldToScreenPoint(transform.position);

		        //取得したscreenPointの値を変数に格納
		        float x = Input.mousePosition.x;
		        float y = Input.mousePosition.y;

		        //オブジェクトの座標からマウス位置(つまりクリックした位置)を引いている。
		        //これでオブジェクトの位置とマウスクリックの位置の差が取得できる。
		        //ドラッグで移動したときのずれを補正するための計算だと考えれば分かりやすい
		        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(x, y, screenPoint.z));
		    }

		    public void OnMouseDrag()
		    {
		        //ドラッグ時のマウス位置を変数に格納
		        float x = Input.mousePosition.x;
		        float y = Input.mousePosition.y;

		        //Debug.Log(x.ToString() + " - " + y.ToString());
		        
		        //ドラッグ時のマウス位置をシーン上の3D空間の座標に変換する
		        Vector3 currentScreenPoint = new Vector3(x, y, screenPoint.z);

		        //上記にクリックした場所の差を足すことによって、オブジェクトを移動する座標位置を求める
		        Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenPoint) + offset;

		        //オブジェクトの位置を変更する
		        transform.position = currentPosition;
		    }
		}

        #endregion



        #region MonoBehaviour methods

        public void OnLevelWasLoaded(int level)
        {  
	        if (level != 5 && sceneLevel == 5) removeMenuButton();
	        
	        if (level == 5) 
	        {
	        	initCompleted = false;
				addMenuButton();
			}

            sceneLevel = level;
        }

        public void LateUpdate()
        {
		  try {
            if (sceneLevel == 5 && initCompleted && isEnabled)
            {
				foreach (ControllIKCMO c in ctrlIKCMO.Values) 
				{
					if (c.Name == "Head") c.Proc(new Vector3(0f, 0f, 0f));
					else                  c.Proc();
				}
	        }
          } catch(Exception ex) { Debug.LogError(LogLabel + "LateUpdate() : "+ ex); return; }
        }


        #endregion



        #region Callbacks

        public void OnClickMenuButton()
        { 
			if (!initCompleted) initialize();
            isEnabled = !isEnabled;
            goMenuButton.GetComponentsInChildren<UIButton>()[0].defaultColor =
            	(isEnabled) ? new Color(1f, 0.9f, 0.9f, 0.9f) : new Color(0.7f, 0.7f, 0.7f, 0.7f);
            foreach (ControllIKCMO c in ctrlIKCMO.Values) c.Visible = isEnabled;
        }

		public void ToggleSphereVisible(GameObject go)
		{
			foreach (ControllIKCMO c in ctrlIKCMO.Values) c.Visible = isEnabled ? !c.Visible : false;
		}
		
        #endregion



        #region Private methods

        private void addMenuButton()
        {
            GameObject goShortcutBase   = GameObject.Find("__GameMain__/SystemUI Root/SystemShortcut/Base");
            GameObject goShortcutGrid   = GameObject.Find("__GameMain__/SystemUI Root/SystemShortcut/Base/Grid");

            if (!goMenuButton)
            {
                // システムショートカットオブジェクトへの参照取得
	            GameObject goSystemShortcut = GameObject.Find("__GameMain__/SystemUI Root/SystemShortcut");
	            GameObject goConfigButton   = GameObject.Find("__GameMain__/SystemUI Root/SystemShortcut/Base/Grid/Config");
	            
	            List<UIAtlas> uiAtlas = new List<UIAtlas>();
	            uiAtlas.AddRange(Resources.FindObjectsOfTypeAll<UIAtlas>());

	            SystemShortcut systemShortcut = goSystemShortcut.GetComponent<SystemShortcut>();

	            // コンフィグパネル呼び出しボタンを複製してコールバック削除
	            EventDelegate orgOnClick_Config = GetFieldValue<SystemShortcut, EventDelegate[]>(systemShortcut, "m_aryDgOnClick")[0];
	            GameObject goConfigButtonCopy = UnityEngine.Object.Instantiate(goConfigButton) as GameObject;
	            EventDelegate.Remove(goConfigButtonCopy.GetComponent<UIButton>().onClick, orgOnClick_Config);

	            // 複製したボタンをさらに複製して、
	            // コールバックとポップアップテキストを追加しショートカットメニューに追加
	            goMenuButton = UnityEngine.Object.Instantiate(goConfigButtonCopy) as GameObject;
	            EventDelegate.Add(goMenuButton.GetComponent<UIButton>().onClick, this.OnClickMenuButton);
	            UIEventTrigger uiEventTrigger = goMenuButton.GetComponent<UIEventTrigger>();
	            EventDelegate.Add( uiEventTrigger.onHoverOver, delegate { systemShortcut.VisibleExplanation("IKCMO Controller", true); } );
	            EventDelegate.Add( uiEventTrigger.onHoverOut,  delegate { systemShortcut.VisibleExplanation("IKCMO Controller", false); } );
	            EventDelegate.Add( uiEventTrigger.onDragStart, delegate { systemShortcut.VisibleExplanation("IKCMO Controller", false); } );
	            goMenuButton.GetComponentsInChildren<UISprite>()[0].atlas = uiAtlas.FirstOrDefault(a => a.name == "AtlasPreset");
	            goMenuButton.GetComponentsInChildren<UISprite>()[0].spriteName = "cm3d2_edit_clothesicon_nude";
	            goMenuButton.GetComponentsInChildren<UIButton>()[0].defaultColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
	            
	            GameObject.Destroy(goConfigButtonCopy);
	        }
            SetChild(goShortcutGrid, goMenuButton);

			goShortcutBase.GetComponent<UISprite>().width += 60;
            goShortcutGrid.GetComponent<UIGrid>().Reposition();
            
            GameObject goToView = FindChild(GameObject.Find("UI Root").transform.Find("PresetButtonPanel").gameObject, "View");
            GameObject goCancel = FindChild(GameObject.Find("UI Root").transform.Find("ViewCancel").gameObject, "Cancel");

            UIEventListener.Get(goToView).onClick += (UIEventListener.VoidDelegate)this.ToggleSphereVisible;
            UIEventListener.Get(goCancel).onClick += (UIEventListener.VoidDelegate)this.ToggleSphereVisible;
        }
        
        private void removeMenuButton()
        {
            GameObject goShortcutBase   = GameObject.Find("__GameMain__/SystemUI Root/SystemShortcut/Base");
            GameObject goShortcutGrid   = GameObject.Find("__GameMain__/SystemUI Root/SystemShortcut/Base/Grid");

			goShortcutGrid.GetComponent<UIGrid>().RemoveChild(goMenuButton.transform);
			goShortcutBase.GetComponent<UISprite>().width -= 60;
		}
		
		

		private void initialize()
		{
		  try {
			maid = GameMain.Instance.CharacterMgr.GetMaid(0);
			TBody body = maid.body0;
			Transform trBone = body.m_Bones.transform;
			
			ctrlIKCMO["HandL"] = new ControllIKCMO("HandL",
				GetFieldValue<TBody, TBody.IKCMO>(maid.body0, "ikLeftArm"),
				GetFieldValue<TBody, Transform>(maid.body0, "UpperArmL"),
				GetFieldValue<TBody, Transform>(maid.body0, "ForearmL"),
				GetFieldValue<TBody, Transform>(maid.body0, "HandL"));

			ctrlIKCMO["HandR"] = new ControllIKCMO("HandR",
				GetFieldValue<TBody, TBody.IKCMO>(maid.body0, "ikRightArm"),
				GetFieldValue<TBody, Transform>(maid.body0, "UpperArmR"),
				GetFieldValue<TBody, Transform>(maid.body0, "ForearmR"),
				GetFieldValue<TBody, Transform>(maid.body0, "HandR"));

			ctrlIKCMO["LegL"] = new ControllIKCMO("LegL", body, body.Thigh_L, body.Calf_L, body.Calf_L.Find("Bip01 L Foot"));
			ctrlIKCMO["LegR"] = new ControllIKCMO("LegR", body, body.Thigh_R, body.Calf_R, body.Calf_R.Find("Bip01 R Foot"));

			ctrlIKCMO["Head"] = new ControllIKCMO("Head", body, 
				FindChild(trBone, "Bip01 Neck"), FindChild(trBone, "Bip01 Head"), FindChild(trBone, "Bip01 Head"));
				
          } catch(Exception ex) { Debug.LogError(LogLabel + "initialize() : "+ ex); return; }

			initCompleted = true;
		}
		
        #endregion



        internal static void WriteComponent(GameObject go)
        {
            Component[] compos = go.GetComponents<Component>();
            foreach(Component c in compos){ Debug.Log(go.name +":"+ c.GetType().Name); }
        }


        #region Utility methods

        internal static Transform FindChild(Transform tr, string s)
        {
			return FindChild(tr.gameObject, s).transform;
		}
        internal static GameObject FindChild(GameObject go, string s)
        {
            if (go == null) return null;
            GameObject target = null;
            
            foreach (Transform tc in go.transform)
            {
                if (tc.gameObject.name == s) return tc.gameObject;
                target = FindChild(tc.gameObject, s);
                if (target) return target;
            } 
            
            return null;
        }

        internal static void SetChild(GameObject parent, GameObject child)
        {
            child.layer                   = parent.layer;
            child.transform.parent        = parent.transform;
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale    = Vector3.one;
            child.transform.rotation      = Quaternion.identity;
        }

        internal static FieldInfo GetFieldInfo<T>(string name)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            return typeof(T).GetField(name, bf);
        }

        internal static TResult GetFieldValue<T, TResult>(T inst, string name)
        {
            if (inst == null)  return default(TResult);

            FieldInfo field = GetFieldInfo<T>(name);
            if (field == null) return default(TResult);

            return (TResult)field.GetValue(inst);
        }
        
        #endregion

    }
}