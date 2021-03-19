using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/*
  TODO: Delete old connections and made Node.cs simpler. This and Node.cs should be the only scripts after. (Maybe create a script for connections)
 */

public class NodeEditor : EditorWindow
{
    private List<Node> nodes;

    List<Rect> windows = new List<Rect>();
    List<int> windowsToAttach = new List<int>();
    List<int> attachedWindows = new List<int>();

    private Vector2 offset;
    private Vector2 drag;

    private string title;

    [MenuItem("Window/Node editor")]
    static void ShowEditor()
    {
        NodeEditor editor = EditorWindow.GetWindow<NodeEditor>();
        editor.titleContent = new GUIContent("Node Editor");
    }


    void OnGUI()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);

        if (windowsToAttach.Count == 2)
        {
            attachedWindows.Add(windowsToAttach[0]);
            attachedWindows.Add(windowsToAttach[1]);
            windowsToAttach = new List<int>();
        }

        if (attachedWindows.Count >= 2)
        {
            for (int i = 0; i < attachedWindows.Count; i += 2)
            {
                DrawNodeCurve(windows[attachedWindows[i]], windows[attachedWindows[i + 1]]);
            }
        }

        BeginWindows();

        if (GUILayout.Button("Create Node"))
        {
            windows.Add(new Rect(10, 10, 100, 100));
        }

        for (int i = 0; i < windows.Count; i++)
        {
            windows[i] = GUI.Window(i, windows[i], DrawNodeWindow, "Window " + i);
        }

        EndWindows();

        ProcessEvents(Event.current);
    }

    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    //ClearConnectionSelection();
                }

                if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0)
                {
                    OnDrag(e.delta);
                }
                break;
        }
    }

    private void OnDrag(Vector2 delta)
    {
        drag = delta;

        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Drag(delta);
            }
        }

        GUI.changed = true;
    }

    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));
        genericMenu.ShowAsContext();
    }

    void DrawNodeWindow(int id)
    {
        title = GUILayout.TextField(title);

        if (GUILayout.Button("Attach"))
        {
            windowsToAttach.Add(id);
        }

        GUI.DragWindow();
    }


    void DrawNodeCurve(Rect start, Rect end)
    {
        Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
        Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;
        Color shadowCol = new Color(0, 0, 0, 0.06f);

        for (int i = 0; i < 3; i++)
        {// Draw a shadow
            Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
        }

        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 1);
    }

    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }

        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void OnClickAddNode(Vector2 mousePosition)
    {
        if (nodes == null)
        {
            nodes = new List<Node>();
        }

        //nodes.Add(new Node(mousePosition, 100, 100));
    }
}