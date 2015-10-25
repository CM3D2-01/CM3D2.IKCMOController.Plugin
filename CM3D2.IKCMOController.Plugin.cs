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
    [PluginName("CM3D2 IKCMOController"), PluginVersion("0.0.0.1")]
    public class IKCMOController : UnityInjector.PluginBase
    {
        #region Constants
        
        public const string PluginName = "IKCMOController";
        public const string Version    = "0.0.0.1";

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
		        //�J�������猩���I�u�W�F�N�g�̌��݈ʒu����ʈʒu���W�ɕϊ�
		        screenPoint = Camera.main.WorldToScreenPoint(transform.position);

		        //�擾����screenPoint�̒l��ϐ��Ɋi�[
		        float x = Input.mousePosition.x;
		        float y = Input.mousePosition.y;

		        //�I�u�W�F�N�g�̍��W����}�E�X�ʒu(�܂�N���b�N�����ʒu)�������Ă���B
		        //����ŃI�u�W�F�N�g�̈ʒu�ƃ}�E�X�N���b�N�̈ʒu�̍����擾�ł���B
		        //�h���b�O�ňړ������Ƃ��̂����␳���邽�߂̌v�Z���ƍl����Ε�����₷��
		        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(x, y, screenPoint.z));
		    }

		    public void OnMouseDrag()
		    {
		        //�h���b�O���̃}�E�X�ʒu��ϐ��Ɋi�[
		        float x = Input.mousePosition.x;
		        float y = Input.mousePosition.y;

		        //Debug.Log(x.ToString() + " - " + y.ToString());
		        
		        //�h���b�O���̃}�E�X�ʒu���V�[�����3D��Ԃ̍��W�ɕϊ�����
		        Vector3 currentScreenPoint = new Vector3(x, y, screenPoint.z);

		        //��L�ɃN���b�N�����ꏊ�̍��𑫂����Ƃɂ���āA�I�u�W�F�N�g���ړ�������W�ʒu�����߂�
		        Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenPoint) + offset;

		        //�I�u�W�F�N�g�̈ʒu��ύX����
		        transform.position = currentPosition;
		    }
		}

        #endregion



        #region MonoBehaviour methods

        public void OnLevelWasLoaded(int level)
        {  
	        if (level != 5 && sceneLevel == 5) 
	        {
				isEnabled = false;
				ctrlIKCMO.Clear();
				GearMenu.Buttons.Remove(goMenuButton);
			}
	        
	        if (level == 5) 
	        {
	        	initCompleted = false;

	            if (!goMenuButton)
	            {
		            List<UIAtlas> uiAtlas = new List<UIAtlas>();
		            uiAtlas.AddRange(Resources.FindObjectsOfTypeAll<UIAtlas>());
		            UIAtlas uiAtlasPreset = uiAtlas.FirstOrDefault(a => a.name == "AtlasPreset");

		            // GearMenu�𗘗p���ăV�X�e�����j���[�Ƀ{�^���ǉ�
		            goMenuButton = GearMenu.Buttons.Add(IKCMOController.PluginName, "IKCMO Controller", uiAtlasPreset, "cm3d2_edit_clothesicon_nude" ,this.OnClickMenuButton);
		            goMenuButton.GetComponentsInChildren<UIButton>()[0].defaultColor = buttonColor(isEnabled);
		        }
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

        public void OnClickMenuButton(GameObject go)
        { 
			if (!initCompleted) initialize();
            isEnabled = !isEnabled;

            go.GetComponentsInChildren<UIButton>()[0].defaultColor = buttonColor(isEnabled);
            foreach (ControllIKCMO c in ctrlIKCMO.Values) c.Visible = isEnabled;
        }

		public void OnClickViewOrCancel(GameObject go)
		{
			toggleSphereVisible();
		}

        #endregion



        #region Private methods

		private void initialize()
		{
		  try {

			maid = GameMain.Instance.CharacterMgr.GetMaid(0);
			TBody body = maid.body0;
			Transform trBone = body.m_Bones.transform;
			
            // IKCMO�R���g���[���쐬
			ctrlIKCMO["HandL"] = new ControllIKCMO("HandL",
				GetFieldValue<TBody, TBody.IKCMO>(body, "ikLeftArm"),
				GetFieldValue<TBody, Transform>(body, "UpperArmL"),
				GetFieldValue<TBody, Transform>(body, "ForearmL"),
				GetFieldValue<TBody, Transform>(body, "HandL"));

			ctrlIKCMO["HandR"] = new ControllIKCMO("HandR",
				GetFieldValue<TBody, TBody.IKCMO>(body, "ikRightArm"),
				GetFieldValue<TBody, Transform>(body, "UpperArmR"),
				GetFieldValue<TBody, Transform>(body, "ForearmR"),
				GetFieldValue<TBody, Transform>(body, "HandR"));

			ctrlIKCMO["LegL"] = new ControllIKCMO("LegL", body, body.Thigh_L, body.Calf_L, body.Calf_L.Find("Bip01 L Foot"));
			ctrlIKCMO["LegR"] = new ControllIKCMO("LegR", body, body.Thigh_R, body.Calf_R, body.Calf_R.Find("Bip01 R Foot"));

			ctrlIKCMO["Head"] = new ControllIKCMO("Head", body, 
				FindChild(trBone, "Bip01 Neck"), FindChild(trBone, "Bip01 Head"), FindChild(trBone, "Bip01 Head"));

            // view�{�^��cancel�{�^���t�b�N
            GameObject goToView = FindChild(GameObject.Find("UI Root").transform.Find("PresetButtonPanel").gameObject, "View");
            GameObject goCancel = FindChild(GameObject.Find("UI Root").transform.Find("ViewCancel").gameObject, "Cancel");
            UIEventListener.Get(goToView).onClick += (UIEventListener.VoidDelegate)this.OnClickViewOrCancel;
            UIEventListener.Get(goCancel).onClick += (UIEventListener.VoidDelegate)this.OnClickViewOrCancel;

          } catch(Exception ex) { Debug.LogError(LogLabel + "initialize() : "+ ex); return; }

			initCompleted = true;
		}

		private void toggleSphereVisible()
		{
			foreach (ControllIKCMO c in ctrlIKCMO.Values) c.Visible = isEnabled ? !c.Visible : false;
		}
		
		public Color buttonColor(bool b)
		{
			return (b) ? new Color(1f, 0.9f, 0.9f, 0.9f) : new Color(0.7f, 0.7f, 0.7f, 0.7f);
		}

        #endregion



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


// https://github.com/neguse11/cm3d2_plugins_okiba/blob/master/Lib/GearMenu.cs
namespace GearMenu
{
    /// <summary>
    /// ���ԃ��j���[�ւ̃A�C�R���o�^
    /// </summary>
    public static class Buttons
    {
        // ���ʖ��̎��́B�������O��ێ����邱�ƁB�ڍׂ� SetOnReposition ���Q��
        static string Name_ = "CM3D2.GearMenu.Buttons";

        // �o�[�W����������̎��́B���P�A���������ꍇ�͕�����̎����������傫���l�ɍX�V���邱��
        static string Version_ = Name_ + " 0.0.2.0-useAtlasSprite";

        /// <summary>
        /// ���ʖ�
        /// </summary>
        public static string Name { get { return Name_; } }

        /// <summary>
        /// �o�[�W����������
        /// </summary>
        public static string Version { get { return Version_; } }

        /// <summary>
        /// ���ԃ��j���[�Ƀ{�^����ǉ�
        /// </summary>
        /// <param name="plugin">�{�^����ǉ�����v���O�C���B�A�C�R���ւ̃}�E�X�I�[�o�[���ɖ��O�ƃo�[�W�������\�������</param>
        /// <param name="pngData">�A�C�R���摜�Bnull��(�V�X�e���A�C�R���g�p)�B32x32�s�N�Z����PNG�t�@�C��</param>
        /// <param name="action">�R�[���o�b�N�Bnull��(�R�[���o�b�N�폜)�B�A�C�R���N���b�N���ɌĂяo�����R�[���o�b�N</param>
        /// <returns>�������ꂽ�{�^����GameObject</returns>
        /// <example>
        /// �{�^���ǉ���
        /// <code>
        /// public class MyPlugin : UnityInjector.PluginBase {
        ///     void Awake() {
        ///         GearMenu.Buttons.Add(this, null, GearMenuCallback);
        ///     }
        ///     void GearMenuCallback(GameObject goButton) {
        ///         Debug.LogWarning("GearMenuCallback�Ăяo��");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static GameObject Add(UnityInjector.PluginBase plugin, byte[] pngData, Action<GameObject> action)
        {
            return Add(null, plugin, pngData, action);
        }

        /// <summary>
        /// ���ԃ��j���[�Ƀ{�^����ǉ�
        /// </summary>
        /// <param name="name">�{�^���I�u�W�F�N�g���Bnull��</param>
        /// <param name="plugin">�{�^����ǉ�����v���O�C���B�A�C�R���ւ̃}�E�X�I�[�o�[���ɖ��O�ƃo�[�W�������\�������</param>
        /// <param name="pngData">�A�C�R���摜�Bnull��(�V�X�e���A�C�R���g�p)�B32x32�s�N�Z����PNG�t�@�C��</param>
        /// <param name="action">�R�[���o�b�N�Bnull��(�R�[���o�b�N�폜)�B�A�C�R���N���b�N���ɌĂяo�����R�[���o�b�N</param>
        /// <returns>�������ꂽ�{�^����GameObject</returns>
        /// <example>
        /// �{�^���ǉ���
        /// <code>
        /// public class MyPlugin : UnityInjector.PluginBase {
        ///     void Awake() {
        ///         GearMenu.Buttons.Add(GetType().Name, this, null, GearMenuCallback);
        ///     }
        ///     void GearMenuCallback(GameObject goButton) {
        ///         Debug.LogWarning("GearMenuCallback�Ăяo��");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static GameObject Add(string name, UnityInjector.PluginBase plugin, byte[] pngData, Action<GameObject> action)
        {
            var pluginNameAttr = Attribute.GetCustomAttribute(plugin.GetType(), typeof(PluginNameAttribute)) as PluginNameAttribute;
            var pluginVersionAttr = Attribute.GetCustomAttribute(plugin.GetType(), typeof(PluginVersionAttribute)) as PluginVersionAttribute;
            string pluginName = (pluginNameAttr == null) ? plugin.Name : pluginNameAttr.Name;
            string pluginVersion = (pluginVersionAttr == null) ? string.Empty : pluginVersionAttr.Version;
            string label = string.Format("{0} {1}", pluginName, pluginVersion);
            return Add(name, label, pngData, action);
        }

        /// <summary>
        /// ���ԃ��j���[�Ƀ{�^����ǉ�
        /// </summary>
        /// <param name="label">�c�[���`�b�v�e�L�X�g�Bnull��(�c�[���`�b�v��\��)�B�A�C�R���ւ̃}�E�X�I�[�o�[���ɕ\�������</param>
        /// <param name="pngData">�A�C�R���摜�Bnull��(�V�X�e���A�C�R���g�p)�B32x32�s�N�Z����PNG�t�@�C��</param>
        /// <param name="action">�R�[���o�b�N�Bnull��(�R�[���o�b�N�폜)�B�A�C�R���N���b�N���ɌĂяo�����R�[���o�b�N</param>
        /// <returns>�������ꂽ�{�^����GameObject</returns>
        /// <example>
        /// �{�^���ǉ���
        /// <code>
        /// public class MyPlugin : UnityInjector.PluginBase {
        ///     void Awake() {
        ///         GearMenu.Buttons.Add("�e�X�g", null, GearMenuCallback);
        ///     }
        ///     void GearMenuCallback(GameObject goButton) {
        ///         Debug.LogWarning("GearMenuCallback�Ăяo��");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static GameObject Add(string label, byte[] pngData, Action<GameObject> action)
        {
            return Add(null, label, pngData, action);
        }

        /// <summary>
        /// ���ԃ��j���[�Ƀ{�^����ǉ�
        /// </summary>
        /// <param name="name">�{�^���I�u�W�F�N�g���Bnull��</param>
        /// <param name="label">�c�[���`�b�v�e�L�X�g�Bnull��(�c�[���`�b�v��\��)�B�A�C�R���ւ̃}�E�X�I�[�o�[���ɕ\�������</param>
        /// <param name="pngData">�A�C�R���摜�Bnull��(�V�X�e���A�C�R���g�p)�B32x32�s�N�Z����PNG�t�@�C��</param>
        /// <param name="action">�R�[���o�b�N�Bnull��(�R�[���o�b�N�폜)�B�A�C�R���N���b�N���ɌĂяo�����R�[���o�b�N</param>
        /// <returns>�������ꂽ�{�^����GameObject</returns>
        /// <example>
        /// �{�^���ǉ���
        /// <code>
        /// public class MyPlugin : UnityInjector.PluginBase {
        ///     void Awake() {
        ///         GearMenu.Buttons.Add(GetType().Name, "�e�X�g", null, GearMenuCallback);
        ///     }
        ///     void GearMenuCallback(GameObject goButton) {
        ///         Debug.LogWarning("GearMenuCallback�Ăяo��");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static GameObject Add(string name, string label, byte[] pngData, Action<GameObject> action)
        {
            GameObject goButton = null;

            // ���ɑ��݂���ꍇ�͍폜���đ��s
            if (Contains(name))
            {
                Remove(name);
            }

            if (action == null)
            {
                return goButton;
            }

            try
            {
                // �M�A���j���[�̎q�Ƃ��āA�R���t�B�O�p�l���Ăяo���{�^���𕡐�
                goButton = NGUITools.AddChild(Grid, UTY.GetChildObject(Grid, "Config", true));

                // ���O��ݒ�
                if (name != null)
                {
                    goButton.name = name;
                }

                // �C�x���g�n���h���ݒ�i�����ɁA�����玝���Ă����n���h���͍폜�j
                EventDelegate.Set(goButton.GetComponent<UIButton>().onClick, () => { action(goButton); });

                // �|�b�v�A�b�v�e�L�X�g��ǉ�
                {
                    UIEventTrigger t = goButton.GetComponent<UIEventTrigger>();
                    EventDelegate.Add(t.onHoverOut, () => { SysShortcut.VisibleExplanation(null, false); });
                    EventDelegate.Add(t.onDragStart, () => { SysShortcut.VisibleExplanation(null, false); });
                    SetText(goButton, label);
                }

                // PNG �C���[�W��ݒ�
                {
                    if (pngData == null)
                    {
                        pngData = DefaultIcon.Png;
                    }

                    // �{���̓X�v���C�g���폜���������A�폜����ƃp�l���̃��l�Ƃ�Tween���������삵�Ȃ�
                    // (���삳������@��������Ȃ�) �̂ŁA�X�v���C�g��`�悵�Ȃ��悤�ɐݒ肷��
                    // ���Ƃ��Ǝ����Ă����X�v���C�g���폜����ꍇ�͈ȉ��̃R�[�h���g���Ɨǂ�
                    //     NGUITools.Destroy(goButton.GetComponent<UISprite>());
                    UISprite us = goButton.GetComponent<UISprite>();
                    us.type = UIBasicSprite.Type.Filled;
                    us.fillAmount = 0.0f;

                    // �e�N�X�`���𐶐�
                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(pngData);

                    // �V�����e�N�X�`���X�v���C�g��ǉ�
                    UITexture ut = NGUITools.AddWidget<UITexture>(goButton);
                    ut.material = new Material(ut.shader);
                    ut.material.mainTexture = tex;
                    ut.MakePixelPerfect();
                }

                // �O���b�h���̃{�^�����Ĕz�u
                Reposition();
            }
            catch
            {
                // ���ɃI�u�W�F�N�g������Ă����ꍇ�͍폜
                if (goButton != null)
                {
                    NGUITools.Destroy(goButton);
                    goButton = null;
                }
                throw;
            }
            return goButton;
        }

        public static GameObject Add(string name, string label, UIAtlas atlas, string spriteName, Action<GameObject> action)
        {
            GameObject goButton = null;

            // ���ɑ��݂���ꍇ�͍폜���đ��s
            if (Contains(name))
            {
                Remove(name);
            }

            if (action == null)
            {
                return goButton;
            }

            try
            {
                // �M�A���j���[�̎q�Ƃ��āA�R���t�B�O�p�l���Ăяo���{�^���𕡐�
                goButton = NGUITools.AddChild(Grid, UTY.GetChildObject(Grid, "Config", true));

                // ���O��ݒ�
                if (name != null)
                {
                    goButton.name = name;
                }

                // �C�x���g�n���h���ݒ�i�����ɁA�����玝���Ă����n���h���͍폜�j
                EventDelegate.Set(goButton.GetComponent<UIButton>().onClick, () => { action(goButton); });

                // �|�b�v�A�b�v�e�L�X�g��ǉ�
                {
                    UIEventTrigger t = goButton.GetComponent<UIEventTrigger>();
                    EventDelegate.Add(t.onHoverOut, () => { SysShortcut.VisibleExplanation(null, false); });
                    EventDelegate.Add(t.onDragStart, () => { SysShortcut.VisibleExplanation(null, false); });
                    SetText(goButton, label);
                }

                // SpriteData�ݒ�
                {
                    UISprite us = goButton.GetComponent<UISprite>();
                    us.type       = UIBasicSprite.Type.Sliced;
                    us.atlas      = atlas;
                    us.spriteName = spriteName;
                    us.SetDimensions(32, 32);
                }

                // �O���b�h���̃{�^�����Ĕz�u
                Reposition();
            }
            catch
            {
                // ���ɃI�u�W�F�N�g������Ă����ꍇ�͍폜
                if (goButton != null)
                {
                    NGUITools.Destroy(goButton);
                    goButton = null;
                }
                throw;
            }
            return goButton;
        }

        /// <summary>
        /// ���ԃ��j���[����{�^�����폜
        /// </summary>
        /// <param name="name">�{�^�����BAdd()�ɗ^�������O</param>
        public static void Remove(string name)
        {
            Remove(Find(name));
        }

        /// <summary>
        /// ���ԃ��j���[����{�^�����폜
        /// </summary>
        /// <param name="go">�{�^���BAdd()�̖߂�l</param>
        public static void Remove(GameObject go)
        {
            NGUITools.Destroy(go);
            Reposition();
        }

        /// <summary>
        /// ���ԃ��j���[���̃{�^���̑��݂��m�F
        /// </summary>
        /// <param name="name">�{�^�����BAdd()�ɗ^�������O</param>
        public static bool Contains(string name)
        {
            return Find(name) != null;
        }

        /// <summary>
        /// ���ԃ��j���[���̃{�^���̑��݂��m�F
        /// </summary>
        /// <param name="go">�{�^���BAdd()�̖߂�l</param>
        public static bool Contains(GameObject go)
        {
            return Contains(go.name);
        }

        /// <summary>
        /// �{�^���ɘg������
        /// </summary>
        /// <param name="name">�{�^�����BAdd()�ɗ^�������O</param>
        /// <param name="color">�g�̐F</param>
        public static void SetFrameColor(string name, Color color)
        {
            SetFrameColor(Find(name), color);
        }

        /// <summary>
        /// �{�^���ɘg������
        /// </summary>
        /// <param name="go">�{�^���BAdd()�̖߂�l</param>
        /// <param name="color">�g�̐F</param>
        public static void SetFrameColor(GameObject go, Color color)
        {
            var uiTexture = go.GetComponentInChildren<UITexture>();
            if (uiTexture == null)
            {
                return;
            }
            var tex = uiTexture.mainTexture as Texture2D;
            if (tex == null)
            {
                return;
            }
            for (int x = 1; x < tex.width - 1; x++)
            {
                tex.SetPixel(x, 0, color);
                tex.SetPixel(x, tex.height - 1, color);
            }
            for (int y = 1; y < tex.height - 1; y++)
            {
                tex.SetPixel(0, y, color);
                tex.SetPixel(tex.width - 1, y, color);
            }
            tex.Apply();
        }

        /// <summary>
        /// �{�^���̘g������
        /// </summary>
        /// <param name="name">�{�^�����BAdd()�ɗ^�������O</param>
        public static void ResetFrameColor(string name)
        {
            ResetFrameColor(Find(name));
        }

        /// <summary>
        /// �{�^���̘g������
        /// </summary>
        /// <param name="go">�{�^����GameObject�BAdd()�̖߂�l</param>
        public static void ResetFrameColor(GameObject go)
        {
            SetFrameColor(go, DefaultFrameColor);
        }

        /// <summary>
        /// �}�E�X�I�[�o�[���̃e�L�X�g�w��
        /// </summary>
        /// <param name="name">�{�^�����BAdd()�ɗ^�������O</param>
        /// <param name="label">�}�E�X�I�[�o�[���̃e�L�X�g�Bnull��</param>
        public static void SetText(string name, string label)
        {
            SetText(Find(name), label);
        }

        /// <summary>
        /// �}�E�X�I�[�o�[���̃e�L�X�g�w��
        /// </summary>
        /// <param name="go">�{�^����GameObject�BAdd()�̖߂�l</param>
        /// <param name="label">�}�E�X�I�[�o�[���̃e�L�X�g�Bnull��</param>
        public static void SetText(GameObject go, string label)
        {
            var t = go.GetComponent<UIEventTrigger>();
            t.onHoverOver.Clear();
            EventDelegate.Add(t.onHoverOver, () => { SysShortcut.VisibleExplanation(label, label != null); });
            var b = go.GetComponent<UIButton>();

            // ���Ƀz�o�[���Ȃ������ύX����
            if (b.state == UIButtonColor.State.Hover)
            {
                SysShortcut.VisibleExplanation(label, label != null);
            }
        }

        // �V�X�e���V���[�g�J�b�g����GameObject��������
        static GameObject Find(string name)
        {
            Transform t = GridUI.GetChildList().FirstOrDefault(c => c.gameObject.name == name);
            return t == null ? null : t.gameObject;
        }

        // �O���b�h���̃{�^�����Ĕz�u
        static void Reposition()
        {
            // �K�v�Ȃ� UIGrid.onReposition��ݒ�A�Ăяo�����s��
            SetAndCallOnReposition(GridUI);

            // ����� UIGrid.Update �������ɃO���b�h���̃{�^���Ĕz�u���s����悤���N�G�X�g
            GridUI.repositionNow = true;
        }

        // �K�v�ɉ����� UIGrid.onReposition ��o�^�A�Ăяo��
        static void SetAndCallOnReposition(UIGrid uiGrid)
        {
            string targetVersion = GetOnRepositionVersion(uiGrid);

            // �o�[�W���������� null �̏ꍇ�A�m��Ȃ��N���X���o�^�ς݂Ȃ̂ł�����߂�
            if (targetVersion == null)
            {
                return;
            }

            // �����o�^����Ă��Ȃ����A�������Â��o�[�W������������V���� onReposition ��o�^����
            if (targetVersion == string.Empty || string.Compare(targetVersion, Version, false) < 0)
            {
                uiGrid.onReposition = (new OnRepositionHandler(Version)).OnReposition;
            }

            // PreOnReposition �����ꍇ�͂�����Ăяo��
            if (uiGrid.onReposition != null)
            {
                object target = uiGrid.onReposition.Target;
                if (target != null)
                {
                    Type type = target.GetType();
                    MethodInfo mi = type.GetMethod("PreOnReposition");
                    if (mi != null)
                    {
                        mi.Invoke(target, new object[] { });
                    }
                }
            }
        }

        // UIGrid.onReposition ��ێ�����I�u�W�F�N�g�̃o�[�W����������𓾂�
        //  null            �m��Ȃ��N���X�������̓o�[�W���������񂾂���
        //  string.Empty    UIGrid.onReposition�����o�^������
        //  ���̑�          �擾�����o�[�W����������
        static string GetOnRepositionVersion(UIGrid uiGrid)
        {
            if (uiGrid.onReposition == null)
            {
                // ���o�^������
                return string.Empty;
            }

            object target = uiGrid.onReposition.Target;
            if (target == null)
            {
                // Delegate.Target �� null �Ƃ������Ƃ́A
                // UIGrid.onReposition �� static �ȃ��\�b�h�Ȃ̂ŁA���Ԃ�m��Ȃ��N���X
                return null;
            }

            Type type = target.GetType();
            if (type == null)
            {
                // �^��񂪎��Ȃ��̂ŁA������߂�
                return null;
            }

            FieldInfo fi = type.GetField("Version", BindingFlags.Instance | BindingFlags.Public);
            if (fi == null)
            {
                // public �� Version �����o�[�������Ă��Ȃ��̂ŁA���Ԃ�m��Ȃ��N���X
                return null;
            }

            string targetVersion = fi.GetValue(target) as string;
            if (targetVersion == null || !targetVersion.StartsWith(Name))
            {
                // �m��Ȃ��o�[�W���������񂾂���
                return null;
            }

            return targetVersion;
        }

        public static SystemShortcut SysShortcut { get { return GameMain.Instance.SysShortcut; } }
        public static UIPanel SysShortcutPanel { get { return SysShortcut.GetComponent<UIPanel>(); } }
        public static UISprite SysShortcutExplanation
        {
            get
            {
                Type type = typeof(SystemShortcut);
                FieldInfo fi = type.GetField("m_spriteExplanation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi == null)
                {
                    return null;
                }
                return fi.GetValue(SysShortcut) as UISprite;
            }
        }
        public static GameObject Base { get { return SysShortcut.gameObject.transform.Find("Base").gameObject; } }
        public static UISprite BaseSprite { get { return Base.GetComponent<UISprite>(); } }
        public static GameObject Grid { get { return Base.gameObject.transform.Find("Grid").gameObject; } }
        public static UIGrid GridUI { get { return Grid.GetComponent<UIGrid>(); } }
        public static readonly Color DefaultFrameColor = new Color(1f, 1f, 1f, 0f);

        // UIGrid.onReposition�����p�̃N���X
        // Delegate.Target�̒l�𐶂������߂ɁAstatic �ł͂Ȃ��C���X�^���X�Ƃ��Đ���
        class OnRepositionHandler
        {
            public string Version;

            public OnRepositionHandler(string version)
            {
                this.Version = version;
            }

            public void OnReposition()
            {
            }

            public void PreOnReposition()
            {
                var g = GridUI;
                var b = BaseSprite;

                // ratio : ��ʉ����ɑ΂���{�^���S�̂̉����̔䗦�B0.5 �Ȃ��ʔ���
                float ratio = 3.0f / 4.0f;
                float pixelSizeAdjustment = UIRoot.GetPixelSizeAdjustment(Base);

                // g.cellWidth  = 39;
                g.cellHeight = g.cellWidth;
                g.arrangement = UIGrid.Arrangement.CellSnap;
                g.sorting = UIGrid.Sorting.None;
                g.pivot = UIWidget.Pivot.TopRight;
                g.maxPerLine = (int)(Screen.width / (g.cellWidth / pixelSizeAdjustment) * ratio);

                var children = g.GetChildList();
                int itemCount = children.Count;
                int spriteItemX = Math.Min(g.maxPerLine, itemCount);
                int spriteItemY = Math.Max(1, (itemCount - 1) / g.maxPerLine + 1);
                int spriteWidthMargin = (int)(g.cellWidth * 3 / 2 + 8);
                int spriteHeightMargin = (int)(g.cellHeight / 2);
                float pivotOffsetY = spriteHeightMargin * 1.5f + 1f;

                b.pivot = UIWidget.Pivot.TopRight;
                b.width = (int)(spriteWidthMargin + g.cellWidth * spriteItemX);
                b.height = (int)(spriteHeightMargin + g.cellHeight * spriteItemY + 2f);

                // (946,502) �͂��Ƃ� Base �� localPosition �̒l
                // ���̃I�u�W�F�N�g����l�����Ȃ����낤���H
                Base.transform.localPosition = new Vector3(946.0f, 502.0f + pivotOffsetY, 0.0f);

                // �����ł́A����(spriteItemY)�ɉ����ĉ������ɕ␳����Ӗ���������Ȃ��B
                // ���Ԃ񉽂���������Ă���
                Grid.transform.localPosition = new Vector3(
                    -2.0f + (-spriteItemX - 1 + spriteItemY - 1) * g.cellWidth,
                    -1.0f - pivotOffsetY,
                    0f);

                {
                    int a = 0;
                    string[] specialNames = GameMain.Instance.CMSystem.NetUse ? OnlineButtonNames : OfflineButtonNames;
                    foreach (Transform child in children)
                    {
                        int i = a++;

                        // �V�X�e���������Ă���I�u�W�F�N�g�̏ꍇ�͓��ʂɏ��Ԃ�����
                        int si = Array.IndexOf(specialNames, child.gameObject.name);
                        if (si >= 0)
                        {
                            i = si;
                        }

                        float x = (-i % g.maxPerLine + spriteItemX - 1) * g.cellWidth;
                        float y = (i / g.maxPerLine) * g.cellHeight;
                        child.localPosition = new Vector3(x, -y, 0f);
                    }
                }

                // �}�E�X�I�[�o�[���̃e�L�X�g�̈ʒu���w��
                {
                    UISprite sse = SysShortcutExplanation;
                    Vector3 v = sse.gameObject.transform.localPosition;
                    v.y = Base.transform.localPosition.y - b.height - sse.height;
                    sse.gameObject.transform.localPosition = v;
                }
            }

            // �I�����C�����̃{�^���̕��я��B�C���f�N�X�̎Ⴂ�����E�ɂȂ�
            static string[] OnlineButtonNames = new string[] {
                "Config", "Ss", "SsUi", "Shop", "ToTitle", "Info", "Exit"
            };

            // �I�t���C�����̃{�^���̕��я��B�C���f�N�X�̎Ⴂ�����E�ɂȂ�
            static string[] OfflineButtonNames = new string[] {
                "Config", "Ss", "SsUi", "ToTitle", "Info", "Exit"
            };
        }
    }
    

    // �f�t�H���g�A�C�R��
    internal static class DefaultIcon
    {
        // 32x32 �s�N�Z���� PNG �C���[�W
        public static byte[] Png = new byte[] {
            0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a,0x00,0x00,0x00,0x0d,0x49,0x48,0x44,0x52,
            0x00,0x00,0x00,0x20,0x00,0x00,0x00,0x20,0x08,0x06,0x00,0x00,0x00,0x73,0x7a,0x7a,
            0xf4,0x00,0x00,0x00,0x04,0x73,0x42,0x49,0x54,0x08,0x08,0x08,0x08,0x7c,0x08,0x64,
            0x88,0x00,0x00,0x00,0x09,0x70,0x48,0x59,0x73,0x00,0x00,0x0e,0xc4,0x00,0x00,0x0e,
            0xc4,0x01,0x95,0x2b,0x0e,0x1b,0x00,0x00,0x05,0x00,0x49,0x44,0x41,0x54,0x58,0x85,
            0xc5,0x97,0x5d,0x48,0x54,0x5b,0x14,0xc7,0x7f,0xe7,0x8c,0x8e,0x58,0xd2,0xd5,0xa0,
            0x32,0x9b,0x04,0x4b,0x8d,0xa9,0x89,0x63,0x2a,0x42,0x5c,0xe8,0x83,0x3e,0x1f,0x8a,
            0xca,0x32,0x21,0x8c,0x02,0x53,0xa4,0x9e,0x82,0x62,0x1e,0x24,0x7b,0x28,0xc2,0x5e,
            0x7b,0xa8,0x09,0x4a,0x28,0x49,0x08,0x4b,0xa2,0x04,0x83,0x1e,0xa3,0x7a,0xa9,0x39,
            0xc7,0x8f,0x29,0x30,0x24,0x11,0xc3,0x09,0x43,0x34,0x61,0xa6,0xd3,0x7c,0xec,0x1e,
            0xe6,0xce,0x6e,0xc6,0x39,0x33,0x8a,0xdd,0xee,0x5d,0x6f,0x6b,0xef,0xb5,0xcf,0xff,
            0x77,0xd6,0x59,0x7b,0xed,0x7d,0x14,0x21,0x84,0xe0,0x7f,0xb4,0xac,0x3f,0x2d,0x10,
            0x0a,0x85,0x18,0x1e,0x1e,0x46,0xd7,0x75,0x34,0x4d,0x63,0xf3,0xe6,0xcd,0x7f,0x16,
            0x20,0x1a,0x8d,0x32,0x3e,0x3e,0x8e,0xd7,0xeb,0x45,0xd7,0x75,0x06,0x07,0x07,0x09,
            0x06,0x83,0x00,0xcc,0xcc,0xcc,0xfc,0x19,0x80,0xa9,0xa9,0x29,0x0c,0xc3,0x40,0xd7,
            0x75,0x0c,0xc3,0xe0,0xeb,0xd7,0xaf,0x96,0x71,0xba,0xae,0xa7,0x8c,0xfd,0x16,0x40,
            0x5f,0x5f,0x1f,0x4f,0x9f,0x3e,0x65,0x74,0x74,0x74,0x41,0xf1,0x9f,0x3f,0x7f,0x66,
            0x72,0x72,0x92,0x15,0x2b,0x56,0xc4,0x87,0x84,0xba,0x58,0x71,0xbf,0xdf,0xcf,0xad,
            0x5b,0xb7,0x2c,0xc5,0xb3,0xb3,0xb3,0x71,0xb9,0x5c,0x96,0xeb,0xde,0xbd,0x7b,0x97,
            0xe8,0x8a,0x45,0x65,0x40,0x08,0x41,0x47,0x47,0x07,0xa1,0x50,0x08,0x00,0x9b,0xcd,
            0x46,0x59,0x59,0x19,0x9a,0xa6,0x51,0x51,0x51,0x81,0xd3,0xe9,0xe4,0xf5,0xeb,0xd7,
            0x0c,0x0d,0x0d,0xa5,0xac,0xd5,0x75,0x9d,0xfd,0xfb,0xf7,0xc7,0x5d,0x65,0x41,0x00,
            0x81,0x40,0x80,0x27,0x4f,0x9e,0x10,0x0e,0x87,0x51,0x55,0x15,0x21,0x04,0x7e,0xbf,
            0x9f,0xda,0xda,0x5a,0x34,0x4d,0xc3,0xe5,0x72,0xb1,0x64,0xc9,0x12,0x19,0x1f,0x0c,
            0x06,0xe9,0xe8,0xe8,0x90,0x7e,0x5d,0x5d,0x1d,0xdd,0xdd,0xdd,0x00,0xf4,0xf7,0xf7,
            0x13,0x8d,0x46,0x51,0xd5,0x58,0xf2,0xe7,0x05,0x08,0x06,0x83,0xb4,0xb5,0xb5,0xe1,
            0xf3,0xf9,0xe4,0xd8,0xd9,0xb3,0x67,0xb9,0x71,0xe3,0x46,0xda,0x35,0x8f,0x1f,0x3f,
            0x96,0x85,0x58,0x53,0x53,0x43,0x43,0x43,0x03,0xcf,0x9e,0x3d,0xe3,0xfb,0xf7,0xef,
            0xcc,0xcc,0xcc,0x30,0x3a,0x3a,0xca,0xba,0x75,0xeb,0x00,0xc8,0x58,0x03,0x81,0x40,
            0x80,0x4b,0x97,0x2e,0x25,0x89,0x37,0x35,0x35,0x71,0xf0,0xe0,0xc1,0xb4,0x6b,0xbe,
            0x7c,0xf9,0xc2,0xa3,0x47,0x8f,0x62,0x6f,0x97,0x95,0x45,0x73,0x73,0x33,0x76,0xbb,
            0x1d,0x4d,0xd3,0x64,0x4c,0x42,0x1d,0xa4,0x2f,0xc2,0x40,0x20,0x90,0xf2,0xe6,0x67,
            0xce,0x9c,0xe1,0xc8,0x91,0x23,0x69,0xc5,0x85,0x10,0xdc,0xbd,0x7b,0x17,0xd3,0x34,
            0x01,0x38,0x7c,0xf8,0x30,0x6b,0xd6,0xac,0x01,0xa0,0xaa,0xaa,0x4a,0xc6,0x25,0x6c,
            0x47,0xc3,0x12,0xc0,0x2a,0xed,0x8d,0x8d,0x8d,0xd4,0xd6,0xd6,0xa2,0x28,0x4a,0x5a,
            0x00,0x9f,0xcf,0xc7,0xcb,0x97,0x2f,0x01,0xc8,0xcf,0xcf,0xa7,0xbe,0xbe,0x5e,0xce,
            0x25,0x02,0xbc,0x7f,0xff,0x1e,0xd3,0x34,0x51,0x14,0xa5,0x2a,0x05,0xc0,0x2a,0xed,
            0x0d,0x0d,0x0d,0x1c,0x3d,0x7a,0x34,0xa3,0x78,0x24,0x12,0xc1,0xe3,0xf1,0x48,0xff,
            0xd4,0xa9,0x53,0xe4,0xe5,0xe5,0x49,0x7f,0xf5,0xea,0xd5,0x14,0x15,0x15,0x01,0x60,
            0x9a,0x26,0x1f,0x3e,0x7c,0x00,0xe6,0xd4,0x80,0x55,0xda,0x8f,0x1f,0x3f,0xce,0x89,
            0x13,0x27,0x32,0x8a,0x03,0xbc,0x78,0xf1,0x82,0x91,0x91,0x11,0x00,0x4a,0x4b,0x4b,
            0xd9,0xb3,0x67,0x4f,0xd2,0xbc,0xa2,0x28,0x54,0x56,0x56,0x02,0xb1,0x6d,0xbb,0x7c,
            0xf9,0x72,0x20,0x61,0x17,0x58,0x89,0x97,0x97,0x97,0xb3,0x61,0xc3,0x06,0xde,0xbc,
            0x79,0x93,0x51,0x5c,0x08,0xc1,0xbd,0x7b,0xf7,0xa4,0xdf,0xdc,0xdc,0x8c,0xcd,0x66,
            0x4b,0x89,0xab,0xae,0xae,0xa6,0xb7,0xb7,0x97,0x43,0x87,0x0e,0x51,0x5c,0x5c,0x1c,
            0x03,0x13,0x42,0x08,0x2b,0xf1,0xc5,0xda,0xf6,0xed,0xdb,0x71,0xbb,0xdd,0x96,0x19,
            0x0b,0x04,0x02,0xb4,0xb4,0xb4,0x70,0xf3,0xe6,0x4d,0xf2,0xf2,0xf2,0x50,0x14,0x45,
            0x51,0x01,0xa6,0xa7,0xa7,0x99,0x98,0x98,0xf8,0x6d,0x71,0x80,0x55,0xab,0x56,0xa5,
            0xfd,0x5c,0xd9,0xd9,0xd9,0xac,0x5c,0xb9,0x92,0xdc,0xdc,0x5c,0x39,0xa6,0xc4,0x2f,
            0x24,0xe3,0xe3,0xe3,0xb8,0xdd,0x6e,0xa6,0xa6,0xa6,0xe4,0x64,0x49,0x49,0x09,0x5b,
            0xb6,0x6c,0x99,0x57,0x54,0x08,0x41,0x6f,0x6f,0x2f,0xa1,0x50,0x08,0xbb,0xdd,0xce,
            0xed,0xdb,0xb7,0x29,0x2c,0x2c,0x4c,0x89,0xf3,0xf9,0x7c,0x5c,0xb8,0x70,0x81,0x73,
            0xe7,0xce,0x71,0xe0,0xc0,0x01,0x14,0x45,0xf9,0xd5,0x8a,0x1d,0x0e,0x07,0xd7,0xaf,
            0x5f,0x4f,0x82,0x18,0x1b,0x1b,0xe3,0xe4,0xc9,0x93,0x6c,0xdd,0xba,0x75,0x5e,0x88,
            0xac,0xac,0x2c,0xba,0xbb,0xbb,0xf9,0xf1,0xe3,0x07,0x77,0xee,0xdc,0xa1,0xb5,0xb5,
            0x35,0x25,0x13,0x6f,0xdf,0xbe,0x05,0xe0,0xfe,0xfd,0xfb,0x6c,0xdb,0xb6,0x0d,0x98,
            0xb3,0x0b,0xe2,0x10,0xf1,0x0a,0x8d,0x44,0x22,0x5c,0xbb,0x76,0x4d,0x2e,0xcc,0x64,
            0xf5,0xf5,0xf5,0x14,0x14,0x14,0x00,0xf0,0xea,0xd5,0x2b,0x06,0x07,0x07,0x53,0x62,
            0xbc,0x5e,0x2f,0x00,0xb3,0xb3,0xb3,0xf2,0xa0,0x4a,0xe9,0x03,0x73,0x21,0xc2,0xe1,
            0x30,0x57,0xaf,0x5e,0xa5,0xbf,0xbf,0x3f,0x23,0xc0,0xd2,0xa5,0x4b,0x39,0x7d,0xfa,
            0xb4,0xf4,0x3d,0x1e,0x0f,0x91,0x48,0x44,0xfa,0xd3,0xd3,0xd3,0x7c,0xfc,0xf8,0x31,
            0x26,0xaa,0xaa,0xf2,0xb8,0xb6,0xec,0x84,0x0e,0x87,0x83,0xf6,0xf6,0x76,0xf9,0x46,
            0xa6,0x69,0x72,0xf9,0xf2,0x65,0x06,0x06,0x06,0x32,0x42,0xec,0xde,0xbd,0x9b,0xb2,
            0xb2,0x32,0x00,0x3e,0x7d,0xfa,0xc4,0xf3,0xe7,0xcf,0xe5,0x9c,0x61,0x18,0xc4,0xef,
            0xbf,0xeb,0xd7,0xaf,0x67,0xd9,0xb2,0x65,0x08,0x21,0x7a,0xd2,0x9e,0x05,0x6b,0xd7,
            0xae,0x4d,0x81,0x68,0x6b,0x6b,0xcb,0x08,0xa1,0xaa,0x2a,0x2d,0x2d,0x2d,0xd2,0xef,
            0xec,0xec,0x64,0x76,0x76,0x16,0x20,0xe9,0x33,0x26,0x14,0xf6,0xdf,0x19,0x4f,0xc3,
            0xe2,0xe2,0x62,0x4b,0x88,0x78,0x1b,0xb5,0x32,0xa7,0xd3,0xc9,0xce,0x9d,0x3b,0x81,
            0xd8,0x25,0xb4,0xab,0xab,0x0b,0x21,0x84,0xfc,0xfe,0x90,0x74,0x2e,0xfc,0xa5,0x2c,
            0xe4,0xbf,0x60,0x6c,0x6c,0x0c,0x8f,0xc7,0x43,0x38,0x1c,0x06,0xc0,0x6e,0xb7,0xe3,
            0x70,0x38,0xa8,0xa8,0xa8,0xc0,0xe5,0x72,0x25,0xf5,0x7c,0x80,0xc9,0xc9,0x49,0x9a,
            0x9a,0x9a,0x30,0x4d,0x13,0x9b,0xcd,0xc6,0xc5,0x8b,0x17,0x69,0x6f,0x6f,0x07,0x20,
            0x27,0x27,0x87,0x87,0x0f,0x1f,0x92,0x93,0x93,0x03,0x10,0x5d,0x10,0xc0,0x5c,0x8b,
            0x46,0xa3,0x9c,0x3f,0x7f,0x9e,0xe1,0xe1,0x61,0x54,0x55,0xa5,0xb4,0xb4,0x14,0x4d,
            0xd3,0xd0,0x34,0x8d,0x8d,0x1b,0x37,0x92,0x9b,0x9b,0x4b,0x57,0x57,0x17,0x9d,0x9d,
            0x9d,0x40,0x6c,0x8b,0xc6,0xe1,0xab,0xab,0xab,0xb9,0x72,0xe5,0x8a,0x7c,0xd4,0xa2,
            0x00,0xe0,0x57,0x53,0x99,0x6b,0x36,0x9b,0x0d,0xa7,0xd3,0x49,0x79,0x79,0x39,0x3d,
            0x3d,0x3d,0x29,0xf3,0x8d,0x8d,0x8d,0x1c,0x3b,0x76,0x4c,0x02,0x2c,0xfa,0x56,0xbc,
            0x69,0xd3,0x26,0xf6,0xed,0xdb,0x97,0xd2,0x6c,0x22,0x91,0x08,0x43,0x43,0x43,0x96,
            0xe2,0x80,0x3c,0x11,0xff,0x31,0x65,0xd1,0x19,0x88,0xdb,0xb7,0x6f,0xdf,0x92,0x7e,
            0x4a,0xfc,0x7e,0x7f,0xda,0xd8,0xfc,0xfc,0x7c,0x1e,0x3c,0x78,0x20,0x2f,0xa4,0x80,
            0xf8,0x6d,0x80,0x44,0x13,0x42,0x30,0x31,0x31,0x81,0xd7,0xeb,0xc5,0x30,0x0c,0x06,
            0x06,0x06,0xe4,0x36,0x04,0xd8,0xb1,0x63,0x07,0x6e,0xb7,0x3b,0x69,0xc9,0xbf,0x0a,
            0x30,0xd7,0xc2,0xe1,0x30,0x23,0x23,0x23,0xe8,0xba,0x8e,0xae,0xeb,0xec,0xdd,0xbb,
            0x97,0x5d,0xbb,0x76,0xfd,0x77,0x00,0x0b,0xb1,0x9f,0x14,0xb4,0x4a,0x8e,0x1b,0x9d,
            0x16,0x64,0x00,0x00,0x00,0x00,0x49,0x45,0x4e,0x44,0xae,0x42,0x60,0x82,
        };
    }
}

