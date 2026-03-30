/// <summary>
/// Global raycast counter. Visibility systems call Increment() for each cast.
/// ServerStats reads and resets it once per second.
/// </summary>
public static class RaycastCounter
{
    private static int _count;

    public static void Increment() => _count++;

    public static int GetAndReset()
    {
        int value = _count;
        _count = 0;
        return value;
    }
}
