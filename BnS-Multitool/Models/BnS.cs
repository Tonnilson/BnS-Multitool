using BnS_Multitool.Extensions;
using BnS_Multitool.NCServices;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BnS_Multitool.Models
{
    /// <summary>
    /// BnS specific game stuff & service functionality
    /// </summary>
    // GameName, LoginAddr, CDN and IPAddress are custom attributes so you'll need the code from AttributeExtensions.cs
    public enum ERegion
    {
        [Description("NA")]
        [Region("NCWest")]
        [GameId("BnS_UE4")]
        [RegistryPath("SOFTWARE\\Wow6432Node\\NCWest\\BnS_UE4\\")]
        [LauncherAddr("updater.nclauncher.ncsoft.com")]
        [Cligate("cligate.ncsoft.com")]
        [CDN("d37ob46rk09il3.cloudfront.net")]
        [GameIPAddress("18.235.123.165")]
        NA,
        [Description("EU")]
        [Region("NCWest")]
        [GameId("BnS_UE4")]
        [RegistryPath("SOFTWARE\\Wow6432Node\\NCWest\\BnS_UE4\\")]
        [Cligate("cligate.ncsoft.com")]
        [LauncherAddr("updater.nclauncher.ncsoft.com")]
        [CDN("d37ob46rk09il3.cloudfront.net")]
        [GameIPAddress("3.75.38.202")]
        EU,
        [Description("TW")]
        [Region("NCTaiwan")]
        [GameId("TWBNSUE4")]
        [RegistryPath("SOFTWARE\\Wow6432Node\\NCTaiwan\\TWBNS22\\")]
        [LauncherAddr("up4svr.plaync.com.tw")]
        [Cligate("rccligate.plaync.com.tw")]
        [CDN("mmorepo.cdn.plaync.com.tw")]
        [GameIPAddress("210.242.83.163")]
        TW,
        [Description("JP")]
        [Region("NCJapan")]
        [GameId("BNS_JPN_UE4")]
        [RegistryPath("SOFTWARE\\Wow6432Node\\PlayNC\\BNS_JPN_UE4\\")]
        [LauncherAddr("BnSUpdate.ncsoft.jp")]
        [CDN("")]
        [GameIPAddress("106.186.46.101")]
        JP
    }

    public enum ServiceCommands
    {
        CompanyInfoRequest,
        ServiceInfoGameListRequest,
        GameInfoLauncherRequest,
        GameInfoUpdateRequest,
        GameInfoExeEnableRequest,
        ServiceInfoDisplayRequest,
        VersionInfoReleaseRequest,
        VersionInfoForwardRequest,
        GameInfoLanguageRequest,
        GameInfoLevelUpdateRequest,
        Max
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ELanguage
    {
        [Description("English")]
        EN,
        [Description("BPortuguese")] // Yes I have a B in there because the game uses that
        PT,
        [Description("German")]
        DE,
        [Description("French")]
        FR,
        [Description("ChineseT")]
        ZHTW,
        [Description("Japanese")]
        JP
    }

    public class BnS
    {
        private readonly Settings _settings;
        private readonly httpClient _httpClient;
        private ILogger<BnS> _logger;

        private DateTime _lastLookup;
        private string _buildNumber = "";
        
        public string BuildNumber
        {
            set { _buildNumber = value; }
            get
            {
                if (DateTime.Now >= _lastLookup || _LastRegion != _settings.Account.REGION)
                    QueryServices();
                return _buildNumber;
            }
        }

        private bool _loginAvailable = false;
        public bool LoginAvailable
        {
            set { _loginAvailable = value; }
            get
            {
                if (DateTime.Now >= _lastLookup || _LastRegion != _settings.Account.REGION)
                    QueryServices();

                return _loginAvailable;
            }
        }

        private string _RepositoryServerAddress = "";
        public string RepositoryServerAddress
        {
            set { _RepositoryServerAddress = value; }
            get
            {
                if (DateTime.Now >= _lastLookup || _LastRegion != _settings.Account.REGION)
                    QueryServices();

                return _RepositoryServerAddress;
            }
        }

        public BnS(Settings settings, ILogger<BnS> logger, httpClient http)
        {
            _settings = settings;
            _logger = logger;
            _httpClient = http;
        }

        public void Initialize()
        {
            // I was going to do something here.. I'll remember later... maybe.. hopefully?
            QueryServices();
        }

        private void QueryServices()
        {
            _buildNumber = NCLauncherService<string>(BnS.ServiceRequest.Build).GetAwaiter().GetResult(); // Still using this because of build relay option
            _loginAvailable = GetGameInfoExeEnable(_settings.Account.REGION)?.ExeEnableFlag != 0 ? true : false;
            var cdn = GetGameInfoUpdateRequest(_settings.Account.REGION);
            RepositoryServerAddress = cdn.RepositoryServerAddress ?? _settings.Account.REGION.GetAttribute<CDNAttribute>().Name;
            _LastRegion = _settings.Account.REGION;
            _lastLookup = DateTime.Now.AddMinutes(1);
        }

        public enum ServiceRequest
        {
            [DefaultValue((short)6)]
            Build,
            [DefaultValue((short)4)]
            Login,
            [DefaultValue((short)3)]
            CDN
        }

        public class BUILD_RELAY_RESPONSE
        {
            public string? BUILD_NUMBER { get; set; }
        }

        private Socket? _Socket;
        private ERegion _LastRegion;

        protected bool Connect(ERegion regionInfo = 0)
        {
            bool result = false;
            try
            {
                IPAddress ipaddress = Dns.GetHostAddresses(regionInfo.GetAttribute<LauncherAddrAttribute>().Name).FirstOrDefault();
                if (ipaddress == null)
                {
                    result = false;
                }
                else
                {
                    IPEndPoint remoteEP = new IPEndPoint(ipaddress, 27500);
                    _Socket = new Socket(ipaddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    IAsyncResult asyncResult = _Socket.BeginConnect(remoteEP, null, null);
                    if (!asyncResult.AsyncWaitHandle.WaitOne(3000, false))
                    {
                        throw new SocketException(10060);
                    }
                    _Socket.EndConnect(asyncResult);
                    _Socket.SendTimeout = 5000;
                    result = true;
                }
            }
            catch (SocketException ex)
            {
                if (_Socket != null)
                {
                    _Socket.Close();
                    _Socket = null;
                }
            }

            return result;
        }

        private void Disconnect()
        {
            if (_Socket != null)
            {
                try { _Socket.Shutdown(SocketShutdown.Both); }
                catch (Exception)
                {

                }
                finally
                {
                    _Socket.Close();
                    _Socket = null;
                }
            }
        }

        private dynamic SocketSendReceive<T>(byte[] sendBuffer, ERegion regionInfo = 0)
        {
            try
            {
                if (_Socket == null || !_Socket.Connected || _LastRegion != regionInfo)
                {
                    Disconnect();
                    Connect(regionInfo);
                }

                _LastRegion = regionInfo;

                IAsyncResult asyncResult = _Socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, null, null);
                if (asyncResult == null)
                {
                    return null;
                }
                else
                {
                    asyncResult.AsyncWaitHandle.WaitOne();
                    _Socket.EndSend(asyncResult);
                    asyncResult.AsyncWaitHandle.Close();
                    byte[] buffer = new byte[4096];
                    IAsyncResult asyncResult2 = _Socket.BeginReceive(buffer, 0, 4096, SocketFlags.None, null, null);
                    if (asyncResult2 == null)
                    {
                        return null;
                    }
                    else
                    {
                        asyncResult2.AsyncWaitHandle.WaitOne();
                        if (_Socket == null)
                        {
                            return null;
                        }
                        else
                        {
                            int length = _Socket.EndReceive(asyncResult2);
                            asyncResult2.AsyncWaitHandle.Close();
                            var pack = Deserialize<T>(buffer, length);

                            if (pack == null)
                                return null;

                            return (T)Convert.ChangeType(pack.Data, typeof(T));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public GameInfoUpdateAcknowledgement GetGameInfoUpdateRequest(ERegion regionInfo)
        {
            string gameId = regionInfo.GetAttribute<GameIdAttribute>().Name;
            GameInfoUpdateRequest data = new GameInfoUpdateRequest { GameId = gameId };
            PacketPack pack = PacketPack.Factory((ushort)ServiceCommands.GameInfoUpdateRequest, data);
            byte[] buffer = Serialize<GameInfoUpdateRequest>(pack);
            GameInfoUpdateAcknowledgement received = SocketSendReceive<GameInfoUpdateAcknowledgement>(buffer, regionInfo);
            return received;
        }

        public GameInfoLanguageAcknowledgement GetGameInfoLanguageRequest(ERegion regionInfo)
        {
            string gameId = regionInfo.GetAttribute<GameIdAttribute>().Name;
            GameInfoLanguageRequest data = new GameInfoLanguageRequest { GameId = gameId };
            PacketPack pack = PacketPack.Factory((ushort)ServiceCommands.GameInfoLanguageRequest, data);
            byte[] buffer = Serialize<GameInfoLanguageRequest>(pack);
            GameInfoLanguageAcknowledgement received = SocketSendReceive<GameInfoLanguageAcknowledgement>(buffer, regionInfo);
            return received;
        }

        public ServiceInfoDisplayAcknowledgement GetServiceInfoDisplay(ERegion regionInfo)
        {
            string gameId = regionInfo.GetAttribute<GameIdAttribute>().Name;
            ServiceInfoDisplayRequest data = new ServiceInfoDisplayRequest { GameId = gameId, CompanyId = 15 };
            PacketPack pack = PacketPack.Factory((ushort)ServiceCommands.ServiceInfoDisplayRequest, data);
            byte[] buffer = Serialize<ServiceInfoDisplayRequest>(pack);
            ServiceInfoDisplayAcknowledgement received = SocketSendReceive<ServiceInfoDisplayAcknowledgement>(buffer, regionInfo);
            return received;
        }

        public VersionInfoReleaseAcknowledgement GetVersionInfoRelease(ERegion regionInfo)
        {
            string gameId = regionInfo.GetAttribute<GameIdAttribute>().Name;
            VersionInfoReleaseRequest data = new VersionInfoReleaseRequest { GameId = gameId };
            PacketPack pack = PacketPack.Factory((ushort)ServiceCommands.VersionInfoReleaseRequest, data);
            byte[] buffer = Serialize<VersionInfoReleaseRequest>(pack);
            VersionInfoReleaseAcknowledgement received = SocketSendReceive<VersionInfoReleaseAcknowledgement>(buffer, regionInfo);
            return received;
        }

        public GameInfoExeEnableAcknowledgement GetGameInfoExeEnable(ERegion regionInfo)
        {
            string gameId = regionInfo.GetAttribute<GameIdAttribute>().Name;
            GameInfoExeEnableRequest data = new GameInfoExeEnableRequest { GameId = gameId };
            PacketPack pack = PacketPack.Factory((ushort)ServiceCommands.GameInfoExeEnableRequest, data);
            byte[] buffer = Serialize<GameInfoExeEnableRequest>(pack);
            GameInfoExeEnableAcknowledgement received = SocketSendReceive<GameInfoExeEnableAcknowledgement>(buffer, regionInfo);
            return received;
        }

        private byte[] Serialize<T>(PacketPack pack)
        {
            byte[] result = null;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Position = 4L;
                ProtoBuf.Serializer.Serialize<T>(ms, (T)Convert.ChangeType(pack.Data, typeof(T)));
                ushort value = (ushort)ms.Position;
                ushort num = (ushort)(ms.Position - 4L);
                ms.Position = 0L;
                byte[] bytes = BitConverter.GetBytes(value);
                ms.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(pack.Command);
                ms.Write(bytes, 0, bytes.Length);
                result = ms.ToArray();
            }
            return result;
        }

        private PacketPack Deserialize<T>(byte[] buffer, int length)
        {
            BitConverter.ToInt16(buffer, 0);
            ushort num = (ushort)BitConverter.ToInt16(buffer, 2);
            if ((ushort)BitConverter.ToInt32(buffer, 4) != 0)
                return null;

            PacketPack packetPack;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(buffer, 8, length - 8);
                ms.Position = 0L;
                packetPack = new PacketPack { Command = num };
                packetPack.Data = ProtoBuf.Serializer.Deserialize<T>(ms);
            }

            return packetPack;
        }

        /*
            This is overly complex for no reason, don't take notes from me. This should be deprecated but I like keeping for reference
            It's preferred you use protobuf (protocol buffer serializer) and the above code rather than this, protobuf should be available for various languages.
            For those that want to port this to a different language and don't know C# and can't or won't use protobuf, this is the basic packet structure.
                        size of packet, null, query type, null, 10 (0xA), length of game_name, game_name
                        (Byte array: 0x0D, 0x00, 0x06, 0x00, 0x0A, 0x07, 0x42, 0x6E, 0x53, 0x5F, 0x55, 0x45, 0x34)

                        query types:
                            1 = game list
                            2 = Launch params?
                            3 = BASE_UPDATE_URL
                            4 = in-game login service status
                            5 = info service
                            6 = Current Region info (Game build # etc)
                            7 = Future region info (same as above but prepatch)
                            8 = info language
                            9 = info level update (deprecated)

                        game_name is just a region build identifier i.e BnS_UE4 for NCW live server
        */
        public async Task<T> NCLauncherService<T>(ServiceRequest req)
        {
            if(req == ServiceRequest.Build && _settings.System.BUILD_RELAY && _settings.Account.REGION != ERegion.TW)
            {
                try
                {
                    BUILD_RELAY_RESPONSE response = await _httpClient.DownloadJson<BUILD_RELAY_RESPONSE>(Properties.Settings.Default.MainServerAddr + "build_relay.json");
                    return (T)Convert.ChangeType(response.BUILD_NUMBER, typeof(T));
                    
                } catch (Exception ex)
                {
                    // I only log that there was an issue using my server as the relay, that way the user can try the regular NC service to try and get the build #
                    _logger.LogError(ex, "Failed to contact relay server");
                }
            }

            try
            {
                short queryType = req.GetDefaultValue();
                var loginServer = _settings.Account.REGION.GetAttribute<LauncherAddrAttribute>().Name ?? ERegion.NA.GetAttribute<LauncherAddrAttribute>().Name;
                var gameName = _settings.Account.REGION.GetAttribute<GameIdAttribute>().Name ?? ERegion.NA.GetAttribute<GameIdAttribute>().Name;
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(loginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write(queryType);
                bw.Write((byte)10);
                bw.Write((byte)gameName.Length);
                bw.Write(Encoding.ASCII.GetBytes(gameName));
                bw.BaseStream.Position = 0L;
                bw.Write((short)ms.Length);

                ns.Write(ms.ToArray(), 0, (int)ms.Length);
                bw.Dispose();
                ms.Dispose();

                ms = new MemoryStream();
                BinaryReader br = new BinaryReader(ms);

                byte[] byte_array = new byte[1024];
                int num = 0;
                do
                {
                    num = await ns.ReadAsync(byte_array, 0, byte_array.Length);
                    if (num > 0)
                        ms.Write(byte_array, 0, num);
                } while (num == byte_array.Length);

                if(req == ServiceRequest.Build)
                {
                    ms.Position = 9L;
                    br.ReadBytes(br.ReadByte() + 5);
                    int version = br.ReadByte(); // This should be offset 0x22
                    if (br.ReadInt16() != 40)
                    {
                        ms.Position -= 2;
                        version += 128 * (br.ReadByte() - 1);
                    }

                    return (T)Convert.ChangeType(version.ToString(), typeof(T));
                } else if (req == ServiceRequest.Login)
                {
                    ms.Position = ms.Length - 1;
                    var loginAvailable = br.ReadBoolean();
                    return (T)Convert.ChangeType(loginAvailable, typeof(T));
                } else
                {
                    br.ReadBytes(br.ReadByte() + 1);
                    var url = br.ReadString();
                    return (T)Convert.ChangeType(url, typeof(T));
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error getting result for service {0}", req.GetType().Name));
                if (typeof(T) == typeof(bool)) return (T)Convert.ChangeType(false,typeof(T));
                return (T)Convert.ChangeType(string.Empty, typeof(T));
            }
        }
    }
}
