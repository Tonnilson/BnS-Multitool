namespace BnS_Multitool.NCServices
{
    internal class PacketPack
    {
        public ushort Command { get; set; }
        public object Data { get; set; }

        public static PacketPack Factory(ushort command, object data)
        {
            return new PacketPack
            {
                Command = command,
                Data = data
            };
        }
    }
}
