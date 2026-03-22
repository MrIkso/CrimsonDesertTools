import std.mem;

#pragma array_limit 99999999999
#pragma pattern_limit 99999999999

struct TextEntry { 
    u32 pad;     
    u32 keyLen;
    char key[keyLen];
    u32 textLen;
    char text[textLen];  
    u32 index;
   
};

struct Loc {
    u32 unk;    
    TextEntry text[while($ < std::mem::size())];
};

Loc loc @ 0x0;