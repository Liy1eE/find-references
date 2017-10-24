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
    private string _selectedFold = "";
    private bool _isGlobal = true;
    private Vector2 _scrollPos;

    private void OnGUI()
    {
        GUILayout.Space(10);

        _isGlobal = EditorGUILayout.ToggleLeft("全局查找", _isGlobal);
        GUI.enabled = !_isGlobal;

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginVertical();
        if (GUILayout.Button("选择目录"))
            _selectedFold = EditorUtility.OpenFolderPanel("选择查找目录", "", "");
        GUILayout.Label("当前查询目录：" + _selectedFold);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUI.enabled = true;

        GUILayout.Space(10);
        if (GUILayout.Button(new GUIContent("初始化数据", "当资源改变需要重新初始化数据")))
        {
            string fold = _isGlobal ? Application.dataPath : _selectedFold;
            Debug.Log(fold);
            if (string.IsNullOrEmpty(fold))
                Debug.LogError("抱歉，请先选择目录！");
            else
                UpdateFiles(fold);
        }

        _currObject = Selection.activeObject;

        GUILayout.Space(10);
        _currObject = EditorGUILayout.ObjectField("当前选中：", _currObject, typeof(Object), false);

        GUILayout.Space(10);
        if (GUILayout.Button("查找当前选中资源"))
        {
            if (_fileDatas.Count == 0)
            {
                Debug.LogError("抱歉，第一次使用请初始化数据！");
                _foundObject = null;
                _resultObjects = null;
            }
            else
            {
                if (_currObject == null)
                    Debug.LogError("抱歉，请选中后使用！");
                else
                {
                    _foundObject = _currObject;
                    var fileDatas = Find(_currObject);
                    _resultObjects = fileDatas.Select(x => x.GetOjbect()).ToArray();
                }
            }
        }

        GUILayout.Space(30);
        _foundObject = EditorGUILayout.ObjectField("被查找：", _foundObject, typeof(Object), false);
        GUILayout.Space(30);

        if (_resultObjects != null)
        {
            GUILayout.Label("查找结果：");
            if (_resultObjects.Length > 0)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));

                foreach (var resultObject in _resultObjects)
                    EditorGUILayout.ObjectField("", resultObject, typeof(Object), false);

                EditorGUILayout.EndScrollView();
            }
            else
                GUILayout.Label("未找到该资源的引用：" + _foundObject.name);
        }
    }

    private void UpdateFiles(string path)
    {
        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(x => _extensions.Contains(Path.GetExtension(x).ToLower())).ToArray();

        int idx = 0;

        _fileDatas.Clear();
        EditorApplication.update = () =>
        {
            string file = files[idx];
            EditorUtility.DisplayProgressBar("初始化数据中", file, idx / (float)files.Length);

            _fileDatas.Add(new FileData(file, File.ReadAllText(file)));

            idx++;

            if (idx < files.Length)
                return;

            EditorUtility.ClearProgressBar();
            idx = 0;
            EditorApplication.update = null;
            Debug.Log("初始化完成！");
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