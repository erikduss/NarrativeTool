using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathTypes
{
    NONE,
    GOOD,
    BAD
}

public interface Connectable
{
    Rect rect { get; set; }
    int ID { get; set; }
}

public class ToolInterfaces 
{
    
}
