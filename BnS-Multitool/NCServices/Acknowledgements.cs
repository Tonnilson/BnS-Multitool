using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BnS_Multitool.NCServices
{
    [ProtoContract(Name = "GameInfoUpdateAcknowledgement")]
    [Serializable]
    public class GameInfoUpdateAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        [ProtoMember(2, IsRequired = false, Name = "RepositoryServerAddress", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string RepositoryServerAddress
        {
            get { return _RepositoryServerAddress; }
            set { _RepositoryServerAddress = value; }
        }

        [ProtoMember(3, IsRequired = false, Name = "TrackerAddress", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string TrackerAddress
        {
            get
            {
                return _TrackerAddress;
            }
            set { _TrackerAddress = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private string _RepositoryServerAddress = "";
        private string _TrackerAddress = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "CompanyInfoAcknowledgement")]
    [Serializable]
    public class CompanyInfoAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "CompanyId", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint CompanyId
        {
            get
            {
                return this._CompanyId;
            }
            set
            {
                this._CompanyId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "CompanyName", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string CompanyName
        {
            get
            {
                return this._CompanyName;
            }
            set
            {
                this._CompanyName = value;
            }
        }

        [ProtoMember(3, IsRequired = false, Name = "LicenseVersion", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint LicenseVersion
        {
            get
            {
                return this._LicenseVersion;
            }
            set
            {
                this._LicenseVersion = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private uint _CompanyId;
        private string _CompanyName = "";
        private uint _LicenseVersion;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoExeEnableAcknowledgement")]
    [Serializable]
    public class GameInfoExeEnableAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "ExeEnableFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint ExeEnableFlag
        {
            get
            {
                return this._ExeEnableFlag;
            }
            set
            {
                this._ExeEnableFlag = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private uint _ExeEnableFlag;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoLanguageAcknowledgement")]
    [Serializable]
    public class GameInfoLanguageAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "GameLanguageArgument", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameLanguageArgument
        {
            get
            {
                return this._GameLanguageArgument;
            }
            set
            {
                this._GameLanguageArgument = value;
            }
        }

        [ProtoMember(3, IsRequired = false, Name = "GameLanguagePak", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameLanguagePak
        {
            get
            {
                return this._GameLanguagePak;
            }
            set
            {
                this._GameLanguagePak = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private string _GameLanguageArgument = "";
        private string _GameLanguagePak = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoLaunchAcknowledgement")]
    [Serializable]
    public class GameInfoLaunchAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "ExeArgument", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string ExeArgument
        {
            get
            {
                return this._ExeArgument;
            }
            set
            {
                this._ExeArgument = value;
            }
        }

        [ProtoMember(3, IsRequired = false, Name = "ExeFileName", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string ExeFileName
        {
            get
            {
                return this._ExeFileName;
            }
            set
            {
                this._ExeFileName = value;
            }
        }

        [ProtoMember(4, IsRequired = false, Name = "GameName", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameName
        {
            get
            {
                return this._GameName;
            }
            set
            {
                this._GameName = value;
            }
        }

        [ProtoMember(5, IsRequired = false, Name = "UpdateMethodCode", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint UpdateMethodCode
        {
            get
            {
                return this._UpdateMethodCode;
            }
            set
            {
                this._UpdateMethodCode = value;
            }
        }

        [ProtoMember(6, IsRequired = false, Name = "UpdateServerAddress2", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string UpdateServerAddress2
        {
            get
            {
                return this._UpdateServerAddress2;
            }
            set
            {
                this._UpdateServerAddress2 = value;
            }
        }

        [ProtoMember(7, IsRequired = false, Name = "AutoUpdateFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint AutoUpdateFlag
        {
            get
            {
                return this._AutoUpdateFlag;
            }
            set
            {
                this._AutoUpdateFlag = value;
            }
        }

        [ProtoMember(8, IsRequired = false, Name = "AutoExeFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint AutoExeFlag
        {
            get
            {
                return this._AutoExeFlag;
            }
            set
            {
                this._AutoExeFlag = value;
            }
        }

        [ProtoMember(9, IsRequired = false, Name = "AutoSeparateUpdateFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint AutoSeparateUpdateFlag
        {
            get
            {
                return this._AutoSeparateUpdateFlag;
            }
            set
            {
                this._AutoSeparateUpdateFlag = value;
            }
        }

        [ProtoMember(10, IsRequired = false, Name = "AutoInstallFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint AutoInstallFlag
        {
            get
            {
                return this._AutoInstallFlag;
            }
            set
            {
                this._AutoInstallFlag = value;
            }
        }

        [ProtoMember(11, IsRequired = false, Name = "AutoExitFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint AutoExitFlag
        {
            get
            {
                return this._AutoExitFlag;
            }
            set
            {
                this._AutoExitFlag = value;
            }
        }

        [ProtoMember(12, IsRequired = false, Name = "HideFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint HideFlag
        {
            get
            {
                return this._HideFlag;
            }
            set
            {
                this._HideFlag = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private string _ExeArgument = "";
        private string _ExeFileName = "";
        private string _GameName = "";
        private uint _UpdateMethodCode;
        private string _UpdateServerAddress2 = "";
        private uint _AutoUpdateFlag;
        private uint _AutoExeFlag;
        private uint _AutoSeparateUpdateFlag;
        private uint _AutoInstallFlag;
        private uint _AutoExitFlag;
        private uint _HideFlag;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoLevelUpdateAcknowledgement")]
    [Serializable]
    public class GameInfoLevelUpdateAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "GameLevelUpdate", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameLevelUpdate
        {
            get
            {
                return this._GameLevelUpdate;
            }
            set
            {
                this._GameLevelUpdate = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }
        private string _GameId = "";
        private string _GameLevelUpdate = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "ServiceInfoDisplayAcknowledgement")]
    [Serializable]
    public class ServiceInfoDisplayAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "CompanyId", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint CompanyId
        {
            get
            {
                return this._CompanyId;
            }
            set
            {
                this._CompanyId = value;
            }
        }

        [ProtoMember(3, IsRequired = false, Name = "SkinName", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string SkinName
        {
            get
            {
                return this._SkinName;
            }
            set
            {
                this._SkinName = value;
            }
        }

        [ProtoMember(4, IsRequired = false, Name = "PostPageUrl", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string PostPageUrl
        {
            get
            {
                return this._PostPageUrl;
            }
            set
            {
                this._PostPageUrl = value;
            }
        }

        [ProtoMember(5, IsRequired = false, Name = "FullDownloadUrl", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string FullDownloadUrl
        {
            get
            {
                return this._FullDownloadUrl;
            }
            set
            {
                this._FullDownloadUrl = value;
            }
        }

        [ProtoMember(6, IsRequired = false, Name = "FullDownloadType", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint FullDownloadType
        {
            get
            {
                return this._FullDownloadType;
            }
            set
            {
                this._FullDownloadType = value;
            }
        }

        [ProtoMember(7, IsRequired = false, Name = "FullDownloadSize", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string FullDownloadSize
        {
            get
            {
                return this._FullDownloadSize;
            }
            set
            {
                this._FullDownloadSize = value;
            }
        }

        [ProtoMember(8, IsRequired = false, Name = "FullDownloadVersion", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string FullDownloadVersion
        {
            get
            {
                return this._FullDownloadVersion;
            }
            set
            {
                this._FullDownloadVersion = value;
            }
        }

        [ProtoMember(9, IsRequired = false, Name = "GameDisplayCode", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint GameDisplayCode
        {
            get
            {
                return this._GameDisplayCode;
            }
            set
            {
                this._GameDisplayCode = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private uint _CompanyId;
        private string _SkinName = "";
        private string _PostPageUrl = "";
        private string _FullDownloadUrl = "";
        private uint _FullDownloadType;
        private string _FullDownloadSize = "";
        private string _FullDownloadVersion = "";
        private uint _GameDisplayCode;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "ServiceInfoGameListAcknowledgement")]
    [Serializable]
    public class ServiceInfoGameListAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "CompanyId", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint CompanyId
        {
            get
            {
                return this._CompanyId;
            }
            set
            {
                this._CompanyId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "Count", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint Count
        {
            get
            {
                return this._Count;
            }
            set
            {
                this._Count = value;
            }
        }

        [ProtoMember(3, Name = "GameId", DataFormat = DataFormat.Default)]
        public List<string> GameId
        {
            get
            {
                return this._GameId;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private uint _CompanyId;
        private uint _Count;
        private readonly List<string> _GameId = new List<string>();
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "VersionInfoForwardAcknowledgement")]
    [Serializable]
    public class VersionInfoForwardAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "P2PEnableFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint P2PEnableFlag
        {
            get
            {
                return this._P2PEnableFlag;
            }
            set
            {
                this._P2PEnableFlag = value;
            }
        }

        [ProtoMember(3, IsRequired = false, Name = "P2PPeerCount", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint P2PPeerCount
        {
            get
            {
                return this._P2PPeerCount;
            }
            set
            {
                this._P2PPeerCount = value;
            }
        }

        [ProtoMember(4, IsRequired = false, Name = "ForwardVersion", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint ForwardVersion
        {
            get
            {
                return this._ForwardVersion;
            }
            set
            {
                this._ForwardVersion = value;
            }
        }

        [ProtoMember(5, IsRequired = false, Name = "ForwardMultiVersionUpdateMode", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint ForwardMultiVersionUpdateMode
        {
            get
            {
                return this._ForwardMultiVersionUpdateMode;
            }
            set
            {
                this._ForwardMultiVersionUpdateMode = value;
            }
        }

        [ProtoMember(6, IsRequired = false, Name = "ForwardSequentialUpdateThreshold", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint ForwardSequentialUpdateThreshold
        {
            get
            {
                return this._ForwardSequentialUpdateThreshold;
            }
            set
            {
                this._ForwardSequentialUpdateThreshold = value;
            }
        }

        [ProtoMember(7, IsRequired = false, Name = "ForwardDirectUpdateThreshold", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint ForwardDirectUpdateThreshold
        {
            get
            {
                return this._ForwardDirectUpdateThreshold;
            }
            set
            {
                this._ForwardDirectUpdateThreshold = value;
            }
        }

        [ProtoMember(8, IsRequired = false, Name = "ForwardFileInfoHashValue", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string ForwardFileInfoHashValue
        {
            get
            {
                return this._ForwardFileInfoHashValue;
            }
            set
            {
                this._ForwardFileInfoHashValue = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private uint _P2PEnableFlag;
        private uint _P2PPeerCount;
        private uint _ForwardVersion;
        private uint _ForwardMultiVersionUpdateMode;
        private uint _ForwardSequentialUpdateThreshold;
        private uint _ForwardDirectUpdateThreshold;
        private string _ForwardFileInfoHashValue = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "VersionInfoReleaseAcknowledgement")]
    [Serializable]
    public class VersionInfoReleaseAcknowledgement : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get
            {
                return this._GameId;
            }
            set
            {
                this._GameId = value;
            }
        }

        [ProtoMember(2, IsRequired = false, Name = "P2PEnableFlag", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint P2PEnableFlag
        {
            get
            {
                return this._P2PEnableFlag;
            }
            set
            {
                this._P2PEnableFlag = value;
            }
        }

        [ProtoMember(3, IsRequired = false, Name = "P2PPeerCount", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint P2PPeerCount
        {
            get
            {
                return this._P2PPeerCount;
            }
            set
            {
                this._P2PPeerCount = value;
            }
        }

        [ProtoMember(4, IsRequired = false, Name = "GlobalVersion", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint GlobalVersion
        {
            get
            {
                return this._GlobalVersion;
            }
            set
            {
                this._GlobalVersion = value;
            }
        }

        [ProtoMember(5, IsRequired = false, Name = "DownloadFileIndex", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint DownloadFileIndex
        {
            get
            {
                return this._DownloadFileIndex;
            }
            set
            {
                this._DownloadFileIndex = value;
            }
        }

        [ProtoMember(6, IsRequired = false, Name = "TotalDownloadFileCount", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint TotalDownloadFileCount
        {
            get
            {
                return this._TotalDownloadFileCount;
            }
            set
            {
                this._TotalDownloadFileCount = value;
            }
        }

        [ProtoMember(7, IsRequired = false, Name = "MultiVersionUpdateMode", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint MultiVersionUpdateMode
        {
            get
            {
                return this._MultiVersionUpdateMode;
            }
            set
            {
                this._MultiVersionUpdateMode = value;
            }
        }

        [ProtoMember(8, IsRequired = false, Name = "SequentialUpdateThreshold", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint SequentialUpdateThreshold
        {
            get
            {
                return this._SequentialUpdateThreshold;
            }
            set
            {
                this._SequentialUpdateThreshold = value;
            }
        }

        [ProtoMember(9, IsRequired = false, Name = "DirectUpdateThreshold", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint DirectUpdateThreshold
        {
            get
            {
                return this._DirectUpdateThreshold;
            }
            set
            {
                this._DirectUpdateThreshold = value;
            }
        }

        [ProtoMember(10, IsRequired = false, Name = "FileInfoHashValue", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string FileInfoHashValue
        {
            get
            {
                return this._FileInfoHashValue;
            }
            set
            {
                this._FileInfoHashValue = value;
            }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private uint _P2PEnableFlag;
        private uint _P2PPeerCount;
        private uint _GlobalVersion;
        private uint _DownloadFileIndex;
        private uint _TotalDownloadFileCount;
        private uint _MultiVersionUpdateMode;
        private uint _SequentialUpdateThreshold;
        private uint _DirectUpdateThreshold;
        private string _FileInfoHashValue = "";
        private IExtension extensionObject;
    }
}
