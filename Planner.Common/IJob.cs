namespace Planner.Common
{
    public interface IJob
    {
        string Name { get; }

        void OnStartUp();

        void Execute(System.Threading.CancellationToken cancellationToken);
    }
}
