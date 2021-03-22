using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class NodeBasedEditor : EditorWindow
{
    //lists of nodes and connections.
    private List<Node> nodes;
    private List<Connection> connections;

    private List<TriggerNodeInfo> sceneItems = new List<TriggerNodeInfo>();

    //the styles for the nodes (visual styles)
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;

    private GUIStyle titleStyle;

    private int nodesCreated = 0;

    // instead of connection points, have links to windows instead. (or a connection line with info) 
    private Connectable selectedInPoint;
    private Connectable selectedOutPoint;
    private PathTypes selectedType = PathTypes.NONE;

    public bool connecting = false;

    private Vector2 offset;
    private Vector2 drag;

    private float nodeWidth = 200;
    private float nodeHeight = 200;

    private int currentWindowID = 0;

    [MenuItem("Window/Node Based Editor")]
    private static void OpenWindow()
    {
        NodeBasedEditor window = GetWindow<NodeBasedEditor>();
        window.titleContent = new GUIContent("Node Based Editor");
    }

    private void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

        titleStyle = new GUIStyle();
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        titleStyle.border = new RectOffset(8, 8, 8, 8);

        inPointStyle = new GUIStyle();
        inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
        inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
        inPointStyle.border = new RectOffset(4, 4, 12, 12);

        outPointStyle = new GUIStyle();
        outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
        outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
        outPointStyle.border = new RectOffset(4, 4, 12, 12);

        LoadData();
    }

    private void LoadData()
    {
        List<GameObject> loadedObjects = GameObject.FindGameObjectsWithTag("NarrativeTrigger").ToList();
        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach(GameObject obj in loadedObjects)
        {
            TriggerNodeInfo tempScript = obj.GetComponent<TriggerNodeInfo>();
            if (tempScript != null)
            {
                sceneItems.Add(tempScript);
            }
            else
            {
                objectsToRemove.Add(obj);
            }
        }

        if(objectsToRemove.Count > 0)
        {
            Debug.LogWarning("WARNING: Some gameobjects with the tag NarrativeTrigger don't have a TriggerNodeInfo script. Please fix or remove those!");
        }

        if(nodes == null)
        {
            nodes = new List<Node>();
        }

        foreach (TriggerNodeInfo info in sceneItems)
        {
            Node tempNode = new Node(info.rect, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, titleStyle, OnClickRemoveNode, this, nodesCreated);

            tempNode.ID = info.ID;
            tempNode.title = info.stepDescription;
            tempNode.showAudio = info.showAudio;
            tempNode.playedAudioClips = info.playedAudioClips;
            tempNode.delays = info.delays;
            tempNode.pathType = info.pathType;
            tempNode.scrollViewVector = info.scrollViewVector;
            tempNode.worldPosition = info.transform.position;

            nodes.Add(tempNode);
        }

        ConnectionsManager conManager = GetConnectionManager();

        List<ConnectionInfo> connectionsToRemove = new List<ConnectionInfo>();

        if(connections == null)
        {
            connections = new List<Connection>();
        }

        if(nodes.Count > 0)
        {
            foreach (ConnectionInfo info in conManager.connections)
            {
                bool bothNodesExist = false;

                Node inPoint = nodes.Where(t => t.ID == info.inPoint.ID).First();
                Node outPoint = nodes.Where(t => t.ID == info.outPoint.ID).First();

                Connection tempCon = new Connection(inPoint, outPoint, info.connectionType, OnClickRemoveConnection);

                if (inPoint != null && outPoint != null)
                {
                    bothNodesExist = true;
                }
                else
                {
                    connectionsToRemove.Add(info);
                }

                if (bothNodesExist)
                {
                    inPoint.AddNewConnection(tempCon);
                    outPoint.AddNewConnection(tempCon);

                    connections.Add(tempCon);
                }
            }
        }
        else
        {
            connectionsToRemove = conManager.connections;
        }

        if(connectionsToRemove.Count > 0)
        {
            foreach(ConnectionInfo inf in connectionsToRemove)
            {
                conManager.connections.Remove(inf);
            }
            connectionsToRemove.Clear();
        }
    }

    private void SaveData()
    {
        foreach(Node _node in nodes)
        {
            TriggerNodeInfo script = sceneItems.Where(t => t.ID == _node.ID).First();
            GameObject obj = script.gameObject;
            obj.name = "Generated_Node_" + script.ID;

            script.SaveTriggerData(_node.rect, _node.ID, _node.title, _node.showAudio, _node.playedAudioClips, _node.delays, null, _node.pathType, _node.scrollViewVector, _node.worldPosition);
        }

        ConnectionsManager conManager = GetConnectionManager();

        List<ConnectionInfo> conList = new List<ConnectionInfo>();

        foreach (Connection con in connections)
        {
            conList.Add(new ConnectionInfo(con.inPoint, con.outPoint, con.connectionType));
        }

        conManager.SaveConnections(conList);
    }

    private ConnectionsManager GetConnectionManager()
    {
        //This can be changed to the desired gameobject you want to attach the ConnectionManager script to.
        GameObject gameManagerObj = GameObject.FindGameObjectWithTag("GameController");

        if (gameManagerObj == null)
        {
            gameManagerObj = new GameObject();
            gameManagerObj.name = "GameManager";
            gameManagerObj.tag = "GameController";
            gameManagerObj.AddComponent<ConnectionsManager>();
        }

        ConnectionsManager conManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<ConnectionsManager>();
        if (conManager == null)
        {
            conManager = gameManagerObj.AddComponent<ConnectionsManager>();
        }

        return conManager;
    }

    private void OnGUI()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);

        if (GUILayout.Button("Save", GUILayout.Width(75), GUILayout.Height(50)))
        {
            SaveData();
        }

        BeginWindows();

        currentWindowID = 0;

        if (nodes != null)
        {
            foreach (Node nod in nodes)
            {
                nod.rect = GUI.Window(currentWindowID, nod.rect, nod.DrawNodeWindow, "Story Node " + currentWindowID);
                currentWindowID++;
            }
        }

        EndWindows();
        
        DrawConnections();

        DrawConnectionLine(Event.current);

        ProcessNodeEvents(Event.current);
        ProcessEvents(Event.current);

        if (GUI.changed) Repaint();
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

    private void DrawConnections()
    {
        if (connections != null)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                connections[i].Draw();
            } 
        }
    }

    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    ClearConnectionSelection();
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

    private void ProcessNodeEvents(Event e)
    {
        if (nodes != null)
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                bool guiChanged = nodes[i].ProcessEvents(e);

                if (guiChanged)
                {
                    GUI.changed = true;
                }
            }
        }
    }

    private void DrawConnectionLine(Event e)
    {
        if (selectedInPoint != null && selectedOutPoint == null)
        {
            Handles.DrawBezier(
                selectedInPoint.rect.center + new Vector2(selectedInPoint.rect.width / 2, 0),
                e.mousePosition,
                selectedInPoint.rect.center - Vector2.left * 75,
                e.mousePosition + Vector2.left * 75,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }

        if (selectedOutPoint != null && selectedInPoint == null)
        {
            Handles.DrawBezier(
                selectedOutPoint.rect.center,
                e.mousePosition,
                selectedOutPoint.rect.center - Vector2.left * 50f,
                e.mousePosition + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }
    }

    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition)); 
        genericMenu.ShowAsContext();
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

    private void OnClickAddNode(Vector2 mousePosition)
    {
        if (nodes == null)
        {
            nodes = new List<Node>();
        }

        Rect _rect = new Rect(mousePosition.x, mousePosition.y, nodeWidth, nodeHeight);
        nodes.Add(new Node(_rect, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, titleStyle, OnClickRemoveNode, this, nodesCreated));
        nodesCreated++;

        GameObject nodeObject = new GameObject();
        nodeObject.name = "Generated_Node_" + (nodesCreated-1);
        nodeObject.tag = "NarrativeTrigger";

        nodeObject.AddComponent<BoxCollider>();
        nodeObject.AddComponent<TriggerNodeInfo>();

        nodeObject.GetComponent<BoxCollider>().size = new Vector3(10, 10, 1);
        TriggerNodeInfo script = nodeObject.GetComponent<TriggerNodeInfo>();

        Node currentNode = nodes[nodes.Count - 1];

        script.SaveTriggerData(currentNode.rect, currentNode.ID, currentNode.title, currentNode.showAudio, currentNode.playedAudioClips, currentNode.delays, null, currentNode.pathType, currentNode.scrollViewVector, currentNode.worldPosition);

        sceneItems.Add(script);
    }

    public void OnClickInPoint(Connectable inPoint, PathTypes type)
    {
        connecting = true;
        selectedInPoint = inPoint;
        selectedType = type;

        if (selectedOutPoint != null)
        {
            if (selectedOutPoint.ID != selectedInPoint.ID)
            {
                CreateConnection();
                ClearConnectionSelection(); 
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    public void OnClickOutPoint(Connectable outPoint)
    {
        selectedOutPoint = outPoint;

        if (selectedInPoint != null)
        {
            if (selectedOutPoint.ID != selectedInPoint.ID)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    private void OnClickRemoveNode(Node node)
    {
        if (connections != null)
        {
            List<Connection> connectionsToRemove = new List<Connection>();

            //for when the node gets removed, remove the connections.
            connectionsToRemove.AddRange(node.nodeCons);

            for (int i = 0; i < connectionsToRemove.Count; i++)
            {
                RemoveNodeConnection(connectionsToRemove[i]);
            }

            connectionsToRemove = null;
        }

        nodes.Remove(node);
    }

    private void RemoveNodeConnection(Connection con)
    {
        Node inPoint = nodes.Where(p => p.ID == con.inPoint.ID).First();
        Node outPoint = nodes.Where(p => p.ID == con.outPoint.ID).First();

        inPoint.RemoveConnection(con);
        outPoint.RemoveConnection(con);

        connections.Remove(con);
    }

    private void OnClickRemoveConnection(Connection connection)
    {
        RemoveNodeConnection(connection);
    }

    private void CreateConnection()
    {
        if (connections == null)
        {
            connections = new List<Connection>();
        }

        Node inPoint = nodes.Where(p => p.ID == selectedInPoint.ID).First();
        Node outPoint = nodes.Where(p => p.ID == selectedOutPoint.ID).First();

        Connection newCon = new Connection(selectedInPoint, selectedOutPoint, selectedType, OnClickRemoveConnection);

        inPoint.AddNewConnection(newCon);
        outPoint.AddNewConnection(newCon);

        connections.Add(newCon);
    }

    private void ClearConnectionSelection()
    {
        selectedInPoint = null;
        selectedOutPoint = null;
        selectedType = PathTypes.NONE;

        connecting = false;
    }
}
