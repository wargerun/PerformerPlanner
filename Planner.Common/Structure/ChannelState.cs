namespace Planner.Common.Structure
{
    public enum ChannelState
    {
        /// <summary>
        /// требуется открыть вкладку, так как стример подрубил
        /// </summary>
        New,

        /// <summary>
        /// Активный канал
        /// </summary>
        Live,

        /// <summary>
        /// Стример офлайн, можно закрыть вкладку 
        /// </summary>
        Ofline,
    }
}
