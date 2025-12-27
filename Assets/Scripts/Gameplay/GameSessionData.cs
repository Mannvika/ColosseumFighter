using System.Collections.Generic;

public static class GameSessionData
{
    // Key: ClientID, Value: Champion Index
    public static Dictionary<ulong, int> CharacterSelections = new Dictionary<ulong, int>();

    public static void Clear()
    {
        CharacterSelections.Clear();
    }
}