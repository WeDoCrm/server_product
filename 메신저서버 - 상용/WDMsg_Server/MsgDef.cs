namespace WDMsgServer
{
    public class ConstDef {
        public const string APP_CONFIG_NAME = "{0}.exe.config";
        public const string WINPCAP = "\\WinPcap_4_1_2.exe";
        public const string WINPCAP_INSTALLNAME = "WinPcap 4.1.2";
        public const string RBT_TYPE_SIP = "rbt_type_sip";
        public const string REG_APP_NAME = "WeDo Server";
        public const string REG_APP_NAME_DEMO = "WeDo Server Demo";
        public const string WORK_DIR = "c:\\MiniCTI";
        public const string UPDT_DIR = "\\AutoUpdater_Server";
        public const string UPDT_DIR_DEMO = "\\AutoUpdater_Server_Demo";
        public const string NIC_SIP = "SIP";
        public const string NIC_LG_KP = "LG";
        public const string NIC_CID_PORT1 = "CI1";
        public const string NIC_CID_PORT2 = "CI2";
        public const string NIC_SS_KP = "SS";

        public const bool DEBUG = false;

        public const string LICENSE_KEY = "jf04234[0945252";
        public const string LICENSE_DIR = "\\license\\";
        public const string LICENSE_FILE = "*license.ini";

        public const string VERSION = "2.0.12";

    }

    public enum LicenseResult
    {
        ERR_MAC_ADDR = -4,
        ERR_NO_FILE = -3,
        ERR_INVALID_FILE = -2,
        ERR_UNREGISTERED = -1,
        ERR_EXPIRED = 0,
        SUCCESS = 1,
        WARN_30_DAYS = 2,
        WARN_7_DAYS = 3
    }
}