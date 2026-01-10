public struct LateUpdateEvent : IMEvent
{
    public static int eventID => ConstEventID.LateUpdateEvent;
    public int EventID => eventID;
    
    public float deltaTime;
}