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
    public bool showAudio = true;

    public List<AudioClip> playedAudioClips = new List<AudioClip>();
    public List<int> delays = new List<int>();

    public bool isDragged;
    public bool isSelected;

    private NodeBasedEditor editorInstance;

    private List<Connection> nodeConnections = new List<Connection>();

    public GUIStyle style;
    public GUIStyle defaultNodeStyle;
    public GUIStyle selectedNodeStyle;
    private GUIStyle titleStyle;

    public Action<Node> OnRemoveNode;

    private string nodeTitle;
    private PathTypes pathType = PathTypes.NONE;

    private Vector2 scrollViewVector = Vector2.zero;

    int indexNumber;
    bool show = false;

    public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedStyle, GUIStyle inPointStyle, GUIStyle outPointStyle, GUIStyle _titleStyle, Action<Node> OnClickRemoveNode, NodeBasedEditor editor, int nodeID)
    {
        privRect = new Rect(position.x, position.y, width, height);
        style = nodeStyle;
        ID = nodeID;
        defaultNodeStyle = nodeStyle;
        selectedNodeStyle = selectedStyle;
        titleStyle = _titleStyle;
        OnRemoveNode = OnClickRemoveNode;
        editorInstance = editor;

        playedAudioClips.Add(null);
        delays.Add(0);
    }

    public void Drag(Vector2 delta)
    {
        privRect.position += delta;
    }

    public void DrawNodeWindow(int id)
    {
        title = GUILayout.TextField(title);

        showAudio = EditorGUILayout.Foldout(showAudio, "Audio Clips: ");

        if (showAudio)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Audio clip 0:", GUILayout.Width(75));
            playedAudioClips[0] = (AudioClip)EditorGUILayout.ObjectField(playedAudioClips[0], typeof(AudioClip), false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Delay: ", GUILayout.Width(50));
            delays[0] = EditorGUILayout.IntField(delays[0]);
            GUILayout.EndHorizontal();
        }
        

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
