using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureGate.Core;
using Common.Util.Helpers;

namespace PureGate.Teach
{
    public class Model
    {
        public string ModelName { get; set; } = "";
        public string ModelInfo { get; set; } = "";
        public string ModelPath { get; set; } = "";

        public string InspectImagePath { get; set; } = "";

        public string SaigeModelPath { get; set; } = "";
        public global::PureGate.AIEngineType SaigeEngineType { get; set; } = global::PureGate.AIEngineType.IAD;

        public List<InspWindow> InspWindowList { get; set; }

        public Model()
        {
            InspWindowList = new List<InspWindow>();
        }

        public InspWindow AddInspWindow(InspWindowType windowType)
        {
            InspWindow inspWindow = InspWindowFactory.Inst.Create(windowType);
            InspWindowList.Add(inspWindow);

            return inspWindow;
        }

        public bool AddInspWindow(InspWindow inspWindow)
        {
            if (inspWindow is null)
                return false;

            InspWindowList.Add(inspWindow);

            return true;
        }

        public bool DelInspWindow(InspWindow inspWindow)
        {
            if (InspWindowList.Contains(inspWindow))
            {
                InspWindowList.Remove(inspWindow);
                return true;
            }
            return false;
        }

        public bool DelInspWindowList(List<InspWindow> inspWindowList)
        {
            int before = InspWindowList.Count;
            InspWindowList.RemoveAll(w => inspWindowList.Contains(w));
            return InspWindowList.Count < before;
        }

        public void CreateModel(string path, string modelName, string modelInfo)
        {
            ModelPath = path;
            ModelName = modelName;
            ModelInfo = modelInfo;
        }

        public Model Load(string path)
        {
            Model model = XmlHelper.LoadXml<Model>(path);
            if (model == null)
                return null;

            foreach (var window in model.InspWindowList)
            {
                window.LoadInspWindow(model);
            }

            return model;
        }

        //모델 저장함수
        public void Save()
        {
            if (ModelPath == "")
                return;

            XmlHelper.SaveXml(ModelPath, this);

            foreach (var window in InspWindowList)
            {
                window.SaveInspWindow(this);
            }
        }

        //모델 다른 이름으로 저장함수
        public void SaveAs(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            // 확장자 보정: 사용자가 TEST만 입력해도 TEST.xml로 저장되도록
            if (!filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                filePath += ".xml";

            // 폴더 존재 확인
            string dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir) || Directory.Exists(dir) == false)
                return;

            // 모델 정보 갱신
            ModelPath = filePath;
            ModelName = Path.GetFileNameWithoutExtension(filePath);

            // 실제 저장
            Save();
        }
    }
}
