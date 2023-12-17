namespace Celeste.Mod.LessCoreInputLag;

public class LessCoreInputLagSettings : EverestModuleSettings
{
    private SolutionEnum solution = SolutionEnum.None;

    public SolutionEnum Solution
    {
        get => solution;
        set
        {
            if (value != solution)
                Instance?.SwitchToSolution(value);
            solution = value;
        }
    }
}