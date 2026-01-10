public struct FixedUpdateEvent : IMEvent
{
    public static int eventID => ConstEventID.FixedUpdateEvent;
    public int EventID => eventID;

    public float deltaTime;
}