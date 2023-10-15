using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BnS_Multitool.Messages
{
    public class NotifyIconMessage : ValueChangedMessage<bool>
    {
        public NotifyIconMessage(bool value) : base(value)
        {
        }
    }
}
