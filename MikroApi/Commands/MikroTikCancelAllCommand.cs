namespace DanilovSoft.MikroApi
{
    internal class MikroTikCancelAllCommand : MikroTikCommand
    {
        internal MikroTikCancelAllCommand(string selfTag) : base("/cancel")
        {
            SetTag(selfTag);
        }
    }
}
