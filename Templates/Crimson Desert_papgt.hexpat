// PackGroupTree (.papgt)
// This is the Root Index file for the game's Virtual File System (VFS).
// It acts as a master map that describes all existing resource groups.
// Used for validating .pamt files and constructing physical archive paths.


// Metadata Group Information
struct PackMetaInfo {
    u32 folderHash;
    u32 nameOffset; // Offset of the folder name within the stringBlock.
    u32 crc;        // Expected crc header of the corresponding 0.pamt file.
                    // If the actual crc of the 0.pamt file does not match this value, 
                    // the game throws a "packMeta Crc mismatch" error.
};

// Global File Header (12 bytes)
struct Header {
    u32 version;    // Metadata format version (typically 935 / 0xA7030000 in Crimson Desert).
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