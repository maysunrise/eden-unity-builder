using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
/// <summary>
/// Main world class, chunk management, loading chunks
/// </summary>
public class World : MonoBehaviour
{

    public static World Instance;

    [Tooltip("World name")]
    public string Name;

    [Header("Terrain settings")]
    public WorldType Type;

    [Tooltip("World seed")]
    public int WorldSeed = 1;

    public TerrainMaterial TerrainMaterialSettings;

    [Tooltip("Size of chunks. Recommended use 16")]
    public static int ChunkSize = 16; // recomended use 16

    [Header("Generation settings")]
    public int chunksPerFrame = 4;

    public float updateDistance = 16f;

    public float generationDistance = 128f;

    public Vector4 PlayerPosition = new Vector3();

    public Vector4 HomePosition = new Vector3();

    public Vector4 SpawnPosition = new Vector3();

    public GameObject ChunkPrefab;

    [Tooltip("Chunk list")]
    public Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();

    public Text LoadingText;

    [Tooltip("The queue of chunks to be destroyed. In order not to block the main thread")]
    public List<GameObject> QueueDestroy = new List<GameObject>();

    private bool _firstLoading;

    private Vector3 _curPos = Vector3.zero;

    private long _chunkFrame = 0;

    public static int TREE_SPACING = 2;

    public Text BottomTextInfo;

    public Collider SegmentBounds;

    private bool _hasLoadSegment;

    private EdenWorldDecoder _decoder;

    public Vector3 SegmentPos;

    public bool _segmentLoaded;

    public bool NewWorldFormat; // false - old, true - new

    [Serializable]
    public class TerrainMaterial
    {
        public Material SolidMaterial;
        public Material TransparentMaterial;
        public Material AnimatedMaterial;
        public float Tiling;
        public float UVPadding;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Chunks = new Dictionary<Vector3Int, Chunk>();

        if (WorldSeed == 0)
        {
            Type = WorldType.Flat;
        }
        else
        {
            Type = WorldType.Normal;
        }

        _hasLoadSegment = true;
        _decoder = EdenWorldDecoder.Instance;
        Teleport(SpawnPosition, SpawnPosition.w);

        LoadWorldSegment(new Vector2Int((int)SpawnPosition.x, (int)SpawnPosition.z));
    }

    public void LoadWorldSegment(Vector2Int pos)
    {
        Vector3 p = new Vector3(pos.x, 0, pos.y);
        _decoder.StartCoroutine(_decoder.LoadSegment2(p));
    }
    public void SaveWorldSegment(Vector2Int pos)
    {
        Vector3 p = new Vector3(pos.x, 0, pos.y);
        _decoder.StartCoroutine(_decoder.SaveSegment(p));
    }

    private void Update()
    {
        generationDistance = 64f;

        if (_firstLoading) // first loading for world, init all
        {
            int loadProgress = (int)Mathf.Clamp(((Chunks.Count / generationDistance) * 100), 0, 100); // progressbar
            LoadingText.text = "Loading world... " + loadProgress + "%";

            if (_segmentLoaded)
            {
                LoadingText.text = "";
                GameController.Instance.TogglePlayerController(true);
                //  GameController.Instance.ActivatePlayer();
                GameController.Instance.CloseMainMenu();
                GameController.Instance.ToggleMainCamera(true);
                _firstLoading = false;
            }
        }
        else
        {
            if (_segmentLoaded && !SegmentBounds.bounds.Contains(PlayerPosition))
            {
                BottomTextInfo.text = "Loading area...";
                GameController.Instance.TogglePlayerController(false);
                //GameController.Instance.DeactivatePlayer();
                Vector2Int center = new Vector2Int((int)PlayerPosition.x, (int)PlayerPosition.z);
                _segmentLoaded = false;
                SaveWorldSegment(center);
                //LoadWorldSegment(center);
            }
        }
        SegmentPos = SegmentBounds.transform.position;
    }

    public bool ContainsIndexSegment(Vector3Int index) =>
      index.x >= SegmentPos.x - 68 && index.x < SegmentPos.x + 68 &&
      index.y >= 0 && index.y < 256 &&
      index.z >= SegmentPos.z - 68 && index.z < SegmentPos.z + 68;

    private void OnEnable()
    {
        _firstLoading = true;
        Name = Name.Replace(".eden", "");
    }

    private void LateUpdate()
    {
        PlayerPosition = GameController.Instance.Player.transform.position;
        _curPos = PlayerPosition;
        chunksPerFrame = 1024;
    }

    public bool ChunkExists(Vector3 p)
    {
        return ChunkExists((int)p.x, (int)p.y, (int)p.z);
    }

    public Chunk FindChunk(Vector3 p)
    {
        return FindChunk((int)p.x, (int)p.y, (int)p.z);
    }

    public bool ChunkExists(int x, int y, int z)
    {
        x = Mathf.FloorToInt((float)x / ChunkSize) * ChunkSize;
        y = Mathf.FloorToInt((float)y / ChunkSize) * ChunkSize;
        z = Mathf.FloorToInt((float)z / ChunkSize) * ChunkSize;

        Chunk c = null; Chunks.TryGetValue(new Vector3Int(x, y, z), out c);
        return c != null;
    }

    public bool ContainsIndex(Vector3Int pos) // Check contains index global
    {
        Chunk c = FindChunk(pos.x, pos.y, pos.z);
        if (c != null)
        {
            return c.ContainsIndex(pos);
        }
        else
        {
            return false;
        }
    }

    public bool ContainsIndex(int x, int y, int z) // Check contains index global
    {
        Chunk c = FindChunk(x, y, z);
        if (c != null)
        {
            return c.ContainsIndex(new Vector3Int(x, y, z));
        }
        else
        {
            return false;
        }
    }

    public bool ContainsBlock(int x, int y, int z)
    {
        return GetBlock(x, y, z).BlockType != BlockType.Air;
    }

    public void DestroyChunk(int x, int y, int z)
    {
        x = Mathf.FloorToInt((float)x / ChunkSize) * ChunkSize;
        y = Mathf.FloorToInt((float)y / ChunkSize) * ChunkSize;
        z = Mathf.FloorToInt((float)z / ChunkSize) * ChunkSize;
        Chunk c;
        Chunks.TryGetValue(new Vector3Int(x, y, z), out c);
        if (c != null)
        {
            QueueDestroy.Add(c.gameObject);
            Chunks.Remove(new Vector3Int(x, y, z));
        }
    }

    public Block GetBlock(Vector3 pos)
    {
        return GetBlock((int)pos.x, (int)pos.y, (int)pos.z);
    }

    public Block GetBlock(int x, int y, int z)
    {
        Chunk c = FindChunk(x, y, z);

        if (c == null)
            return new Block();

        Block b = c.GetBlock(x - c.Position.x, y - c.Position.y, z - c.Position.z);
        if (b != null)
        {
            return b;
        }
        else
        {
            return new Block();
        }
    }

    public void SetBlock(int x, int y, int z, BlockType block)
    {
        Chunk c = FindChunk(x, y, z);
        if (c == null)
        {
            Vector3 t = new Vector3(Mathf.FloorToInt((float)x / ChunkSize) * ChunkSize, Mathf.FloorToInt((float)y / ChunkSize) * ChunkSize, Mathf.FloorToInt((float)z / ChunkSize) * ChunkSize);
            c = CreateChunk(t);
        }
        c.SetBlock(x - c.Position.x, y - c.Position.y, z - c.Position.z, block);
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        Chunk c = FindChunk(x, y, z);

        if (c == null)
        {
            Vector3 t = new Vector3(Mathf.FloorToInt((float)x / ChunkSize) * ChunkSize, Mathf.FloorToInt((float)y / ChunkSize) * ChunkSize, Mathf.FloorToInt((float)z / ChunkSize) * ChunkSize);
            c = CreateChunk(t);
        }
        c.SetBlock(x - c.Position.x, y - c.Position.y, z - c.Position.z, block);
    }

    public void SetColor(int x, int y, int z, Paintings color)
    {
        Chunk c = FindChunk(x, y, z);
        if (c != null)
        {
            Block block = c.GetBlock(x - c.Position.x, y - c.Position.y, z - c.Position.z);
            if (block != null)
            {
                block.Painting = color;
                c.SetBlock(x - c.Position.x, y - c.Position.y, z - c.Position.z, block);
            }
        }
    }

    public Chunk FindChunk(int x, int y, int z)
    {
        x = Mathf.FloorToInt((float)x / ChunkSize) * ChunkSize;
        y = Mathf.FloorToInt((float)y / ChunkSize) * ChunkSize;
        z = Mathf.FloorToInt((float)z / ChunkSize) * ChunkSize;

        Chunk c = null;
        Chunks.TryGetValue(new Vector3Int(x, y, z), out c);
        if (c == null)
        {
            return null;
        }
        else
        {
            return c;
        }
    }

    public Chunk[] GetNearChunks(Vector3 pos, float distance)
    {
        List<Chunk> nearChunks = new List<Chunk>();

        Chunk[] values = Chunks.Values.ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            Chunk value = values[i];
            if (Vector3.Distance(pos, value.gameObject.transform.position) <= distance)
                nearChunks.Add(value);
        }

        return nearChunks.ToArray();
    }

    IEnumerator CheckQueue()
    {
        while (true)
        {
            yield return null;
            if (QueueDestroy.Count > 0)
            {
                StartCoroutine(ClearQueue());
            }
        }
    }

    public void RemoveSegment()
    {
        foreach (Chunk chunk in Chunks.Values)
        {
            Destroy(chunk.gameObject);
        }
        Chunks.Clear();
    }

    IEnumerator ClearQueue()
    {
        for (int i = 0; i < QueueDestroy.Count; i++)
        {
            if (QueueDestroy.Count > 128)
            {
                Destroy(QueueDestroy[i]);
                QueueDestroy.RemoveAt(i);
                Debug.LogWarning("Immediate destroying all chunks!", gameObject);
            }
            yield return new WaitForSeconds(0.1f);
            if (i >= 0 && i < QueueDestroy.Count)
            {
                Destroy(QueueDestroy[i]);
                QueueDestroy.RemoveAt(i);
            }
        }
    }
    /// <summary>
    /// Checks if an adjacent chunk needs to be refresh to avoid spaces between chunks
    /// </summary>
    public void RefreshNearChunks(Chunk startChunk, int x, int y, int z)
    {
        int localX = x - startChunk.Position.x;
        int localY = y - startChunk.Position.y;
        int localZ = z - startChunk.Position.z;
        int s = ChunkSize - 1;

        if (localX == 0)
        {
            FindChunk(x - 1, y, z).RefreshAsync();
        }
        else if (localX == s)
        {
            FindChunk(x + 1, y, z).RefreshAsync();
        }

        if (localZ == 0)
        {
            FindChunk(x, y, z - 1).RefreshAsync();
        }
        else if (localZ == s)
        {
            FindChunk(x, y, z + 1).RefreshAsync();
        }

        if (localY == 0)
        {
            FindChunk(x, y - 1, z).RefreshAsync();
        }
        else if (localY == s)
        {
            FindChunk(x, y + 1, z).RefreshAsync();
        }

    }

    public Vector3 FindCenterOfTransforms(Transform[] transforms)
    {
        var bound = new Bounds(transforms[0].position, Vector3.zero);
        for (int i = 1; i < transforms.Length; i++)
        {
            bound.Encapsulate(transforms[i].position);
        }
        return bound.center;
    }

    IEnumerator UpdateGeneration()
    {
        int ChunkSize;
        Vector3 lastPos = _curPos + Vector3.one * updateDistance;
        Vector3[] chunkGrid;

        while (true)
        {

            if (!_hasLoadSegment && !SegmentBounds.bounds.Contains(PlayerPosition))
            {
                Debug.Log("Loading next segment...");
                BottomTextInfo.text = "Loading...";
                SegmentBounds.transform.position = Vector3Int.CeilToInt(PlayerPosition + new Vector4(8, 0, 8));
                //GameController.Instance.DeactivatePlayer();
                RemoveSegment();
                _hasLoadSegment = true;
                if (EdenWorldDecoder.Instance.CurrentPathWorld.Length > 0)
                {
                    EdenWorldDecoder.Instance.LoadSegment();
                }
                if (_chunkFrame % chunksPerFrame == 0)
                {
                    yield return null; // Minimizes lag.
                }
            }

            if (_hasLoadSegment)
            {
                if (lastPos != _curPos)
                {
                    ChunkSize = World.ChunkSize;
                    _curPos.y = 16;
                    lastPos = _curPos;
                    chunkGrid = GetChunkGrid(lastPos, ChunkSize);

                    for (int i = 0; i < chunkGrid.Length; i++)
                    {
                        if (lastPos != _curPos)
                        {
                            break;
                        }

                        Vector3 v = chunkGrid[i];
                        bool chunkExists = false;
                        if (v.y > 0 && v.y < 128)
                        {
                            chunkExists = ChunkExists(v);
                        }

                        if (_firstLoading) // first loading for world, init all
                        {
                            int loadProgress = (int)Mathf.Clamp(((Chunks.Count / generationDistance) * 100), 0, 100); // progressbar
                            LoadingText.text = "Loading world... " + loadProgress + "%";

                            if (loadProgress > 10)
                            {
                                // GameController.Instance.ActivatePlayer();
                                SegmentBounds.transform.position = PlayerPosition;
                                EdenWorldDecoder.Instance.LoadSegment();
                            }
                            if (loadProgress >= 100)
                            {
                                //  StartCoroutine(RefreshSegment());
                                GameController.Instance.CloseMainMenu();
                                GameController.Instance.ToggleMainCamera(true);
                                _firstLoading = false;
                                // Vector3 boundsCenter = FindCenterOfTransforms(SegmentBounds.transform.GetComponentsInChildren<Transform>());
                                // SegmentBounds.transform.position = boundsCenter;
                            }
                        }
                        if (!chunkExists)
                        {
                            CreateChunk(v);

                            _chunkFrame++;
                            int loadProgress = (int)Mathf.Clamp(((Chunks.Count / generationDistance) * 100), 0, 100);
                            if (loadProgress >= 100)
                            {
                                //StartCoroutine(RefreshSegment());
                                BottomTextInfo.text = "";
                                //  GameController.Instance.ActivatePlayer();
                                _hasLoadSegment = false;
                                // Vector3 boundsCenter = FindCenterOfTransforms(transform.GetComponentsInChildren<Transform>());
                                // SegmentBounds.transform.position = boundsCenter;
                            }
                            else
                            {
                                BottomTextInfo.text = "Loading...";
                            }

                            if (_chunkFrame % chunksPerFrame == 0)
                            {
                                yield return null; // Minimizes lag.
                            }
                        }

                    }
                }
            }
            yield return null;
        }
    }

    public Chunk CreateChunk(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x = Mathf.FloorToInt((float)x / ChunkSize) * ChunkSize;
        y = Mathf.FloorToInt((float)y / ChunkSize) * ChunkSize;
        z = Mathf.FloorToInt((float)z / ChunkSize) * ChunkSize;
        if (FindChunk(x, y, z) == null)
        {
            Chunk chunk = Instantiate(ChunkPrefab, new Vector3(x, y, z), Quaternion.identity).GetComponent<Chunk>();
            chunk.transform.parent = transform;
            chunk.InitData();
            Chunks.Add(Vector3Int.CeilToInt(pos), chunk);
            return chunk;
        }
        else
        {
            return FindChunk(x, y, z);
        }
    }

    IEnumerator RefreshSegment()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Refreshing world...");
        Dictionary<Vector3Int, Chunk>.ValueCollection values = Chunks.Values;
        foreach (Chunk chunk in values)
        {
            chunk.Refresh();
            yield return null;
        }
    }

    public bool IsInGrid(Vector3 pos, List<Vector3> list)
    {
        return list.Exists(x => x == pos);
    }

    public Vector3[] GetChunkGrid(Vector3 pos, int chunkSize)
    {
        List<Vector3> grid = new List<Vector3>();
        int lowX = ChunkRound(pos.x - generationDistance, chunkSize);
        int lowY = ChunkRound(pos.y - generationDistance, chunkSize);
        int lowZ = ChunkRound(pos.z - generationDistance, chunkSize);

        int highX = ChunkRound(pos.x + generationDistance, chunkSize);
        int highY = ChunkRound(pos.y + generationDistance, chunkSize);
        int highZ = ChunkRound(pos.z + generationDistance, chunkSize);

        for (int x = lowX; x <= highX; x += chunkSize)
        {
            for (int y = lowY; y <= highY; y += chunkSize)
            {
                for (int z = lowZ; z <= highZ; z += chunkSize)
                {
                    grid.Add(new Vector3(x, y, z));
                }
            }
        }
        return grid.ToArray();
        /*
        List<Pair> grid = new List<Pair>();

        int lowX = ChunkRound(pos.x - generationDistance, chunkSize);
        int lowY = ChunkRound(pos.y - generationDistance, chunkSize);
        int lowZ = ChunkRound(pos.z - generationDistance, chunkSize);

        int highX = ChunkRound(pos.x + generationDistance, chunkSize);
        int highY = ChunkRound(pos.y + generationDistance, chunkSize);
        int highZ = ChunkRound(pos.z + generationDistance, chunkSize);

        for (int x = lowX; x <= highX; x += chunkSize)
        {
            for (int y = lowY; y <= highY; y += chunkSize)
            {
                for (int z = lowZ; z <= highZ; z += chunkSize)
                {
                    Vector3 v = new Vector3(x, y, z);
                    float distance = Vector3.Distance(v, pos);
                    //float distance = SquareDistance(v.x, v.z, pos.x,pos.z, generationDistance, generationDistance);
                    if (!ChunkExists(v))
                    {
                        grid.Add(new Pair(distance, v));
                    }
                }
            }
        }

        return grid.OrderBy(o => o.distance).Select(o => o.pos).ToArray();
        */
    }
    private static int ChunkRound(float v, int ChunkSize)
    {
        return Mathf.FloorToInt(v / ChunkSize) * ChunkSize;
    }

    public void Sphere(Vector3 pos, int radius, BlockType type, bool isExplosion)
    {
        if (radius <= 0)
        {
            return;
        }

        int posx = Mathf.FloorToInt(pos.x);
        int posy = Mathf.FloorToInt(pos.y);
        int posz = Mathf.FloorToInt(pos.z);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (Vector3.Distance(pos, pos + new Vector3(x, y, z)) < radius)
                    {
                        if (isExplosion)
                        {
                            Block block = GetBlock(posx + x, posy + y, posz + z);
                            if (block != null)
                            {
                                if (GetBlock(posx, posy, posz).Painting == Paintings.Unpainted && block.BlockType != BlockType.Bedrock)
                                {
                                    if (block.BlockType != BlockType.TNT)
                                    {
                                        SetBlock(posx + x, posy + y, posz + z, type);
                                    }
                                }
                                else
                                {
                                    SetColor(posx + x, posy + y, posz + z, GetBlock(posx, posy, posz).Painting);
                                }
                            }
                        }
                    }
                }
            }
        }
        SetBlock(posx, posy, posz, type);
    }

    private struct Pair
    {
        public float distance;
        public Vector3 pos;

        public Pair(float distance, Vector3 pos)
        {
            this.distance = distance;
            this.pos = pos;
        }
    }

    public void Save()
    {
        BottomTextInfo.text = "Saving...";
        //EdenWorldDecoder.Instance.SaveWorld(Application.persistentDataPath + "/" + Name + ".eden");
    }

    public void SaveAndQuit()
    {
        BottomTextInfo.text = "Saving and quiting...";
        // EdenWorldDecoder.Instance.SaveWorld(Application.persistentDataPath + "/" + Name + ".eden");
        Invoke("QuitToMenu", 1f);
    }

    public void QuitToMenu()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public void SetHome()
    {
        HomePosition = PlayerMovement.Instance.transform.position;
        BottomTextInfo.text = "New home.";
        Invoke("CloseInfoText", 1f);
    }

    public void WrapHome()
    {
        Teleport(HomePosition);
        BottomTextInfo.text = "Teleportation...";
        Invoke("CloseInfoText", 1f);
    }

    public void Teleport(Vector3 pos)
    {
        GameController.Instance.Player.GetComponent<CharacterController>().enabled = false;
        GameController.Instance.Player.GetComponent<PlayerMovement>().enabled = false;
        GameController.Instance.Player.transform.position = pos;
        GameController.Instance.Player.GetComponent<CharacterController>().enabled = true;
        GameController.Instance.Player.GetComponent<PlayerMovement>().enabled = true;
    }
    public void Teleport(Vector3 pos, float yaw)
    {
        GameController.Instance.Player.GetComponent<CharacterController>().enabled = false;
        GameController.Instance.Player.GetComponent<PlayerMovement>().enabled = false;
        GameController.Instance.Player.transform.position = pos;
        transform.eulerAngles = new Vector3(0, yaw, 0);
        GameController.Instance.Player.GetComponent<CharacterController>().enabled = true;
        GameController.Instance.Player.GetComponent<PlayerMovement>().enabled = true;
    }

    void CloseInfoText()
    {
        BottomTextInfo.text = "";
    }

}