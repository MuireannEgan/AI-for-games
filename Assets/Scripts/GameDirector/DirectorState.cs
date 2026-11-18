
public enum DirectorState
{
    Idle, //Not checking for encounters(hub area)
    Approaching, //Player is getting closer to the enemy
    Engaged, // Player is within encounter range
    Disengaging, // player is moving away from enemy
    Searching // no active encounter, still checking for another
}