
public enum DirectorState
{
    Idle, //Not checking for encounters(hub area)
    Engaged, // Player is within encounter range
    Searching // no active encounter, still checking for another
}