// PackGroupTree (.papgt)
// This is the Root Index file for the game's Virtual File System (VFS).
// It acts as a master map that describes all existing resource groups.
// Used for validating .pamt files and constructing physical archive paths.

// credit LukeFZ 
// https://github.com/LukeFZ/pycrimson/blob/master/src/pycrimson/_files/_papgt.py
enum LanguageType : u16 {
    KOR = 1 << 0,
    ENG = 1 << 1,
    JPN = 1 << 2,
    RUS = 1 << 3,
    TUR = 1 << 4,
    SPA_ES = 1 << 5,
    SPA_MX = 1 << 6,
    FRE = 1 << 7,
    GER = 1 << 8,
    ITA = 1 << 9,
    POL = 1 << 10,
    POR_BR = 1 << 11,
    ZHO_TW = 1 << 12,
    ZHO_CN = 1 << 13,
    ALL = 0x3FFF,
};

// Metadata Group Information
struct PackMetaInfo {
    u8 isOptional;
    LanguageType languageType;
    u8 zero;
    u32 nameOffset; // Offset of the folder name within the stringBlock.
    u32 crc;        // Expected crc header of the corresponding 0.pamt file.
                    // If the actual crc of the 0.pamt file does not match this value, 
                    // the game throws a "packMeta Crc mismatch" error.
};

// Global File Header (12 bytes)
struct Header {
    u32 unknown;
    u32 fileCrc;    // Checksum of the entire file payload (calculated after the header).
                    // Verified by sub_140F3A450 to prevent unauthorized metadata tampering.
    u8 groupCount;  // Total number of archive groups (folders) described in this file.
    u16 vfsFlags; 
    u8 pad;
};

// Folder Name Structure
struct FolderNames {
    char name[4];   // Folder name
    u8 nullTerm;    // Null terminator (\0).
};

struct Papgt {
    Header header;
    PackMetaInfo infos[header.groupCount]; // Array of references to group metadata
    
    // Directory Name Block
    // Used for dynamic file path construction.
    // Paths are built using the template: [RootPath]/[FolderName]/0.pamt
    u32 stringBlockSize; 
    
    // The number of entries matches header.groupCount.
    FolderNames folders[header.groupCount];
};

Papgt pagt @ 0x0;