using MMICoSimulation;
using MMIUnity.TargetEngine;
using MMIUnity.TargetEngine.Scene;
using UnityEngine;

/// <summary>
/// For multiple agents scenes we need access to protected fields in AvatarBehavior.
/// Hack class, delete as soon as proper alternative is found.
/// </summary>
public class ExposedAvatarBehaviour : AvatarBehavior
{
    public MMIAvatar MMIAvatar => avatar;
    public MMICoSimulator MMICoSimulator => CoSimulator;
    public MMISettings MMISettings;

    /// <summary>
    /// The walk point an avatar will follow during walk operations by default
    /// </summary>
    public MMISceneObject LinkedWalkPoint { get; set; }

    // ToDo: Dirty trick to have avatar initialized during the spawn, fix it by moving initialization in Awake in AvatarBehavior
    void Awake()
    {
        avatar = GetComponent<MMIAvatar>(); // initialization is necessary in Awake()
    }

    protected override void Start()
    {
        // do nothing, so initialization won't happen twice
    }

    private void OnDrawGizmosSelected()
    {
        //if(LinkedWalkPoint != null)
            //Debug.DrawLine(transform.position, LinkedWalkPoint.transform.position, Color.red);
    }

    protected override void GUIBehaviorInput()
    {
        // base.GUIBehaviorInput(); // we don't need button from AvatarBehavior
    }
}
