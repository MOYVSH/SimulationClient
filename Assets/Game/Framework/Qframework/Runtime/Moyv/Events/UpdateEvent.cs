public struct UpdateEvent : IMEvent
{
    public static int eventID => ConstEventID.UpdateEvent;
    public int EventID => eventID;
    
    public float deltaTime;
}