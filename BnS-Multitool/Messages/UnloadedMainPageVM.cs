using CommunityToolkit.Mvvm.Messaging.Messages;
namespace BnS_Multitool.Messages
{
    public class UnloadedMainPageVM : ValueChangedMessage<bool>
    {
        public UnloadedMainPageVM(bool value) : base(value)
        {
        }
    }
}
