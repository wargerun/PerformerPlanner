namespace Planner.Common
{
    public interface IJob
    {
        string Name { get; }
        void Execute(System.Threading.CancellationToken cancellationToken);
    }
}
