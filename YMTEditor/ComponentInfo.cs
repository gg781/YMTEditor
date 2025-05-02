namespace YMTEditor
{
    public class ComponentInfo
    {
        public string infopedXml_audioID { get; set; } //unknown usage audioid
        public string infopedXml_audioID2 { get; set; } //unknown usage audioid2
        public string[] infopedXml_expressionMods { get; set; } //probably expressionMods(?) - used for heels for example
        public int infoFlags { get; set; } //unknown usage
        public string infoInclusions { get; set; } //unknown usage
        public string infoExclusions { get; set; } //unknown usage
        public string infopedXml_vfxComps { get; set; } //unknown usage - always "PV_COMP_HEAD" (?)
        public int infopedXml_flags { get; set; } //unknown usage flags

        public int infopedXml_compIdx { get; set; } //component id (jbib = 11, feet = 6, etc)
        public int infopedXml_drawblIdx { get; set; } //drawable index (000, 001, 002, etc)

        public ComponentInfo(string pedXml_audioID, string pedXml_audioID2, string[] pedXml_expressionMods, int flags, string inclusions, string exclusions, string pedXml_vfxComps, int pedXml_flags, int pedXml_compIdx, int pedXml_drawblIdx)
        {
            infopedXml_audioID = pedXml_audioID;
            infopedXml_audioID2 = pedXml_audioID2;
            infopedXml_expressionMods = pedXml_expressionMods;
            infoFlags = flags;
            infoInclusions = inclusions;
            infoExclusions = exclusions;
            infopedXml_vfxComps = pedXml_vfxComps;
            infopedXml_flags = pedXml_flags;

            infopedXml_compIdx = pedXml_compIdx;
            infopedXml_drawblIdx = pedXml_drawblIdx;
        }

        public ComponentInfo(int componentId, int drawableIndex)
        {
            infopedXml_audioID = "none";
            infopedXml_audioID2 = "none";
            infopedXml_expressionMods = new string[] { "0", "0", "0", "0", "0" };
            infoFlags = 0;
            infoInclusions = "0";
            infoExclusions = "0";
            infopedXml_vfxComps = "PV_COMP_HEAD";
            infopedXml_flags = 0;

            infopedXml_compIdx = componentId;
            infopedXml_drawblIdx = drawableIndex;
        }
    }
}