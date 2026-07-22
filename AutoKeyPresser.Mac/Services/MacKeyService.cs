using AutoKeyPresser.Models;

namespace AutoKeyPresser.Mac.Services;

public static class MacKeyService
{
    public const ushort Shift = 56;
    public const ushort RightShift = 60;
    public const ushort Control = 59;
    public const ushort RightControl = 62;
    public const ushort Option = 58;
    public const ushort RightOption = 61;

    private static readonly Dictionary<ushort, string> Names = new()
    {
        [0]="A",[1]="S",[2]="D",[3]="F",[4]="H",[5]="G",[6]="Z",[7]="X",[8]="C",[9]="V",[11]="B",
        [12]="Q",[13]="W",[14]="E",[15]="R",[16]="Y",[17]="T",[18]="1",[19]="2",[20]="3",[21]="4",
        [22]="6",[23]="5",[24]="=",[25]="9",[26]="7",[27]="-",[28]="8",[29]="0",[30]="]",[31]="O",
        [32]="U",[33]="[",[34]="I",[35]="P",[37]="L",[38]="J",[39]="'",[40]="K",[41]=";",[42]="\\",
        [43]=",",[44]="/",[45]="N",[46]="M",[47]=".",[48]="Tab",[49]="Space",[50]="`",[51]="Backspace",
        [53]="Escape",[65]="Num .",[67]="Num *",[69]="Num +",[71]="Num Clear",[75]="Num /",[76]="Num Enter",
        [78]="Num -",[81]="Num =",[82]="Num 0",[83]="Num 1",[84]="Num 2",[85]="Num 3",[86]="Num 4",
        [87]="Num 5",[88]="Num 6",[89]="Num 7",[91]="Num 8",[92]="Num 9",[96]="F5",[97]="F6",
        [98]="F7",[99]="F3",[100]="F8",[101]="F9",[103]="F11",[109]="F10",[111]="F12",[115]="Home",
        [116]="Page Up",[117]="Delete",[118]="F4",[119]="End",[120]="F2",[121]="Page Down",[122]="F1",
        [123]="Left Arrow",[124]="Right Arrow",[125]="Down Arrow",[126]="Up Arrow"
    };

    public static bool TryGetModifier(ushort key, out KeyModifiers modifier)
    {
        modifier = key switch
        {
            Shift or RightShift => KeyModifiers.Shift,
            Control or RightControl => KeyModifiers.Control,
            Option or RightOption => KeyModifiers.Alt,
            _ => KeyModifiers.None
        };
        return modifier != KeyModifiers.None;
    }

    public static string Name(ushort key) => Names.TryGetValue(key, out var name) ? name : $"Key {key}";
    public static string Display(ushort key, KeyModifiers modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Option");
        parts.Add(Name(key));
        return string.Join(" + ", parts);
    }
}
