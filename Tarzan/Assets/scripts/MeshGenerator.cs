using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour{

    public int wallHeight = 5;
    public int textureTileSize = 10;

    public bool AreCollidersPolygons = true;
    public bool liquidEffector = false;

    public Material[] materials;

    public bool filterOnMaterialType;
    public int filterMaterialType = -1;

    public GameObject cavePrefab;
    public GameObject wallPrefab;

    public PhysicsMaterial2D physicsMaterial2D;

    public Material invisibleMaterial;

    Chunk[,] chunks;

    bool[,] isRendering;

    int simultaneousThreads = 0;



    public void GenerateMesh(int[,] map, int startX, int startY, int chunkSize, float squareSize, bool forceClear = true)
    {
        StartCoroutine(ThreadMesh(map, startX, startY, chunkSize, squareSize, forceClear));
    }

    IEnumerator ThreadMesh(int[,] map, int startX, int startY, int chunkSize, float squareSize, bool forceClear)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        int borderSeemX = startX < width-chunkSize ? 1 : 0;
        int borderSeemY = startY < height-chunkSize ? 1 : 0;

        int chunksX = width / chunkSize;
        int chunksY = height / chunkSize;
        int chunkIndexX = startX / chunkSize;
        int chunkIndexY = startY / chunkSize;


        if (chunks == null)
        {
            chunks = new Chunk[chunksX, chunksY];
        }
        else if (forceClear)
        {
            foreach (Chunk chunkItem in chunks)
            {
                if (chunkItem.caveMesh != null)
                    Destroy(chunkItem.caveMesh.gameObject);

                if (chunkItem.wallMesh != null)
                    Destroy(chunkItem.wallMesh.gameObject);

                if (chunkItem.polygonColliders != null)
                {
                    foreach (PolygonCollider2D collider in chunkItem.polygonColliders)
                    {
                        if (collider != null)
                            Destroy(collider);
                    }
                }
            }
        }

        Chunk chunk = chunks[chunkIndexX, chunkIndexY];

        if (chunk == null || forceClear)
        {
            chunk = new Chunk();
            chunks[chunkIndexX, chunkIndexY] = chunk;
        }

        while (chunk.isRendering)
        {
            yield return null;
        }

        chunk.isRendering = true;

        chunk.Clear();

        if (chunk.squareGrid == null)
        {
            chunk.squareGrid = new SquareGrid(map, startX, startY, chunkSize, borderSeemX, borderSeemY, squareSize);
        }

        SquareGrid squareGrid = chunk.squareGrid;
        //squareGrid = new SquareGrid(map, startX, startY, chunkSize, borderSeemX, borderSeemY, squareSize);
        squareGrid.Update(map, startX, startY, chunkSize, borderSeemX, borderSeemY, squareSize, filterOnMaterialType, filterMaterialType);

        if (!squareGrid.dirty)
        {
            chunk.isRendering = false;
            yield break;
        }

        chunk.vertices = new List<Vector3>();
        chunk.triangles = new List<int>[materials.Length];

        List<int>[] triangles = chunk.triangles;
        List<Vector3> vertices = chunk.vertices;

        int len = triangles.Length;
        for (int i = 0; i < len; i++)
        {
            triangles[i] = new List<int>();
        }


        int endX = startX + chunkSize + borderSeemX-1;
        int endY = startY + chunkSize + borderSeemY-1;
        chunk.StartTriangulate(startX, startY, endX, endY);

        //print("Start Triangulate");
        yield return StartCoroutine(chunk.WaitFor());

        MeshFilter cave = chunk.caveMesh;
        if (cave == null)
        {
            GameObject caveGO = Instantiate(cavePrefab);
            cave = caveGO.GetComponent<MeshFilter>();
            cave.transform.SetParent(this.transform, false);
            chunk.caveMesh = cave;
            cave.mesh = new Mesh();
        }


        Mesh mesh = cave.sharedMesh;
        mesh.Clear();
        mesh.SetVertices(vertices);

        int subMeshCount = 0;
        len = triangles.Length;
        for ( int i = 0; i < len; i++)
        {
            if (triangles[i].Count > 0)
                subMeshCount += 1;
        }

        mesh.subMeshCount = subMeshCount;
        MeshRenderer meshRenderer = cave.GetComponent<MeshRenderer>();
        Material[] newMaterials = new Material[subMeshCount];

        subMeshCount = 0;
        for ( int i = 0; i < len; i++)
        {
            List<int> triangleList = triangles[i];
            if (triangles[i].Count > 0)
            {
                mesh.SetTriangles(triangleList, subMeshCount);
                newMaterials[subMeshCount] = materials[i];
                subMeshCount++;
            }
        }

        meshRenderer.materials = newMaterials;

        int tileAmount = textureTileSize;
        Vector2[] uvs = new Vector2[vertices.Count];

        for (int i=0; i < uvs.Length; i++) {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }
        /*
        len = vertices.Count;
        float size = width > height ? width : height;
        float sizeFactor = size/2*squareSize;
        for (int i =0; i < len; i ++) 
        {
            float percentX = Mathf.InverseLerp(-sizeFactor,sizeFactor,vertices[i].x);
            float percentY = Mathf.InverseLerp(-sizeFactor,sizeFactor,vertices[i].z);
            uvs[i] = new Vector2(percentX, percentY);

        }*/
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        CreateWallMesh(chunk, startX, startY, width, height, chunkSize);

        chunk.isRendering = false;
    }

    void CreateWallMesh(Chunk chunk, int startX, int startY, int width, int height, int chunkSize)
    {
        List<Vector3> wallVertices = new List<Vector3>();


        List<int>[] wallTriangles;

        if (invisibleMaterial)
            wallTriangles = new List<int>[materials.Length+1];
        else
            wallTriangles = new List<int>[materials.Length];

        for (int i = 0; i < wallTriangles.Length; i++)
        {
            wallTriangles[i] = new List<int>();
        }

        MeshFilter wallMesh = chunk.wallMesh;
        if (wallMesh == null)
        {
            GameObject caveGO = Instantiate(wallPrefab);
            wallMesh = caveGO.GetComponent<MeshFilter>();
            wallMesh.transform.SetParent(this.transform, false);
            chunk.wallMesh = wallMesh;
            wallMesh.mesh = new Mesh();
        }

        chunk.CalcultateMeshOutlines();

        List<List<int>> outlines = chunk.outlines;
        List<Vector3> vertices = chunk.vertices;
        Dictionary<int, int> materialDictionary = chunk.materialDictionary;

        int len = 0;
        Vector3 direction = Vector3.up;
        foreach (List<int> outline in outlines)
        {
            int vertexDirection = outline[1] - outline[0];
            if (vertexDirection == 1)
                outline.Reverse();

            len = outline.Count-1;
            for (int i = 0; i < len; i++)
            {
                int startIndex = wallVertices.Count;


                //print("vertices[outline[i]] : "+vertices[outline[i]]);


                wallVertices.Add(vertices[outline[i]]); //left
                wallVertices.Add(vertices[outline[i+1]]); //right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); //bottom left
                wallVertices.Add(vertices[outline[i+1]] - Vector3.up * wallHeight); // bottom right

                int vertice0 = startIndex + 0;
                int vertice1 = startIndex + 1;
                int vertice2 = startIndex + 2;
                int vertice3 = startIndex + 3;

                int maxMaterial = Mathf.Max(materialDictionary[outline[i]], materialDictionary[outline[i+1]]);

                if (maxMaterial == -1)
                    continue;

                //dont draw walls inside the if invisible material
                if (invisibleMaterial != null)
                {
                    if ((int)vertices[outline[i]].x % (int)(chunkSize) == -3 && (int)vertices[outline[i+1]].x % (int)(chunkSize) == -3)
                    {
                        maxMaterial = materials.Length;
                    }

                    if ((int)vertices[outline[i]].x % (int)(chunkSize) == 1 && (int)vertices[outline[i+1]].x % (int)(chunkSize) == 1)
                    {
                        maxMaterial = materials.Length;
                    }

                    if ((int)vertices[outline[i]].z % (int)(chunkSize) == 1 && (int)vertices[outline[i+1]].z % (int)(chunkSize) == 1)
                    {
                        maxMaterial = materials.Length;
                    }
                }

                wallTriangles[maxMaterial].Add(vertice0);
                wallTriangles[maxMaterial].Add(vertice1);
                wallTriangles[maxMaterial].Add(vertice3);

                wallTriangles[maxMaterial].Add(vertice3);
                wallTriangles[maxMaterial].Add(vertice2);
                wallTriangles[maxMaterial].Add(vertice0);

            }
        }
        Mesh mesh = wallMesh.sharedMesh;
        mesh.Clear();
        mesh.vertices = wallVertices.ToArray();

        int subMeshCount = 0;
        len = wallTriangles.Length;
        for ( int i = 0; i < len; i++)
        {
            if (wallTriangles[i].Count > 0)
                subMeshCount += 1;
        }
        mesh.subMeshCount = subMeshCount;
        MeshRenderer meshRenderer = wallMesh.GetComponent<MeshRenderer>();
        Material[] newMaterials = new Material[subMeshCount];

        subMeshCount = 0;
        for ( int i = 0; i < len; i++)
        {
            List<int> triangleList = wallTriangles[i];
            if (wallTriangles[i].Count > 0)
            {
                mesh.SetTriangles(triangleList, subMeshCount);
                if (i == materials.Length)
                {
                    newMaterials[subMeshCount] = invisibleMaterial;
                }
                else
                    newMaterials[subMeshCount] = materials[i];
                
                subMeshCount++;
            }
        }
        meshRenderer.materials = newMaterials;

        int tileAmount = textureTileSize;
        Vector2[] uvs = new Vector2[wallVertices.Count];
        len = wallVertices.Count;
        float totalDistance = 0;
        float lastDistance = 0;
        for (int j=0, i =0; i < len; i++) 
        {
            Vector3 vertice = wallVertices[i];
           
            // We want to compare distance between 0-1, 4-5, etc...
            // 2--3,6--7
            // |   |   |
            // 0--1,4--5
            if (i % 4 == 1) // do 0-1, 4-5, etc..
            {
                Vector2 diff = new Vector2();
                diff.x = vertice.x - wallVertices[i-1].x;
                diff.y = vertice.z - wallVertices[i-1].z;
                lastDistance = diff.magnitude;
                totalDistance += lastDistance;
                uvs[i] = new Vector2(totalDistance, vertice.y);
            }
            else if (i % 4 == 2) // go back if 2,6,etc...
            {
                uvs[i] = new Vector2(totalDistance-lastDistance, vertice.y);
            }
            else
            {
                uvs[i] = new Vector2(totalDistance, vertice.y);
            }
        }

        mesh.uv = uvs;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        if (AreCollidersPolygons)
            CreateCollisionPolygons(chunk);
        else
            CreateCollisionEdges(chunk);
    }

    void CreateCollisionEdges(Chunk chunk)
    {
        List<EdgeCollider2D> edgeColliderList = chunk.edgeColliders;
        List<List<int>> outlines = chunk.outlines;
        List<Vector3> vertices = chunk.vertices;

        if (edgeColliderList == null)
        {
            edgeColliderList = new List<EdgeCollider2D>();
            chunk.edgeColliders = edgeColliderList;
        }

        foreach (EdgeCollider2D collider in edgeColliderList)
        {
            Destroy(collider);
        }

        foreach (List<int> outline in outlines) 
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            edgeCollider.sharedMaterial = physicsMaterial2D;
            edgeCollider.isTrigger = liquidEffector;
            edgeColliderList.Add(edgeCollider);

            List<Vector2> edgePoints = new List<Vector2>();

            int len = outline.Count;
            for (int i =0; i < len; i ++) 
            {
                edgePoints.Add(new Vector2(vertices[outline[i]].x,vertices[outline[i]].z));
            }
            edgeCollider.points = edgePoints.ToArray();
        }
    }

    void CreateCollisionPolygons(Chunk chunk)
    {
        List<PolygonCollider2D> edgeColliderList = chunk.polygonColliders;
        List<List<int>> outlines = chunk.outlines;
        List<Vector3> vertices = chunk.vertices;

        if (edgeColliderList == null)
        {
            edgeColliderList = new List<PolygonCollider2D>();
            chunk.polygonColliders = edgeColliderList;
        }

        foreach (PolygonCollider2D collider in edgeColliderList)
        {
            Destroy(collider);
        }

        foreach (List<int> outline in outlines) 
        {
            PolygonCollider2D edgeCollider = gameObject.AddComponent<PolygonCollider2D>();
            edgeCollider.sharedMaterial = physicsMaterial2D;
            edgeCollider.isTrigger = liquidEffector;
            edgeColliderList.Add(edgeCollider);

            List<Vector2> edgePoints = new List<Vector2>();

            int len = outline.Count;
            for (int i =0; i < len; i ++) 
            {
                edgePoints.Add(new Vector2(vertices[outline[i]].x,vertices[outline[i]].z));
            }
            edgeCollider.points = edgePoints.ToArray();
        }
    }


    void SwitchUp(ref Vector3 pos)
    {
        float yy = pos.y;
        pos.y = pos.z;
        pos.z = yy;
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        public int[] vertices; 

        public Triangle(int vertexIndexA, int vertexIndexB, int vertexIndexC)
        {
            this.vertexIndexA = vertexIndexA;
            this.vertexIndexB = vertexIndexB;
            this.vertexIndexC = vertexIndexC;

            vertices = new int[3];
            vertices[0] = vertexIndexA;
            vertices[1] = vertexIndexB;
            vertices[2] = vertexIndexC;
        }

        public int this[int i]
        {
            get {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    class Chunk : ThreadedJob
    {
        public bool isRendering = false;

        public SquareGrid squareGrid;

        public MeshFilter caveMesh;
        public MeshFilter wallMesh;
        public List<PolygonCollider2D> polygonColliders;
        public List<EdgeCollider2D> edgeColliders;

        public List<Vector3> vertices;
        public List<int>[] triangles;

        public Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
        public Dictionary<int, int> materialDictionary = new Dictionary<int, int>();
        public List<List<int>> outlines = new List<List<int>>();
        public HashSet<int> checkedVertices = new HashSet<int>();

        int startX;
        int startY;
        int endX;
        int endY;

        protected override void ThreadFunction()
        {
            for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                {
                    TriangulateSquare(squareGrid.squares[x,y]);
                }
            }
        }

        protected override void OnFinished()
        {
            //print("Triangulation complete!");
        }

        public void StartTriangulate(int startX, int startY, int endX, int endY)
        {
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;

            Start();
        }

        void TriangulateSquare(Square square)
        {
            switch (square.configuration)
            {
                case 0:
                    break;

                    //1 point
                case 1:
                    MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                    break;
                case 2:
                    MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                    break;
                case 4:
                    MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                    break;
                case 8:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                    break;

                    //2 points
                case 3:
                    MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 6:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                    break;
                case 9:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                    break;
                case 12:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                    break;
                case 5:
                    MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                    break;
                case 10:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    break;

                    //3 points
                case 7:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 11:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.topRight, square.bottomRight, square.centreBottom, square.centreLeft, square.topLeft);
                    break;

                    //4 point
                case 15:
                    MeshFromPoints(square.bottomRight, square.bottomLeft, square.topLeft, square.topRight);
                    //checkedVertices.Add(square.bottomLeft.vertexIndex);
                    //checkedVertices.Add(square.topLeft.vertexIndex);
                    //checkedVertices.Add(square.topRight.vertexIndex);
                    //checkedVertices.Add(square.bottomRight.vertexIndex);
                    break;
            }
        }

        void MeshFromPoints(params Node[] points)
        {
            AssignVertices(points);

            int len = points.Length;

            if (len >= 3)
                CreateTriangle(points[0], points[1], points[2]);

            if (len >= 4)
                CreateTriangle(points[0], points[2], points[3]);

            if (len >= 5)
                CreateTriangle(points[0], points[3], points[4]);

            if (len >= 6)
                CreateTriangle(points[0], points[4], points[5]);

        }

        void AssignVertices(Node[] points)
        {
            int len = points.Length;
            for (int i = 0; i < len; i++)
            {
                Node node = points[i];
                if (node.vertexIndex == -1)
                {
                    node.vertexIndex = vertices.Count;
                    materialDictionary[node.vertexIndex] = node.material;
                    vertices.Add(node.position);
                }
            }
        }

        void CreateTriangle(Node a, Node b, Node c)
        {
            int maxMaterial = Mathf.Max(a.material, b.material, c.material);
            triangles[maxMaterial].Add(a.vertexIndex);
            triangles[maxMaterial].Add(b.vertexIndex);
            triangles[maxMaterial].Add(c.vertexIndex);

            Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex); //TODO: send in material to Triangle

            AddTriangleToDictionary(triangle.vertexIndexA, triangle);
            AddTriangleToDictionary(triangle.vertexIndexB, triangle);
            AddTriangleToDictionary(triangle.vertexIndexC, triangle);

        }

        void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
        {
            if (triangleDictionary.ContainsKey(vertexIndexKey))
            {
                triangleDictionary[vertexIndexKey].Add(triangle);
            }
            else
            {
                List<Triangle> triangleList = new List<Triangle>();
                triangleList.Add(triangle);
                triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        public void CalcultateMeshOutlines()
        {
            int len = vertices.Count;
            for (int vertexIndex = 0; vertexIndex < len; vertexIndex++)
            {
                if (!checkedVertices.Contains(vertexIndex))
                {
                    int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex != -1)
                    {
                        checkedVertices.Add(vertexIndex);
                        List<int> newOutline = new List<int>();

                        newOutline.Add(vertexIndex);
                        outlines.Add(newOutline);

                        int lastVertexIndex = FollowOutline(newOutlineVertex, outlines.Count-1);

                        if (IsOutlineEdge(vertexIndex, lastVertexIndex))
                            outlines[outlines.Count-1].Add(vertexIndex);
                    }
                }
            }

            JoinOutlines();
        }

        void JoinOutlines()
        {
            List<List<int>> outlinesToRemove = new List<List<int>>();
            for (int i = 0; i < outlines.Count; i++)
            {
                List<int> outlineA = outlines[i];
                for (int j = outlines.Count-1; j > i; j--)
                {
                    List<int> outlineB = outlines[j];
                    if (outlineA == outlineB)
                        continue;

                    if (IsOutlineEdge(outlineA[0], outlineB[0]) ||
                        IsOutlineEdge(outlineA[outlineA.Count-1], outlineB[0]) ||
                        IsOutlineEdge(outlineA[0], outlineB[outlineB.Count-1]) ||
                        IsOutlineEdge(outlineA[outlineA.Count-1], outlineB[outlineB.Count-1]))
                    {
                        outlineB.Reverse();
                        outlineA.InsertRange(0, outlineB);
                        outlinesToRemove.Add(outlineB);
                    }
                }
            }

            foreach (List<int> outline in outlinesToRemove)
            {
                outlines.Remove(outline);
            }

            if (outlinesToRemove.Count >= 1)
                JoinOutlines();
        }

        int FollowOutline(int vertexIndex, int outlineIndex)
        {
            outlines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int newVertexIndex = GetConnectedOutlineVertex(vertexIndex);

            if (newVertexIndex != -1)
            {
                return FollowOutline(newVertexIndex, outlineIndex);
            }

            return vertexIndex;
        }

        int GetConnectedOutlineVertex(int vertexIndex)
        {
            List<Triangle> triangles = triangleDictionary[vertexIndex];

            int len = triangles.Count;
            for (int i = 0; i < len; i++)
            {
                Triangle triangle = triangles[i];

                for (int j = 0; j < 3; j++)
                {
                    int vertexB = triangle[j];

                    if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(vertexIndex, vertexB))
                        {
                            return vertexB;
                        }
                    }
                }
            }

            return -1;
        }


        bool IsOutlineEdge(int vertexA, int vertexB)
        {
            List<Triangle> trianglesA = triangleDictionary[vertexA];

            int sharedTriangleCount = 0;
            int len = trianglesA.Count;
            for (int i = 0; i < len; i++)
            {
                if (trianglesA[i].Contains(vertexB))
                {
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1)
                    {
                        break;
                    }
                }
            }
            return sharedTriangleCount == 1;
        }
        public void Clear()
        {
            triangleDictionary.Clear();
            materialDictionary.Clear();
            checkedVertices.Clear();
            outlines.Clear();
        }
    }

    public class SquareGrid
    {
        public Square[,] squares;

        int nodeCountX;
        int nodeCountY;
        float mapWidth;
        float mapHeight;
        ControlNode[,] controlNodes;

        public bool dirty;

        public SquareGrid(int[,] map, int startX, int startY, int chunkSize, int borderSeemX, int borderSeemY, float squareSize)
        {
            nodeCountX = map.GetLength(0);
            nodeCountY = map.GetLength(1);
            mapWidth = nodeCountX * squareSize;
            mapHeight = nodeCountY * squareSize;

            int controlNodeLengthX = chunkSize + borderSeemX;
            int controlNodeLengthY = chunkSize + borderSeemY;

            controlNodes = new ControlNode[controlNodeLengthX, controlNodeLengthY];

            for (int x = 0; x < controlNodeLengthX; x++)
            {
                for (int y = 0; y < controlNodeLengthY; y++)
                {
                    Vector3 pos = new Vector3(-mapWidth/2f + (startX+x) * squareSize + squareSize / 2f, 0, -mapHeight/2f + (startY+y) * squareSize + squareSize / 2f);

                    controlNodes[x, y] = new ControlNode(pos, squareSize);
                }
            }

            int squareLengthX = chunkSize + borderSeemX -1;
            int squareLengthY = chunkSize  + borderSeemY -1;

            squares = new Square[squareLengthX, squareLengthY];

            for (int x = 0; x < squareLengthX; x++)
            {
                for (int y = 0; y < squareLengthY; y++)
                {
                    squares[x,y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
                }
            }

        }

        public void Update(int[,] map, int startX, int startY, int chunkSize, int borderSeemX, int borderSeemY, float squareSize, bool filterOnMaterialType, int filterMaterialType)
        {
            dirty = false;

            int controlNodeLengthX = chunkSize + borderSeemX;
            int controlNodeLengthY = chunkSize + borderSeemY;
            for (int x = 0; x < controlNodeLengthX; x++)
            {
                for (int y = 0; y < controlNodeLengthY; y++)
                {
                    int materialType = map[startX+x,startY+y];
                    if (filterOnMaterialType)
                    {
                        if (materialType == filterMaterialType)
                        {
                            materialType = 1;
                        }
                        else
                        {
                            materialType = 0;
                        }
                    }

                    int currentMaterial = controlNodes[x, y].material;

                    if (currentMaterial == -2 || materialType != currentMaterial+1)
                        dirty = true;

                    controlNodes[x, y].Update(materialType);
                }
            }

            if (!dirty)
                return;

            int squareLengthX = chunkSize + borderSeemX -1;
            int squareLengthY = chunkSize  + borderSeemY -1;
            for (int x = 0; x < squareLengthX; x++)
            {
                for (int y = 0; y < squareLengthY; y++)
                {
                    squares[x,y].Update();
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centreTop, centreRight, centreLeft, centreBottom;

        public int configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomLeft = bottomLeft;
            this.bottomRight = bottomRight;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreLeft = bottomLeft.above;
            centreBottom = bottomLeft.right;

        }

        public void Update()
        {
            configuration = 0;

            centreTop.vertexIndex = -1;
            centreRight.vertexIndex = -1;
            centreLeft.vertexIndex = -1;
            centreBottom.vertexIndex = -1;
            topLeft.vertexIndex = -1;
            topRight.vertexIndex = -1;
            bottomRight.vertexIndex = -1;
            bottomLeft.vertexIndex = -1;

            //if (centreTop.material == -1)
            centreTop.material =  Mathf.Max(topLeft.material, topRight.material);
            //if (centreRight.material == -1)
            centreRight.material = Mathf.Max(bottomRight.material, topRight.material);
            //if (centreLeft.material == -1)
            centreLeft.material = Mathf.Max(bottomLeft.material, topLeft.material);
            //if (centreBottom.material == -1)
            centreBottom.material = Mathf.Max(bottomRight.material, bottomLeft.material);

            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

    public class Node
    {
        public int material = -2;
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 pos)
        {
            position = pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active = false;
        public Node above, right;

        public ControlNode(Vector3 pos, float squareSize) : base(pos)
        {
            above = new Node(position + Vector3.forward * squareSize/2f);
            right = new Node(position + Vector3.right * squareSize/2f);

        }

        public void Update(int materialType)
        {
            this.active = materialType > 0;
            this.material = materialType-1;

            above.material = material;
            right.material = material;
        }

    }
}

