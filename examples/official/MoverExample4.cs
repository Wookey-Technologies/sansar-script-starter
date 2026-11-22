using Sansar;
using Sansar.Script;
using Sansar.Simulation;

// Enable "Movable From Script" on an upright object. If it has physics,
// set its motion type to Keyframed. The object bobs smoothly while turning
// at a constant rate around its local up axis.
public class MoverExample4 : SceneObjectScript
{
    public override void Init()
    {
        if (!ObjectPrivate.IsMovable)
        {
            Log.Write("MoverExample4 requires a movable object (Keyframed if it has physics).");
            return;
        }

        StartCoroutine(BobAndTurn);
    }

    private void BobAndTurn()
    {
        var mover = ObjectPrivate.Mover;
        var quarterTurn = Quaternion.FromAngleAxis(Mathf.PiOverTwo, Vector.ObjectUp);
        var up = new Vector(0.0f, 0.0f, 1.0f);
        var down = new Vector(0.0f, 0.0f, -1.0f);

        while (true)
        {
            // Each command uses one duration for both components. Smoothstep
            // eases the bob at its ends; Linear keeps the rotation rate steady.
            mover.AddMoveOffset(up, quarterTurn, 2.0, MoveMode.Smoothstep, MoveMode.Linear);
            // Wait for the second combined move's single completion event.
            WaitFor(done => mover.AddMoveOffset(down, quarterTurn, 2.0, MoveMode.Smoothstep, MoveMode.Linear, done));
        }
    }
}
