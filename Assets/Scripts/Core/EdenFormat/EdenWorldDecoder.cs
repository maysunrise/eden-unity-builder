using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Loading/Converting and Saving .eden files (File Format for Eden World Builder)
/// </summary>
public class EdenWorldDecoder : MonoBehaviour // Based on https://mrob.com/pub/vidgames/eden-file-format.html
{
    public static string worldName;

    private World world;

    public static EdenWorldDecoder Instance;

    public string CurrentPathWorld;

    int sizeX;
    int sizeY;

    void Start()
    {
        Instance = this;
        world = World.Instance;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    private struct WorldFileHeader
    {
    }

    public struct ColumnIndex
    {
        public int x;

        public int z;

        public ulong chunk_offset;
    }

    public List<ColumnIndex> maColIndices;

    private int[] Unhandled;

    private const int CHUNKS_PER_COLUMN_IN_FILE = 4;

    long directoryoffset;

    private byte[] WorldBytes; // Raw world bytes

    int offsetPos = 0;

    public void LoadWorld(string path)
    {
        CurrentPathWorld = path;
        if (CurrentPathWorld.Length == 0)
        {
            return;
        }
        if (CurrentPathWorld.Contains(".eden"))
        {

            maColIndices = new List<ColumnIndex>();
            // Unhandled = new int[112];
            FileStream fileStream = new FileStream(path, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            try
            {
                world.WorldSeed = binaryReader.ReadInt32();
                offsetPos += 4;
                int num7 = 65535;
                float num = binaryReader.ReadSingle();
                offsetPos += 4;
                float num2 = binaryReader.ReadSingle();
                offsetPos += 4;
                float yy = binaryReader.ReadSingle();
                offsetPos += 4;
                // Debug.Log("Xpos : " + num);
                // Debug.Log("Ypos : " + num2);
                // Debug.Log("Zpos : " + yy);
                Vector3 housePos = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
                offsetPos += 4;
                offsetPos += 4;
                offsetPos += 4;
                float yawCamera = binaryReader.ReadSingle();
                offsetPos += 4;
                long offset = binaryReader.ReadInt64();
                offsetPos += 8;
                directoryoffset = offset;
                char[] array = new char[1500];
                string text = new string(binaryReader.ReadChars(1500), 0, 50);
                Debug.Log("world name " + text);
                Debug.Log("seed " + world.WorldSeed);
                fileStream.Seek(100L, SeekOrigin.Current);
                long position = binaryReader.BaseStream.Position;
                int num3 = int.MinValue;
                int num4 = int.MaxValue;
                int num5 = int.MinValue;
                int num6 = int.MaxValue;
                fileStream.Seek(offset, SeekOrigin.Begin);

                while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
                {
                    ColumnIndex item = default(ColumnIndex);
                    item.x = binaryReader.ReadInt32();
                    item.z = binaryReader.ReadInt32();
                    item.chunk_offset = binaryReader.ReadUInt64();
                    //Debug.Log(item.x + " " + item.z + " - " + item.chunk_offset);
                    maColIndices.Add(item);
                    if (item.x > num3)
                    {
                        num3 = item.x;
                    }
                    if (item.z > num5)
                    {
                        num5 = item.z;
                    }
                    if (item.x < num4)
                    {
                        num4 = item.x;
                    }
                    if (item.z < num6)
                    {
                        num6 = item.z;
                    }
                    num7--;
                    if (num7 <= 0)
                    {
                        break;
                    }
                }
                int num8 = (num3 + num4) / 2;
                int num9 = (num5 + num6) / 2;
                num8 = num8 / 16 * 16;
                num9 = num9 / 16 * 16;
                sizeX = num8;
                sizeY = num9;
                world.HomePosition = new Vector3(housePos.x - sizeX * 16, housePos.y, housePos.z - sizeY * 16);
                world.SpawnPosition = new Vector4(num - sizeX * 16, num2, yy - sizeY * 16, yawCamera);
            }
            finally
            {
                binaryReader.Close();
                fileStream.Close();
            }
            // WorldBytes = File.ReadAllBytes(path);
            world.NewWorldFormat = false;
            // ConvertOldWorld();
        }
        else if (CurrentPathWorld.Contains(".eden2"))
        {
            world.NewWorldFormat = true;
        }
    }


    /*
    public IEnumerator SaveSegment(Vector3 center)
    {
        List<Vector3> vectors = World.Instance.GetChunkGrid(center, World.ChunkSize).ToList<Vector3>();

        FileStream fileStream = new FileStream(CurrentPathWorld, FileMode.Open);
        BinaryWriter binaryWriter = new BinaryWriter(fileStream);
        try
        {
            for (int i = 0; i < vectors.Count; i++)
            {
                Chunk c = world.FindChunk(vectors[i]);
                if (c != null && c.isDirty )
                {
                    ColumnIndex columidx;
                    //columidx = SaveColumIndex((int)vectors[i].x, (int)vectors[i].z);
                   // Debug.Log("For Save" + columidx.chunk_offset);
                   // fileStream.Seek((long)(columidx.chunk_offset), SeekOrigin.Begin);
                    //binaryWriter.Write(columidx.x);
                   // binaryWriter.Write(columidx.z);
                   // binaryWriter.Write(columidx.chunk_offset);
                    c.FromSave = true;
                }
            }

            for (int i = 0; i < maColIndices.Count; i++)
            {
                binaryWriter.Seek((int)maColIndices[i].chunk_offset, SeekOrigin.Begin);
               // Debug.Log("Exists " + maColIndices[i].chunk_offset);
                //CHUNKS_PER_COLUMN_IN_FILE - 4 chunks in height
                for (int j = 0; j < CHUNKS_PER_COLUMN_IN_FILE; j++)
                {
                    int num10 = (maColIndices[i].x - sizeX) * 16;
                    int num11 = (maColIndices[i].z - sizeY) * 16;
                    Vector3 p = new Vector3(num10, j * 16, num11);

                    if (world.ChunkExists(p) && world.FindChunk(p).isDirty) // Replace exist chunk in save
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            for (int l = 0; l < 16; l++)
                            {
                                for (int m = 0; m < 16; m++)
                                {
                                    int num12 = (m << 8) + (l << 4) + k;
                                    //fileStream.Position = num12;
                                    binaryWriter.Write((sbyte)world.GetBlock(k + num10, m + (j * 16), l + num11).BlockType);
                                    //sbyte num13 = binaryReader.ReadSByte();
                                    //world.SetBlock(k + num10, m + (j * 16), l + num11, (BlockType)num13);
                                }
                            }
                        }
                        for (int n = 0; n < 16; n++)
                        {
                            for (int num14 = 0; num14 < 16; num14++)
                            {
                                for (int num15 = 0; num15 < 16; num15++)
                                {
                                    int num16 = (num15 << 8) + (num14 << 4) + n;
                                    // fileStream.Position = num16;
                                    binaryWriter.Write((byte)world.GetBlock(n + num10, num15 + (j * 16), num14 + num11).Painting);
                                    // byte b = binaryReader.ReadByte();
                                    // world.SetColor(n + num10, num15 + (j * 16), num14 + num11, (Paintings)b);
                                }
                            }
                        }
                        if (j % CHUNKS_PER_COLUMN_IN_FILE == 0)
                        {
                            yield return null;
                        }
                    }
                }
            }
        }
        finally
        {
            binaryWriter.Close();
            fileStream.Close();
        }

        Debug.Log("Segment saved");
        yield return null;
        world.RemoveSegment();
        yield return null;
        StartCoroutine(LoadSegment2(center));
    }
    */

    private void Update()
    {

    }

    bool containsXY(int x, int y)
    {
        for (int i = 0; i < maColIndices.Count; i++)
        {
            if (maColIndices[i].x == x && maColIndices[i].z == y)
            {
                return true;
            }
        }
        return false;
    }

    public byte[] SaveNewChunks()
    {
        int num = maColIndices.Min((ColumnIndex p) => p.x);
        int num2 = maColIndices.Max((ColumnIndex p) => p.x);
        int num3 = maColIndices.Min((ColumnIndex p) => p.z);
        int num4 = maColIndices.Max((ColumnIndex p) => p.z);
        int largest = (int)maColIndices[0].chunk_offset;
        for (int index = 0; index < maColIndices.Count; index++)
        {
            if ((int)maColIndices[index].chunk_offset > largest) largest = (int)maColIndices[index].chunk_offset;
        }
        int num5 = largest;
        int num6 = largest;
        for (int i = num; i <= num2; i++)
        {
            for (int j = num3; j <= num4; j++)
            {
                bool flag = !containsXY(i, j);
                if (flag)
                {
                    num6 += 32768;

                    ColumnIndex item = default(ColumnIndex);
                    item.x = i;
                    item.z = j;
                    item.chunk_offset = (ulong)num6;
                    maColIndices.Add(item);
                }
            }
        }

        int num7 = maColIndices.Count * 32768 + 192;
        byte[] bytes = new byte[num7 + maColIndices.Count * 16];
        for (int k2 = 0; k2 < 32; k2++)
        {
            bytes[k2] = WorldBytes[k2];
        }
        bytes[32] = (byte)num7;
        bytes[33] = (byte)(num7 >> 8);
        bytes[34] = (byte)(num7 >> 16);
        bytes[35] = (byte)(num7 >> 24);

        for (int l = 36; l < 192; l++)
        {
            bytes[l] = WorldBytes[l];
        }
        for (int num8 = 0; num8 < maColIndices.Count; num8++)
        {
            int num9 = (maColIndices[num8].x - sizeX) * 16;
            int num10 = (maColIndices[num8].x - sizeY) * 16;
            for (int m = 0; m < 4; m++)
            {
                for (int n = 0; n < 16; n++)
                {
                    for (int num11 = 0; num11 < 16; num11++)
                    {
                        for (int num12 = 0; num12 < 16; num12++)
                        {
                            bytes[num8 + m * 8192 + n * 256 + num11 * 16 + num12] = (byte)UnityEngine.Random.Range(1, 5);
                            bytes[num8 + m * 8192 + n * 256 + num11 * 16 + num12 + 4096] = 0;
                        }
                    }
                }
            }
        }
        int num13 = 192;
        for (int index = 0; index < maColIndices.Count; index++)
        {
            bytes[num7] = (byte)maColIndices[index].x;
            bytes[num7 + 1] = (byte)(maColIndices[index].x >> 8);
            bytes[num7 + 2] = (byte)(maColIndices[index].x >> 16);
            bytes[num7 + 3] = (byte)(maColIndices[index].x >> 24);
            bytes[num7 + 4] = (byte)maColIndices[index].z;
            bytes[num7 + 5] = (byte)(maColIndices[index].z >> 8);
            bytes[num7 + 6] = (byte)(maColIndices[index].z >> 16);
            bytes[num7 + 7] = (byte)(maColIndices[index].z >> 24);
            bytes[num7 + 8] = (byte)num13;
            bytes[num7 + 9] = (byte)(num13 >> 8);
            bytes[num7 + 10] = (byte)(num13 >> 16);
            bytes[num7 + 11] = (byte)(num13 >> 24);
            bytes[num7 + 12] = 0;
            bytes[num7 + 13] = 0;
            bytes[num7 + 14] = 0;
            bytes[num7 + 15] = 0;
            num13 += 32768;
            num7 += 16;
        }
        Array.Resize<byte>(ref WorldBytes, bytes.Length);
        WorldBytes = bytes;
        /*
         for (int i = 0; i < vectors.Count; i++)
            {
                Chunk c = world.FindChunk(vectors[i]);
                if (c != null && c.isDirty)
                {
                    ColumnIndex columidx;
                    //columidx = SaveColumIndex((int)vectors[i].x, (int)vectors[i].z);
                    // Debug.Log("For Save" + columidx.chunk_offset);
                    // fileStream.Seek((long)(columidx.chunk_offset), SeekOrigin.Begin);
                    //binaryWriter.Write(columidx.x);
                    // binaryWriter.Write(columidx.z);
                    // binaryWriter.Write(columidx.chunk_offset);
                    c.FromSave = true;
                }
            } 
        */
        return bytes;
    }

    public void ReadDirectory()
    {
        maColIndices.Clear();
        FileStream fileStream = new FileStream(CurrentPathWorld, FileMode.Open);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        try
        {
            int num7 = 65535;
            int num3 = int.MinValue;
            int num4 = int.MaxValue;
            int num5 = int.MinValue;
            int num6 = int.MaxValue;
            fileStream.Seek((long)directoryoffset, SeekOrigin.Begin);

            while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
            {
                ColumnIndex item = default(ColumnIndex);
                item.x = binaryReader.ReadInt32();
                item.z = binaryReader.ReadInt32();
                item.chunk_offset = binaryReader.ReadUInt64();
                maColIndices.Add(item);
                if (item.x > num3)
                {
                    num3 = item.x;
                }
                if (item.z > num5)
                {
                    num5 = item.z;
                }
                if (item.x < num4)
                {
                    num4 = item.x;
                }
                if (item.z < num6)
                {
                    num6 = item.z;
                }
                // Debug.Log("Located Chunk " + maColIndices.Count + " at " + item.x + ", " + item.z + " offset " + item.chunk_offset / 1024uL + "k");
                num7--;
                if (num7 <= 0)
                {
                    break;
                }
            }
            int num8 = (num3 + num4) / 2;
            int num9 = (num5 + num6) / 2;
            num8 = num8 / 16 * 16;
            num9 = num9 / 16 * 16;
            sizeX = num8;
            sizeY = num9;
        }
        finally
        {
            binaryReader.Close();
            fileStream.Close();
        }
    }

    /*
     ColumnIndex structure
     public int x;

     public int z;

     public ulong chunk_offset;
    */

    public ColumnIndex SaveColumIndex(int x, int y)
    {
        ColumnIndex item = default(ColumnIndex);
        item.x = sizeX + (int)(x / 16);
        item.z = sizeY + (int)(y / 16);
        ulong tableOfContentsIndex = (ulong)(maColIndices.Count * 8 * 4096);
        item.chunk_offset = tableOfContentsIndex;//((ulong)maColIndices.Count * 32768 * 192);
        maColIndices.Add(item);
        return item;
    }
    private long offset2;
    public IEnumerator SaveSegment(Vector3 center)
    {
        List<Vector3> vectors = World.Instance.GetChunkGrid(center, World.ChunkSize).ToList();

        FileStream fileStream = new FileStream(CurrentPathWorld, FileMode.Open); // Saving exists file
        BinaryWriter binaryWriter = new BinaryWriter(fileStream);
        try
        {
            offset2 = directoryoffset;
            Debug.Log("Offset position " + offsetPos);
            // Saving in the table of contents
            for (int i = 0; i < vectors.Count; i++)
            {
                Chunk c = world.FindChunk(vectors[i]);
                if (c != null && c.isDirty)
                {
                    fileStream.Seek(offsetPos*16, SeekOrigin.Begin);
                    directoryoffset += 32768;
                    binaryWriter.Write((int)directoryoffset);
                }
                if (c != null && c.isDirty && !c.FromSave)
                {


                    ColumnIndex columidx;
                    columidx = SaveColumIndex((int)vectors[i].x, (int)vectors[i].z);
                    //fileStream.Seek(0, SeekOrigin.End);
                    //binaryWriter.Write(columidx.x);
                    //binaryWriter.Write(columidx.z);
                    //binaryWriter.Write((int)columidx.chunk_offset);
                    // Debug.Log("Saved new at " + columidx.x + " " + columidx.z + " - " + columidx.chunk_offset);
                    c.isDirty = false;
                    c.FromSave = true;
                }
            }

            for (int i = 0; i < maColIndices.Count; i++)
            {
                binaryWriter.Seek((int)maColIndices[i].chunk_offset, SeekOrigin.Begin);
                // Debug.Log("Exists " + maColIndices[i].chunk_offset);
                //CHUNKS_PER_COLUMN_IN_FILE - 4 chunks in height

                for (int j = 0; j < CHUNKS_PER_COLUMN_IN_FILE; j++)
                {
                    int num10 = (maColIndices[i].x - sizeX) * 16;
                    int num11 = (maColIndices[i].z - sizeY) * 16;
                    Vector3 p = new Vector3(num10, j * 16, num11);

                    if (world.ChunkExists(p) && world.FindChunk(p).isDirty) // Replace exist chunk in save
                    {
                        // Debug.Log("Saved exist at " + maColIndices[i].x + " " + maColIndices[i].z + " - " + maColIndices[i].chunk_offset);

                        for (int k = 0; k < 16; k++)
                        {
                            for (int l = 0; l < 16; l++)
                            {
                                for (int m = 0; m < 16; m++)
                                {
                                    int num12 = (m << 8) + (l << 4) + k;
                                    //fileStream.Position = num12;
                                    binaryWriter.Write((sbyte)world.GetBlock(k + num10, m + (j * 16), l + num11).BlockType);
                                    //sbyte num13 = binaryReader.ReadSByte();
                                    //world.SetBlock(k + num10, m + (j * 16), l + num11, (BlockType)num13);
                                }
                            }
                        }
                        for (int n = 0; n < 16; n++)
                        {
                            for (int num14 = 0; num14 < 16; num14++)
                            {
                                for (int num15 = 0; num15 < 16; num15++)
                                {
                                    int num16 = (num15 << 8) + (num14 << 4) + n;
                                    // fileStream.Position = num16;
                                    binaryWriter.Write((byte)world.GetBlock(n + num10, num15 + (j * 16), num14 + num11).Painting);
                                    // byte b = binaryReader.ReadByte();
                                    // world.SetColor(n + num10, num15 + (j * 16), num14 + num11, (Paintings)b);
                                }
                            }
                        }

                        if (j % CHUNKS_PER_COLUMN_IN_FILE == 0)
                        {
                            yield return null;
                        }
                    }
                }
            }
            fileStream.Seek(directoryoffset-12000, SeekOrigin.Begin);
            for (int i = 0; i < 12000; i++) // Creature data
            {
                binaryWriter.Write((byte)0); // Empty data
            }
            
           // int tableOfContentsIndex = 192 + maColIndices.Count * 8 * 4096;
            fileStream.Seek(directoryoffset, SeekOrigin.Begin); // directoryoffset
            int lastX = -1;
            int lastZ = -1;
            for (int i = 0; i < maColIndices.Count; i++)
            {
                ColumnIndex columidx = maColIndices[i];
                if (lastX != columidx.x && lastZ != columidx.z)
                {
                    //offset2 += 32768;
                    // fileStream.Seek(offset2, SeekOrigin.Begin);
                    lastX = columidx.x;
                    lastZ = columidx.z;

                    binaryWriter.Write(columidx.x);
                    binaryWriter.Write(columidx.z);
                    binaryWriter.Write(columidx.chunk_offset);
                }

            }
            //  byte[] buffer = SaveNewChunks();
            //  binaryWriter.Write(buffer);
        }
        finally
        {
            binaryWriter.Close();
            fileStream.Close();
        }
        Debug.Log("Segment saved");
        yield return null;
        world.RemoveSegment();
        yield return null;
        StartCoroutine(LoadSegment2(center));
    }

    public IEnumerator LoadSegment2(Vector3 center)
    {
        List<Vector3> vectors = World.Instance.GetChunkGrid(center, World.ChunkSize).ToList<Vector3>();
        List<Chunk> chunksForUpdate = new List<Chunk>();
        if (CurrentPathWorld.Length > 0)
        {
            FileStream fileStream = new FileStream(CurrentPathWorld, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            try
            {
                Debug.Log("Loading segment...");
                for (int i = 0; i < maColIndices.Count; i++)
                {
                    fileStream.Seek((long)maColIndices[i].chunk_offset, SeekOrigin.Begin);
                    //Debug.Log("LLLLL " + maColIndices[i].chunk_offset);
                    //CHUNKS_PER_COLUMN_IN_FILE - 4 chunks in height
                    for (int j = 0; j < CHUNKS_PER_COLUMN_IN_FILE; j++)
                    {
                        int num10 = (maColIndices[i].x - sizeX) * 16;
                        int num11 = (maColIndices[i].z - sizeY) * 16;
                        Vector3 p = new Vector3(num10, j * 16, num11);
                        if (world.IsInGrid(p, vectors))
                        {
                            if (!world.ChunkExists(p))
                            {
                                Chunk c = world.CreateChunk(p);
                                c.isLoaded = true;
                                c.isDirty = true;
                                for (int k = 0; k < 16; k++)
                                {
                                    for (int l = 0; l < 16; l++)
                                    {
                                        for (int m = 0; m < 16; m++)
                                        {
                                            int num12 = (m << 8) + (l << 4) + k;

                                            sbyte num13 = binaryReader.ReadSByte();
                                            world.SetBlock(k + num10, m + (j * 16), l + num11, (BlockType)num13);
                                        }
                                    }
                                }
                                for (int n = 0; n < 16; n++)
                                {
                                    for (int num14 = 0; num14 < 16; num14++)
                                    {
                                        for (int num15 = 0; num15 < 16; num15++)
                                        {
                                            int num16 = (num15 << 8) + (num14 << 4) + n;
                                            byte b = binaryReader.ReadByte();
                                            world.SetColor(n + num10, num15 + (j * 16), num14 + num11, (Paintings)b);
                                        }
                                    }
                                }
                                vectors.Remove(p);
                                c.FromSave = true;
                                chunksForUpdate.Add(c);
                                if (j % CHUNKS_PER_COLUMN_IN_FILE == 0)
                                {
                                    yield return null;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                binaryReader.Close();
                fileStream.Close();
            }
        }
        for (int i = 0; i < vectors.Count; i++)
        {
            Chunk c = world.CreateChunk(vectors[i]);
            c.isLoaded = false;
            c.FromSave = false;
            chunksForUpdate.Add(c);
        }

        yield return null;
        for (int j = 0; j < chunksForUpdate.Count; j++)
        {
            chunksForUpdate[j].RefreshAsync();
        }
        yield return null;
        world._segmentLoaded = true;
        GameController.Instance.TogglePlayerController(true);
        world.BottomTextInfo.text = "";
    }

    // old
    public void LoadSegment() // FIX Z-INVERTED CHUNK
    {
        FileStream fileStream = new FileStream(CurrentPathWorld, FileMode.Open);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        try
        {
            for (int i = 0; i < maColIndices.Count; i++)
            {
                fileStream.Seek((long)maColIndices[i].chunk_offset, SeekOrigin.Begin);
                for (int j = 0; j < 4; j++)
                {
                    int num10 = (maColIndices[i].x - sizeX) * 16;
                    int num11 = (maColIndices[i].z - sizeY) * 16;

                    if (Vector3.Distance(world.PlayerPosition, new Vector3(num10, world.PlayerPosition.y, num11)) < world.generationDistance + 64)
                    {
                        if (world.ChunkExists(new Vector3(num10, j * 16, num11)))
                        {
                            return;
                        }
                        Chunk c = world.CreateChunk(new Vector3(num10, j * 16, num11));
                        c.isLoaded = true;
                        c.isDirty = true;
                        for (int k = 0; k < 16; k++)
                        {
                            for (int l = 0; l < 16; l++)
                            {
                                for (int m = 0; m < 16; m++)
                                {
                                    int num12 = (m << 8) + (l << 4) + k;

                                    sbyte num13 = binaryReader.ReadSByte();
                                    world.SetBlock(k + num10, m + (j * 16), l + num11, (BlockType)num13);
                                }
                            }
                        }
                        for (int n = 0; n < 16; n++)
                        {
                            for (int num14 = 0; num14 < 16; num14++)
                            {
                                for (int num15 = 0; num15 < 16; num15++)
                                {
                                    int num16 = (num15 << 8) + (num14 << 4) + n;
                                    byte b = binaryReader.ReadByte();
                                    world.SetColor(n + num10, num15 + (j * 16), num14 + num11, (Paintings)b);
                                }
                            }
                        }
                        c.Refresh();
                    }
                }
            }
        }
        finally
        {
            binaryReader.Close();
            fileStream.Close();
        }
    }
}
