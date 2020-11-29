using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    //경로를 탐색할 맵의 크기를 지정.
    public Vector2Int bottomLeft, topRight;
    Vector2Int startPos, endPos;

    public List<Node> FinalList;
    List<Node> OpenList, CloseList;

    int sizeX, sizeY;
    Node[,] nodeArray;
    Node startNode, endNode, currentNode;

    //시작점, 종료점 게임 오브젝트.
    GameObject startPoint, endPoint;

    //경로를 그려줄 LineRender.
    LineRenderer path;
    int pathNumber;
    bool nextPath;

    public InputField speed;
    float drawSpeed;
    
    bool isDrawn;

    Vector2Int mousePos;
    Camera cam;
    int buttonNumber;

    public Tile wallTile;
    public Tilemap map;
    GameObject warningText;

    //방향.T : Top, B : Bottom, M : Middle  -  L : Left, R : Right, C : Center
    enum Direction
    {
        TL, TC, TR,
        ML, MC, MR,
        BL, BC, BR
    }

	// Use this for initialization
	void Start () {
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        path = GameObject.Find("Line").GetComponent<LineRenderer>();
        warningText = GameObject.Find("Canvas").transform.Find("Warning").gameObject;

        startPoint = GameObject.Find("Startpoint");
        endPoint = GameObject.Find("Endpoint");

        startPos = new Vector2Int(Mathf.RoundToInt(startPoint.transform.position.x), Mathf.RoundToInt(startPoint.transform.position.y));
        endPos = new Vector2Int(Mathf.RoundToInt(endPoint.transform.position.x), Mathf.RoundToInt(endPoint.transform.position.y));

        OpenList = new List<Node>();
        CloseList = new List<Node>();
        FinalList = new List<Node>();
    }	

    /*맵이 지정된 범위에서 타일맵 콜라이더가 적용된 레이어 Wall을 찾아내 해당 좌표를 Node 배열에 표기.
    이때, 전체 맵을 작성한다.*/
    void ReadMap()
    {
        sizeX = topRight.x - bottomLeft.x + 1;
        sizeY = topRight.y - bottomLeft.y + 1;

        nodeArray = new Node[sizeY, sizeX];
        for(int y = 0; y < sizeY; y++)
        {
            for(int x = 0; x < sizeX; x++)
            {
                bool isWall = false;
                foreach (Collider2D col in Physics2D.OverlapCircleAll(new Vector2(x + bottomLeft.x, y + bottomLeft.y), 0.4f))
                    if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        isWall = true;

                nodeArray[y, x] = new Node(isWall, x + bottomLeft.x, y + bottomLeft.y);
            }
        }
    }

    /*현재 노드의 8방위를 확인해서 G와 H를 계산하고 OpenList에 추가. 
     아래의 조건에 해당한다면 다음으로 넘어가고, 목표지점에 도달하면 CloseList에 추가하고 종료.
     OpenList에 포함된 노드라면 더 작은 F를 갖는 쪽으로 수정.*/
    bool Search()
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Direction dir = GetDirection(x, y);
                int arrayX = currentNode.x - bottomLeft.x;
                int arrayY = currentNode.y - bottomLeft.y;

                if (arrayX + x < 0 || arrayX + x >= sizeX) continue;
                if (arrayY + y < 0 || arrayY + y >= sizeY) continue;

                Node temp = nodeArray[y + arrayY, x + arrayX];                

                if (CloseList.Contains(temp)) continue;
                if (temp.isWall) continue;
                if (Corner(dir, arrayX, arrayY)) continue;
                if (Goal(temp))
                {
                    endNode.H = CalculateH(temp);
                    endNode.G = CalculateG(dir, currentNode.G);
                    endNode.parentNode = currentNode;
                    CloseList.Add(endNode);
                    return true;
                }
                foreach (Node node in OpenList)
                    if (temp.x == node.x && temp.y == node.y)
                    {
                        temp.H = node.H;
                        temp.G = node.G;
                    }
                if(temp.F == 0)
                {
                    temp.H = CalculateH(temp);
                    temp.G = CalculateG(dir, currentNode.G);
                    temp.parentNode = currentNode;
                    OpenList.Add(temp);
                }
                else
                {
                    int tempG = CalculateG(dir, currentNode.G);
                    if(temp.G > tempG)
                    {
                        temp.G = tempG;
                        temp.parentNode = currentNode;
                    }
                }

            }
        }
        return false;
    }

    //방향을 반환하는 함수.
    Direction GetDirection(int x, int y)
    {
        if (x == -1 && y == -1) return Direction.BL;
        else if (x == 0 && y == -1) return Direction.BC;
        else if (x == 1 && y == -1) return Direction.BR;
        else if (x == -1 && y == 0) return Direction.ML;
        else if (x == 0 && y == 0) return Direction.MC;
        else if (x == 1 && y == 0) return Direction.MR;
        else if (x == -1 && y == 1) return Direction.TL;
        else if (x == 0 && y == 1) return Direction.TC;
        else return Direction.TR;
    }

    //현재 노드로부터 입력 받은 방향으로 이동할 때, 벽이 존재하는 지 확인.
    bool Corner(Direction dir, int x, int y)
    {
        switch (dir)
        {
            case Direction.BL:
                if(nodeArray[y, x - 1].isWall || nodeArray[y - 1, x].isWall)
                    return true;             
                break;
            case Direction.BR:
                if (nodeArray[y, x + 1].isWall || nodeArray[y - 1, x].isWall)
                    return true;
                break;
            case Direction.TL:
                if (nodeArray[y, x - 1].isWall || nodeArray[y + 1, x].isWall)
                    return true;
                break;
            case Direction.TR:
                if (nodeArray[y, x + 1].isWall || nodeArray[y + 1, x].isWall)
                    return true;
                break;
        }
        return false;
    }

    //목표 지점인지 확인한다.
    bool Goal(Node comparison)
    {
        if (comparison.x == endNode.x && comparison.y == endNode.y)
            return true;

        return false;
    }

    //H값을 계산.
    int CalculateH(Node node)
    {
        Vector2Int t = new Vector2Int(node.x - endNode.x, node.y - endNode.y);
        if (t.x < 0) t.x = t.x * -1;
        if (t.y < 0) t.y = t.y * -1;
        return (t.x + t.y) * 10;
    }

    //G값을 계산.
    int CalculateG(Direction dir, int currentG)
    {
        if (dir == Direction.ML || dir == Direction.MR || dir == Direction.TC || dir == Direction.BC)
            return currentG + 10;
        else if (dir == Direction.TL || dir == Direction.TR || dir == Direction.BL || dir == Direction.BR)
            return currentG + 14;
        return -1;
    }

    /*경로탐색 시작. OpenList에 더이상 남아있는 노드가 없을 때까지 탐색.
     OpenList의 노드 중 가장 F가 작은 노드를 다음 currentNode로 정함.*/
    void Pathfinding()
    {
        ReadMap();
        startNode = nodeArray[startPos.y - bottomLeft.y, startPos.x - bottomLeft.x];
        endNode = nodeArray[endPos.y - bottomLeft.y, endPos.x - bottomLeft.x];

        currentNode = startNode;

        CloseList.Add(startNode);        

        while (!Search())
        {
            if (OpenList.Count == 0)
            {
                warningText.SetActive(true);
                isDrawn = false;
                return;
            }
            Node temp = null;
            foreach(Node element in OpenList)
            {
                if (temp == null)
                    temp = element;
                else if (temp.F > element.F)
                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                            if (currentNode.x + x == element.x && currentNode.y + y == element.y)
                                temp = element;
            }

            currentNode = temp;
            OpenList.Remove(temp);
            CloseList.Add(temp);
        }
        GetFinalList();              
    }

    //endNode부터 부모노드(parent)를 찾아 거슬러 올라가며 최종 경로를 확정.
    void GetFinalList()
    {
        Node temp = endNode;        
        while(temp != null)
        {
            FinalList.Add(temp);
            temp = temp.parentNode;
        }
        FinalList.Reverse();

        pathNumber = 0;

        nextPath = true;
        isDrawn = true;
    }

    //startPoint부터 endPoint까지의 경로 그리기. LineRender 이용.
    void DrawPath()
    {
        if (pathNumber >= FinalList.Count - 1)
        {
            isDrawn = false;
            nextPath = false;
            pathNumber = 0;
            return;
        }        

        int dx = 0, dy = 0;
        path.positionCount = pathNumber + 2;

        if (nextPath)
        {
            nextPath = false;
            path.SetPosition(pathNumber, new Vector3(FinalList[pathNumber].x, FinalList[pathNumber].y, 0));
            path.SetPosition(pathNumber + 1, new Vector3(FinalList[pathNumber].x, FinalList[pathNumber].y, 0));
        }

        if (FinalList[pathNumber + 1].x - FinalList[pathNumber].x == 1)
            dx = 1;
        else if (FinalList[pathNumber + 1].x - FinalList[pathNumber].x == -1)
            dx = -1;
        if (FinalList[pathNumber + 1].y - FinalList[pathNumber].y == 1)
            dy = 1;
        else if (FinalList[pathNumber + 1].y - FinalList[pathNumber].y == -1)
            dy = -1;
        path.SetPosition(pathNumber + 1, new Vector3((path.GetPosition(pathNumber + 1).x + Time.deltaTime * drawSpeed * dx), (path.GetPosition(pathNumber + 1).y + Time.deltaTime * drawSpeed * dy), 0.0f));

        if ((dx == 1 && path.GetPosition(pathNumber + 1).x >= FinalList[pathNumber + 1].x) || (dx == -1 && path.GetPosition(pathNumber + 1).x <= FinalList[pathNumber + 1].x)
            || (dy == 1 && path.GetPosition(pathNumber + 1).y >= FinalList[pathNumber + 1].y) || (dy == -1 && path.GetPosition(pathNumber + 1).y <= FinalList[pathNumber + 1].y))
        {
            nextPath = true;
            pathNumber++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
            warningText.SetActive(false);
        if (Input.GetMouseButton(0))
        {
            //mousePos = new Vector2Int(Mathf.RoundToInt(Input.mousePosition.x), Mathf.RoundToInt(Input.mousePosition.y));
            Vector2 worldMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos = new Vector2Int(Mathf.RoundToInt(worldMousePos.x), Mathf.RoundToInt(worldMousePos.y));
            if (mousePos.x < bottomLeft.x || mousePos.x > topRight.x || mousePos.y < bottomLeft.y || mousePos.y > topRight.y)
                return;
            switch (buttonNumber)
            {
                case 1:
                    SetStartPoint();
                    break;
                case 2:
                    SetEndPoint();
                    break;
                case 3:
                    SetWall();
                    break;
                case 4:
                    EraseWall();
                    break;
            }
        }
        if (isDrawn)
            DrawPath();
    }

    //버튼의 클릭이벤트 관련 함수들.
    public void ClickStartPointBtn()
    {
        ClickResetBtn();
        buttonNumber = 1;        
        ReadMap();
    }

    public void ClickEndPointBtn()
    {
        ClickResetBtn();
        buttonNumber = 2;        
        ReadMap();
    }

    public void ClickBlockBtn()
    {
        ClickResetBtn();
        buttonNumber = 3;        
        ReadMap();
    }

    public void ClickRoadBtn()
    {
        ClickResetBtn();
        buttonNumber = 4;        
        ReadMap();
    }

    public void ClickPlayBtn()
    {
        ClickResetBtn();
        if (!isDrawn)
        {
            path.positionCount = 0;
            if (speed.text == "")
                drawSpeed = 1.0f;
            else
                drawSpeed = float.Parse(speed.text);
            Pathfinding();
        }        
    }

    public void ClickResetBtn()
    {
        buttonNumber = 0;
        OpenList.Clear();
        CloseList.Clear();
        FinalList.Clear();
        path.positionCount = 0;
        isDrawn = false;
    }

    //StartPoint를 마우스 클릭 좌표로 이동.
    void SetStartPoint()
    {        
        if(!nodeArray[mousePos.y - bottomLeft.y, mousePos.x - bottomLeft.x].isWall && mousePos != endPos)
        {
            startPos = mousePos;
            startPoint.transform.position = new Vector2(mousePos.x, mousePos.y);
        }            
    }

    //EndPoint를 마우스 클릭좌표로 이동.
    void SetEndPoint()
    {        
        if (!nodeArray[mousePos.y - bottomLeft.y, mousePos.x - bottomLeft.x].isWall && mousePos != startPos)
        {
            endPos = mousePos;
            endPoint.transform.position = new Vector2(mousePos.x, mousePos.y);
        }            
    }

    //타일맵에 Wall 타일을 추가. 좌표는 마우스 클릭 좌표를 카메라를 통해 월드좌표로 변환.
    void SetWall()
    {        
        if(!nodeArray[mousePos.y - bottomLeft.y, mousePos.x - bottomLeft.x].isWall && mousePos != startPos && mousePos != endPos)
        {
            Vector3Int currentCell = map.WorldToCell(cam.ScreenToWorldPoint(Input.mousePosition));
            map.SetTile(currentCell, wallTile);
        }
    }

    //클릭 좌표의 Wall 타일을 제거.
    void EraseWall()
    {
        if (nodeArray[mousePos.y - bottomLeft.y, mousePos.x - bottomLeft.x].isWall)
        {
            Vector3Int currentCell = map.WorldToCell(cam.ScreenToWorldPoint(Input.mousePosition));
            map.SetTile(currentCell, null);
        }
    }    
}
