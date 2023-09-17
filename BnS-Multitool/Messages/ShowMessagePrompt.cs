using CommunityToolkit.Mvvm.Messaging.Messages;
using static BnS_Multitool.Services.MessageService;

namespace BnS_Multitool.Messages
{
    public class ShowMessagePrompt : ValueChangedMessage<MessagePrompt>
    {
        public ShowMessagePrompt(MessagePrompt value) : base(value)
        {
        }
    }
}
