using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class FileData
{
    public FileData(string fullPath, string txt)
    {
        FullPath = fullPath;
        Txt = txt;
    }

    public Object GetOjbect()
    {
        string assetPath = "Assets" + FullPath.Replace(Application.dataPath, "").Replace('\\', '/');
        return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
    }

    public string FullPath { get; private set; }
    public string Txt { get; private set; }
}

public class FindReferences : EditorWindow
{
    private Object _currObject;
    private Object _foundObject;
    private Object[] _resultObjects;
    private string[] _files;
    private readonly List<FileData> _fileDatas = new List<FileData>();
    private readonly List<string> _extensions = new List<string> { ".prefab", ".unity", ".mat", ".asset" };

    private void OnGUI()
    {
        GUILayout.Space(10);
        if (GUILayout.Button("刷新数据"))
        {
            UpdateFiles();
        }

        _currObject = Selection.activeObject;
        GUILayout.Space(10);
        _currObject = EditorGUILayout.ObjectField("当前选中：", _currObject, typeof(Object), false);
        GUILayout.Space(10);
        _foundObject = EditorGUILayout.ObjectField("被查找：", _foundObject, typeof(Object), false);
        GUILayout.Space(20);

        if (_resultObjects != null)
        {
            GUILayout.Label("查找结果：");
            foreach (var resultObject in _resultObjects)
            {
                EditorGUILayout.ObjectField("", resultObject, typeof(Object), false);
            }
        }

        if (GUILayout.Button("查找当前选中资源"))
        {
            if (_fileDatas.Count == 0)
            {
                Debug.LogError("抱歉，请刷新数据后使用！");
                _foundObject = null;
                _resultObjects = null;
            }
            else
            {
                if (_currObject == null)
                {
                    Debug.LogError("抱歉，请选中后使用！");
                }
                else
                {
                    _foundObject = _currObject;
                    var fileDatas = Find(_currObject);
                    _resultObjects = fileDatas.Select(x => x.GetOjbect()).ToArray();
                }
            }
        }
    }

    private void UpdateFiles()
    {
        var files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
            .Where(x => _extensions.Contains(Path.GetExtension(x).ToLower())).ToArray();

        int idx = 0;

        _fileDatas.Clear();
        EditorApplication.update = () =>
        {
            string file = files[idx];
            EditorUtility.DisplayProgressBar("匹配资源中", file, idx / (float)files.Length);

            _fileDatas.Add(new FileData(file, File.ReadAllText(file)));

            idx++;

            if (idx < files.Length)
                return;

            EditorUtility.ClearProgressBar();
            idx = 0;
            EditorApplication.update = null;
            Debug.Log("刷新完成！");
        };
    }

    private FileData[] Find(Object foundObject)
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        string assetPath = AssetDatabase.GetAssetPath(foundObject);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        return _fileDatas.Where(x => Regex.IsMatch(x.Txt, guid)).ToArray();
    }

    [MenuItem("Window/打开查找引用窗口", false)]
    private static void Init()
    {
        var window = GetWindow<FindReferences>("查找引用");
        Selection.selectionChanged = window.Repaint;
    }
}
