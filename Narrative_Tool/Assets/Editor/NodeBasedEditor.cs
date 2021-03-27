using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

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
    private float nodeHeight = 250;

    private int currentWindowID = 0;
    private int highestID = 0; //The higest ID is needed to make sure there wont be duplicate IDs

    private GameObject nodesParent;

    private float saveTime = 30;
    private float nextSave = 0;

    [MenuItem("Window/Node Based Editor")]
    private static void OpenWindow()
    {
        NodeBasedEditor window = GetWindow<NodeBasedEditor>();
        window.titleContent = new GUIContent("Node Based Editor");
    }

    private void OnEnable()
    {
        //Only enable the editor when the game is not running.
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
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
    }

    private void LoadData()
    {
        //Objects can only be loaded when the have the correct tag.
        List<GameObject> loadedObjects = GameObject.FindGameObjectsWithTag("NarrativeTrigger").ToList();
        List<GameObject> objectsToRemove = new List<GameObject>();

        //The triggers need a parent in the scene, that way it does not get messy in the scene. (This can be changed to a different tag etc).
        nodesParent = GameObject.FindGameObjectWithTag("ObjectsParent");
        if(nodesParent == null)
        {
            nodesParent = new GameObject();
            nodesParent.name = "GeneratedTriggers";
            nodesParent.tag = "ObjectsParent";
        }

        //Every gamobject that gets loaded in must have a TriggerNodeInfo script. Otherwise there's no data to be loaded.
        foreach (GameObject obj in loadedObjects)
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

        //For every node we load in the data and create a new node.
        foreach (TriggerNodeInfo info in sceneItems)
        {
            Node tempNode = new Node(info.rect, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, titleStyle, OnClickRemoveNode, this, nodesCreated);

            //We need to update the highest node id to make sure we dont get duplicate IDs.
            if(info.ID > highestID)
            {
                highestID = info.ID;
            }

            tempNode.ID = info.ID;
            tempNode.title = info.stepDescription;
            tempNode.showAudio = info.showAudio;
            tempNode.playedAudioClips = info.playedAudioClips;
            tempNode.delays = info.delays;
            tempNode.pathType = info.pathType;
            tempNode.scrollViewVector = info.scrollViewVector;
            tempNode.worldPosition = info.transform.position;

            nodes.Add(tempNode);
            nodesCreated++;
        }

        //We need the ConnectionManager to load in the connecitions between nodes.
        ConnectionsManager conManager = GetConnectionManager();

        List<ConnectionInfo> connectionsToRemove = new List<ConnectionInfo>();

        if(connections == null)
        {
            connections = new List<Connection>();
        }

        if(nodes.Count > 0)
        {
            if(conManager.connections != null)
            {

                foreach (ConnectionInfo info in conManager.connections)
                {
                    bool bothNodesExist = false;

                    Node inPoint = nodes.Where(t => t.ID == info.inPointID).First();
                    Node outPoint = nodes.Where(t => t.ID == info.outPointID).First();

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
                        //The connection needs to be added to both nodes (This way, in the game it knows which node(s) it needs to enable) and the connection list. 
                        inPoint.AddNewConnection(tempCon);
                        outPoint.AddNewConnection(tempCon);

                        connections.Add(tempCon);
                    }
                }
            }
        }
        else
        {
            //if there are no nodes, we need to clear the connections
            connectionsToRemove = conManager.connections;
        }

        if(connectionsToRemove != null)
        {
            if (connectionsToRemove.Count > 0)
            {
                foreach (ConnectionInfo inf in connectionsToRemove)
                {
                    conManager.connections.Remove(inf);
                }
                connectionsToRemove.Clear();
            }
        }
    }

    private void SaveData()
    {
        foreach(Node _node in nodes)
        {
            TriggerNodeInfo script = sceneItems.Where(t => t.ID == _node.ID).First();
            GameObject obj = script.gameObject;
            obj.name = "Generated_Node_" + script.ID; //The name shows the node ID to make it easier.

            List<Vector2> v2Cons = null;

            if(_node.nodeCons.Count > 0)
            {
                v2Cons = new List<Vector2>();
                foreach (Connection con in _node.nodeCons)
                {
                    v2Cons.Add(new Vector2(con.inPoint.ID, con.outPoint.ID));
                }
            }

            script.SaveTriggerData(_node.rect, _node.ID, _node.title, _node.showAudio, _node.playedAudioClips, _node.delays, v2Cons, _node.pathType, _node.scrollViewVector, _node.worldPosition);
        }

        ConnectionsManager conManager = GetConnectionManager();

        List<ConnectionInfo> conList = new List<ConnectionInfo>();

        foreach (Connection con in connections)
        {
            conList.Add(new ConnectionInfo(con.inPoint.ID, con.outPoint.ID, con.connectionType));
        }

        conManager.SaveConnections(conList);

        //When the nodes and connections get saved, save the scene too to make sure everything is saved.
        string[] path = EditorSceneManager.GetActiveScene().path.Split(char.Parse("/"));
        path[path.Length - 1] = path[path.Length - 1];
        bool saveOK = EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), string.Join("/", path));
        Debug.Log("Saved the Nodes and the Scene " + (saveOK ? "Sucessfully" : "Error!"));
        nextSave = (float)EditorApplication.timeSinceStartup + saveTime;
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
                nod.rect = GUI.Window(currentWindowID, nod.rect, nod.DrawNodeWindow, "Story Node " + nod.ID);
                currentWindowID++;
            }
        }

        EndWindows();
        
        DrawConnections();

        DrawConnectionLine(Event.current);

        ProcessNodeEvents(Event.current);
        ProcessEvents(Event.current);

        if (GUI.changed) Repaint();

        if (EditorApplication.timeSinceStartup > nextSave)
        {
            CheckForUpdatedPositions();
            SaveData();
        }
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
        nodes.Add(new Node(_rect, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, titleStyle, OnClickRemoveNode, this, (highestID+1)));
        highestID++;

        GameObject nodeObject = new GameObject();
        nodeObject.name = "Generated_Node_" + (highestID);
        nodeObject.tag = "NarrativeTrigger";
        nodeObject.transform.parent = nodesParent.transform;

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

        //if the node is removed, remove the object from the scene too.
        GameObject objToRemove = sceneItems.Find(x => x.ID == node.ID).gameObject;
        DestroyImmediate(objToRemove);

        nodes.Remove(node);
    }

    private void RemoveNodeConnection(Connection con)
    {
        Node inPoint = nodes.Where(p => p.ID == con.inPoint.ID).First();
        Node outPoint = nodes.Where(p => p.ID == con.outPoint.ID).First();

        //remove the connection from the nodes too.
        inPoint.RemoveConnection(con);
        outPoint.RemoveConnection(con);

        connections.Remove(con);
    }

    private void OnClickRemoveConnection(Connection connection)
    {
        RemoveNodeConnection(connection);
    }

    private void CheckForUpdatedPositions()
    {
        foreach(TriggerNodeInfo nodeInfo in sceneItems)
        {
            //when the current position of the gameobject is different than the last saved position, it needs to be updated in the node editor.
            if (nodeInfo.worldPosition != nodeInfo.gameObject.transform.position)
            {
                Node selectedNode = nodes.Find(x => x.ID == nodeInfo.ID);
                if (selectedNode != null)
                {
                    selectedNode.worldPosition = nodeInfo.gameObject.transform.position;
                }
            }
        }
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

    public GameObject SelectObjectInScene(int ID)
    {
        GameObject selection = sceneItems.Find(x => x.ID == ID).gameObject;
        return selection;
    }
}
