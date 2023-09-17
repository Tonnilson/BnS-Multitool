using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BnS_Multitool.Messages
{
    public class CloseMessage : ValueChangedMessage<bool>
    {
        public CloseMessage(bool value) : base(value)
        {
        }
    }
}
