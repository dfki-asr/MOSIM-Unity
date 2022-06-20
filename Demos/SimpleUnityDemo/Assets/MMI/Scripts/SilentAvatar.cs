using MMIUnity.TargetEngine.Scene;

// Same as MMIAvatar, but without printing messages on screen
public class SilentAvatar : MMIAvatar
{
    protected override void OnGUI()
    {
        // Do nothing
    }
}
