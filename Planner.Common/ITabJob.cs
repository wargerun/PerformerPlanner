using Planner.Common.Structure;

namespace Planner.Common
{
    public interface ITabJob
    {
        string Name { get; }

        /// <summary>
        /// Выполняется на открытие новой вкладки
        /// </summary>
        void OnStartUp();

        /// <summary>
        /// Может ли выполнится метод Execute
        /// </summary>
        /// <param name="browserTab"></param>
        /// <returns></returns>
        bool CanExecute(BrowserTab browserTab);

        /// <summary>
        /// Выполняется на каждую итерацию джоба
        /// </summary>
        /// <param name="cancellationToken"></param>
        void Execute(System.Threading.CancellationToken cancellationToken);

        /// <summary>
        /// Выполняется после закрытия вкладки
        /// </summary>
        void OnClosed();
    }
}
