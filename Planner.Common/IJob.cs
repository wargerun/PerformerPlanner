namespace Planner.Common
{
    public interface IJob
    {
        string Name { get; }
        void Execute();
    }
}
