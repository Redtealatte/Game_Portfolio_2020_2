using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {
    public int x, y, G, H;
    public int F { get { return G + H; } }

    public bool isWall;
    public Node parentNode = null;

    public Node(bool _isWall, int _x, int _y)
    {
        x = _x;
        y = _y;
        isWall = _isWall;
    }
}
