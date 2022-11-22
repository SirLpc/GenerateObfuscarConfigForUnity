using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Obfuscar.Editor
{
    [CreateAssetMenu(fileName = "ObfuscarConfigInfo", menuName = "ObfuscarConfigInfo", order = 0)]
    public class ObfuscarConfigInfo : ScriptableObject
    {
        public string InPath = ".";
        public string OutPath = ".\\Obfuscator_Output";
        [SerializeField] 
        private string _obfuscarXmlFileSaveName = "obfuscar.xml";
        
        public string AssemblySearchPath = "$(InPath)\\Builds\\Game_Data\\Managed\\";
        public string[] ObfuscarModules = new[] { "$(InPath)\\Builds\\Game_Data\\Managed\\Game.dll" };
        [SerializeField]
        private string[] _obfuscarModuleIgnoreAttributes = new[] { nameof(SerializeField) };
        
        public bool HidePrivateApi = true;
        public bool KeepPublicApi = true;

        [SerializeField]
        private string _obfuscarToolFilePath = ".\\Tools\\Obfuscar\\Obfuscar.Console.exe";

        [ContextMenu("GenerateObfuscarXml")]
        public void GenerateObfuscarXml()
        {
            XmlDocument xml = new XmlDocument();
            
            var declaration = xml.CreateXmlDeclaration("1.0", "", "");
            xml.AppendChild(declaration);
            
            var obfuscator = xml.CreateElement("Obfuscator");
            xml.AppendChild(obfuscator);

            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var fieldInfo in fields)
            {
                switch (fieldInfo.Name)
                {
                    default:
                        var commonNode = CreateCommonNode(xml, fieldInfo);
                        obfuscator.AppendChild(commonNode);
                        break;
                    case nameof(ObfuscarModules):
                        foreach (var module in ObfuscarModules)
                        {
                            var moduleNode = CreateModuleNode(module, xml);
                            obfuscator.AppendChild(moduleNode);
                        }
                        break;
                    case nameof(AssemblySearchPath):
                        var searchPathNode = xml.CreateElement(nameof(AssemblySearchPath));
                        searchPathNode.SetAttribute("path", AssemblySearchPath);
                        obfuscator.AppendChild(searchPathNode);
                        break;
                }
            }

            xml.Save(Path.Combine(InPath, _obfuscarXmlFileSaveName));
            Debug.Log("GenerateObfuscarXml complete.");
        }

        [ContextMenu("DoObfuscar")]
        public void DoObfuscar()
        {
            ProcessStartInfo info = new ProcessStartInfo(_obfuscarToolFilePath);
            info.WorkingDirectory = InPath;
            info.Arguments = $" {_obfuscarXmlFileSaveName}";
            var ps = Process.Start(info);
            ps.WaitForExit(1 * 60 * 1000);
            Debug.Log("DoObfuscar complete.");
        }

        [ContextMenu("GenXmlAndDoObfuscar")]
        public void GenXmlAndDoObfuscar()
        {
            GenerateObfuscarXml();
            DoObfuscar();
        }
        
        [ContextMenu("GenXmlAndDoObfuscarAndReplace")]
        public void GenXmlAndDoObfuscarAndReplace()
        {
            GenXmlAndDoObfuscar();
            BackOriginalDllAndReplace();
        }

        private void BackOriginalDllAndReplace()
        {
            var originalBackupDir = Path.Combine(OutPath, "OriginalBackup");
            if (Directory.Exists(originalBackupDir))
            {
                Directory.Delete(originalBackupDir, true);
            }

            Directory.CreateDirectory(originalBackupDir);

            foreach (var obfuscarModule in ObfuscarModules)
            {
                var orgFile = obfuscarModule.Replace("$(InPath)", InPath);
                var dllName = Path.GetFileName(orgFile);
                var dstFile = Path.Combine(originalBackupDir, dllName);
                File.Copy(orgFile, dstFile, true);

                var confusedFile = Path.Combine(OutPath, dllName);
                File.Copy(confusedFile, orgFile, true);
            }
            Debug.Log("BackOriginalDllAndReplace complete.");
        }

        private XmlElement CreateCommonNode(XmlDocument xml, FieldInfo fieldInfo)
        {
            var childNode = xml.CreateElement("Var");
            childNode.SetAttribute("name", fieldInfo.Name);
            childNode.SetAttribute("value",
                fieldInfo.FieldType == typeof(bool)
                    ? fieldInfo.GetValue(this).ToString().ToLower()
                    : fieldInfo.GetValue(this).ToString());
            return childNode;
        }

        private XmlElement CreateModuleNode(string dllPath, XmlDocument xml)
        {
            var moduleNode = xml.CreateElement("Module");
            moduleNode.SetAttribute("file", dllPath);
            
            string assemblyName = Path.GetFileNameWithoutExtension(dllPath);
            var assembly = Assembly.Load(assemblyName);
            foreach (var type in assembly.DefinedTypes)
            {
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var fieldInfo in fields)
                {
                    if (HasIgnoreAttribute(fieldInfo.CustomAttributes))
                    {
                        var skipFieldNode = xml.CreateElement("SkipField");
                        skipFieldNode.SetAttribute("type", type.FullName);
                        skipFieldNode.SetAttribute("attrib", "");
                        skipFieldNode.SetAttribute("name", fieldInfo.Name);

                        moduleNode.AppendChild(skipFieldNode);
                    }
                }

                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var methodInfo in methods)
                {
                    if (
                        HasIgnoreAttribute(methodInfo.CustomAttributes)
                        || Array.IndexOf(_unityMessageMethodNames, methodInfo.Name) >= 0
                        )
                    {
                        var skipMethodNode = xml.CreateElement("SkipMethod");
                        skipMethodNode.SetAttribute("type", type.FullName);
                        skipMethodNode.SetAttribute("attrib", "");
                        skipMethodNode.SetAttribute("name", methodInfo.Name);

                        moduleNode.AppendChild(skipMethodNode);
                    }
                }
            }

            return moduleNode;
        }

        private bool HasIgnoreAttribute(IEnumerable<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (Array.IndexOf(_obfuscarModuleIgnoreAttributes, attribute.AttributeType.Name) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly string[] _unityMessageMethodNames = new string[]
        {
            // ScriptableObject
            "Awake",
            "OnDestroy",
            "OnDisable",
            "OnEnable",
            "OnValidate",
            "Reset",
            // MonoBehaviour
            "Awake",
            "FixedUpdate",
            "LateUpdate",
            "OnAnimatorIK",
            "OnAnimatorMove",
            "OnApplicationFocus",
            "OnApplicationPause",
            "OnApplicationQuit",
            "OnAudioFilterRead",
            "OnBecameInvisible",
            "OnBecameVisible",
            "OnCollisionEnter",
            "OnCollisionEnter2D",
            "OnCollisionExit",
            "OnCollisionExit2D",
            "OnCollisionStay",
            "OnCollisionStay2D",
            "OnConnectedToServer",
            "OnControllerColliderHit",
            "OnDestroy",
            "OnDisable",
            "OnDisconnectedFromServer",
            "OnDrawGizmos",
            "OnDrawGizmosSelected",
            "OnEnable",
            "OnFailedToConnect",
            "OnFailedToConnectToMasterServer",
            "OnGUI",
            "OnJointBreak",
            "OnJointBreak2D",
            "OnMasterServerEvent",
            "OnMouseDown",
            "OnMouseDrag",
            "OnMouseEnter",
            "OnMouseExit",
            "OnMouseOver",
            "OnMouseUp",
            "OnMouseUpAsButton",
            "OnNetworkInstantiate",
            "OnParticleCollision",
            "OnParticleSystemStopped",
            "OnParticleTrigger",
            "OnParticleUpdateJobScheduled",
            "OnPlayerConnected",
            "OnPlayerDisconnected",
            "OnPostRender",
            "OnPreCull",
            "OnPreRender",
            "OnRenderImage",
            "OnRenderObject",
            "OnSerializeNetworkView",
            "OnServerInitialized",
            "OnTransformChildrenChanged",
            "OnTransformParentChanged",
            "OnTriggerEnter",
            "OnTriggerEnter2D",
            "OnTriggerExit",
            "OnTriggerExit2D",
            "OnTriggerStay",
            "OnTriggerStay2D",
            "OnValidate",
            "OnWillRenderObject",
            "Reset",
            "Start",
            "Update",
        };
    }
}