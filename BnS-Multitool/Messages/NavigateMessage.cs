using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BnS_Multitool.Messages
{
    public class NavigateMessage : ValueChangedMessage<string>
    {
        public NavigateMessage(string value) : base(value)
        {

        }
    }
}