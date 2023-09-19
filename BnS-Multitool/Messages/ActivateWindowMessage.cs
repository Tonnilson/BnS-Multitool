using CommunityToolkit.Mvvm.Messaging.Messages;
namespace BnS_Multitool.Messages
{
    public class ActivateWindowMessage : ValueChangedMessage<bool>
    {
        public ActivateWindowMessage(bool value) : base(value)
        {
        }
    }
}
