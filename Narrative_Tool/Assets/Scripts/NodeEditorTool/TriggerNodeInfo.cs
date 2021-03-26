using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerNodeInfo : MonoBehaviour
{
    public Rect rect;
    public int ID;

    //components
    public string stepDescription;

    //showAudio is for the editor only, should the foldout be folded out or not?
    [HideInInspector] public List<bool> showAudio = new List<bool>();

    public List<AudioClip> playedAudioClips = new List<AudioClip>();
    public List<int> delays = new List<int>();

    [HideInInspector] public bool isDragged = false;
    [HideInInspector] public bool isSelected = false;

    //only need to know the in and out points. X of the vector is in point, Y of the vector is out point. (one of them is ALWAYS this node ID)
    public List<Vector2> nodeConnections;
    
    public PathTypes pathType;

    public Vector2 scrollViewVector;

    private Vector3 worldPosition;

    private ConnectionsManager conManager;

    public void SaveTriggerData(Rect _rect, int _ID, string _desc, List<bool> _showOptions, List<AudioClip> _audio, List<int> _delays, List<Vector2> _cons, PathTypes _type, Vector2 _svVec, Vector3 _worldPos)
    {
        rect = _rect;
        ID = _ID;
        stepDescription = _desc;
        showAudio = _showOptions;
        playedAudioClips = _audio;
        delays = _delays;
        nodeConnections = _cons;
        pathType = _type;
        scrollViewVector = _svVec;

        if(_worldPos != transform.position)
        {
            worldPosition = _worldPos;
            transform.position = _worldPos;
        }
    }

    private void EnableNextTriggers()
    {
        List<int> triggerID = new List<int>();
        if(nodeConnections.Count > 0)
        {
            foreach(Vector2 con in nodeConnections)
            {
                if(con.x == ID && con.y == ID)
                {
                    Debug.LogWarning("This trigger has itself as a connection.");
                }
                else if(con.x == ID)
                {
                    triggerID.Add((int)con.y);
                }
                else if(con.y == ID)
                {
                    triggerID.Add((int)con.x);
                }
            }
        }

        if(triggerID.Count > 0)
        {
            conManager.EnableTriggers(triggerID);
        }
    }

    public void InvokeTrigger()
    {
        EnableNextTriggers();
    }

    // Start is called before the first frame update
    void Awake()
    {
        conManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<ConnectionsManager>();
        conManager.LoadTriggerInfo(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
