namespace CrimsonDesertTools.Parser.PackGroupTree
{
    // credit LukeFZ
    // https://github.com/LukeFZ/pycrimson/blob/master/src/pycrimson/_files/_papgt.py
    public enum PackGroupLanguageType : ushort
    {
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
    }
}
