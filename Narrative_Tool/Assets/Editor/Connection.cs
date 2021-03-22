using System;
using UnityEditor;
using UnityEngine;

public class Connection
{
    public Connectable inPoint;
    public Connectable outPoint;
    public Action<Connection> OnClickRemoveConnection;

    public PathTypes connectionType;

    private bool isSelected = false;

    public Connection(Connectable starrRect, Connectable endRect, PathTypes connectType, Action<Connection> OnClickRemoveConnection)
    {
        inPoint = starrRect;
        outPoint = endRect;
        connectionType = connectType;
        this.OnClickRemoveConnection = OnClickRemoveConnection;
    }

    public void Draw()
    {
        if(connectionType == PathTypes.GOOD)
        {
            Handles.DrawBezier(
            inPoint.rect.center + new Vector2(inPoint.rect.width/2,0),
            outPoint.rect.center - new Vector2(inPoint.rect.width / 2, 0),
            inPoint.rect.center - Vector2.left * 75,
            outPoint.rect.center + Vector2.left * 75,
            Color.green,
            null,
            2f
            );
        }
        else if(connectionType == PathTypes.BAD)
        {
            Handles.DrawBezier(
            inPoint.rect.center + new Vector2(inPoint.rect.width / 2, 0),
            outPoint.rect.center - new Vector2(inPoint.rect.width / 2, 0),
            inPoint.rect.center - Vector2.left * 75,
            outPoint.rect.center + Vector2.left * 75,
            Color.red,
            null,
            2f
            );
        }
        else
        {
            Handles.DrawBezier(
            inPoint.rect.center + new Vector2(inPoint.rect.width / 2, 0),
            outPoint.rect.center - new Vector2(inPoint.rect.width / 2, 0),
            inPoint.rect.center - Vector2.left * 75f,
            outPoint.rect.center + Vector2.left * 75,
            Color.white,
            null,
            2f
            );
        }
        

        if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleHandleCap))
        {
            if (OnClickRemoveConnection != null)
            {
                OnClickRemoveConnection(this);
            }
        }
    }
}
