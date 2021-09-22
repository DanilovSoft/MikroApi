namespace DanilovSoft.MikroApi
{
    internal class MikroTikCancelAllCommand : MikroTikCommand
    {
        /// <summary>
        /// Собственный тег.
        /// </summary>
        private readonly string _selfTag;
        private readonly MikroTikSocket _socket;

        internal MikroTikCancelAllCommand(string selfTag, MikroTikSocket socket) : base("/cancel")
        {
            _selfTag = selfTag;
            _socket = socket;

            SetTag(selfTag);
        }
    }
}
