namespace Planner.Common.Structure
{
    public enum TabState
    {
        /// <summary>
        /// Новая вкладка
        /// </summary>
        New,

        /// <summary>
        /// Активная вкладка, становится на следующую итерацию после новой
        /// </summary>
        Active,

        /// <summary>
        /// Закрытая вкладка
        /// </summary>
        Closed,
    }
}
