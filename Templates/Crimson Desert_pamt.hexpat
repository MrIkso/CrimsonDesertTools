struct Header {
    u32 crc;
    u32 pazCount;
    u32 unk;
};

struct PazInfo {
    u32 index;
    u32 crc;
    u32 fileSize;
};

struct String {
    u8 size;
    char text[size];
};

// 20 bytes
struct FileInfo {
    u32 nameIndex;
    u32 offset;
    u32 compressSize;
    u32 decompressSize;
    u16 pazIndex;
    u16 flags;
};

struct DirHashTable{
    u32 hash;
    u32 nameOffset;
    u32 fileStartIndex;
    u32 fileCount;
};

struct PackMeta {
    Header header;
    
    PazInfo pazInfo[header.pazCount];
    u32 dirblockSize;
    u8 dirData[dirblockSize];
    u32 fileNamesBlockSize;
   
    u8 filenamesData[fileNamesBlockSize];
    
    u32 dirHashTableCount;
    DirHashTable dirHashTable[dirHashTableCount];
    u32 filesCount;
    FileInfo fileInfo[filesCount];
};

PackMeta packMeta @ 0x0;