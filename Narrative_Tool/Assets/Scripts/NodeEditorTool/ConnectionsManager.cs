using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionInfo
{
    public Connectable inPoint;
    public Connectable outPoint;

    public PathTypes connectionType;

    public ConnectionInfo(Connectable _inPoint, Connectable _outPoint, PathTypes _connectionType)
    {
        inPoint = _inPoint;
        outPoint = _outPoint;
        connectionType = _connectionType;
    }
}

public class ConnectionsManager : MonoBehaviour
{
    public List<ConnectionInfo> connections = new List<ConnectionInfo>();

    public void SaveConnections(List<ConnectionInfo> cons)
    {
        connections = cons;
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
