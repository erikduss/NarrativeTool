using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ConnectionInfo
{
    public int inPointID;
    public int outPointID;

    public PathTypes connectionType;

    public ConnectionInfo(int _inPoint, int _outPoint, PathTypes _connectionType)
    {
        inPointID = _inPoint;
        outPointID = _outPoint;
        connectionType = _connectionType;
    }
}

public class ConnectionsManager : MonoBehaviour
{
    [SerializeField] public List<ConnectionInfo> connections;

    public List<TriggerNodeInfo> inactiveTriggers = new List<TriggerNodeInfo>();
    private List<TriggerNodeInfo> activeTriggers = new List<TriggerNodeInfo>();

    public void SaveConnections(List<ConnectionInfo> cons)
    {
        connections = cons;
    }

    public void LoadTriggerInfo(TriggerNodeInfo trig)
    {
        inactiveTriggers.Add(trig);
        if(trig.ID != 0)
        {
            trig.gameObject.SetActive(false);
        }
    }

    private void DisableAllTriggers()
    {
        if(activeTriggers.Count > 0)
        {
            foreach(TriggerNodeInfo trigger in activeTriggers)
            {
                trigger.gameObject.SetActive(false);
                inactiveTriggers.Add(trigger);
            }
            activeTriggers.Clear();
        }
    }

    public void EnableTriggers(List<int> triggerIDs)
    {
        if(activeTriggers.Count > 0)
        {
            DisableAllTriggers();
        }

        foreach(int id in triggerIDs)
        {
            TriggerNodeInfo foundTrigger = inactiveTriggers.Find(x => x.ID == id);
            foundTrigger.gameObject.SetActive(true);
            activeTriggers.Add(foundTrigger);
            inactiveTriggers.Remove(foundTrigger);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
