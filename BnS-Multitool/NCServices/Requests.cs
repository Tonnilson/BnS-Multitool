using ProtoBuf;
using System;
using System.ComponentModel;

namespace BnS_Multitool.NCServices
{
    [ProtoContract(Name = "GameInfoUpdateRequest")]
    [Serializable]
    public class GameInfoUpdateRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "CompanyInfoRequest")]
    [Serializable]
    public class CompanyInfoRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "CompanyId", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint CompanyId
        {
            get { return _CompanyId; }
            set { _CompanyId = value; }

        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private uint _CompanyId;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoExeEnableRequest")]
    [Serializable]
    public class GameInfoExeEnableRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }
        private string _GameId = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoLanguageRequest")]
    [Serializable]
    public class GameInfoLanguageRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoLaunchRequest")]
    [Serializable]
    public class GameInfoLaunchRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "GameInfoLevelUpdateRequest")]
    [Serializable]
    public class GameInfoLevelUpdateRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "ServiceInfoDisplayRequest")]
    [Serializable]
    public class ServiceInfoDisplayRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        [ProtoMember(2, IsRequired = false, Name = "CompanyId", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint CompanyId
        {
            get { return _CompanyId; }
            set { _CompanyId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private uint _CompanyId;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "ServiceInfoGameListRequest")]
    [Serializable]
    public class ServiceInfoGameListRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "CompanyId", DataFormat = DataFormat.TwosComplement)]
        [DefaultValue(0L)]
        public uint CompanyId
        {
            get { return _CompanyId; }
            set { _CompanyId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private uint _CompanyId;
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "VersionInfoForwardRequest")]
    [Serializable]
    public class VersionInfoForwardRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private IExtension extensionObject;
    }

    [ProtoContract(Name = "VersionInfoReleaseRequest")]
    [Serializable]
    public class VersionInfoReleaseRequest : IExtensible
    {
        [ProtoMember(1, IsRequired = false, Name = "GameId", DataFormat = DataFormat.Default)]
        [DefaultValue("")]
        public string GameId
        {
            get { return _GameId; }
            set { _GameId = value; }
        }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref this.extensionObject, createIfMissing);
        }

        private string _GameId = "";
        private IExtension extensionObject;
    }
}
