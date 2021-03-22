using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Node : Connectable
{
    public Rect rect { get { return privRect; } set {;} }
    private Rect privRect;
    public int ID { get; set; }

    //components
    public string title;
    public List<bool> showAudio = new List<bool>();

    public List<AudioClip> playedAudioClips = new List<AudioClip>();
    public List<int> delays = new List<int>();

    public bool isDragged;
    public bool isSelected;

    private NodeBasedEditor editorInstance;

    public List<Connection> nodeCons { get { return nodeConnections; } }
    private List<Connection> nodeConnections = new List<Connection>();

    public GUIStyle style;
    public GUIStyle defaultNodeStyle;
    public GUIStyle selectedNodeStyle;
    private GUIStyle titleStyle;

    public Action<Node> OnRemoveNode;

    private string nodeTitle;
    public PathTypes pathType = PathTypes.NONE;

    public Vector2 scrollViewVector = Vector2.zero;

    public Vector3 worldPosition = Vector3.zero;

    public Node(Rect _rect, GUIStyle nodeStyle, GUIStyle selectedStyle, GUIStyle inPointStyle, GUIStyle outPointStyle, GUIStyle _titleStyle, Action<Node> OnClickRemoveNode, NodeBasedEditor editor, int nodeID)
    {
        privRect = _rect;
        style = nodeStyle;
        ID = nodeID;
        defaultNodeStyle = nodeStyle;
        selectedNodeStyle = selectedStyle;
        titleStyle = _titleStyle;
        OnRemoveNode = OnClickRemoveNode;
        editorInstance = editor;

        playedAudioClips.Add(null);
        delays.Add(0);
        showAudio.Add(true);
    }

    public void AddNewConnection(Connection con)
    {
        nodeConnections.Add(con);
    }

    public void RemoveConnection(Connection con)
    {
        nodeConnections.Remove(con);
    }

    public void Drag(Vector2 delta)
    {
        privRect.position += delta;
    }

    public void DrawNodeWindow(int id)
    {
        title = GUILayout.TextField(title);

        scrollViewVector = EditorGUILayout.BeginScrollView(scrollViewVector);
            for(int i=0; i<playedAudioClips.Count; i++)
            {
                showAudio[i] = EditorGUILayout.Foldout(showAudio[i], "Audio clip " + i + ":");

                if (showAudio[i])
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Audio clip " + i + ":", GUILayout.Width(75));
                    playedAudioClips[i] = (AudioClip)EditorGUILayout.ObjectField(playedAudioClips[i], typeof(AudioClip), false);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Delay: ", GUILayout.Width(50));
                    delays[i] = EditorGUILayout.IntField(delays[i]);
                    GUILayout.EndHorizontal();
                }
            }

        if (GUILayout.Button("Add Clip"))
        {
            playedAudioClips.Add(null);
            delays.Add(0);
            showAudio.Add(true);
        }
        if (GUILayout.Button("Remove Clip"))
        {
            if(playedAudioClips.Count > 1) //ALWAYS need at least 1 clip
            {
                playedAudioClips.RemoveAt(playedAudioClips.Count - 1);
                delays.RemoveAt(delays.Count - 1);
                showAudio.RemoveAt(showAudio.Count - 1);
            }
        }
        EditorGUILayout.EndScrollView();

        worldPosition = EditorGUILayout.Vector3Field("Trigger World Position: ", worldPosition);

        //TODO: Add button to grab selected object in the scene

        //GUI.DragWindow();
    }

public bool ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (rect.Contains(e.mousePosition))
                    {
                        if (editorInstance.connecting)
                        {
                            editorInstance.OnClickOutPoint(this);
                        }
                        isDragged = true;
                        GUI.changed = true;
                        isSelected = true;
                        style = selectedNodeStyle;
                    }
                    else
                    {
                        GUI.changed = true;
                        isSelected = false;
                        style = defaultNodeStyle;
                    }
                }

                if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                isDragged = false;
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }

        return false;
    }

    private void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add Good Impact Connection"), false, StartConnection, PathTypes.GOOD);
        genericMenu.AddItem(new GUIContent("Add Bad Impact Connection"), false, StartConnection, PathTypes.BAD);
        genericMenu.AddItem(new GUIContent("Add Neutral Impact Connection"), false, StartConnection, PathTypes.NONE);
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.ShowAsContext();
    }

    private void StartConnection(object obj)
    {
        switch (obj.ToString())
        {
            case "GOOD":
                editorInstance.OnClickInPoint(this, PathTypes.GOOD);
                pathType = PathTypes.GOOD;
                break;
            case "BAD":
                editorInstance.OnClickInPoint(this, PathTypes.BAD);
                pathType = PathTypes.BAD;
                break;
            default:
                editorInstance.OnClickInPoint(this, PathTypes.NONE);
                pathType = PathTypes.NONE;
                break;
        }
    }

    private void OnClickRemoveNode()
    {
        if (OnRemoveNode != null)
        {
            OnRemoveNode(this);
        }
    }
}
