using CommunityToolkit.Mvvm.Messaging.Messages;
namespace BnS_Multitool.Messages
{
    public class LaunchMessage : ValueChangedMessage<int>
    {
        public LaunchMessage(int value) : base(value)
        {

        }
    }
}