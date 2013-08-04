using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Data.SqlClient;
using System.Xml;
using PacketDotNet;
using SharpPcap;
using System.Diagnostics;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data;
using MySql.Data.Types;
using CallControl;
using System.Net.NetworkInformation;
using WindowsInstaller;
using Microsoft.Win32;


namespace WDMsgServer
{
    public partial class MsgSvrForm : Form
    {

        #region 기본 서버설정 정보...

        //라이센스파일로 교체 2013/05/13
        //라이선스 서버 정보
        //private string SVR_HOST = null;
        //private string LICENSE_PORT = null;
        private LicenseHandler mLicenseHandler;
        private string licenseDir;
        private string MACID = null;
        private string mVersion;

        private static System.Windows.Forms.Timer timerForLicense;


        //DB 정보
        private string WDdbHost = null;
        private string WDdbName = null;
        private string WDdbUser = null;
        private string WDdbPass = null;


        //기본 설정 정보
        private string AppName = "";
        private string AppConfigName = "";
        private string AppRegName = "";
        private string UpdateTargetDir = "";
        private string UpdateAppDir = "";


        private System.Windows.Forms.Timer callLog_timer;
        private int listenport = 0;
        private int sendport = 0;
        private int checkport = 0;
        private int crmport = 0;
        private int fileport = 0;
        private IPEndPoint sender = null;
        private IPEndPoint filesender = null;
        private Socket crmSocket = null;
        protected internal static bool svrStart = false;
        private AddTextDelegate AddText = null;
        private bool listnerstarted = false;
        private StreamWriter sw = null;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private string[] teamName = null;
        private XmlDocument xmldoc = new XmlDocument();
        private SIPMessage SIPM = null;
        private Hashtable clientList = null;
        private ICaptureDevice dev = null;
        private SetNICForm nicform = null;
        private CaptureDeviceList deviceList = null;
        private SerialPort serialport;
        private string server_type = null;
        private string server_device = null;
        private string com_code;
        private string com_name;
        private int auto_start;
        private bool WinPcap = false;
        private MySqlConnection conn_callog = new MySqlConnection();
        private MySqlConnection conn_ring = new MySqlConnection();

        private CommCtl commctl = new CommCtl();
        private static Thread ReceiverThread = null;
        private static Thread StatThread = null;
        private Thread ListenThread = null;
        private Thread CheckThread;
        private Thread CrmReceiverThread;
        private ArrayList tList = new ArrayList();          //팀이름 저장
        private Hashtable ThreadList = new Hashtable();
        private Hashtable CustomerList_Primary = new Hashtable();
        private Hashtable CustomerList_Backup = new Hashtable();
        private Hashtable Statlist = new Hashtable();
        private Hashtable TeamNameList = new Hashtable();
        private Hashtable SocketList = new Hashtable();
        private ArrayList SendErrorList = new ArrayList();
        protected internal static string serverip = null;
        private DirectoryInfo di = null; //로그폴더

        public Hashtable InClientList = null;  //로그인 사용자 EndPoint정보 테이블(key=id, value=IPEndPoint)
        private Hashtable InClientStat = new Hashtable();
        private Hashtable ExtensionList = new Hashtable(); //로그인 사용자 내선리스트(key = 내선번호, value = IPEndPoint)
        private Hashtable CallLogTable = new Hashtable();
        private Hashtable ClientInfoList = new Hashtable();
        private Hashtable ExtensionIDpair = new Hashtable();//내선과 id 정보 테이블

        private ArrayList TeamList = new ArrayList();  //구성 : M|팀이름|id|이름|....
        private UdpClient filesock = null;
        private UdpClient listenSock = null;
        private UdpClient sendSock = null;
        private UdpClient checkSock = null;
        private Socket statsock = null;
        private Socket tcpsock = null;
        private Socket licensesock = null;

        private MemberListForm MemList = null;
        private CallTestForm calltestform = null;
        private DBInfoForm dbinfo = null;
        private bool CanFileWrite = false;
        private bool CustomerCacheSwitch = false;
        private bool CustomerCacheReload = false;
        private static bool serviceStart = false;
        Process winp;

        delegate void stringDele(string str);
        delegate void ringingDele(string st1, string st2, string st3);
        delegate void AbandonDele();
        delegate void NoParamDele();

        #endregion

        public MsgSvrForm()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                
            }
        }

        private void MsgSvrForm_Load(object sender, EventArgs e)
        {

            string temp = Process.GetCurrentProcess().ProcessName;
            if (temp.IndexOf('.') < 0 ) 
            {
                AppName = temp;
            } else {
                AppName = temp.Substring(0, temp.IndexOf('.'));
            }

            AppConfigName = string.Format(ConstDef.APP_CONFIG_NAME, temp);
            AppRegName = ConstDef.REG_APP_NAME;
            UpdateTargetDir = ConstDef.WORK_DIR + ConstDef.UPDT_DIR;
            UpdateAppDir = Application.StartupPath + ConstDef.UPDT_DIR;
            licenseDir = ConstDef.WORK_DIR + ConstDef.LICENSE_DIR;

            svr_FileCheck();                        //로그파일, 폴더 생성
            logWrite("svr_FileCheck() 완료!");
            commctl.OnEvent += new CommCtl.CommCtl_MessageDelegate(RecvMessage);
            loadConfigData();
            timerForLicense = new System.Windows.Forms.Timer();
            timerForLicense.Interval = 3600000;
            timerForLicense.Tick += new EventHandler(timerForLicense_Tick);
            timerForLicense.Start();

            callLog_timer = new System.Windows.Forms.Timer();
            callLog_timer.Interval = 300000;
            callLog_timer.Tick += new EventHandler(callLog_timer_Tick);
            callLog_timer.Start();

            if (server_type != null && server_device != null)
            {
                startServer();
            }
            else
            {
                setDevice();
            }
        }

        private void callLog_timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (server_type.Equals(ConstDef.NIC_CID_PORT1) 
                    || server_type.Equals(ConstDef.NIC_CID_PORT2)
                    || server_type.Equals(ConstDef.NIC_CID_PORT4)
                    || server_type.Equals(ConstDef.NIC_LG_KP))
                {
                    lock (CallLogTable)
                    {
                        foreach (DictionaryEntry de in CallLogTable)
                        {
                            string call_id = de.Key.ToString();
                            string[] arr = call_id.Split('$');
                            if (arr.Length > 1)
                            {
                                string time = arr[0]; //yyyyMMddHHmmss
                                int year = Convert.ToInt32(time.Substring(0, 4));
                                int month = Convert.ToInt32(time.Substring(4, 2));
                                int day = Convert.ToInt32(time.Substring(6, 2));
                                int hour = Convert.ToInt32(time.Substring(8, 2));
                                int minute = Convert.ToInt32(time.Substring(10, 2));
                                int second = Convert.ToInt32(time.Substring(12, 2));

                                if (day != DateTime.Now.Day)
                                {
                                    logWrite("CallLogTable[" + de.Key.ToString() + "] 삭제");
                                    CallLogTable.Remove(de.Key);
                                }
                                else if (hour != DateTime.Now.Hour)
                                {
                                    logWrite("CallLogTable[" + de.Key.ToString() + "] 삭제");
                                    CallLogTable.Remove(de.Key);
                                }
                                else if ((DateTime.Now.Minute - minute) > 5)
                                {
                                    logWrite("CallLogTable[" + de.Key.ToString() + "] 삭제");
                                    CallLogTable.Remove(de.Key);
                                }
                            }
                            else
                            {
                                logWrite("callLog_timer_Tick Error : call_id is not devided two string[]");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }

        private void timerForLicense_Tick(object sender, EventArgs e)
        {
            int hour = DateTime.Now.Hour;
            if (hour == 1)
            {
                registerLicenseInfo();
            }
            
        }

        private void loadConfigData()
        {
            try
            {
                WDdbHost = System.Configuration.ConfigurationSettings.AppSettings["DB_HOST"];
                WDdbName = System.Configuration.ConfigurationSettings.AppSettings["DB_NAME"];
                WDdbUser = System.Configuration.ConfigurationSettings.AppSettings["DB_USER"];
                WDdbPass = System.Configuration.ConfigurationSettings.AppSettings["DB_PASS"];
                listenport = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["SVR_LISTENPORT"]);
                sendport = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["SVR_SENDPORT"]);
                checkport = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["SVR_CHECKPORT"]);
                crmport = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["SVR_CRMPORT"]);
                fileport = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["SVR_FILEPORT"]);
                com_code = System.Configuration.ConfigurationSettings.AppSettings["COM_CODE"];
                server_type = System.Configuration.ConfigurationSettings.AppSettings["SVR_TYPE"];

                mVersion = ConstDef.VERSION;

                if (System.Configuration.ConfigurationSettings.AppSettings["AUTO_START"].Length > 0)
                {
                    auto_start = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["AUTO_START"]);
                }
                server_device = System.Configuration.ConfigurationSettings.AppSettings["DEVICE"];
                if (server_type.Length == 0)
                {
                    server_type = null;
                }

                if (server_device.Length == 0)
                {
                    server_device = null;
                }
            }
            catch (Exception ex)
            {
                logWrite("loadConfigData() Exception : "+ex.ToString());
            }
        }

        //private void showVersionCheckForm()
        //{
        //    versioncheckform = new VersionCheckForm();
        //    versioncheckform.TopMost = true;
        //    versioncheckform.Show();
        //}

        //private void VersionCheckFormClose()
        //{
        //    try
        //    {
        //        versioncheckform.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        logWrite(ex.ToString());
        //    }
        //}

        //private void VersionCheck()
        //{
        //    Thread versioncheckthread = new Thread(new ThreadStart(showVersionCheckForm));
        //    versioncheckthread.Start();
        //    NoParamDele checkdele = new NoParamDele(VersionCheckFormClose);
            

        //    bool isUpdate = false;
        //    try
        //    {
        //        Uri ftpuri = new Uri(FtpHost);
        //        FtpWebRequest wr = (FtpWebRequest)WebRequest.Create(ftpuri);
        //        wr.Method = WebRequestMethods.Ftp.ListDirectory;
        //        wr.Credentials = new NetworkCredential(FtpUsername, passwd);
        //        FtpWebResponse wres = (FtpWebResponse)wr.GetResponse();
        //        Stream st = wres.GetResponseStream();
        //        string SVRver = null;

        //        if (st.CanRead)
        //        {
        //            StreamReader sr = new StreamReader(st);
        //            SVRver = sr.ReadLine();
        //        }

        //        logWrite("Server Version = " + SVRver);
        //        logWrite("Client Version = " + version);

        //        if (SVRver.Equals(version.Trim()))
        //        {
        //            version = SVRver;

        //            logWrite("Last Version is already Installed!");
        //            Invoke(checkdele);
        //        }
        //        else
        //        {
        //            string[] ver = SVRver.Split('.');
        //            string[] now = version.Split('.');
        //            for (int v = 0; v < ver.Length; v++)
        //            {
        //                if (!ver[v].Equals(now[v]))
        //                {
        //                    if (Convert.ToInt32(ver[v]) > Convert.ToInt32(now[v]))
        //                    {
        //                        Invoke(checkdele);
        //                        NoParamDele dele = new NoParamDele(requestUpdate);
        //                        Invoke(dele);
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logWrite(ex.ToString());
        //    }
        //    noActive = true;

        //    //return isUpdate;
        //}

       
        // Cross-Thread 호출를 실행하기 위해 사용합니다.
        private delegate void AddTextDelegate(string strText);  //로그기록 델리게이트
        private delegate void MakeTree();    //전체 사용자 리스트 생성 델리게이트
        private delegate string GetInformation(string str);
        private delegate ArrayList GetNoticeListDelegate();

        private void start_Click(object sender, EventArgs e)
        {
            startServer();
        }

        private void startServer()
        {
            try
            {
                ShowNetworkInterfaces();
                //자동버전체크 폐기 2013/05/18 
                //VersionCheck();
                logWrite("Installed Version = " + mVersion);

                if (server_device == null && server_type == null)
                {
                    setDevice();
                }
                else
                {
                    registerLicenseInfo();
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void initService()
        {
            try
            {
                checkFirstStart(com_code, com_name);
                commctl.Select_Type(server_type);
                commctl.Connect(server_device);
                ListenThread = new Thread(StartListener);
                ListenThread.Start();
                svrStart = true;
                start.Visible = false;
                loadCustomerList();
                serviceStart = true;
                MnServerStart.Enabled = false;
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }

        private void checkFirstStart(string com_code, string com_name)
        {
            MySqlConnection conn = GetmysqlConnection();
            try
            {
                
                conn.Open();
                if (conn.State == ConnectionState.Open)
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("param_comcd", MySqlDbType.VarChar).Value = com_code; 
                    cmd.Parameters.Add("param_comnm", MySqlDbType.VarChar).Value = com_name;
                    cmd.CommandText = "select COM_CD, COM_NM from t_company where COM_CD = @param_comcd";
                    MySqlDataReader dr = null;

                    dr = cmd.ExecuteReader();

                    if (!dr.HasRows)
                    {
                        dr.Close();
                        int count = 0;

                        try
                        {
                            cmd.CommandText = "insert into t_company(COM_CD, COM_NM) values(@param_comcd, @param_comnm)";
                            count = cmd.ExecuteNonQuery();
                            if (count > 0)
                            {
                                logWrite(com_name + " 을 회사코드 " + com_code + " 로 DB 등록 성공");
                            }
                        }
                        catch (MySqlException ex1)
                        {
                            logWrite(ex1.Message);
                        }

                        try
                        {
                            cmd.CommandText = "insert into t_user(COM_CD, USER_ID, USER_NM, DEPART_CD, GRADE, USER_PWD, TEAM_NM) values(@param_comcd, 'admin', '관리자', '01', '01', 'admin', '기본')";
                            count = cmd.ExecuteNonQuery();
                            if (count > 0)
                            {
                                logWrite("test 기본 사용자 등록 완료");
                            }
                        }
                        catch (Exception ex2)
                        {
                            logWrite(ex2.Message);
                        }

                        try
                        {
                            cmd.CommandText = "update t_s_code set COM_CD=@param_comcd";
                            count = cmd.ExecuteNonQuery();
                            if (count > 0)
                            {
                                logWrite("t_s_code 테이블 회사코드 등록 완료");
                            }
                        }
                        catch (Exception ex3)
                        {
                            logWrite(ex3.Message);
                        }

                        try
                        {
                            cmd.CommandText = "update t_l_code set COM_CD=@param_comcd";
                            count = cmd.ExecuteNonQuery();
                            if (count > 0)
                            {
                                logWrite("t_l_code 테이블 회사코드 등록 완료");
                            }
                        }
                        catch (Exception ex4)
                        {
                            logWrite(ex4.Message);
                        }
                    }
                    else
                    {
                        logWrite("DB 데이터 점검 완료");
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }

            if (conn.State == ConnectionState.Connecting)
            {
                conn.Close();
            }
        }

        /// <summary>
        /// 파일에서 라이센스정보를 읽어, 유효성검사를 한다.
        /// </summary>
        private void registerLicenseInfo()
        {
            try
            {
                logWrite("라이선스 체크중.....");
                mLicenseHandler = new LicenseHandler(this.licenseDir, this.MACID);
                mLicenseHandler.LogWriteHandler += this.OnLogWrite;
                //파일읽음&라이센스값 decode
                if (mLicenseHandler.ReadLicense())
                {

                    stringDele dele = new stringDele(disposeLicenseResult);
                    Invoke(dele, mLicenseHandler.ResultMessage);
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result">코드&회사명&만료일자</param>
        private void disposeLicenseResult(string result)
        {
            try
            {
                //result = -1 : 등록되어있지 않음
                //result = 1 : 라이선스 만료
                //result = 2 : 인증
                //result = 3: 30일 남음
                //result = 4: 7일 이하 남음
                //result = 5: 중복등록
                string license_message = "";
                string[] license_info = result.Split('&');

                if (license_info.Length < 1)
                {
                    throw new Exception("라이센스 결과코드 오류");
                }
                LicenseResult resultCode = (LicenseResult)Convert.ToInt16(license_info[0]);

                if (license_info.Length > 1)
                {
                    this.com_name = license_info[1];
                }

                switch (resultCode)
                {
                    case LicenseResult.ERR_INVALID_FILE:
                        MessageBox.Show("라이센스파일이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "라이센스파일이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;
                    case LicenseResult.ERR_MAC_ADDR:
                        MessageBox.Show("Mac 주소값이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "Mac 주소값이 유효하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;
                    case LicenseResult.ERR_NO_FILE:
                        MessageBox.Show("라이센스파일이 존재하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "라이센스파일이 존재하지 않습니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;

                    case LicenseResult.ERR_UNREGISTERED:
                        MessageBox.Show("등록되지 않은 회사코드입니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.");
                        license_message = "등록되지 않은 회사코드입니다.\r\n 관리자에게 문의하세요.\r\n서버를 종료합니다.";
                        Process.GetCurrentProcess().Kill();
                        break;

                    case LicenseResult.ERR_EXPIRED:
                        MessageBox.Show("라이선스가 만료되었습니다. 연장 후 시작해 주세요.\r\n서버를 종료합니다.");
                        license_message = "라이선스가 만료되었습니다. 연장 후 시작해 주세요.\r\n서버를 종료합니다.";
                        foreach (DictionaryEntry de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(license_message, (IPEndPoint)de.Value);
                            }
                        }
                        Process.GetCurrentProcess().Kill();
                        break;

                    case LicenseResult.SUCCESS:
                        if (serviceStart == false)
                        {
                            initService();
                        }
                        logWrite("라이선스 인증 완료");
                        //MessageBox.Show("인증성공");

                        break;

                    case LicenseResult.WARN_30_DAYS:
                        if (serviceStart == false)
                        {
                            initService();
                        }
                        logWrite("라이선스 인증 완료");
                        //MessageBox.Show("라이선스 만료 30일 전입니다.");
                        license_message = "라이선스 만료 30일 전입니다.";
                        foreach (DictionaryEntry de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(license_message, (IPEndPoint)de.Value);
                            }
                        }
                        break;

                    case LicenseResult.WARN_7_DAYS:
                        if (serviceStart == false)
                        {
                            initService();
                        }
                        logWrite("라이선스 인증 완료");
                        MessageBox.Show("라이선스 만료 7일 전입니다.");
                        license_message = "라이선스 만료 7일 전입니다.";
                        foreach (DictionaryEntry de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(license_message, (IPEndPoint)de.Value);
                            }
                        }
                        break;

                        //case "5":
                        //    MessageBox.Show("해당 회사코드로 이미 사용중입니다.");
                        //    license_message = "해당 회사코드로 이미 사용중입니다.";
                        //    if (InClientList != null)
                        //    {
                        //        foreach (DictionaryEntry de in InClientList)
                        //        {
                        //            if (de.Value != null)
                        //            {
                        //                SendMsg(license_message, (IPEndPoint)de.Value);
                        //            }
                        //        }
                        //    }

                        break;
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }


        public void ShowNetworkInterfaces()
        {
            try
            {
                IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                logFileWrite("Interface information for " +
                        computerProperties.HostName + ":" + computerProperties.DomainName);
                if (nics == null || nics.Length < 1)
                {
                    logWrite("네트워크 어뎁터 검색 실패");
                    MessageBox.Show("현재 컴퓨터에서 네트워크 카드를 찾을수 없습니다.\\r\\n 서버가 종료됩니다.");
                    Process.GetCurrentProcess().Kill();
                }
                else
                {
                    logWrite("  Number of interfaces : " + nics.Length);
                    foreach (NetworkInterface adapter in nics)
                    {
                        IPInterfaceProperties properties = adapter.GetIPProperties();
                        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet || adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || adapter.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                        {
                            logFileWrite("Name : " + adapter.Description);
                            logFileWrite(String.Empty.PadLeft(adapter.Description.Length, '='));
                            logFileWrite("Interface type : " + adapter.NetworkInterfaceType);
                            logFileWrite("Physical address : ");
                            PhysicalAddress address = adapter.GetPhysicalAddress();
                            MACID = address.ToString();
                            logFileWrite("MAC : " + MACID);
                            logFileWrite("");
                            logFileWrite("##################");
                            logFileWrite("");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }


        private void setCompanyCode(string com_cd)
        {
            try
            {
                xmldoc.Load(AppConfigName);
                XmlNode node = xmldoc.SelectSingleNode("//appSettings");
                if (node.HasChildNodes)
                {
                    XmlNodeList nodelist = node.ChildNodes;
                    foreach (XmlNode itemNode in nodelist)
                    {
                        if (itemNode.Attributes["key"].Value.Equals("COM_CODE"))
                        {
                            itemNode.Attributes["value"].Value = com_cd;
                            break;
                        }
                    }
                }
                xmldoc.Save(AppConfigName);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void loadCustomerList()
        {
            try
            {

                conn_ring = GetmysqlConnection();
                conn_ring.Open();


                if (conn_ring.State == ConnectionState.Open)
                {
                    string queryString = "select C.CUSTOMER_NM, C.C_TELNO, C.H_TELNO, C.C_TELNO1, C.H_TELNO1, D.TELNO " +
                        "FROM " +
                        "t_customer C LEFT JOIN " +
                        "(SELECT B.TELNO, A.CUSTOMER_NM FROM t_customer A, t_customer_telno B WHERE A.CUSTOMER_ID=B.CUSTOMER_ID GROUP BY B.TELNO) D " +
                        "ON C.CUSTOMER_NM=D.CUSTOMER_NM";
                    MySqlCommand command = new MySqlCommand();
                    command.CommandText = queryString;
                    command.Connection = conn_ring;
                    MySqlDataReader dr = command.ExecuteReader();
                    if (dr != null && dr.HasRows)
                    {
                        while (dr.Read())
                        {

                            string cName = dr.GetString(0);

                            if (dr.FieldCount > 5)
                            {
                                if (!dr.IsDBNull(1))
                                {
                                    if (dr.GetString(1).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(1)] = cName;
                                        CustomerList_Backup[dr.GetString(1)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(2))
                                {
                                    if (dr.GetString(2).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(2)] = cName;
                                        CustomerList_Backup[dr.GetString(2)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(3))
                                {
                                    if (dr.GetString(3).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(3)] = cName;
                                        CustomerList_Backup[dr.GetString(3)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(4))
                                {
                                    if (dr.GetString(4).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(4)] = cName;
                                        CustomerList_Backup[dr.GetString(4)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(5))
                                {
                                    if (dr.GetString(5).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(5)] = cName;
                                        CustomerList_Backup[dr.GetString(5)] = cName;
                                    }
                                }
                            }
                        }

                        logWrite("CustomerListCache 데이터로드 완료");

                        //foreach (DictionaryEntry de in CustomerList_Primary)
                        //{
                        //    logFileWrite("CustomerList_Primary[" + de.Key.ToString() + "] = " + de.Value.ToString());
                        //}
                        //foreach (DictionaryEntry de in CustomerList_Backup)
                        //{
                        //    logFileWrite("CustomerList_Backup[" + de.Key.ToString() + "] = " + de.Value.ToString());
                        //}

                    }
                    dr.Close();
                }

                if (conn_ring.State == ConnectionState.Open)
                {
                    conn_ring.Close();
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
                if (conn_ring.State == ConnectionState.Open)
                {
                    conn_ring.Close();
                }
            }
        }

        private void reloadCustomerListCache()
        {

            CustomerCacheReload = true;

            conn_ring = GetmysqlConnection();
            conn_ring.Open();

            try
            {
                if (conn_ring.State == ConnectionState.Open)
                {
                    string queryString = "select C.CUSTOMER_NM, C.C_TELNO, C.H_TELNO, C.C_TELNO1, C.H_TELNO1, D.TELNO " +
                        "FROM " +
                        "t_customer C LEFT JOIN " +
                        "(SELECT B.TELNO, A.CUSTOMER_NM FROM t_customer A, t_customer_telno B WHERE A.CUSTOMER_ID=B.CUSTOMER_ID GROUP BY B.TELNO) D " +
                        "ON C.CUSTOMER_NM=D.CUSTOMER_NM";
                    MySqlCommand command = new MySqlCommand();
                    command.CommandText = queryString;
                    command.Connection = conn_ring;
                    MySqlDataReader dr = command.ExecuteReader();
                    if (dr != null && dr.HasRows)
                    {
                        string cName = "";

                        if (CustomerCacheSwitch == false)
                        {

                            while (dr.Read())
                            {
                                cName = dr.GetString(0);

                                if (!dr.IsDBNull(1))
                                {
                                    if (dr.GetString(1).Length > 1)
                                    {
                                        CustomerList_Backup[dr.GetString(1)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(2))
                                {
                                    if (dr.GetString(2).Length > 1)
                                    {
                                        CustomerList_Backup[dr.GetString(2)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(3))
                                {
                                    if (dr.GetString(3).Length > 1)
                                    {
                                        CustomerList_Backup[dr.GetString(3)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(4))
                                {
                                    if (dr.GetString(4).Length > 1)
                                    {
                                        CustomerList_Backup[dr.GetString(4)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(5))
                                {
                                    if (dr.GetString(5).Length > 1)
                                    {
                                        CustomerList_Backup[dr.GetString(5)] = cName;
                                    }
                                }
                            }
                            //foreach (DictionaryEntry de in CustomerList_Backup)
                            //{
                            //    logWrite("CustomerList_Backup[" + de.Key.ToString() + "] = " + de.Value.ToString());
                            //}

                            CustomerCacheSwitch = true;
                        }
                        else
                        {
                            while (dr.Read())
                            {
                                cName = dr.GetString(0);

                                if (!dr.IsDBNull(1))
                                {
                                    if (dr.GetString(1).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(1)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(2))
                                {
                                    if (dr.GetString(2).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(2)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(3))
                                {
                                    if (dr.GetString(3).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(3)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(4))
                                {
                                    if (dr.GetString(4).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(4)] = cName;
                                    }
                                }
                                if (!dr.IsDBNull(5))
                                {
                                    if (dr.GetString(5).Length > 1)
                                    {
                                        CustomerList_Primary[dr.GetString(5)] = cName;
                                    }
                                }
                            }

                            //foreach (DictionaryEntry de in CustomerList_Primary)
                            //{
                            //    logWrite("CustomerList_Primary[" + de.Key.ToString() + "] = " + de.Value.ToString());
                            //}

                            CustomerCacheSwitch = false;

                        }

                    }
                    dr.Close();
                }
                if (conn_ring.State == ConnectionState.Open)
                {
                    conn_ring.Close();
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
                if (conn_ring.State == ConnectionState.Open)
                {
                    conn_ring.Close();
                }
            }
            logWrite("고객정보 cache 갱신 : " + CustomerCacheSwitch.ToString());
            CustomerCacheReload = false;
        }

        private void setDevice()
        {
            try
            {
                nicform = new SetNICForm();

                if (com_code.Length > 0)
                {
                    nicform.tbx_com_code.Text = com_code;
                }

                if (server_type != null && server_type.Length > 0)
                {
                    switch (server_type)
                    {
                        case ConstDef.NIC_SIP:
                            nicform.rbt_type_sip.Checked = true;

                            break;

                        case ConstDef.NIC_LG_KP:
                            nicform.rbt_type_lg.Checked = true;
                            break;

                        case ConstDef.NIC_CID_PORT1:
                            nicform.rbt_type_cid1.Checked = true;
                            break;
                        case ConstDef.NIC_CID_PORT2:
                            nicform.rbt_type_cid2.Checked = true;
                            break;
                        case ConstDef.NIC_CID_PORT4:
                            nicform.rbt_type_cid4.Checked = true;
                            break;
                        case ConstDef.NIC_SS_KP:
                            nicform.rbt_type_ss.Checked = true;
                            break;
                    }
                }

                if (server_device != null && server_device.Length > 0)
                {
                    nicform.comboBox1.SelectedItem = server_device;
                }

                nicform.btn_comfirm.MouseClick += new MouseEventHandler(btn_comfirm_MouseClick);
                nicform.btn_cancel.MouseClick += new MouseEventHandler(btn_cancel_MouseClick);
                nicform.rbt_type_cid1.CheckedChanged += new EventHandler(rbt_type_CheckedChanged);
                nicform.rbt_type_cid2.CheckedChanged += new EventHandler(rbt_type_CheckedChanged);
                nicform.rbt_type_cid4.CheckedChanged += new EventHandler(rbt_type_CheckedChanged);
                nicform.rbt_type_lg.CheckedChanged += new EventHandler(rbt_type_CheckedChanged);
                nicform.rbt_type_ss.CheckedChanged += new EventHandler(rbt_type_CheckedChanged);
                nicform.rbt_type_sip.CheckedChanged += new EventHandler(rbt_type_CheckedChanged);
                nicform.Show();
                nicform.Activate();
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }


        private void rbt_type_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                RadioButton rbt = (RadioButton)sender;
                if (rbt.Checked == true)
                {
                    stringDele changerbt = new stringDele(chageRBTstatus);
                    Invoke(changerbt, new object[]{rbt.Name});
                    logWrite("통화장치 타입변경 : " + rbt.Name);
                    server_type = rbt.Tag.ToString();
                    logWrite("server_type : " + server_type);
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }

        private bool checkWincapInstall()
        {

            string MACHINE_NAME = Environment.MachineName;
            string WINPCAP_NAME = ConstDef.WINPCAP_INSTALLNAME;
            bool isAppInstalled = false;
            try
            {
                isAppInstalled = CommonUtil.IsAppInstalled(MACHINE_NAME, WINPCAP_NAME);
              //  string msg = string.Format("Application '{0}' is {1} on the remote-machine '{2}'!",
              //WINPCAP_NAME,
              //isAppInstalled ? "installed" : "NOT installed",
              //MACHINE_NAME);
              //  MessageBox.Show(msg);
            }
            catch (Exception ex)
            {
                logWrite("WinPcap Not Installed.:"+ex.Message);
            }

            return isAppInstalled;
        }

        private void winp_Exited(object sender, EventArgs e)
        {
            
        }

        private void chageRBTstatus(string rbtname)
        {
            int count = nicform.groupBox1.Controls.Count;
            bool isExist = false;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (nicform.groupBox1.Controls[i].Name.Equals(rbtname))
                    {
                        RadioButton rbt = (RadioButton)nicform.groupBox1.Controls[i];
                        rbt.Checked = true;
                    }
                    else
                    {
                        RadioButton rbt = (RadioButton)nicform.groupBox1.Controls[i];
                        rbt.Checked = false;
                    }
                }


                if (rbtname.Equals(ConstDef.RBT_TYPE_SIP))
                {

                    isExist = checkWincapInstall();

                    if (isExist == true)
                    {
                        listupDevice(rbtname);
                    }
                    else
                    {
                        if (isExist == false)
                        {
                            DialogResult result = MessageBox.Show("SIP 폰 사용의 경우, WinPcap 프로그램을 설치해야 합니다.\r\n 설치 하시겠습니까?", "알림", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes)
                            {
                                Process.Start(Application.StartupPath + ConstDef.WINPCAP);
                            }
                        }
                    }
                }
                else
                {
                    listupDevice(rbtname);
                }
            }

        }

        private void listupDevice(string rbtname)
        {
            try
            {
                nicform.comboBox1.Items.Clear();
                nicform.comboBox1.Items.Add("::::::::::::장 치 선 택::::::::::::");
                if (rbtname.Equals(ConstDef.RBT_TYPE_SIP))
                {
                    deviceList = CaptureDeviceList.Instance;
                    if (deviceList.Count != 0)
                    {
                        foreach (ICaptureDevice d in deviceList)
                        {
                            nicform.comboBox1.Items.Add(d.Description);
                        }

                        if (server_device != null && !server_type.Equals(ConstDef.NIC_SIP))
                        {
                            nicform.comboBox1.DroppedDown = true;
                        }

                        else if (server_device == null)
                        {
                            nicform.comboBox1.DroppedDown = true;
                        }
                    }
                }
                else
                {
                    serialport = new SerialPort();
                    string[] ports = SerialPort.GetPortNames();
                    if (ports.Length > 0)
                    {
                        foreach (string item in ports)
                        {
                            nicform.comboBox1.Items.Add(item);
                        }

                        if (server_device != null && server_type.Equals(ConstDef.NIC_SIP))
                        {
                            nicform.comboBox1.DroppedDown = true;
                        }
                        else if (server_device == null)
                        {
                            nicform.comboBox1.DroppedDown = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_cancel_MouseClick(object sender, MouseEventArgs e)
        {
            nicform.Close();
        }

        private void btn_comfirm_MouseClick(object sender, MouseEventArgs e)
        {
            string nicName = "";
            string autoStartValue = "";

            nicName = (string)nicform.comboBox1.SelectedItem;

            server_device = nicName;

            if (server_device.Length > 1)
            {
                if (nicform.tbx_com_code.Text.Trim().Length > 0)
                {
                    com_code = nicform.tbx_com_code.Text.Trim();
                    setCompanyCode(com_code);
                }

                if (auto_start == 0)
                {
                    DialogResult result = MessageBox.Show("WeDo 서버를 자동실행 설정하시겠습니까?", "알림", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        autoStartValue = "1";
                        setAutoStart(true);
                    }
                    else
                    {
                        autoStartValue = "0";
                    }
                }

                setSVR_typeXml(AppConfigName, server_type, server_device, autoStartValue);
                nicform.Close();
                startServer();
            }
            else
            {
                setDevice();
            }
        }


        private void setAutoStart(bool value)
        {
            try
            {
                if (value == true)
                {
                    System.Configuration.ConfigurationSettings.AppSettings.Set("AUTO_START", "1");
                    RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rkApp.SetValue(AppRegName , Application.ExecutablePath.ToString(), RegistryValueKind.String);
                    rkApp.Close();
                }
                else
                {
                    System.Configuration.ConfigurationSettings.AppSettings.Set("AUTO_START", "0");
                    RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (rkApp.GetValue(AppRegName) != null)
                    {
                        rkApp.DeleteValue(AppRegName);
                    }
                    rkApp.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        

        private string GetTeamName(string id)
        {
            
            string teamname = "";
            try
            {
                if (TeamNameList != null)
                {
                    teamname = (string)TeamNameList[id];
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
            return teamname;
        }

        /// <summary>
        /// 사용자 리스트 생성, 통신소켓 생성 및 Listen 쓰레드 시작
        /// </summary>
        public void StartListener()                 //서버 시작 메소드 
        {
            try
            {
                InClientList = new Hashtable();            //로그인 사용자 EndPoint정보 저장할 hash테이블
                logWrite("InClientList Hashtable 생성!");

                //MakeTree mt = new MakeTree(makeList);
                //Invoke(mt, null);

                IPEndPoint listen = new IPEndPoint(IPAddress.Any, listenport); //수신 전용 EndPoint 설정
                IPEndPoint send = new IPEndPoint(IPAddress.Any, sendport);       //발신 전용 EndPoint 설정 
                IPEndPoint check = new IPEndPoint(IPAddress.Any, checkport);       //서버체크 전용 EndPoint 설정 
                IPEndPoint crm = new IPEndPoint(IPAddress.Any, crmport);       //서버체크 전용 EndPoint 설정 

                listenSock = new UdpClient(listen);                         //수신 전용 Udp소켓 생성
                sendSock = new UdpClient(send);                             //발신 전용 Udp소켓 생성
                checkSock = new UdpClient(check);                           //서버체크 전용 Udp 소켓 생성
                crmSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                

                try
                {
                    crmSocket.Bind(crm);
                }
                catch (Exception ex)
                {
                    logWrite("CRM 통신 전용 소켓을 특정 EndPoint 바인딩하는데 실패하였습니다.");
                }
                filesender = new IPEndPoint(IPAddress.Any, 9001);

                try
                {
                    ListenThread = new Thread(new ThreadStart(ReceiveMsg));
                    ListenThread.Start();
                    logWrite("Listener Thread 시작!");

                    CheckThread = new Thread(new ThreadStart(ReceiveCheck));
                    CheckThread.Start();
                    logWrite("ReceiveCheck Thread 시작!");

                    CrmReceiverThread = new Thread(new ThreadStart(ReceiveCRMRequest));
                    CrmReceiverThread.Start();
                    logWrite("CrmReceiverThread Thread 시작!");
                    logWrite("서버 준비 완료!");
                }
                catch (Exception e)
                {
                    logWrite("Listener 쓰레드 시작 에러 : " + e.ToString());
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        /// <summary>
        /// 사용자의 Connect에 응답하여 사용자 전용 소켓 쓰레드를 시작한다.
        /// </summary>
        //private void Listener()
        //{
        //    try
        //    {
        //        tcpsock.Listen(5);
        //        while (true)
        //        {
        //            Socket listen = tcpsock.Accept();
        //            listen.Blocking = true;
        //            logWrite("연결요청 허용!");
        //            ReceiverThread = new Thread(new ParameterizedThreadStart(ReceiveMsg));
        //            ReceiverThread.Start(listen);
        //            logWrite("ReceiverThread 시작!");
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}


        public void ReceiveMsg()
        {
            try
            {
                sender = new IPEndPoint(IPAddress.Any, 8881);
                byte[] buffer = null;

                while (true)
                {
                    try
                    {
                        buffer = listenSock.Receive(ref sender);
                        logWrite("수신!");
                        logWrite("sender IP : " + sender.Address.ToString());
                        //logWrite("sender port : " + sender.Port.ToString());

                        if (buffer != null && buffer.Length != 0) //정상적으로 수신하면 응답
                        {
                            listenSock.Send(buffer, buffer.Length, sender);

                            string msg = Encoding.UTF8.GetString(buffer);

                            int mode = getMode(msg);

                            logWrite("수신 메시지 : " + msg);
                            ArrayList list = new ArrayList();
                            list.Add(mode);
                            list.Add(msg);
                            list.Add(sender);
                            Thread msgThread = new Thread(new ParameterizedThreadStart(receiveReq));
                            msgThread.Start(list);
                        }
                    }
                    catch (SocketException e)
                    {
                        logWrite("ReceiveMsg() 에러 : " + e.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        public void ReceiveCheck()
        {
            try
            {
                IPEndPoint checker = new IPEndPoint(IPAddress.Any, 8885);
                byte[] buffer = null;

                while (true)
                {
                    try
                    {
                        buffer = checkSock.Receive(ref checker);
                        //logWrite("서버체크 수신!");
                        checkSock.Send(buffer, buffer.Length, checker); 
                        //logWrite("sender IP : " + sender.Address.ToString());
                        //logWrite("sender port : " + sender.Port.ToString());
                    }
                    catch (SocketException e)
                    {
                        logWrite("ReceiveMsg() 에러 : " + e.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        public void ReceiveCRMRequest()
        {
            try
            {
                crmSocket.Listen(10);
                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[56];
                        Socket tempSocket = crmSocket.Accept();
                        logWrite("CRM 접속");
                        int count = tempSocket.Receive(buffer);
                        logWrite("CRM Request 수신!  " + count.ToString() + " byte");
                        byte[] content = new byte[count];
                        for (int i = 0; i < count; i++)
                        {
                            content[i] = buffer[i];
                        }
                        string crmmsg = Encoding.Default.GetString(content);
                        logWrite("CRM Request Message : " + crmmsg);
                        string[] tempStr = crmmsg.Split('&');
                        int mode = Convert.ToInt32(tempStr[0]);
                        ArrayList list = new ArrayList();
                        list.Add(mode);
                        list.Add(crmmsg);
                        Thread msgThread = new Thread(new ParameterizedThreadStart(receiveReq));
                        msgThread.Start(list);
                    }
                    catch (SocketException e)
                    {
                        logWrite("ReceiveCRMRequest() 에러 : " + e.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        public void ReceiveMsg(object obj)
        {
            
            try
            {
                byte[] buffer = null;
                Socket listenSock = (Socket)obj; 

                while (true)
                {
                    try
                    {
                        Thread.Sleep(100);
                        if (listenSock.Connected == false || listenSock == null)
                        {
                            break;
                        }
                        if (listenSock.Available != 0)
                        {
                            buffer = new byte[listenSock.Available];
                            int receivesize = listenSock.Receive(buffer);
                            logWrite("수신!");

                            if (receivesize > 0) //정상적으로 수신하면 응답
                            {
                                string msg = Encoding.UTF8.GetString(buffer);

                                int mode = getMode(msg);

                                logWrite("수신 메시지 : " + msg);
                                ArrayList list = new ArrayList();
                                list.Add(mode);
                                list.Add(msg);
                                list.Add(listenSock);
                                Thread msgThread = new Thread(new ParameterizedThreadStart(receiveReq));
                                msgThread.Start(list);
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        //logWrite("ReceiveMsg() 에러 : " + e.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                //logWrite(exception.ToString());
            }
        }


        #region tcp테스트 receiveReq
        //public void receiveReq(int mode, string msg, IPEndPoint iep)
        //public void receiveReq(object Obj)
        //{
        //    try
        //    {
        //        logWrite("receiveReq thread 시작!");
        //        Thread.Sleep(10);
        //        ArrayList List = (ArrayList)Obj;
        //        int mode = (int)List[0];
        //        string msg = (string)List[1];
        //        Socket reSocket = (Socket)List[2];
        //        string[] re = null;

        //        switch (mode)
        //        {
        //            case 8://로그인

        //                re = msg.Split('|'); //msg 구성 : 코드번호|id|비밀번호|내선번호
        //                Login(re, reSocket);

        //                break;

        //            case 2:  //중복 로그인 상황에서 기존접속 끊기 선택한 경우
        //                re = msg.Split('|');  //msg 구성 : 2|id|passwd

        //                SendMsg("u|", (Socket)InClientList[re[1]]); //기존 접속 원격 로그아웃 

        //                lock (InClientList)
        //                {
        //                    Logout(re[1]);      //기존 접속 서버측 로그아웃
        //                }
        //                Login(re, reSocket);

        //                break;

        //            case 1: //공지사항 목록 요청(1|)

        //                SelectNoticeAll(reSocket);
        //                break;


        //            case 0: //대화종료

        //                break;

        //            case 4: //메모 처리
        //                logWrite("메모처리");

        //                re = msg.Split('|'); //msg 구성 : 4|name|발신자id|메시지|수신자id

        //                if (re[1].Equals("m")) //쪽지 전송 실패로 서버측으로 메시지 반송시(4|m|name|발신자id|메시지|수신자id)
        //                {
        //                    InsertNoReceive(re[5], "", re[4], re[1], re[3]);
        //                }
        //                else
        //                {
        //                    InsertNoReceive(re[4], "", re[3], "m", re[2]);
        //                }

        //                break;

        //            case 5: //파일 전송 메시지(5|파일명|파일크기|파일타임키|전송자id|수신자id;id;id...
        //                re = msg.Split('|');
        //                string[] filenameArray = re[1].Split('\\');
        //                string filename = filenameArray[(filenameArray.Length - 1)];
        //                int filesize = int.Parse(re[2]);

        //                Hashtable fileinfotable = new Hashtable();
        //                fileinfotable[re] = filename;

        //                //파일 수신 스레드 시작
        //                lock (filesock)
        //                {
        //                    filesock = new UdpClient(filesender);
        //                    if (!ThreadList.ContainsKey(re[3] + re[4]) || ThreadList[re[3] + re[4]] == null) //같은 파일에 대한 전송 쓰레드가 시작되지 않았다면
        //                    {
        //                        Thread filereceiver = new Thread(new ParameterizedThreadStart(FileReceiver));
        //                        filereceiver.Start(fileinfotable);
        //                        ThreadList[re[3] + re[4]] = filereceiver;
        //                        SendMsg("Y|" + re[1] + "|" + re[3] + "|" + re[5], reSocket);  //re[5]==all 의 경우 전체에 대한 파일 전송
        //                        ErrorConnClear();
        //                    }
        //                }
        //                break;

        //            case 3:   //사용자 로그인 상태 체크요청(코드번호|id)
        //                re = msg.Split('|');
        //                if (InClientList.ContainsKey(re[1]) && InClientList[re[1]] != null)
        //                {
        //                    SendMsg("+|", (Socket)InClientList[re[1]]);
        //                }
        //                else
        //                {
        //                    lock (InClientList)
        //                    {
        //                        Logout(re[1]);
        //                    }
        //                }
        //                break;

        //            case 6:  //공지사항 전달(6|메시지|발신자id|n 또는 e|noticetime)  n : 일반공지 , e : 긴급공지
        //                re = msg.Split('|');
        //                Socket rsock = null;

        //                foreach (DictionaryEntry de in InClientList)
        //                {
        //                    if (de.Value != null)
        //                    {
        //                        rsock = (Socket)de.Value;
        //                        logWrite("공지사항 전송 : " + de.Key.ToString());
        //                        SendMsg("n|" + re[1] + "|" + re[2] + "|" + re[3] + "|" + re[4], rsock);
        //                    }
        //                }
        //                ErrorConnClear();
        //                GetNoticeListDelegate notice = new GetNoticeListDelegate(GetNoticeList);
        //                ArrayList list = (ArrayList)Invoke(notice, null);

        //                InsertNotice(list, re[1], re[2], re[3]);

        //                break;

        //            case 9://로그아웃             

        //                re = msg.Split('|'); //msg 구성 : 코드번호|id

        //                //로그아웃 처리
        //                lock (InClientList)
        //                {
        //                    Logout(re[1]);
        //                }

        //                break;

        //            case 7:  //안읽은 메모 요청

        //                re = msg.Split('|'); //msg 구성 : 코드번호|id

        //                ArrayList memolist = ReadMemo(re[1]);
        //                string cmsg = "Q";
        //                if (memolist != null && memolist.Count != 0)
        //                {
        //                    foreach (object obj in memolist)
        //                    {
        //                        string[] array = (string[])obj;  //string[] { sender, content, time, seqnum }
        //                        if (array.Length != 0)
        //                        {
        //                            string item = array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3];
        //                            cmsg += "|" + item;
        //                        }
        //                    }
        //                }
        //                SendMsg(cmsg, reSocket);

        //                break;

        //            case 10:  //안받은 파일 요청
        //                re = msg.Split('|'); //msg 구성 : 코드번호|id
        //                GetInformation getName = new GetInformation(GetName);
        //                ArrayList filelist = ReadFile(re[1]);
        //                string fmsg = "R";
        //                if (filelist != null && filelist.Count != 0)
        //                {
        //                    foreach (object obj in filelist)
        //                    {
        //                        string[] array = (string[])obj;  //string[] { sender,loc, content, time, size, seqnum }
        //                        if (array.Length != 0)
        //                        {
        //                            string item = array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3] + ";" + array[4] + ";" + array[5];
        //                            fmsg += "|" + item;
        //                        }
        //                    }
        //                }
        //                SendMsg(fmsg, reSocket);
        //                ErrorConnClear();
        //                break;

        //            case 11:  //안읽은 공지 요청(11|id)
        //                re = msg.Split('|'); //msg 구성 : 코드번호|id

        //                ArrayList noticelist = ReadNotice(re[1]);
        //                string nmsg = "T";
        //                if (noticelist != null && noticelist.Count != 0)
        //                {
        //                    foreach (object obj in noticelist)
        //                    {
        //                        string[] array = (string[])obj;  //string[] { sender, content, time, nmode, seqnum }
        //                        if (array.Length != 0)
        //                        {
        //                            string item = array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3] + ";" + array[4];
        //                            nmsg += "|" + item;
        //                        }
        //                    }
        //                }
        //                SendMsg(nmsg, reSocket);
        //                ErrorConnClear();
        //                break;

        //            case 12: //파일 전송 요청
        //                re = msg.Split('|'); //msg 구성 : 12|filenum

        //                //StartSendFile(re[1], );
        //                break;

        //            case 13:  //보낸 공지 리스트 요청
        //                re = msg.Split('|');  //13|id

        //                SelectNoticeList(re[1]);
        //                break;

        //            case 14: //읽은 정보 삭제 요청(14|seqnum)

        //                re = msg.Split('|');

        //                DeleteNoreceive(re[1]);

        //                break;

        //            case 15://관리자 공지 삭제(15|seqnum;seqnum;seqnum;...)
        //                re = msg.Split('|');
        //                string[] msg_array = re[1].Split(';');
        //                DeleteNotice(msg_array);
        //                break;

        //            case 16://대화 메시지 전달(16|Formkey|id/id/..|발신자name|메시지 )
        //                re = msg.Split('|');
        //                string fwdmsg = "d" + msg.Substring(2);
        //                string[] party = re[2].Split('/');
        //                foreach (string item in party)
        //                {
        //                    Socket partySocket = (Socket)InClientList[item];
        //                    if (partySocket == null || partySocket.Connected == false)
        //                    {

        //                    }
        //                    else
        //                    {
        //                        SendMsg(fwdmsg, partySocket);
        //                    }
        //                }

        //                break;
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}
        #endregion

        public void receiveReq(object Obj)
        {
            try
            {
                logWrite("receiveReq thread 시작!");
                Thread.Sleep(10);
                ArrayList List = (ArrayList)Obj;
                int mode = (int)List[0];
                string msg = (string)List[1];
                IPEndPoint iep = null;
                if (List.Count > 2)
                {
                    iep = (IPEndPoint)List[2];
                }
                string[] re = null;

                switch (mode)
                {
                    case 8://로그인
                        logWrite("case 8 로그인 요청");

                        re = msg.Split('|'); //msg 구성 : 코드번호|id|비밀번호|내선번호|ip주소
                        //iep = new IPEndPoint(IPAddress.Parse(re[4]), 8883);
                        Login(re, iep);

                        break;

                    case 2:  //중복 로그인 상황에서 기존접속 끊기 선택한 경우
                        re = msg.Split('|');  //msg 구성 : 2|id

                        SendMsg("u|", (IPEndPoint)InClientList[re[1]]); //기존 접속 원격 로그아웃 
                        

                        Logout(re[1]);      //기존 접속 서버측 로그아웃
                        
                        break;

                    case 1: //공지사항 목록 요청(1|id)

                        re = msg.Split('|');
                        if (InClientList.Count != 0)
                        {
                            if (InClientList[re[1]] != null)
                            {
                                iep = (IPEndPoint)InClientList[re[1]];
                                SelectNoticeAll(re[1]);
                            }
                            else
                            {
                                logWrite("InClientList[" + re[1] + "] is null");
                            }
                        }
                        else
                        {
                            logWrite("InClientList is empty");
                        }
                        break;


                    case 0: //대화종료

                        break;

                    case 4: //메모 처리
                        logWrite("메모처리");

                        re = msg.Split('|'); //msg 구성 : 4|name|발신자id|메시지|수신자id

                        if (re[1].Equals("m")) //쪽지 전송 실패로 서버측으로 메시지 반송시(4|m|name|발신자id|메시지|수신자id)
                        {
                            InsertNoReceive(re[5], "", re[4], re[1], re[3], "x");
                        }
                        else
                        {
                            InsertNoReceive(re[4], "", re[3], "m", re[2], "x");
                        }

                        break;

                    case 5: //파일 전송 메시지(5|파일명|파일크기|파일타임키|전송자id|수신자id;id;id...
                        re = msg.Split('|');
                        string[] filenameArray = re[1].Split('\\');
                        string filename = filenameArray[(filenameArray.Length - 1)];
                        int filesize = int.Parse(re[2]);

                        Hashtable fileinfotable = new Hashtable();
                        fileinfotable[re] = filename;

                        //파일 수신 스레드 시작
                        if (filesock == null)
                        {
                            filesock = new UdpClient(filesender);
                        }

                        lock (filesock)
                        {
                            if (!ThreadList.ContainsKey(re[3] + re[4]) || ThreadList[re[3] + re[4]] == null) //같은 파일에 대한 전송 쓰레드가 시작되지 않았다면
                            {
                                Thread filereceiver = new Thread(new ParameterizedThreadStart(FileReceiver));
                                filereceiver.Start(fileinfotable);
                                ThreadList[re[3] + re[4]] = filereceiver;
                                SendMsg("FS|" + re[1] + "|" + re[3] + "|" + re[5], iep);  //re[5]==all 의 경우 전체에 대한 파일 전송
                            }
                        }
                        break;

                    case 3:   //사용자 로그인 상태 체크요청(코드번호|id)
                        //re = msg.Split('|');
                        //if (InClientList.ContainsKey(re[1]) && InClientList[re[1]] != null)
                        //{
                        //    SendMsg("+|", (IPEndPoint)InClientList[re[1]]);
                        //}
                        //else
                        //{

                        //    Logout(re[1]);

                        //}
                        break;

                    case 6:  //공지사항 전달(6|메시지|발신자id | n 또는 e | noticetime | 제목)  n : 일반공지 , e : 긴급공지
                        re = msg.Split('|');
                        IPEndPoint niep = null;

                        foreach (DictionaryEntry de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                niep = (IPEndPoint)de.Value;
                                logWrite("공지사항 전송 : " + de.Key.ToString());
                                SendMsg("n|" + re[1] + "|" + re[2] + "|" + re[3] + "|" + re[4] + "|" + re[5], niep);
                            }
                        }
                        GetNoticeListDelegate notice = new GetNoticeListDelegate(GetNoticeList);
                        ArrayList list = (ArrayList)Invoke(notice, null);

                        InsertNotice(list, re[4], re[1], re[2], re[3], re[5]);

                        break;

                    case 9://로그아웃             

                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        //로그아웃 처리

                        //SendMsg("u|", (IPEndPoint)InClientList[re[1]]);
                        Logout(re[1]);


                        break;

                    case 7:  //안읽은 메모 요청

                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        ArrayList memolist = ReadMemo(re[1]);
                        string cmsg = "Q";
                        if (memolist != null && memolist.Count != 0)
                        {
                            foreach (object obj in memolist)
                            {
                                string[] array = (string[])obj;  //string[] { sender, content, time, seqnum }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                    cmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(cmsg, iep);

                        break;

                    case 10:  //안받은 파일 요청
                        re = msg.Split('|'); //msg 구성 : 코드번호|id
                        ArrayList filelist = ReadFile(re[1]);
                        string fmsg = "R";
                        if (filelist != null && filelist.Count != 0)
                        {
                            foreach (object obj in filelist)
                            {
                                string[] array = (string[])obj;  //string[] { sender,loc, content, time, size, seqnum }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                    fmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(fmsg, iep);

                        break;

                    case 11:  //안읽은 공지 요청(11|id)
                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        ArrayList noticelist = ReadNotice(re[1]);
                        string nmsg = "T";
                        if (noticelist != null && noticelist.Count != 0)
                        {
                            foreach (object obj in noticelist)
                            {
                                string[] array = (string[])obj;  //string[] { sender, content, time, nmode, seqnum, title }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                    nmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(nmsg, iep);
                        break;

                    case 12: //파일 전송 요청
                        re = msg.Split('|'); //msg 구성 : 12|id|filenum

                        StartSendFile(re[2], re[1]);
                        break;

                    case 13:  //보낸 공지 리스트 요청
                        re = msg.Split('|');  //13|id

                        SelectNoticeList(re[1]);
                        break;

                    case 14: //읽은 정보 삭제 요청(14|seqnum)

                        re = msg.Split('|');

                        DeleteNoreceive(re[1]);

                        break;

                    case 15://관리자 공지 삭제(15|seqnum;seqnum;seqnum;...)
                        re = msg.Split('|');
                        string[] msg_array = re[1].Split(';');
                        DeleteNotice(msg_array);
                        break;


                    case 16://채팅메시지 전달(16|Formkey|id/id/..|발신자name|메시지 ) 구분자 : |(pipe) 

                        string chatmsg = "d" + msg.Substring(2);
                        re = msg.Split('|');
                        string[] ids = re[2].Split('/');
                        foreach (string iditem in ids)
                        {
                            if (InClientList.ContainsKey(iditem))
                            {
                                SendMsg(chatmsg, (IPEndPoint)InClientList[iditem]);
                            }
                        }
                        break;

                    case 17://추가한 상담원 리스트 기존 대화자에게 전송 (17|formkey|id/id/...|name|receiverID)

                        string amsg = "c" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[4]))
                        {
                            SendMsg(amsg, (IPEndPoint)InClientList[re[4]]);
                        }

                        break;

                    case 18: //2명 이상과 대화중 폼을 닫은 경우(q|Formkey|id|receiverID) 
                        string qmsg = "q" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[3]))
                        {
                            SendMsg(qmsg, (IPEndPoint)InClientList[re[3]]);
                        }

                        break;

                    case 19: //쪽지 전송요청(m|recName|recID|content|senderID);

                        string mmsg = "m" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[4]))
                        {
                            SendMsg(mmsg, (IPEndPoint)InClientList[re[4]]);
                        }

                        break;

                    case 20: //상태변경 알림(20|senderid|상태값)

                        string statmsg = "s" + msg.Substring(2);
                        re = msg.Split('|');
                        lock (InClientStat)
                        {
                            InClientStat[re[1]] = re[2];
                        }

                        statmsg += "|" + ((IPEndPoint)InClientList[re[1]]).Address.ToString();
                        foreach (DictionaryEntry de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                SendMsg(statmsg, (IPEndPoint)de.Value);
                            }
                        }
                        break;

                    case 21: //공지 읽음 확인 메시지(21 | receiverid | notice id | sender id)

                        string Nmsg = "C" + msg.Substring(2);
                        re = msg.Split('|');
                        if (InClientList.ContainsKey(re[3]))
                        {
                            SendMsg(Nmsg, (IPEndPoint)InClientList[re[3]]);
                        }
                        break;

                    case 22://고객정보 전달시도(22&ani&senderID&receiverID&일자&시간&CustomerName)
                        string passmsg = "pass" + msg.Substring(2);
                        re = msg.Split('&');
                        passmsg = passmsg.Replace('&', '|');
                        passmsg += "|" + getCustomerNM(re[1]);
                        logWrite("passmsg : " + passmsg);
                        if (InClientList.ContainsKey(re[3]))
                        {
                            if (InClientList[re[3]] != null)
                            {
                                SendMsg(passmsg, (IPEndPoint)InClientList[re[3]]);
                                //SendMsg(passmsg, (IPEndPoint)InClientList[re[2]]);
                            }
                            else
                            {
                                InsertNoReceive(re[3], "N/A", msg, "t", re[2], "t");
                            }
                        }
                        else
                        {
                            InsertNoReceive(re[3], "N/A", msg, "t", re[2], "t");
                        }

                        if (CustomerCacheReload == false)
                            reloadCustomerListCache();
                        break;

                    case 23:  //안읽은 이관 요청

                        re = msg.Split('|'); //msg 구성 : 코드번호|id

                        ArrayList transflist = ReadTransfer(re[1]);
                        string tmsg = "trans";
                        if (transflist != null && transflist.Count != 0)
                        {
                            foreach (object obj in transflist)
                            {
                                string[] array = (string[])obj;  //string[] { sender, content, time, seqnum, type }
                                if (array.Length != 0)
                                {
                                    string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                    tmsg += "|" + item;
                                }
                            }
                        }
                        iep = (IPEndPoint)InClientList[re[1]];
                        SendMsg(tmsg, iep);

                        break;

                    default:

                        logWrite("잘못된 요청 코드 입니다. : " + mode.ToString());
                        break;
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void RecvMessage(string sEvent, string sInfo)
        {
            try
            {
                IPEndPoint iep = null;
                string userid = "";
                if (sEvent.Equals("Connect") || sEvent.Equals("disConnect"))
                {
                    logWrite("Event : " + sEvent + "sInfo : " + sInfo + "\r\n");
                }
                else if (server_type.Equals(ConstDef.NIC_SIP))
                {
                    string[] infoarr = sInfo.Split('|'); //sInfo(FROM | TO | Call_ID)
                    if (infoarr.Length > 1)
                    {
                        logWrite("Event : " + sEvent + "  FROM : " + infoarr[0] + " TO : " + infoarr[1] + "\r\n");

                        if(infoarr[0].Length<5 && infoarr[1].Length<5)
                        {

                            logWrite("Internal Call Event");
                            return;
                        }
                        else if (infoarr[0].Substring(0, 3).Equals(infoarr[1].Substring(0, 3)))
                        {
                            logWrite("Internal Call Event");
                            return;
                        }
                        else if (ExtensionList.ContainsKey(infoarr[0]) && ExtensionList.ContainsKey(infoarr[2]))
                        {
                            logWrite("Internal Call Event");
                            return;
                        }

                        switch (sEvent)
                        {
                            case "Ringing":
                                string cname = "";

                                bool needAnswer = false;////Answer가 안들어오는 경우를 감안 Ringing=>Answer로 바로 처리 20130306   부천 대부 dk전용

                                if (!CallLogTable.ContainsKey(infoarr[2]))
                                {
                                    cname = getCustomerNM(infoarr[0]);
                                    if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                    {
                                        iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                        needAnswer = SendRinging("Ring|" + infoarr[0] + "|" + cname + "|" + server_type, iep);
                                    }

                                    lock (CallLogTable)
                                    {
                                        if (!CallLogTable.ContainsKey(infoarr[2]))
                                        {
                                            CallLogTable[infoarr[2]] = "Ringing";
                                            if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[1]].ToString();

                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[1];
                                            }
                                            //insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "1");
                                            insertCallLog(infoarr[1], infoarr[0], "1", infoarr[2], "1");
                                        }
                                    }

                                    if (CustomerCacheReload == false)
                                        reloadCustomerListCache();
                                }
                                else
                                {
                                    logWrite(infoarr[2] + " is already inbounded");
                                }
                                if (!needAnswer)
                                    break;
                                //break;  //Answer가 안들어오는 경우를 감안 Ringing=>Answer로 바로 처리 20130306   부천 대부 dk전용
                                Thread.Sleep(2000);
                            //case "Answer":

                                if (CallLogTable.ContainsKey(infoarr[2]))
                                {
                                    if (CallLogTable[infoarr[2]].ToString().Equals("Ringing")) //응답시 Answer 중복 이벤트 처리
                                    {
                                        userid = infoarr[1];
                                        if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                        {
                                            iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                            SendMsg("Answer|" + infoarr[0] + "|" + "1", iep); //SIP 폰의 경우 직통전화이므로 바로 전송

                                            if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[1]].ToString();

                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                        }

                                        insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "3");

                                        lock (CallLogTable)
                                        {
                                            CallLogTable[infoarr[2]] = DateTime.Now;
                                        }
                                    }
                                    else
                                    {
                                        logWrite(infoarr[2] + " is already answered");
                                    }
                                }
                                else
                                {
                                    logWrite(infoarr[2] + " is already invalid callid");
                                }
                                break;

                            case "CallConnect": //발신 후 연결

                                lock (CallLogTable)
                                {
                                    CallLogTable[infoarr[2]] = DateTime.Now;
                                }
                                break;

                            case "Dialing":

                                lock (CallLogTable)
                                {
                                    if (!CallLogTable.ContainsKey(infoarr[2]))
                                    {
                                        if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[0]))
                                        {
                                            iep = (IPEndPoint)ExtensionList[infoarr[0]];
                                            SendMsg("Dial|" + infoarr[1] + "|", iep);

                                        }
                                        CallLogTable[infoarr[2]] = DateTime.Now;
                                        insertCallLog(infoarr[0], infoarr[1], "2", infoarr[2], "2");
                                    }
                                }
                                break;

                            case "Abandon":

                                lock (CallLogTable)
                                {
                                    if (CallLogTable.ContainsKey(infoarr[2]))
                                    {
                                        if (!CallLogTable[infoarr[2]].ToString().Equals("A"))
                                        {
                                            if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                            {
                                                iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                                SendMsg("Abandon|" + infoarr[0], iep);

                                            }

                                            if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[1]].ToString();

                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[1];
                                            }

                                            insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "4");
                                            CallLogTable.Remove(infoarr[2]);
                                        }
                                    }
                                    else
                                    {
                                        logWrite(infoarr[2] + " is invalid callid");
                                    }
                                }

                                break;

                            case "HangUp":

                                if (CallLogTable.ContainsKey(infoarr[2]))
                                {
                                    logWrite("통화종료 이벤트!");

                                    if (ExtensionList.Count > 0)
                                    {
                                        if (ExtensionList.ContainsKey(infoarr[0])) //FROM == 사용자일 경우
                                        {
                                            if (ExtensionIDpair.ContainsKey(infoarr[0]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[0]].ToString();

                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[0];
                                            }
                                            insertCallLog2(infoarr[2], infoarr[0], infoarr[1], userid, "2", "5");
                                        }
                                        else if (ExtensionList.ContainsKey(infoarr[1])) //TO == 사용자일 경우
                                        {
                                            if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[1]].ToString();

                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[1];
                                            }
                                            insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "5");
                                        }
                                        else
                                        {
                                            int numlen = infoarr[0].Length - infoarr[1].Length; //리스트에 없는 경우(해당 내선사용자 로그아웃) 짧은 번호를 사용자로 판단
                                            if (numlen > 0)
                                            {
                                                if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                                {
                                                    string tempid = ExtensionIDpair[infoarr[1]].ToString();
                                                    Client cinfo = (Client)ClientInfoList[tempid];
                                                    userid = tempid + "." + cinfo.getName();
                                                }
                                                else
                                                {
                                                    userid = infoarr[1];
                                                }
                                                insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "5");
                                            }
                                            else
                                            {
                                                if (ExtensionIDpair.ContainsKey(infoarr[0]))
                                                {
                                                    string tempid = ExtensionIDpair[infoarr[0]].ToString();
                                                    Client cinfo = (Client)ClientInfoList[tempid];
                                                    userid = tempid + "." + cinfo.getName();
                                                }
                                                else
                                                {
                                                    userid = infoarr[0];
                                                }
                                                insertCallLog2(infoarr[2], infoarr[0], infoarr[1], userid, "2", "5");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int numlen = infoarr[0].Length - infoarr[1].Length; //리스트에 없는 경우(해당 내선사용자 로그아웃) 짧은 번호를 사용자로 판단
                                        if (numlen > 0)
                                        {
                                            if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[1]].ToString();
                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[1];
                                            }
                                            insertCallLog2(infoarr[2], infoarr[1], infoarr[0], userid, "1", "5");
                                        }
                                        else
                                        {
                                            if (ExtensionIDpair.ContainsKey(infoarr[0]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[0]].ToString();
                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[0];
                                            }
                                            insertCallLog2(infoarr[2], infoarr[0], infoarr[1], userid, "2", "5");
                                        }
                                    }
                                }
                                else
                                {
                                    logWrite(infoarr[2] + " is already invalid callid");
                                }
                                break;
                        }
                    }
                }
                else if(server_type.Equals(ConstDef.NIC_LG_KP)) //CID 장치 또는 KEYPHONE
                {
                    logWrite("Event : " + sEvent + "  sInfo : " + sInfo + "\r\n");
                    string[] infoarr = sInfo.Split('>');
                    string call_id = DateTime.Now.ToString("yyyyMMddHHmmss") +"$"+ infoarr[0];
                    switch (sEvent)
                    {
                        case "Ringing":

                            string cname = "";
                            cname = getCustomerNM(sInfo);
                            logWrite("고객이름 : " + cname);
                            foreach (DictionaryEntry de in InClientList)
                            {
                                if (de.Value != null)
                                {
                                    SendRinging("Ring|" + sInfo + "|" + cname + "|" + server_type, (IPEndPoint)de.Value);
                                }
                            }

                            if (CustomerCacheReload == false)
                                reloadCustomerListCache();

                            CallLogTable[call_id] = infoarr[0];
                            insertCallLog("All", infoarr[0], "1", call_id, "1");

                            break;

                        case "Answer":


                            if (infoarr.Length > 1)
                            {
                                logWrite("받은 내선 : " + infoarr[1]);

                                foreach (DictionaryEntry de in InClientList)
                                {
                                    if (de.Value != null)
                                    {
                                        SendRinging("Other|", (IPEndPoint)de.Value);
                                    }
                                }

                                if (ExtensionList.Count > 0 && ExtensionList.ContainsKey(infoarr[1]))
                                {
                                    iep = (IPEndPoint)ExtensionList[infoarr[1]];
                                    SendMsg("Answer|" + infoarr[0] + "|" + "1", iep);
                                }

                                if (CallLogTable.ContainsValue(infoarr[0])) //CallLogTable[call_id] = ani
                                {
                                    foreach (DictionaryEntry de in CallLogTable)
                                    {
                                        if (de.Value.ToString().Equals(infoarr[0]))
                                        {
                                            if (ExtensionIDpair.ContainsKey(infoarr[1]))
                                            {
                                                string tempid = ExtensionIDpair[infoarr[1]].ToString();

                                                Client cinfo = (Client)ClientInfoList[tempid];
                                                userid = tempid + "." + cinfo.getName();
                                            }
                                            else
                                            {
                                                userid = infoarr[1];
                                            }
                                            insertCallLog3(call_id, infoarr[1], infoarr[0], userid, "1", "3");
                                            CallLogTable.Remove(de.Key);
                                            break;
                                        }
                                    }

                                }
                                else
                                {
                                    logWrite("CallLogTable 에 Ringing 정보 없음");
                                }
                            }
                            break;
                    }
                }
                else if (server_type.Equals(ConstDef.NIC_CID_PORT1)
                    || server_type.Equals(ConstDef.NIC_CID_PORT2)
                    || server_type.Equals(ConstDef.NIC_CID_PORT4))
                {
                    logWrite("Event : " + sEvent + "  sInfo : " + sInfo + "\r\n");
                    string[] infoarr = sInfo.Split('>');
                    string call_id = DateTime.Now.ToString("yyyyMMddHHmmss") +"$"+ infoarr[0];
                    switch (sEvent)
                    {

                        case "Ringing" :

                            string cname = "";
                            cname = getCustomerNM(sInfo);
                            logWrite("고객이름 : " + cname);
                            foreach (DictionaryEntry de in InClientList)
                            {
                                if (de.Value != null)
                                {
                                    SendRinging("Ring|" + sInfo + "|" + cname + "|" + server_type, (IPEndPoint)de.Value);
                                }
                            }

                            if (CustomerCacheReload == false)
                                reloadCustomerListCache();

                            CallLogTable[call_id] = infoarr[0];
                            insertCallLog("All", infoarr[0], "1", call_id, "1");

                            break;

                        case "OffHook" :

                            foreach (DictionaryEntry de in InClientList)
                            {
                                if (de.Value != null)
                                {
                                    SendRinging("Other|", (IPEndPoint)de.Value);
                                }
                            }

                            if (CallLogTable.Count > 0) //CallLogTable[call_id] = ani
                            {
                                foreach (DictionaryEntry logitem in CallLogTable)
                                {
                                    foreach (DictionaryEntry clientItem in InClientList)
                                    {
                                        if (clientItem.Value != null)
                                        {
                                            SendMsg("Answer|" + logitem.Value.ToString() + "|" + "1", (IPEndPoint)clientItem.Value);
                                        }
                                    }

                                    insertCallLog3(call_id, "All", logitem.Value.ToString(), "All", "1", "3");
                                    CallLogTable.Remove(logitem.Key);
                                    break;
                                }

                            }
                            else
                            {
                                logWrite("CallLogTable 에 Ringing 정보 없음");
                            }

                            break;

                        case "OnHook" :


                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite("RecvMessage Exception : " + ex.ToString());
            }
        }

        private string getCustomerNM(string ani)
        {
            string cname = "";
            try
            {
                //if (conn_ring.State != ConnectionState.Open)
                //{
                //    conn_ring = GetmysqlConnection();
                //    conn_ring.Open();
                //}

                //MySqlCommand command = new MySqlCommand();
                //command.Connection = conn_ring;
                //command.CommandType = CommandType.Text;
                //command.Parameters.Add("@telnum", MySqlDbType.VarChar).Value = ani;
                //command.CommandText = "select customer_nm from t_customer where c_telno1 = @telnum or h_telno1 = @telnum";

                //MySqlDataReader reader = command.ExecuteReader();

                //if (reader.HasRows)
                //{
                //    while (reader.Read())
                //    {
                //        cname = reader.GetString(0);
                //    }
                //}
                //reader.Close();

                if (CustomerCacheSwitch == false)
                {
                    //foreach (DictionaryEntry de in CustomerList_Primary)
                    //{
                    //    logWrite(de.Key.ToString() + " = " + de.Value.ToString());
                    //}
                    if (CustomerList_Primary.ContainsKey(ani))
                    {
                        cname = CustomerList_Primary[ani].ToString();
                        logWrite("고객이름 찾음 : " + cname);
                    }
                }
                else
                {
                    //foreach (DictionaryEntry de in CustomerList_Backup)
                    //{
                    //    logWrite(de.Key.ToString() + " = " + de.Value.ToString());
                    //}
                    if (CustomerList_Backup.ContainsKey(ani))
                    {
                        cname = CustomerList_Backup[ani].ToString();
                        logWrite("고객이름 찾음 : " + cname);
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite("getCustomerNM() Exception : " + ex.ToString());
            }

            return cname;
        }

        private void insertCallLog2(string call_id, string extension, string ani, string user, string call_type, string call_result) //Answer or HangUp or Abandon
        {
            try
            {
                DateTime start_time = new DateTime();
                string call_start = "";
                string call_end = "";
                int call_duration = 0;
                DateTime result_time = DateTime.Now;
                if (call_result.Equals("3")) // answer
                {
                    if (CallLogTable.ContainsKey(call_id))
                    {
                        call_start = result_time.ToString("yyyyMMddHHmmss");
                        logWrite("call_start = " + call_start);
                        CallLogTable[call_id] = result_time;

                        if (conn_callog.State != ConnectionState.Open)
                        {
                            conn_callog = GetmysqlConnection();
                            conn_callog.Open();
                        }

                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn_callog;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add("@com_cd", MySqlDbType.VarChar).Value = com_code;
                        command.Parameters.Add("@starttime", MySqlDbType.VarChar).Value = call_start;
                        command.Parameters.Add("@ext_num", MySqlDbType.VarChar).Value = extension;
                        command.Parameters.Add("@call_type", MySqlDbType.VarChar).Value = call_type;
                        command.Parameters.Add("@call_result", MySqlDbType.VarChar).Value = call_result;
                        command.Parameters.Add("@ani", MySqlDbType.VarChar).Value = ani;
                        command.Parameters.Add("@call_id", MySqlDbType.VarChar).Value = call_id;
                        command.Parameters.Add("@userid", MySqlDbType.VarChar).Value = user;

                        if (server_type.Equals(ConstDef.NIC_LG_KP))
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "1";
                        }
                        else if (server_type.Equals(ConstDef.NIC_SIP))
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "2";
                        }
                        else
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "3";
                        }

                        command.CommandText = "insert into t_call_history" +
                        "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, PBX_TYPE) " +
                        "VALUE(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @pbx_type)";

                        int count = command.ExecuteNonQuery();

                        if (count != 0)
                        {
                            logWrite("insertCallLog2 : " + call_id + " Call Log DB Insert !");
                        }
                        else
                        {
                            logWrite("insertCallLog2 실패: " + call_id);
                        }
                    }
                    else
                    {
                        logWrite("CallLogTable 에 해당 키 없음 : " + call_id);
                    }
                }
                else if (call_result.Equals("4")) // abandon
                {
                    call_start = result_time.ToString("yyyyMMddHHmmss");
                    logWrite("call_start = " + call_start);
                    
                    if (CallLogTable.ContainsKey(call_id))
                    {
                        CallLogTable.Remove(call_id);

                        if (conn_callog.State != ConnectionState.Open)
                        {
                            conn_callog = GetmysqlConnection();
                            conn_callog.Open();
                        }

                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn_callog;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add("@com_cd", MySqlDbType.VarChar).Value = com_code;
                        command.Parameters.Add("@starttime", MySqlDbType.VarChar).Value = call_start;
                        command.Parameters.Add("@ext_num", MySqlDbType.VarChar).Value = extension;
                        command.Parameters.Add("@call_type", MySqlDbType.VarChar).Value = call_type;
                        command.Parameters.Add("@call_result", MySqlDbType.VarChar).Value = call_result;
                        command.Parameters.Add("@ani", MySqlDbType.VarChar).Value = ani;
                        command.Parameters.Add("@call_id", MySqlDbType.VarChar).Value = call_id;
                        command.Parameters.Add("@userid", MySqlDbType.VarChar).Value = user;

                        if (server_type.Equals(ConstDef.NIC_LG_KP))
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "1";
                        }
                        else if (server_type.Equals(ConstDef.NIC_SIP))
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "2";
                        }
                        else
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "3";
                        }

                        command.CommandText = "insert into t_call_history" +
                        "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, PBX_TYPE) " +
                        "VALUE(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @pbx_type)";

                        int count = command.ExecuteNonQuery();

                        if (count != 0)
                        {
                            logWrite("insertCallLog2 : " + call_id + " Call Log DB Insert !");
                        }
                        else
                        {
                            logWrite("insertCallLog2 실패: " + call_id);
                        }
                    }
                }
                else if (call_result.Equals("5"))//released
                {
                    call_end = result_time.ToString("yyyyMMddHHmmss");
                    logWrite("call_end = " + call_end);

                    if (CallLogTable.ContainsKey(call_id))
                    {
                        start_time = (DateTime)CallLogTable[call_id];
                        call_start = start_time.ToString("yyyyMMddHHmmss");
                        logWrite("call_start = " + call_start);
                        CallLogTable.Remove(call_id);


                        call_duration = (result_time - start_time).Seconds;

                        if (conn_callog.State != ConnectionState.Open)
                        {
                            conn_callog = GetmysqlConnection();
                            conn_callog.Open();
                        }

                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn_callog;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add("@com_cd", MySqlDbType.VarChar).Value = com_code;
                        command.Parameters.Add("@starttime", MySqlDbType.VarChar).Value = call_start;
                        command.Parameters.Add("@endtime", MySqlDbType.VarChar).Value = call_end;
                        command.Parameters.Add("@ext_num", MySqlDbType.VarChar).Value = extension;
                        command.Parameters.Add("@call_type", MySqlDbType.VarChar).Value = call_type;
                        command.Parameters.Add("@call_result", MySqlDbType.VarChar).Value = call_result;
                        command.Parameters.Add("@ani", MySqlDbType.VarChar).Value = ani;
                        command.Parameters.Add("@call_id", MySqlDbType.VarChar).Value = call_id;
                        command.Parameters.Add("@userid", MySqlDbType.VarChar).Value = user;
                        command.Parameters.Add("@duration", MySqlDbType.VarChar).Value = call_duration;

                        if (server_type.Equals(ConstDef.NIC_LG_KP))
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "1";
                        }
                        else if (server_type.Equals(ConstDef.NIC_SIP))
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "2";
                        }
                        else
                        {
                            command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "3";
                        }

                        command.CommandText = "insert into t_call_history" +
                        "(COM_CD, TONG_START_TIME, TONG_END_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, TONG_DURATION, PBX_TYPE) " +
                        "VALUE(@com_cd, @starttime, @endtime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @duration, @pbx_type)";

                        int count = command.ExecuteNonQuery();

                        if (count != 0)
                        {
                            logWrite("insertCallLog2 : " + call_id + " Call Log DB Insert !");
                        }
                        else
                        {
                            logWrite("insertCallLog2 실패: " + call_id);
                        }
                    }
                }
                if (conn_callog.State == ConnectionState.Open)
                {
                    conn_callog.Close();
                }

            }
            catch (Exception ex)
            {
                logWrite("insertCallLog2 Exception : " + ex.ToString());
                if (conn_callog.State == ConnectionState.Open)
                {
                    conn_callog.Close();
                }
            }
        }

        private void insertCallLog3(string call_id, string extension, string ani, string user, string call_type, string call_result) //Answer or HangUp or Abandon
        {
            try
            {
                DateTime start_time = new DateTime();
                string call_start = DateTime.Now.ToString("yyyyMMddHHmmss");
                string call_end = "";
                int call_duration = 0;
                DateTime result_time = DateTime.Now;
                if (call_result.Equals("3")) // answer
                {
                    if (conn_callog.State != ConnectionState.Open)
                    {
                        conn_callog = GetmysqlConnection();
                        conn_callog.Open();
                    }

                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn_callog;
                    command.CommandType = CommandType.Text;
                    command.Parameters.Add("@com_cd", MySqlDbType.VarChar).Value = com_code;
                    command.Parameters.Add("@starttime", MySqlDbType.VarChar).Value = call_start;
                    command.Parameters.Add("@ext_num", MySqlDbType.VarChar).Value = extension;
                    command.Parameters.Add("@call_type", MySqlDbType.VarChar).Value = call_type;
                    command.Parameters.Add("@call_result", MySqlDbType.VarChar).Value = call_result;
                    command.Parameters.Add("@ani", MySqlDbType.VarChar).Value = ani;
                    command.Parameters.Add("@call_id", MySqlDbType.VarChar).Value = call_id;
                    command.Parameters.Add("@userid", MySqlDbType.VarChar).Value = user;

                    if (server_type.Equals(ConstDef.NIC_LG_KP))
                    {
                        command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "1";
                    }
                    else if (server_type.Equals(ConstDef.NIC_SIP))
                    {
                        command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "2";
                    }
                    else
                    {
                        command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "3";
                    }

                    command.CommandText = "insert into t_call_history" +
                    "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, TONG_USER, PBX_TYPE) " +
                    "VALUE(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @userid, @pbx_type)";

                    int count = command.ExecuteNonQuery();

                    if (count != 0)
                    {
                        logWrite("insertCallLog3 : " + call_id + " Call Log DB Insert !");
                    }
                    else
                    {
                        logWrite("insertCallLog3 실패: " + call_id);
                    }

                }


                if (conn_callog.State == ConnectionState.Open)
                {
                    conn_callog.Close();
                }

            }
            catch (Exception ex)
            {
                logWrite("insertCallLog3 Exception : " + ex.ToString());
                if (conn_callog.State == ConnectionState.Open)
                {
                    conn_callog.Close();
                }
            }
        }

        private void insertCallLog(string ext, string ani, string call_type, string call_id, string call_result)
        {
            try
            {
                DateTime maketime = DateTime.Now;
                string startTime = maketime.ToString("yyyyMMddHHmmss");
                if (conn_callog.State != ConnectionState.Open)
                {
                    conn_callog = GetmysqlConnection();
                    conn_callog.Open();
                }

                MySqlCommand command = new MySqlCommand();
                command.Connection = conn_callog;
                command.CommandType = CommandType.Text;
                command.Parameters.Add("@com_cd", MySqlDbType.VarChar).Value = com_code;
                command.Parameters.Add("@starttime", MySqlDbType.VarChar).Value = startTime;
                command.Parameters.Add("@ext_num", MySqlDbType.VarChar).Value = ext;
                command.Parameters.Add("@call_type", MySqlDbType.VarChar).Value = call_type;
                command.Parameters.Add("@call_result", MySqlDbType.VarChar).Value = call_result;
                command.Parameters.Add("@ani", MySqlDbType.VarChar).Value = ani;
                command.Parameters.Add("@call_id", MySqlDbType.VarChar).Value = call_id;

                if (server_type.Equals(ConstDef.NIC_LG_KP))
                {
                    command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "1";
                }
                else if(server_type.Equals(ConstDef.NIC_SIP))
                {
                    command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "2";
                }
                else
                {
                    command.Parameters.Add("@pbx_type", MySqlDbType.VarChar).Value = "3";
                }

                command.CommandText = "insert into t_call_history" +
                "(COM_CD, TONG_START_TIME, EXTENSION_NO, CALL_TYPE, ANI, CALL_ID, CALL_RESULT, PBX_TYPE) " +
                "VALUE(@com_cd, @starttime, @ext_num, @call_type, @ani, @call_id, @call_result, @pbx_type)";

                int count = command.ExecuteNonQuery();

                if (count != 0)
                {
                    logWrite("insertCallLog : " + call_id + " Call Log DB Insert !");
                }
                else
                {
                    logWrite("insertCallLog 실패: " + call_id + " Call Log DB Insert !");
                }
                if (conn_callog.State == ConnectionState.Open)
                {
                    conn_callog.Close();
                }
            }
            catch (Exception ex)
            {
                logWrite("insertCallLog Exception : " + ex.ToString());
                if (conn_callog.State == ConnectionState.Open)
                {
                    conn_callog.Close();
                }
            }
        }


        public SqlConnection GetSqlConnection()
        {
            SqlConnection sqlconn = null;
            try
            {
                string ConnectionString = "server=" + WDdbHost + ";database=" + WDdbName + ";user id=" + WDdbUser + ";password=" + WDdbPass;
                sqlconn = new SqlConnection(ConnectionString);
                sqlconn.Open();

            }
            catch (Exception ex)
            {
                logWrite("GetSqlConnection Exception");
                logWrite(ex.ToString());
            }
            return sqlconn;
        }

        private MySqlConnection GetmysqlConnection()
        {
            MySqlConnection mconn = null;
            try
            {
                string ConnectionString = "server="+WDdbHost+";uid="+WDdbUser+";pwd="+WDdbPass+";database="+WDdbName;

                mconn = new MySqlConnection(ConnectionString);
                //mconn.Open();

            }
            catch (Exception ex)
            {
                logWrite("GetMySqlConnection Exception");
                logWrite(ex.ToString());
            }
            return mconn;
        }

        //private SqlConnection GetSqlConnection()
        //{
        //    string oradb = "user id="+Msgdbid+";password="+Msgdbpasswd+";data source="+Msgdbhost+":1521/"+Msgtnsname;
        //    SqlConnection conn = new SqlConnection(oradb);

        //    return conn;
        //}

        private Hashtable GetMember(string DBType)
        {
            Hashtable result = null;
            try
            {
                switch (DBType)
                {
                    case "my":
                        result = readMemberFromMySql();
                        break;

                    case "ms":
                        //result = readMemberFromMSSql();
                        break;
                };
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
            return result;
        }


        #region readMemberFromMSSql()...

        //private Hashtable readMemberFromMSSql()
        //{
        //    SqlConnection conn = GetSqlConnection();
        //    try
        //    {
        //        conn.Open();
        //        logWrite("DB 접속 성공!(DB HOST : " + WDdbHost + " DB NAME : " + WDdbName + ")");
        //    }
        //    catch (Exception open)
        //    {
        //        logWrite("GetMember() conn.Open() 에러 :" + open.ToString());
        //    }

        //    SqlCommand cmd = new SqlCommand();
        //    cmd.Connection = conn;
        //    string cmdstring = "select info.user_cd, info.user_nm, code.team_nm from tbl_user_inf as info, tbl_team_cd as code where info.team_cd=code.team_cd";
        //    cmd.CommandText = cmdstring;
        //    cmd.CommandType = CommandType.Text;

        //    SqlDataReader reader = null;
        //    try
        //    {
        //        reader = cmd.ExecuteReader();
        //    }
        //    catch (Exception re)
        //    {
        //        logWrite("GetMember() ExecuteReader() 에러 : " + re.ToString());
        //    }

        //    Hashtable clientList = new Hashtable();

        //    try
        //    {
        //        if (reader.HasRows == true)
        //        {
        //            logWrite("GetMember() : 읽어오기 성공!");
        //            while (reader.Read())
        //            {
        //                string id = reader.GetString(0);
        //                string name = reader.GetString(1);
        //                string team = reader.GetString(2);
        //                string com_nm = reader.GetString(3);

        //                Client cl = new Client(id, name, team, "Unknown", com_nm);
        //                clientList.Add(id, cl);

        //            }
        //            conn.Close();
        //            logWrite("conn.Close!");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logWrite("ShowMemList() SqlDataReader 에러 : " + e.ToString());
        //    }

        //    return clientList;
        //}
        #endregion


        private Hashtable readMemberFromMySql()
        {
            Hashtable clientList = new Hashtable();

            try
            {
                MySqlConnection myconn = GetmysqlConnection();

                try
                {
                    myconn.Open();
                }
                catch (Exception ex)
                {
                    logWrite("readMemberFromMySql myconn.open() 에러 : " + ex.ToString());
                }


                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = myconn;
                string cmdstring = "select u.user_id, u.user_nm, u.team_nm, c.com_nm from t_user as u, t_company as c where u.com_cd=c.com_cd";
                cmd.CommandText = cmdstring;
                cmd.CommandType = CommandType.Text;

                MySqlDataReader reader = null;
                try
                {
                    reader = cmd.ExecuteReader();
                }
                catch (Exception re)
                {
                    logWrite("readMemberFromMySql() ExecuteReader() 에러 : " + re.ToString());
                }



                try
                {
                    if (reader.HasRows == true)
                    {
                        logWrite("readMemberFromMySql() : 읽어오기 성공!");
                        while (reader.Read())
                        {
                            string id = reader.GetString(0);
                            string name = reader.GetString(1);
                            string team = null;
                            if (reader.IsDBNull(2))
                            {
                                team = "";
                            }
                            else
                            {
                                team = reader.GetString(2);
                            }
                            string com_nm = reader.GetString(3);
                            logWrite(id + "|" + name + "|" + team);
                        }

                    }
                }
                catch (Exception e)
                {
                    logWrite("ShowMemList() SqlDataReader 에러 : " + e.ToString());
                }
                myconn.Close();
                logWrite("conn.Close!");
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
            return clientList;
        }


        private void ReceiveStat()
        {
            try
            {
                statsock.Listen(100);
                while (true)
                {
                    try
                    {
                        Socket statlisten = statsock.Accept();
                        int buffersize=0;
                        if (statlisten.Available != 0)
                        {
                            buffersize = statlisten.Available;
                            byte[] buffer = new byte[buffersize];
                            int buffercount = statlisten.Receive(buffer);
                            //logWrite("수신!");
                            EndPoint ep = statlisten.RemoteEndPoint;
                            IPEndPoint statiep = (IPEndPoint)ep;
                            //logWrite("sender IP : " + statiep.Address.ToString());
                            //logWrite("sender port : " + statiep.Port.ToString());

                            if (buffer != null && buffer.Length != 0)
                            {
                                string msg = Encoding.UTF8.GetString(buffer);
                                msg = msg.Trim();
                                int statn = Convert.ToInt32(msg);
                                msg = statn.ToString();


                                //if (!Statlist.ContainsKey(statiep.Address.ToString()) || Statlist[statiep.Address.ToString()].ToString().Length == 0)
                                //{
                                //    //logWrite("최초 stat 메시지 : " + msg);
                                //    Statlist[statiep.Address.ToString()] = msg;
                                //    ArrayList list = new ArrayList();
                                //    list.Add(statiep.Address.ToString());
                                //    list.Add(msg);
                                //    Thread statchangethread = new Thread(new ParameterizedThreadStart(ChangePresence));
                                //    statchangethread.Start((object)list);
                                //    //logWrite("statchangethread 생성!");
                                //}
                                //else
                                //{
                                //    //logWrite("변경 stat 메시지 : " + msg);
                                //    string statnum = Statlist[statiep.Address.ToString()].ToString();
                                //    if (!statnum.Equals(msg))
                                //    {
                                //        string currstat = GetPresence(statnum);
                                //        //logWrite("currstat : " + currstat);
                                //        string newstat = GetPresence(msg);
                                //        //logWrite("newstat : " + newstat);
                                //        if (!currstat.Equals(newstat))
                                //        {
                                //            Statlist[statiep.Address.ToString()] = msg;
                                //            ArrayList list = new ArrayList();
                                //            list.Add(statiep.Address.ToString());
                                //            list.Add(msg);
                                //            Thread statchangethread = new Thread(new ParameterizedThreadStart(ChangePresence));
                                //            statchangethread.Start((object)list);
                                //            //logWrite("statchangethread 생성!");
                                //        }
                                //    }
                                //}
                            }
                        }
                        statlisten.Disconnect(false);
                    }
                    catch (SocketException e)
                    {
                        logWrite("ReceiveMsg() 에러 : " + e.ToString());
                    }
                    
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private string GetPresence(string statnum)
        {
            string statstr = statnum;
            try
            {
                switch (statnum)
                {
                    case "2":  //작업

                        statstr = "작업";
                        break;

                    case "3": //대기

                        statstr = "대기";
                        break;

                    case "4":  //작업

                        statstr = "작업";
                        break;

                    case "5":  //보류

                        statstr = "통화";
                        break;

                    case "6": //로그아웃

                        statstr = "로그아웃";
                        break;

                    case "11": //인바운드
                        statstr = "통화";
                        break;

                    case "12": //아웃바운드
                        statstr = "통화";
                        break;

                    case "13": //협의통화
                        statstr = "통화";
                        break;

                    case "14": //내선통화
                        statstr = "통화";
                        break;

                    case "41":  //휴식
                        statstr = "휴식";
                        break;

                    case "42":  //식사
                        statstr = "식사";
                        break;

                    case "43":  //교육
                        statstr = "교육";
                        break;

                    case "44":  //기타
                        statstr = "이석";
                        break;
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
            return statstr;
        }

        private void ChangePresence(object obj)
        {
            try
            {
                ArrayList list = (ArrayList)obj;
                string IPaddr = (string)list[0];
                string statnum = (string)list[1];
                string presence = GetPresence(statnum);
                string statid = null;

                if (InClientList != null && InClientList.Count != 0)
                {
                    foreach (DictionaryEntry de in InClientList)
                    {
                        if (de.Value != null)
                        {
                            string addr = ((IPEndPoint)de.Value).Address.ToString();
                            if (addr.Equals(IPaddr))
                            {
                                //logWrite(IPaddr);
                                //logWrite(addr);
                                statid = de.Key.ToString();

                                break;
                            }
                        }
                    }
                }

                if (InClientList != null && InClientList.Count != 0)
                {
                    foreach (DictionaryEntry de in InClientList)
                    {
                        if (de.Value != null)
                        {
                            SendMsg("s|" + statid + "|" + presence, (IPEndPoint)de.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }

        

        private void FileReceiver(object obj)
        {
            try
            {
                Hashtable fileinfo = (Hashtable)obj;
                string filename = null;
                int filesize = 0;
                int size = 0;
                string[] ids = null;
                string[] msg = null;
                string filesender = null;
                foreach (DictionaryEntry de in fileinfo)
                {
                    filename = (string)de.Value;
                    logWrite("FileReceiver() : filename=" + filename);

                    msg = (string[])de.Key;  //5|파일명|파일크기|파일타임키|전송자id|수신자id;id;id...
                    ids = msg[5].Split(';');
                }
                filesender = msg[4];
                string fsize = msg[2];
                filesize = int.Parse(msg[2]);
                byte[] buffer = null;

                string filelocation = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString();
                DirectoryInfo dinfo = new DirectoryInfo(filelocation);
                if (dinfo.Exists == false)
                {
                    dinfo.Create();
                }
                string tempfilename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\" + filename;
                string filesavename = null;
                FileInfo fi = new FileInfo(tempfilename);
                bool ok = false;
                int num=0;
                if (fi.Exists == true)
                {
                    do
                    {
                        num++;
                        ok = GetFileName(filename, num);
                    } while (ok == false);
                    filesavename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\" + "(" + num.ToString() + ")" + filename;
                }
                else
                    filesavename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\" + filename;

               
                FileStream fs = new FileStream(filesavename, FileMode.Append, FileAccess.Write, FileShare.Read, 40960);
                
                try
                {
                    lock (filesock)
                    {
                        filesock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
                        while (true)
                        {
                            //logWrite("FileReceiver() 수신대기 ");
                            try
                            {
                                buffer = filesock.Receive(ref sender);
                                //logWrite("수신! ");
                            }
                            catch (SocketException se)
                            {
                                logWrite("FileReceiver()  filesock.Receive 에러 : " + se.ToString());
                                break;
                            }
                            if (buffer != null && buffer.Length != 0)
                            {
                                //logWrite("sender IP : " + sender.Address.ToString());
                                //logWrite("sender port : " + sender.Port.ToString());

                                byte[] receivebyte = Encoding.UTF8.GetBytes(buffer.Length.ToString());

                                try
                                {
                                    filesock.Send(receivebyte, receivebyte.Length, sender);  //정상적으로 메시지 수신하면 응답(udp통신의 실패방지)
                                }
                                catch (SocketException se1)
                                {
                                    logWrite("FileReceiver() filesock.Send 에러 : " + se1.ToString());
                                    break;
                                }
                                if (fs.CanWrite == true)
                                {
                                    try
                                    {
                                        fs.Write(buffer, 0, buffer.Length);
                                        fs.Flush();
                                    }
                                    catch (Exception e)
                                    {
                                        logWrite("FileStream.Write() 에러 : " + e.ToString());
                                        break;
                                    }
                                }
                                FileInfo finfo = new FileInfo(filesavename);

                                size = Convert.ToInt32(finfo.Length);

                                if (size >= filesize)
                                {
                                    logWrite("받은 크기 : " + size.ToString());
                                    logWrite("파일 크기 : " + filesize.ToString());
                                    logWrite("파일 전송 완료");
                                    fs.Close();
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (ThreadAbortException e)
                { }
                catch (SocketException e)
                {
                    logWrite("FileReceive() 에러 : " + e.ToString());
                }
                logWrite("FileReceiver 가 중단되었습니다. ");
                if (size!=0&&size >= filesize)
                {
                    logWrite(ids[0]);
                    FileInfoInsert(ids, filename, filesavename, fsize, filesender);
                }
            }
            catch (Exception e3)
            {
                logWrite("FileReceiver() 에러 : " + e3.ToString());
            }
            
        }

        private bool GetFileName(string filename, int num)
        {
            string tempfilename = Application.StartupPath + "\\files\\" + DateTime.Now.ToShortDateString() + "\\(" + num + ")" + filename;
            FileInfo fi = new FileInfo(tempfilename);
            bool ok = false;
            if (fi.Exists == false)
            {
                ok = true;
            }
            return ok;
        }

        private void FileInfoInsert(string[] ids, string filename, string fileloc, string filesize, string filesender )
        {
            try
            {
                logWrite(filename + "/" + fileloc + "/" + filesize + "/" + filesender);
                MySqlConnection conn = GetmysqlConnection();
                int row = 0;
                string loc = null;
                string form = "f";
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    logWrite("FileInfoInsert() conn.Open 에러 :" + e.ToString());
                }
                logWrite("ids.Length : " + ids.Length.ToString());

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@filename", MySqlDbType.VarChar).Value = filename;
                cmd.Parameters.Add("@filetime", MySqlDbType.VarChar).Value = DateTime.Now.ToString();
                cmd.Parameters.Add("@fileloc", MySqlDbType.VarChar).Value = fileloc;
                cmd.Parameters.Add("@filesize", MySqlDbType.VarChar).Value = filesize;
                cmd.CommandText = "insert into t_files(fname, ftime, floc, fsize) values(@filename, @filetime, @fileloc, @filesize)";
                try
                {
                    row = cmd.ExecuteNonQuery();
                }
                catch (Exception ex1)
                {
                    logWrite("FileInfoInsert() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
                }

                if (row != 0)
                {
                    logWrite("file " + filename + " insert DB");
                }

                cmd.CommandText = "select seqnum from t_files where ftime = @filetime";
                MySqlDataReader dr = null;
                try
                {
                    dr = cmd.ExecuteReader();
                }
                catch (Exception ex2)
                {
                    logWrite("FileInfoInsert() ExecuteReader 에러 :" + ex2.ToString());
                }

                if (dr != null)
                {
                    if (dr.Read())
                    {
                        loc = dr.GetValue(0).ToString();
                    }
                }

                try
                {
                    conn.Close();
                }
                catch (Exception close)
                {
                    logWrite("conn close 에러 :" + close.ToString());
                }

                foreach (string tempid in ids)
                {
                    if (tempid != null && tempid.Length != 0)
                    {
                        InsertNoReceive(tempid, loc, filename, "f", filesender, "x");
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void DeleteNoreceive(string seqnum)
        {
            try
            {
                int seq = Convert.ToInt32(seqnum);
                MySqlConnection conn = GetmysqlConnection();
                try
                {
                    conn.Open();
                }
                catch (Exception e1)
                {
                    logWrite("DeleteNoreceive() conn.Open 에러 :" + e1.ToString());
                }
                int row = 0;
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@seq", MySqlDbType.Int32).Value = seq;
                cmd.CommandText = "delete from t_noreceive where seqnum=@seq";
                try
                {
                    row = cmd.ExecuteNonQuery();
                }
                catch (Exception e2)
                {
                    logWrite("DeleteNoreceive() ExecuteNonQuery 에러:" + e2.ToString());
                }
                if (row == 0)
                {
                    logWrite("DeleteNoreceive() 삭제 수행된 행 없음!");
                }
                else logWrite("DeleteNoreceive() 삭제 완료! : " + row.ToString() + " 개 행");
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void DeleteNotice(string[] array)
        {
            try
            {
                
                MySqlConnection conn = GetmysqlConnection();
                try
                {
                    conn.Open();
                }
                catch (Exception e1)
                {
                    logWrite("DeleteNoreceive() conn.Open 에러 :" + e1.ToString());
                }
                int row = 0;
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                MySqlParameter param=cmd.Parameters.Add("@seq", MySqlDbType.Int32);
                foreach (string seqnum in array)
                {
                    if (seqnum.Length != 0)
                    {
                        int seq = Convert.ToInt32(seqnum);
                        param.Value = seq;
                        cmd.CommandText = "delete from t_notices where seqnum=@seq";
                        try
                        {
                            row = cmd.ExecuteNonQuery();
                        }
                        catch (Exception e2)
                        {
                            logWrite("DeleteNoreceive() ExecuteNonQuery 에러:" + e2.ToString());
                        }
                        if (row == 0)
                        {
                            logWrite("DeleteNoreceive() 삭제 수행된 행 없음!");
                        }
                        else logWrite("DeleteNoreceive() 삭제 완료! : " + row.ToString() + " 개 행");
                    }
                }
                conn.Close();
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void SelectNoticeAll(string id)
        {
            try
            {
                string noticeitems = "L";
                string seqnum = null;
                string ntime = null;
                string content = null;
                string nmode = null;
                string sender = null;
                string title = null;

                MySqlConnection conn = GetmysqlConnection();
                try
                {
                    conn.Open();
                }
                catch (Exception e1)
                {
                    logWrite("SelectNoticeAll() conn.Open 에러 :" + e1.ToString());
                }

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "select SEQNUM, NTIME, CONTENT, NMODE, SENDER, ntitle from t_notices order by seqnum desc";
                MySqlDataReader dr = null;
                try
                {
                    dr = cmd.ExecuteReader();
                }
                catch (Exception e2)
                {
                    logWrite("SelectNoticeAll() ExecuteReader 에러:" + e2.ToString());
                }
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        seqnum = dr.GetValue(0).ToString();
                        ntime = dr.GetString(1);
                        content = dr.GetString(2);
                        nmode = dr.GetString(3);
                        sender = dr.GetString(4);
                        title = dr.GetString(5);
                        noticeitems += "|" + ntime + "‡" + content + "‡" + nmode + "‡" + sender + "‡" + seqnum + "‡" + title;
                    }
                }
                try
                {
                    conn.Close();
                }
                catch (Exception close)
                {
                    logWrite("SelectNoticeAll()  conn close 에러 :" + close.ToString());
                }
                logWrite("공지 읽어오기 성공");

                SendMsg(noticeitems, (IPEndPoint)InClientList[id]);
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void SelectNoticeList(string id)
        {
            MySqlConnection conn = GetmysqlConnection();
            Hashtable noticesFromSender = new Hashtable();
            try
            {
                string noticeitems = "t";
                string seqnum = null;
                string ntime = null;
                string content = null;
                string nmode = null;
                string title = null;
                
                try
                {
                    conn.Open();
                }
                catch (Exception e1)
                {
                    logWrite("SelectNoticeList() conn.Open 에러 :" + e1.ToString());
                }

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id", MySqlDbType.VarChar).Value = id;
                cmd.CommandText = "select seqnum, ntime, content, nmode, ntitle from t_notices where sender=@id order by seqnum";
                MySqlDataReader dr = null;
                try
                {
                    dr = cmd.ExecuteReader();
                }
                catch (Exception e2)
                {
                    logWrite("SelectNoticeList() ExecuteReader 에러:" + e2.ToString());
                }

                if (dr != null)
                {
                    while (dr.Read())
                    {
                        seqnum = dr.GetValue(0).ToString();
                        ntime = dr.GetString(1);
                        content = dr.GetString(2);
                        nmode = dr.GetString(3);
                        title = dr.GetString(4);
                        noticesFromSender[seqnum] = "|" + ntime + "†" + content + "†" + nmode + "†" + title + "†";
                    }
                }

                dr.Close();

                string notreader = null;
                cmd.CommandText = "select receiver, loc from t_noreceive where form='n'";
                MySqlDataReader dr1 = null;
                try
                {
                    dr1 = cmd.ExecuteReader();
                }
                catch (Exception e3)
                {
                    logWrite("SelectNoticeList() ExecuteReader 에러:" + e3.ToString());
                }
                if (dr1 != null)
                {
                    while (dr1.Read())
                    {
                        if (noticesFromSender.ContainsKey(dr1.GetValue(1).ToString())) //loc 값이 같은 부재중 공지 수신자 아이디 추가
                        {
                            noticesFromSender[dr1.GetValue(1).ToString()] = noticesFromSender[dr1.GetValue(1).ToString()].ToString() + dr1.GetString(0) + ":";
                        }
                    }
                }

                foreach (DictionaryEntry de in noticesFromSender)
                {
                    logWrite("notices sequence number from not reader [" + de.Key.ToString() + "] = " + de.Value.ToString());
                    noticeitems += de.Value.ToString();
                }

                if (InClientList.ContainsKey(id) && InClientList[id] != null)
                {
                    IPEndPoint iep = (IPEndPoint)InClientList[id];
                    SendMsg(noticeitems, iep);
                }
              
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }

            try
            {
                conn.Close();
            }
            catch (Exception close)
            {
                logWrite("SelectNoticeList()  conn close 에러 :" + close.ToString());
            }
        }

        private void StartSendFile(string filenum, string id)
        {
            try
            {
                string fileloc = null;
                MySqlConnection conn = GetmysqlConnection();
                try
                {
                    conn.Open();
                }
                catch (Exception e1)
                {
                    logWrite("StartSendFile() conn.Open 에러 :" + e1.ToString());
                }
                int num = Convert.ToInt32(filenum);
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@filenum", MySqlDbType.Int32).Value = num;
                cmd.CommandText = "select floc from t_files where seqnum=@filenum";
                MySqlDataReader dr = null;
                try
                {
                    dr = cmd.ExecuteReader();
                }
                catch (Exception e2)
                {
                    logWrite("StartSendFile():" + e2.ToString());
                }
                while (dr.Read())
                {
                    fileloc = dr.GetString(0);
                }
                try
                {
                    conn.Close();
                }
                catch (Exception close)
                {
                    logWrite("StartSendFile()  conn close 에러 :" + close.ToString());
                }
                SendFile(fileloc, id);
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void SendFile(string fileloc, string id) //
        {
            try
            {
                IPEndPoint sendfileIEP = new IPEndPoint(IPAddress.Any, 0);
                UdpClient filesendSock = new UdpClient(sendfileIEP);

                IPEndPoint iep = (IPEndPoint)InClientList[id];
                iep.Port = 9003;    //파일전용 포트로 변경
                logWrite("SendFile() 파일전송 포트 변경 :" + iep.Port.ToString());

                FileInfo fi = new FileInfo(fileloc);
                logWrite("SendFile() FileInfo 인스턴스 생성 : " + fileloc);

                int read = 0;
                byte[] buffer = null;
                byte[] re = null;

                filesendSock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);

                if (fi.Exists == true)
                {
                    BufferedStream bs = new BufferedStream(new FileStream(fileloc, FileMode.Open, FileAccess.Read, FileShare.Read, 40960), 40960);

                    double sendfilesize = Convert.ToDouble(fi.Length);
                    double percent = (40960 / sendfilesize) * 100;
                    double total = 0.0;

                    lock (filesendSock)
                    {
                        while (true)
                        {
                            for (int i = 0; i < 3; i++) //udp 통신의 전송실패 방지
                            {
                                try
                                {
                                    logWrite("FileReceiver IP : " + iep.Address.ToString());
                                    logWrite("FileReceiver port : " + iep.Port.ToString());
                                    if (sendfilesize >= 40960.0)
                                        buffer = new byte[40960];
                                    else buffer = new byte[Convert.ToInt32(sendfilesize)];
                                    read = bs.Read(buffer, 0, buffer.Length);
                                    filesendSock.Send(buffer, buffer.Length, iep);
                                    //logWrite("filesendSock.Send() : " + i.ToString() + " 번째 시도!");
                                }
                                catch (Exception e)
                                {
                                    logWrite("SendFile() BufferedStream.Read() 에러 :" + e.ToString());
                                }
                                try
                                {
                                    re = filesendSock.Receive(ref iep);
                                    int reSize = int.Parse(Encoding.UTF8.GetString(re));
                                    if (reSize == buffer.Length) break;
                                }
                                catch (SocketException e1)
                                { }
                            }

                            if (re == null || re.Length == 0)
                            {
                                logWrite("filesendSock.Send() 상대방이 응답하지 않습니다. 수신자 정보 : " + iep.Address.ToString() + ":" + iep.Port.ToString());
                                break;
                            }
                            else
                            {
                                sendfilesize = (sendfilesize - 40960.0);
                                total += percent;
                                if (total > 100) total = 100.0;
                                string[] totalArray = (total.ToString()).Split('.');
                            }
                            if (total == 100.0)
                            {
                                logWrite("전송완료");
                                filesendSock.Close();
                            }
                            if (total == 100.0) break;
                        }
                    }
                }
                else
                {
                    logWrite("SendFile() 파일이 없음 : " + fileloc);
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void Login(string[] arr,  IPEndPoint iep)
        {
            try
            {
                string result = null;
                Client cl = null;
                string id = arr[1];
                string output = CheckRegist(arr);

                switch (output)
                {
                    case "match":
                        logWrite("case : match (인증성공)");
                        string svrMsg = null;

                        if (InClientList.ContainsKey(id) && InClientList[id] != null)
                        {

                            logWrite("중복 로그인 시도! : " + id.ToString());
                            svrMsg = "a|";
                            SendMsg(svrMsg, iep);

                        }
                        else
                        {
                            ArrayList objlist = getClientInfo(id);
                            cl = (Client)objlist[0];

                            //로그인 사용자 내선번호 등록
                            cl.setPosition(arr[3]);
                            ExtensionIDpair[arr[3]] = cl.getId();
                            logWrite(arr[3] + " = " + cl.getId() + " 등록");
                            //로그인 사용자 IPEndPoint 등록
                            //iep = new IPEndPoint(IPAddress.Parse(arr[4]), sendport);

                            //로그인 사용자 정보리스트에 등록
                            ClientInfoList[cl.getId()] = cl;
                            
                            logWrite(cl.getName() + "(" + cl.getId() + ") 님 로그인 성공!(" + DateTime.Now.ToString() + ")");

                            Statlist[id] = "6"; //로그인 성공시 사용자 프리젠스 6(로그아웃)으로 설정

                            svrMsg = "g|" + cl.getName() + "|" + cl.getTeam() + "|" + cl.getCompany() + "|" + cl.getComCode();

                            SendMsg(svrMsg, iep);     //로그인 클라이언트에게 로그인 성공 알림

                            LoadTreeList((ArrayList)objlist[1], iep);     //멤버 리스트 전송 

                            logWrite("트리리스트 데이터 전송 완료!");

                            //// 다른 모든 클라이언트에게 로그인 사용자 정보 보내기(i|id|소속|ip|이름)
                            string smsg = "i|" + cl.getId() + "|" + cl.getTeam() + "|" + iep.Address.ToString() + "|" + cl.getName();
                            if (InClientList.Count != 0)
                            {
                                foreach (DictionaryEntry de in InClientList)
                                {
                                    if (de.Value != null)
                                        SendMsg(smsg, (IPEndPoint)de.Value);
                                }
                            }

                            //다른 로그인 사용자 정보 전송
                            TransferInList(iep);
                            logWrite("현재 로그인 사용자 정보 전송" + cl.getId());

                            //로그인 사용자 리스트(Hashtable) 등록(key=id , value=Client)
                            lock (InClientList)
                            {
                                InClientList[cl.getId()] = iep;
                                logWrite("InClientList[" + cl.getId() + "] = " + iep.Address.ToString() + ":" + iep.Port.ToString());
                            }

                            lock (InClientStat)
                            {
                                InClientStat[cl.getId()] = "online";
                                logWrite("InClientList[" + cl.getId() + "] = " + iep.Address.ToString() + ":" + iep.Port.ToString());
                            }

                            //로그인 사용자 내선리스트 등록
                            ExtensionList[cl.getPosition()] = iep;

                            logWrite("InClientList에 등록 : " + cl.getId());
                            logWrite("ExtensionList에 등록 : " + cl.getId() + " >> " + cl.getPosition());

                            //부재중 정보 전송
                            GetAbsenceData(cl.getId(), iep);


                        }
                        break;

                    case "mis":

                        SendMsg("f|p", iep);
                        break;

                    case "no":
                        SendMsg("f|n", iep);
                        break;
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        //private void Login(string[] arr, Socket s)
        //{
        //    try
        //    {
        //        string result = null;
        //        Client cl = null;
        //        string id = arr[1];
        //        string output = CheckRegist(arr);

        //        switch (output)
        //        {
        //            case "match":

        //                string svrMsg = null;
        //                cl = getClientInfo(id);

        //                if (InClientList.ContainsKey(id))
        //                {
        //                    if (InClientList[id] != null)  //이미 로그인 되어 있다면(중복 로그인 시도)
        //                    {
        //                        logWrite("중복 로그인 시도! : " + id.ToString());
        //                        svrMsg = "a|";
        //                        SendMsg(svrMsg, s);
        //                    }
        //                }
        //                else
        //                {
        //                    cl.setPosition(arr[3]);
        //                    logWrite(cl.getName() + "(" + cl.getId() + ") 님 로그인 성공!(" + DateTime.Now.ToString() + ")");

        //                    Statlist[id] = "6"; //로그인 성공시 사용자 프리젠스 6(로그아웃)으로 설정

        //                    svrMsg = "g|" + cl.getName() + "|" + cl.getTeam() + "|" + cl.getCompany();

        //                    if (s.Connected == true)
        //                    {
        //                        SendMsg(svrMsg, s);     //로그인 클라이언트에게 로그인 성공 알림
        //                    }
        //                    else
        //                    {
        //                        logWrite("Client Socket Disconnected");
        //                    }

        //                    LoadTreeList(cl.getId(), s);      //멤버 리스트 전송 

        //                    logWrite("트리리스트 데이터 전송 완료!");

        //                    string smsg = "i|" + cl.getId() + "|" + cl.getTeam() + "|" + cl.getName(); //i|id|소속|ip|이름
        //                    if (InClientList.Count != 0)
        //                    {
        //                        foreach (DictionaryEntry de in InClientList)
        //                        {
        //                            if (de.Value != null)
        //                                SendMsg(smsg, (Socket)de.Value);           // 다른 모든 클라이언트에게 로그인 사용자 정보 보내기
        //                        }
        //                    }

        //                    //로그인 사용자 정보 전송
        //                    TransferInList(s);
        //                    logWrite("현재 로그인 사용자 정보 전송" + cl.getId());

        //                    //로그인 사용자 리스트(Hashtable) 등록(key=id , value=Client)
        //                    InClientList[cl.getId()] = s;


        //                    //로그인 사용자 내선리스트 등록
        //                    ExtensionList[cl.getPosition()] = s;

        //                    logWrite("InClientList에 등록 : " + cl.getId());
        //                    logWrite("ExtensionList에 등록 : " + cl.getId() + " >> " + cl.getPosition());

        //                    //부재중 정보 전송
        //                    //GetAbsenceData(cl.getId(), s);

        //                    //MemberList 상태 변경
        //                    AddTextDelegate ChangeStat = new AddTextDelegate(ChangeListStat);
        //                    Invoke(ChangeStat, cl.getId() + "|i");

        //                }
        //                break;

        //            case "mis":

        //                SendMsg("f|p", s);
        //                break;

        //            case "no":
        //                SendMsg("f|n", s);
        //                break;
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}

        private void GetAbsenceData(string id, IPEndPoint iep)
        {
            try
            {
                MySqlConnection conn = GetmysqlConnection();
                int mnum = 0;
                int fnum = 0;
                int nnum = 0;
                int tnum = 0;
                try
                {
                    conn.Open();
                }
                catch (Exception e1)
                {
                    logWrite("GetAbsenceData() conn.Open 에러 :" + e1.ToString());
                }

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id", MySqlDbType.VarChar).Value = id;
                cmd.CommandText = "select form from t_noreceive where receiver=@id";
                MySqlDataReader dr = null;

                try
                {
                    dr = cmd.ExecuteReader();
                }
                catch (Exception e2)
                {
                    logWrite("GetAbsenceData():" + e2.ToString());
                }

                if (dr != null)
                {
                    while (dr.Read())
                    {
                        if (dr.GetString(0).Equals("m")) mnum++;
                        if (dr.GetString(0).Equals("f")) fnum++;
                        if (dr.GetString(0).Equals("n")) nnum++;
                        if (dr.GetString(0).Equals("t")) tnum++;
                    }
                }

                try
                {
                    conn.Close();
                }
                catch (Exception close)
                {
                    logWrite("conn close 에러 :" + close.ToString());
                }

                string msg = "A|" + mnum.ToString() + "|" + fnum.ToString() + "|" + nnum.ToString() + "|" + tnum.ToString();
                SendMsg(msg, iep);

                if (mnum > 0 )
                {
                    ArrayList memolist = ReadMemo(id);
                    string cmsg = "Q";
                    if (memolist != null && memolist.Count != 0)
                    {
                        foreach (object obj in memolist)
                        {
                            string[] array = (string[])obj;  //string[] { sender, content, time, seqnum }
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                cmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(cmsg, iep);
                }

                if (fnum > 0)
                {
                    ArrayList filelist = ReadFile(id);
                    string fmsg = "R";
                    if (filelist != null && filelist.Count != 0)
                    {
                        foreach (object obj in filelist)
                        {
                            string[] array = (string[])obj;  //string[] { sender,loc, content, time, size, seqnum }
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                fmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(fmsg, iep);
                }

                if (nnum > 0)
                {
                    ArrayList noticelist = ReadNotice(id);
                    string nmsg = "T";
                    if (noticelist != null && noticelist.Count != 0)
                    {
                        foreach (object obj in noticelist)
                        {
                            string[] array = (string[])obj;  //string[] { sender, content, time, nmode, seqnum, title }
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3] + "†" + array[4] + "†" + array[5];
                                nmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(nmsg, iep);
                }

                if (tnum > 0)
                {
                    ArrayList translist = ReadTransfer(id);
                    string tmsg = "trans";
                    if (translist != null && translist.Count != 0)
                    {
                        foreach (object obj in translist)
                        {
                            string[] array = (string[])obj;//string[]{sender,content, time, seqnum} , content => pass|ani|senderID|receiverID|일자|시간|CustomerName
                            string temp = array[1];
                            array[1] = temp.Replace('|', '&');
                            if (array.Length != 0)
                            {
                                string item = array[0] + "†" + array[1] + "†" + array[2] + "†" + array[3];
                                tmsg += "|" + item;
                            }
                        }
                    }
                    iep = (IPEndPoint)InClientList[id];
                    SendMsg(tmsg, iep);
                }
               
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }


        private string CheckRegist(string[] arr)
        {
            string result = null;

            MySqlConnection myconn = GetmysqlConnection();

            try
            {
                myconn.Open();

                MySqlCommand command = new MySqlCommand();
                command.Connection = myconn;
                command.Parameters.AddWithValue("@id", arr[1]);
                string query = "select user_pwd from t_user where user_id=@id";
                command.CommandText = query;
                MySqlDataReader reader = command.ExecuteReader();
                string passInDB = null;

                if (reader.HasRows)
                {
                    if (reader.Read())
                    {
                        passInDB = reader.GetString(0);
                        if (passInDB.Equals(arr[2]))
                        {
                            result = "match";
                        }
                        else
                        {
                            result = "mis";
                        }
                    }
                }
                else
                {
                    result = "no";
                }
                myconn.Close();
                logWrite("인증결과 : " + result);
            }
            catch (Exception ex)
            {
                logWrite("CheckRegist Exception : " + ex.ToString());
                if (myconn.State == ConnectionState.Open)
                {
                    myconn.Close();
                }
            }
            
            return result;
        }

        private ArrayList getClientIDs()
        {
            ArrayList idList = new ArrayList();
            try
            {
                MySqlConnection conn = GetmysqlConnection();
                conn.Open();

                if (conn.State == ConnectionState.Open)
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn;
                    string query = "select USER_ID from t_user";
                    command.CommandText = query;
                    MySqlDataReader dr = command.ExecuteReader();

                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            string dbid = dr.GetString(0);
                            idList.Add(dbid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
            return idList;
        }

        private ArrayList getClientInfo(string id)
        {
            ArrayList objlist = new ArrayList();
            Client cl = null;
            ArrayList team_list = new ArrayList();
            Hashtable TeamTable = new Hashtable();
            try
            {
                string TeamString = "M|";

                MySqlConnection conn = GetmysqlConnection();
                conn.Open();
                
                if (conn.State == ConnectionState.Open)
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn;
                    string query = "select u.user_id, u.user_nm, u.team_nm, c.com_nm, u.com_cd from t_user as u, t_company as c where u.com_cd=c.com_cd";
                    command.CommandText = query;
                    MySqlDataReader dr = command.ExecuteReader();

                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            string dbid = dr.GetString(0);
                            string name = dr.GetString(1);
                            string team = dr.GetString(2);
                            string com_nm = dr.GetString(3);
                            string com_cd = dr.GetString(4);

                            if (com_cd.Equals(this.com_code))
                            {
                                if (id.Equals(dbid))
                                {
                                    cl = new Client(id, name, team, "", com_nm, com_cd);
                                }

                                lock (TeamNameList)
                                {
                                    TeamNameList[dbid] = team;
                                    logWrite("TeamNameList[" + dbid + "] = " + team);
                                }
                                string minfo = null;
                                minfo = dr.GetString(0) + "!" + dr.GetString(1);
                                string tname = dr.GetString(2);
                                ArrayList temp = null;
                                if (TeamTable.Count > 0)
                                {
                                    if (TeamTable.ContainsKey(tname))
                                    {
                                        temp = (ArrayList)TeamTable[tname];
                                        temp.Add(minfo);
                                        TeamTable[tname] = temp;
                                    }
                                    else
                                    {
                                        temp = new ArrayList();
                                        temp.Add(minfo);
                                        TeamTable[tname] = temp;
                                    }
                                }
                                else
                                {
                                    temp = new ArrayList();
                                    temp.Add(minfo);
                                    TeamTable[tname] = temp;
                                }
                            }
                        }

                    }

                    logWrite("팀리스트 생성 시작!");

                    foreach (DictionaryEntry de in TeamTable)
                    {
                        if (de.Value != null)
                        {
                            string teamstr = (string)de.Key;
                            ArrayList temp = (ArrayList)de.Value;
                            foreach (string str in temp)
                            {
                                teamstr += "|" + str;
                            }
                            team_list.Add(TeamString + teamstr);
                            logWrite(TeamString + teamstr);
                        }
                    }

                    logWrite("팀리스트 생성 완료!");
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }

            objlist.Add(cl);
            objlist.Add(team_list);

            return objlist;
        }


        private void Logout(string id)
        {
            try
            {
                id = id.ToLower();
                if (InClientList.ContainsKey(id) && InClientList[id] != null)
                {
                    lock (InClientList)
                    {
                        InClientList[id] = null;
                    }
                    logWrite("InClientList로부터 삭제 : " + id);


                    string temp_team = GetTeamName(id);
                    string smsg = "o|" + id + "|" + temp_team;   //smsg= o|id|소속

                    // 다른 모든 클라이언트에게 로그아웃 정보 보내기
                    //2010.4.28일 수정 : 로그아웃 중 다른 쓰레드의 InClientList 접근으로 생기는 오류 방지 lock(InClientList) 
                    lock (InClientList)
                    {
                        foreach (DictionaryEntry de in InClientList)
                        {
                            if (de.Value != null)
                            {
                                if (!de.Key.ToString().Equals(id))
                                {
                                    SendMsg(smsg, (IPEndPoint)de.Value);
                                }
                            }
                        }
                    }
                    logWrite(id + " 로그아웃 완료!");
                }
                else
                {
                    logWrite("Logout() : " + id + "가 InClientList key에서 찾을 수 없습니다.");
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        public void TransferInList(IPEndPoint iep)
        {
            try
            {
                if (InClientList.Count != 0)
                {
                    foreach (DictionaryEntry de in InClientList)
                    {
                        if (de.Value != null)
                        {
                            string ip = ((IPEndPoint)de.Value).Address.ToString();
                            string msg = "y|" + (String)de.Key + "|" + InClientStat[de.Key.ToString()].ToString();       //y|로그인상담원id|(string)IP주소, de.Key=id
                            SendMsg(msg, iep);
                            msg = "IP|" + (String)de.Key + "|" + ip;
                            SendMsg(msg, iep);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        public bool SendMsg(string msg, IPEndPoint iep)
        {
            bool isError = false;
            try
            {

                lock (sendSock)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    byte[] re = null;

                    iep.Port = 8883;

                    sendSock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 500);

                    logWrite("receiver IP : " + iep.Address.ToString());
                    logWrite("receiver port : " + iep.Port.ToString());
                    logWrite("sendMessage : " + msg);
                    for (int i = 0; i < 2; i++)
                    {
                        try
                        {
                            Thread.Sleep(10);
                            if (buffer.Length < 40960)
                            {
                                sendSock.Send(buffer, buffer.Length, iep);
                            }
                            else
                            {
                                sendSock.Send(buffer, 40960, iep);
                            }
                            re = sendSock.Receive(ref iep);

                            logWrite("send() : " + i.ToString() + "번째 시도!");
                            if (re != null && re.Length != 0)
                            {
                                logWrite("응답");
                                //logWrite("보낸 메시지 :" + msg);
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            //logWrite(e.ToString());
                        }
                    }

                    //if (re == null || re.Length == 0)
                    //{
                    //    isError = true;
                    //    logWrite("SendMsg() 에러 : 접속에러 수신자 : " + iep.Address.ToString() + ":" + iep.Port.ToString());
                    //    foreach (DictionaryEntry de in InClientList)
                    //    {
                    //        if (iep.Address.ToString().Equals(((IPEndPoint)de.Value).Address.ToString()))
                    //        {
                    //            if (!SendErrorList.Contains((string)de.Key))
                    //            {
                    //                SendErrorList.Add((string)de.Key);
                    //            }
                    //            break;
                    //        
                    //    }
                    //}
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
            return isError;
        }

        public bool SendRinging(string msg, IPEndPoint iep)
        {
            bool result = false;
            try
            {

                lock (sendSock)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    byte[] re = null;

                    iep.Port = 8883;

                    sendSock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 200);

                    logWrite("receiver IP : " + iep.Address.ToString());
                    logWrite("receiver port : " + iep.Port.ToString());

                    for (int i = 0; i < 2; i++)
                    {
                        try
                        {
                            Thread.Sleep(10);
                            if (buffer.Length < 40960)
                            {
                                sendSock.Send(buffer, buffer.Length, iep);
                            }
                            else
                            {
                                sendSock.Send(buffer, 40960, iep);
                            }
                            re = sendSock.Receive(ref iep);

                            logWrite("send() : " + i.ToString() + "번째 시도!");
                            if (re != null && re.Length != 0)
                            {
                                logWrite("응답");
                                //logWrite("보낸 메시지 :" + msg);
                                result = true;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            logWrite(e.ToString());
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
            return result;
        }

        //public void SendMsg(string msg, Socket sendsocket)
        //{
        //    try
        //    {
        //        byte[] msgbuffer = Encoding.UTF8.GetBytes(msg);
        //        try
        //        {
        //            if (sendsocket != null && sendsocket.Connected == true)
        //            {
        //                byte[] lengbuffer = new byte[9];

        //                byte[] tempbuffer = Encoding.ASCII.GetBytes("SIZE" + msgbuffer.Length.ToString());

        //                for (int i = 0; i < tempbuffer.Length; i++)
        //                {
        //                    lengbuffer[i] = tempbuffer[i];
        //                }

        //                int sendbuffer = sendsocket.Send(lengbuffer);

        //                if (sendbuffer == lengbuffer.Length)
        //                {
        //                    sendsocket.Send(msgbuffer);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            logWrite(e.ToString());
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        logWrite(exception.ToString());
        //    }
        //}

        private ArrayList getBufferList(byte[] buffer)
        {
            ArrayList bufferArray = new ArrayList();
            try
            {
                int size = 0;
                byte[] part = new byte[64000];
                for (int i = 0; i < buffer.Length; i++)
                {
                    part[size] = buffer[i];
                    size++;
                    if (size > 64000)
                    {
                        bufferArray.Add(part);
                        size = 0;
                        part.Initialize();
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite("getBufferList() Exception : " + ex.ToString());
            }
            return bufferArray;
        }

        public void ErrorConnClear(string id) //전송에러 접속자 로그아웃 처리
        {
            try
            {
                Logout(id);
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void InsertNoReceiveforNotice(string receiver, string loc, string content, string form, string sender, string mode, string title)
        {
            try
            {
                int row = 0;
                MySqlConnection conn = GetmysqlConnection();

                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    logWrite("InsertNoReceive() conn.Open 에러 :" + e.ToString());
                }
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@nreceiver", MySqlDbType.VarChar).Value = receiver;
                cmd.Parameters.Add("@ntime", MySqlDbType.VarChar).Value = DateTime.Now.ToString();
                cmd.Parameters.Add("@nloc", MySqlDbType.VarChar).Value = loc;
                cmd.Parameters.Add("@ncontent", MySqlDbType.Text).Value = content;
                cmd.Parameters.Add("@nform", MySqlDbType.VarChar).Value = form;
                cmd.Parameters.Add("@nsender", MySqlDbType.VarChar).Value = sender;
                cmd.Parameters.Add("@nmode", MySqlDbType.VarChar).Value = mode;
                cmd.Parameters.Add("@title", MySqlDbType.VarChar).Value = title;
                cmd.CommandText = "insert into t_noreceive(receiver, time, loc, content, form, sender, nmode, ntitle) values(@nreceiver, @ntime, @nloc, @ncontent, @nform, @nsender, @nmode, @title)";
                try
                {
                    row = cmd.ExecuteNonQuery();
                }
                catch (Exception ex1)
                {
                    logWrite("InsertNoReceive() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
                }
                if (row != 0)
                {
                    logWrite(sender + "to " + receiver + " insert DB");
                }
                try
                {
                    conn.Close();
                }
                catch (Exception ex2)
                {
                    logWrite("InsertNoReceive() conn.Close 에러 :" + ex2.ToString());
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void InsertNoReceive(string receiver, string loc, string content, string form, string sender, string mode)
        {
            try
            {
                int row = 0;
                MySqlConnection conn = GetmysqlConnection();

                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    logWrite("InsertNoReceive() conn.Open 에러 :" + e.ToString());
                }
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@nreceiver", MySqlDbType.VarChar).Value = receiver;
                cmd.Parameters.Add("@ntime", MySqlDbType.VarChar).Value = DateTime.Now.ToString();
                cmd.Parameters.Add("@nloc", MySqlDbType.VarChar).Value = loc;
                cmd.Parameters.Add("@ncontent", MySqlDbType.Text).Value = content;
                cmd.Parameters.Add("@nform", MySqlDbType.VarChar).Value = form;
                cmd.Parameters.Add("@nsender", MySqlDbType.VarChar).Value = sender;
                cmd.Parameters.Add("@mode", MySqlDbType.VarChar).Value = mode;
                cmd.CommandText = "insert into t_noreceive(receiver, time, loc, content, form, sender, nmode) values(@nreceiver, @ntime, @nloc, @ncontent, @nform, @nsender, @mode)";
                try
                {
                    row = cmd.ExecuteNonQuery();
                }
                catch (Exception ex1)
                {
                    logWrite("InsertNoReceive() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
                }
                if (row != 0)
                {
                    logWrite(sender + "to " + receiver + " insert DB");
                }
                try
                {
                    conn.Close();
                }
                catch (Exception ex2)
                {
                    logWrite("InsertNoReceive() conn.Close 에러 :" + ex2.ToString());
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private ArrayList ReadMemo(string id)
        {
            MySqlConnection conn = GetmysqlConnection();

            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                logWrite("ReadMemo() conn.Open 에러 :" + e.ToString());
            }
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add("@id", MySqlDbType.VarChar).Value = id;
            cmd.CommandText = "select sender, content, time, seqnum from t_noreceive where receiver=@id and form='m'";
            MySqlDataReader dr = null;
            try
            {
                dr = cmd.ExecuteReader();
            }
            catch (Exception ex1)
            {
                logWrite("ReadMemo() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
            }
            ArrayList list = new ArrayList();
            if (dr!=null)
            {
                while (dr.Read())
                {
                    string sender = dr.GetString(0);
                    string content = dr.GetString(1);
                    string time = dr.GetString(2);
                    string seqnum = dr.GetValue(3).ToString();
                    string[] str = new string[] { sender, content, time, seqnum };
                    list.Add(str);
                }
            }
            else logWrite(id + " 의 메모 없음!");
            try
            {
                conn.Close();
            }
            catch (Exception ex2)
            {
                logWrite("ReadMemo() conn.Close 에러 :" + ex2.ToString());
            }
            return list;
        }

        private ArrayList ReadNotice(string id)
        {
            ArrayList list = new ArrayList();
            try
            {
                MySqlConnection conn = GetmysqlConnection();

                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    logWrite("ReadNotice() conn.Open 에러 :" + e.ToString());
                }
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id", MySqlDbType.VarChar).Value = id;
                cmd.CommandText = "select sender, content, time, nmode, seqnum, ntitle from t_noreceive where receiver=@id and form='n'";
                MySqlDataReader dr = null;
                try
                {
                    dr = cmd.ExecuteReader();
                }
                catch (Exception ex1)
                {
                    logWrite("ReadNotice() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
                }

                if (dr != null)
                {
                    while (dr.Read())
                    {
                        string sender = dr.GetString(0);
                        string content = dr.GetString(1);
                        string time = dr.GetString(2);
                        string nmode = dr.GetString(3);
                        string seqnum = dr.GetValue(4).ToString();
                        string title = "공지사항";
                        if (!dr.IsDBNull(5))
                        {
                            title = dr.GetString(5);
                        }
                        string[] str = new string[] { sender, content, time, nmode, seqnum, title };
                        list.Add(str);
                    }
                }
                else logWrite(id + "의 공지 없음!");
                try
                {
                    conn.Close();
                }
                catch (Exception ex2)
                {
                    logWrite("ReadNotice() conn.Close 에러 :" + ex2.ToString());
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
            return list;
        }


        private ArrayList ReadFile(string id)
        {
            MySqlConnection conn = GetmysqlConnection();
            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                logWrite("ReadFile() conn.Open 에러 :" + e.ToString());
            }
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add("@id", MySqlDbType.VarChar).Value = id;
            cmd.CommandText = "select tn.sender, tn.loc, tn.content, tn.time, tn.seqnum, tf.fsize from t_noreceive tn, t_files tf where tn.receiver=@id and tn.form='f' and tn.loc=tf.seqnum;";
            MySqlDataReader dr = null;
            try
            {
                dr = cmd.ExecuteReader();
            }
            catch (Exception ex1)
            {
                logWrite("ReadFile() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
            }
            ArrayList list = new ArrayList();
            if (dr != null)
            {
                while (dr.Read())
                {
                    string sender = dr.GetString(0);
                    string loc = dr.GetString(1);
                    int num = Convert.ToInt32(loc);
                    string content = dr.GetString(2);  //파일명
                    string time = dr.GetString(3);
                    string seqnum = dr.GetValue(4).ToString();
                    string fsize = dr.GetValue(5).ToString();
                    logWrite(loc);
                    string[] str = new string[] { sender, loc, content, time, fsize, seqnum}; //content= 파일명
                    list.Add(str);
                }
            }
            else logWrite(id + "의 부재중 파일 없음!");

            try
            {
                conn.Close();
            }
            catch (Exception ex2)
            {
                logWrite("ReadFile() conn.Close 에러 :" + ex2.ToString());
            }
            return list;
        }

        private ArrayList ReadTransfer(string id)
        {
            MySqlConnection conn = GetmysqlConnection();
            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                logWrite("ReadTransfer() conn.Open 에러 :" + e.ToString());
            }
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add("@id", MySqlDbType.VarChar).Value = id;
            cmd.CommandText = "select sender, content, time, seqnum from t_noreceive where receiver=@id and form='t'";
            MySqlDataReader dr = null;
            try
            {
                dr = cmd.ExecuteReader();
            }
            catch (Exception ex1)
            {
                logWrite("ReadTransfer() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
            }
            ArrayList list = new ArrayList();
            if (dr != null)
            {
                while (dr.Read())
                {
                    string sender = dr.GetString(0);
                    string content = dr.GetString(1);
                    string time = dr.GetString(2);
                    string seqnum = dr.GetValue(3).ToString();
                    
                    string[] str = new string[] { sender, content, time, seqnum};
                    list.Add(str);
                }
            }
            
            try
            {
                dr.Close();
                conn.Close();
            }
            catch (Exception ex2)
            {
                logWrite("ReadTransfer() conn.Close 에러 :" + ex2.ToString());
            }

            return list;
        }

        private ArrayList GetNoticeList()
        {
            ArrayList ids = getClientIDs();
            ArrayList list = new ArrayList();
            try
            {
                foreach (string cid in ids)
                {
                    if (InClientList.ContainsKey(cid))
                    {
                        if (InClientList[cid] == null)
                        {
                            list.Add(cid);
                        }
                    }
                    else
                    {
                        list.Add(cid);
                    }
                }
                logWrite("공지사항 전송 : " + list.Count.ToString());
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
            return list;
        }


        private void InsertNotice(ArrayList list, string noticetime, string content, string sender, string mode, string title)
        {
            MySqlConnection conn = GetmysqlConnection();
            try
            {
                int row = 0;
                logWrite("InsertNotice list.count : " + list.Count.ToString());
                

                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    logWrite("InsertNotice() conn.Open 에러 :" + e.ToString());
                }
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@content", MySqlDbType.Text).Value = content;
                cmd.Parameters.Add("@ntime", MySqlDbType.VarChar).Value = noticetime;
                cmd.Parameters.Add("@sender", MySqlDbType.VarChar).Value = sender;
                cmd.Parameters.Add("@nmode", MySqlDbType.VarChar).Value = mode;
                cmd.Parameters.Add("@Title", MySqlDbType.VarChar).Value = title;

                cmd.CommandText = "insert into t_notices(content, ntime, sender, nmode, ntitle) values(@content, @ntime, @sender, @nmode, @Title)";
                try
                {
                    row = cmd.ExecuteNonQuery();
                }
                catch (Exception ex1)
                {
                    logWrite("InsertNotice() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
                }
                if (row != 0)
                {
                    logWrite(" notices insert DB");
                    string loc = "";
                    cmd.CommandText = "select seqnum from t_notices order by seqnum desc";
                    MySqlDataReader dr = null;

                    try
                    {
                        dr = cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        logWrite(ex.ToString());
                    }
                    if (dr.Read())
                    {
                        loc = dr.GetValue(0).ToString();
                        logWrite("seqnum.currval : " + loc);
                    }
                    ///현재 시퀀스값 찾을 때 에러남 -> 사실 불필요한 로직 삭제


                    try
                    {
                        conn.Close();
                    }
                    catch (Exception ex2)
                    {
                        logWrite("InsertNoReceive() conn.Close 에러 :" + ex2.ToString());
                    }

                    if (list != null && list.Count != 0)
                    {
                        foreach (object obj in list)
                        {
                            string tempid = (string)obj;
                            InsertNoReceiveforNotice(tempid, loc, content, "n", sender, mode, title);
                            Thread.Sleep(10);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
                
            }
        }

        public int getListenPort(int port) //임시 테스트용
        {
            
            if (port == 8884) port = 8883;
            if (port == 8886) port = 8885;
            if (port == 8888) port = 8887;
            return port;
        }

        public ArrayList makeList() //리스너 시작시 멤버 리스트 얻어옴.
        {
            ArrayList team_list = new ArrayList();
            Hashtable TeamTable = new Hashtable();
            try
            {
                string TeamString = "M|";
                MySqlConnection conn = GetmysqlConnection();
                conn.Open();
                if (conn.State == ConnectionState.Open)
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "select user_id, user_nm, team_nm from t_user";
                    MySqlDataReader dr = null;

                    dr = cmd.ExecuteReader();

                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            string minfo = null;
                            minfo = dr.GetString(0) + "!" + dr.GetString(1);
                            string tname = dr.GetString(2);
                            ArrayList temp = null;
                            if (TeamTable.Count > 0)
                            {
                                if (TeamTable.ContainsKey(tname))
                                {
                                    temp = (ArrayList)TeamTable[tname];
                                    temp.Add(minfo);
                                    TeamTable[tname] = temp;
                                }
                                else
                                {
                                    temp = new ArrayList();
                                    temp.Add(minfo);
                                    TeamTable[tname] = temp;
                                }
                            }
                            else
                            {
                                temp = new ArrayList();
                                temp.Add(minfo);
                                TeamTable[tname] = temp;
                            }
                        }
                    }
                }

                logWrite("팀리스트 생성 시작!");

                foreach (DictionaryEntry de in TeamTable)
                {
                    if (de.Value != null)
                    {
                        string teamstr = (string)de.Key;
                        ArrayList temp = (ArrayList)de.Value;
                        foreach (string str in temp)
                        {
                            teamstr += "|" + str;
                        }
                        team_list.Add(TeamString + teamstr);
                        logWrite(TeamString + teamstr);
                    }
                }

                logWrite("팀리스트 생성 완료!");
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
            return team_list;
        }


        public void LoadTreeList(ArrayList team_list, IPEndPoint iep) //로그인 상태와는 무관한 전체 사용자 리스트 생성
        {
            try
            {
                foreach (string tempString in team_list)   //TeamList(M|팀명|id!name|id!name|....
                {
                    if (tempString != null && tempString.Length != 0)
                    {
                        SendMsg(tempString, iep);    //팀멤버 목록 전송
                    }
                }
                SendMsg("M|e", iep); //모든 팀리스트 정보 전송완료 메시지 전송
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        
        public int getMode(string msg)
        {
            int mode = 0;
            string[] udata = null;
            try
            {
                msg = msg.Trim();
                if (msg.Contains("|"))
                {
                    udata = msg.Split('|');
                }
                mode = Convert.ToInt32(udata[0]);
             
            }
            catch (Exception ex)
            {
                logWrite("getMode() Exception : " + ex.ToString());
                mode = 10000;
            }
            return mode;
        }

        private void OnLogWrite(object sender, StringEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                logWrite(e.EventString);
            });
        }

        /// <summary>
        /// 서버 로그창에 로그 쓰기 및 로그파일에 쓰기
        /// </summary>
        /// <param name="svrLog"></param>
        public void logWrite(string svrLog)
        {
            try
            {
                AddText = new AddTextDelegate(writeLogBox);
                svrLog = "[" + DateTime.Now.ToString() + "] " + svrLog + "\r\n";
                if (LogBox.InvokeRequired)
                {
                    Invoke(AddText, svrLog);
                    if (CanFileWrite == true)
                        logFileWrite(svrLog);
                }
                else
                {
                    LogBox.AppendText(svrLog);
                    if (CanFileWrite == true)
                        logFileWrite(svrLog);
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        private void writeLogBox(string str)
        {
            LogBox.AppendText(str);
        }

        /// <summary>
        /// 서버 관련 파일 폴더 생성
        /// </summary>
        public void svr_FileCheck()
        {
            try
            {
                DirectoryInfo filefolder = new DirectoryInfo(Application.StartupPath + "\\files");
                if (!filefolder.Exists)
                {
                    filefolder.Create();
                    logWrite(" 폴더 생성!");
                }

                DirectoryInfo logfolder = new DirectoryInfo(Application.StartupPath + "\\log");
                if (!logfolder.Exists)
                {
                    logfolder.Create();
                    logWrite("log 폴더 생성");
                }

                DirectoryInfo updaterDir = new DirectoryInfo(UpdateTargetDir);
                if (!updaterDir.Exists)
                {
                    updaterDir.Create();
                }

                FileInfo[] files = null;
                DirectoryInfo update = new DirectoryInfo(UpdateAppDir);

                if (update.Exists)
                {
                    files = update.GetFiles();
                    foreach (FileInfo fi in files)
                    {
                        FileInfo finfo = new FileInfo(UpdateTargetDir + "\\" + fi.Name);
                        fi.CopyTo(finfo.FullName, true);
                    }
                }
                else
                {
                    logWrite("WeDoUpdater.Exists = false");
                }
            }
            catch (IOException ioe)
            {

            }
            catch (Exception e)
            {
                //logWrite(e.ToString() + " : 폴더를 생성하지 못했습니다.");
            }
            CanFileWrite = true;
        }


        /// <summary>
        /// 로그파일 생성 및 쓰기
        /// </summary>
        /// <param name="_log"></param>
        public void logFileWrite(string _log)
        {
            try
            {
                //di = new DirectoryInfo(Application.StartupPath + "\\log\\" + DateTime.Now.ToShortDateString() + ".log");

                //if (!di.Exists)
                //{
                //    svr_FileCheck();
                //}

                try
                {
                    sw = new StreamWriter(Application.StartupPath + "\\log\\" + DateTime.Now.ToShortDateString() + ".log", true);
                    sw.WriteLine(_log);
                    sw.Flush();
                    sw.Close();
                }
                catch (Exception e)
                {
                    if (this.InvokeRequired)
                    {
                        Invoke(AddText, "logFileWriter() 에러 : " + e.ToString());
                    }
                    else logWrite("logFileWriter() 에러 : " + e.ToString());
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

        /// <summary>
        /// 서버 중지
        /// </summary>
        public void ServerStop()
        {
            try
            {
                timer.Stop();
                if (InClientList != null)
                {
                    if (InClientList.Count != 0)
                    {
                        InClientList.Clear();
                    }
                    logWrite("InClientList 삭제");
                    
                    if (TeamList != null)
                    {
                        if (TeamList.Count != 0) TeamList.Clear();
                    }
                    logWrite("TeamList 삭제");
                    if (Statlist != null)
                    {
                        if (Statlist.Count != 0) Statlist.Clear();
                    }
                    
                    //if (dev.Started == true)
                    //{
                    //    dev.Close();
                    //}
                }
            }
            catch (Exception ex)
            {
                logWrite("ServerStop 에러 : " + ex.ToString());
            }

            try
            {
                if (filesock != null)
                {
                    filesock.Close();
                }
                if (statsock != null)
                {
                    statsock.Close();
                }

                if (ThreadList.Count != 0)
                {
                    foreach (DictionaryEntry de in ThreadList)
                    {
                        if (de.Value != null)
                        {
                            ((Thread)de.Value).Abort();
                        }
                    }
                    ThreadList.Clear();
                }

                if (ReceiverThread != null)
                {

                    if (ReceiverThread.IsAlive == true)
                    {
                        ReceiverThread.Abort();
                        logWrite("ReceiverThread가 종료되었습니다.");
                    }
                }
               
                if (ListenThread != null)
                {
                    
                    if (ListenThread.IsAlive == true)
                    {
                        
                        ListenThread.Abort();
                        listnerstarted = false;
                        logWrite("ListenThread가 종료되었습니다.");
                    }
                }

                if (CheckThread != null)
                {

                    if (CheckThread.IsAlive == true)
                    {

                        CheckThread.Abort();
                        listnerstarted = false;
                        logWrite("StatThread가 종료되었습니다.");
                    }
                }

                commctl.disConnect();
            }
            catch (Exception ex1)
            {
                logWrite("Listenthread close 에러 : "+ex1.ToString());
                svrStart = false;
            }
            start.Visible = true;
            svrStart = false;
            this.Close();
            notify_svr.Visible = false;
            Process.GetCurrentProcess().Kill();
        }

        private void stop_Click(object sender, EventArgs e)
        {
            stopApplication();
        }

        private void stopApplication()
        {
            DialogResult result = MessageBox.Show(this, "정말 메신저 서버를 중단시키겠습니까?", "서버 중단 경고", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                ServerStop();
                Application.ExitThread();
                Application.Exit();
                notify_svr.Visible = false;
                Process.GetCurrentProcess().Kill();
            }
        }

        private void MsgSvrForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }


        private void btn_confirm_ClickforTeam(object sender, EventArgs e)
        {
            try
            {
                string teamname = null;
                Button button = (Button)sender;
                int count = button.Parent.Controls.Count;

                for (int i = 0; i < count; i++)
                {
                    if (button.Parent.Controls[i].Name.Equals("txtbox_teamname"))
                    {
                        TextBox box = (TextBox)button.Parent.Controls[i];
                        if (box.Text.Length != 0)
                        {
                            teamname = box.Text;
                        }
                        break;
                    }
                }
                if (teamname != null && teamname.Length != 0)
                {
                    InsertTeam(teamname);
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }



        private void InsertTeam(string teamname)
        {
            try
            {
                MySqlConnection conn = GetmysqlConnection();
                int row = 0;
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    logWrite("InsertTeam() conn.Open 에러 :" + e.ToString());
                }
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@team", MySqlDbType.VarChar).Value = teamname;
                cmd.CommandText = "insert into team values(@team)";
                try
                {
                    row = cmd.ExecuteNonQuery();
                }
                catch (Exception ex1)
                {
                    logWrite("InsertTeam() cmd.ExecuteNonQuery() 에러 : " + ex1.ToString());
                }
                if (row != 0)
                {
                    logWrite("new Team" + teamname + " insert DB");
                }
                try
                {
                    conn.Close();
                }
                catch (Exception ex2)
                {
                    logWrite("InsertTeam() conn.Close 에러 :" + ex2.ToString());
                }
            }
            catch (Exception exception)
            {
                logWrite(exception.ToString());
            }
        }

       

        private void button1_Click(object sender1, EventArgs e)
        {
            makeCallTestForm();
        }

        private void makeCallTestForm()
        {
            calltestform = new CallTestForm();
            calltestform.btn_confirm.MouseClick += new MouseEventHandler(btn_confirm_MouseClick);
            calltestform.button1.MouseClick += new MouseEventHandler(button1_MouseClick);
            calltestform.Show();
        }

        private void button1_MouseClick(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            CallTestForm form = (CallTestForm)button.Parent;
            form.Close();
        }

        private void btn_confirm_MouseClick(object sender, MouseEventArgs e)
        {
            Thread t1 = new Thread(new ThreadStart(sendTestRing));
            t1.Start();
        }

        private void sendTestRing()
        {
            try
            {
                Thread.Sleep(3000);
                string aniNum = calltestform.txtbox_ani.Text;
                string extNum = calltestform.txtbox_ext.Text;
                int delay = Convert.ToInt32(calltestform.txtbox_time.Text);


                if (server_type.Equals(ConstDef.NIC_LG_KP))
                {
                    RecvMessage("Ringing", aniNum);
                    if (extNum.Length > 0)
                    {
                        Thread.Sleep(delay);
                        RecvMessage("Answer", aniNum + ">" + extNum);
                    }
                }
                else if (server_type.Equals(ConstDef.NIC_SIP))
                {
                    string call_id = DateTime.Now.ToString("yyyyMMddHHmmss#" + aniNum);
                    RecvMessage("Ringing", aniNum + "|" + extNum + "|" + call_id);
                    if (extNum.Length > 0)
                    {
                        Thread.Sleep(delay);
                        RecvMessage("Answer", aniNum + "|" + extNum + "|" + call_id);

                        Thread.Sleep(delay);
                        RecvMessage("HangUp", aniNum + "|" + extNum + "|" + call_id);
                        //RecvMessage("Abandon", aniNum + "|" + extNum + "|" + call_id);
                        
                    }
                }
                else if (server_type.Equals(ConstDef.NIC_CID_PORT1)
                    || server_type.Equals(ConstDef.NIC_CID_PORT2)
                    || server_type.Equals(ConstDef.NIC_CID_PORT4))
                {
                    RecvMessage("Ringing", aniNum);
                    if (extNum.Length > 0)
                    {
                        Thread.Sleep(delay);
                        RecvMessage("OffHook", "");
                    }
                }
            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
            }
        }

       

        private SIPMessage makeSIPConstructor(string data)
        {
            SIPM = new SIPMessage();
            StreamWriter sw = new StreamWriter("PacketDump_" + DateTime.Now.ToShortDateString() + ".txt", true, Encoding.Default);
            try
            {
                string code = "Unknown";
                string method = "Unknown";
                string callid = "Unknown";
                string cseq = "Unknown";
                string from = "Unknown";
                string to = "Unknown";
                string agent = "Unknown";
                string sName = "Unknown";


                StringReader sr = new StringReader(data);
                
                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();
                    string Gubun = "";

                    if (line.Length > 2)
                    {
                        Gubun = line.Substring(0, 3);
                    }
                    if (Gubun.Equals("REG"))
                    {
                        break;
                    }
                    else
                    {
                        if (Gubun.Equals(ConstDef.NIC_SIP))  //Status Line
                        {
                            string[] sipArr = line.Split(' ');
                            if (sipArr.Length > 0)
                            {
                                code = sipArr[1].Trim();
                                method = sipArr[2].Trim();
                                sw.WriteLine("code : "+code + " / method : " + method);
                            }
                        }
                        else if (Gubun.Equals("INV"))
                        {
                            method = "INVITE";
                        }
                        else if (Gubun.Equals("CAN"))
                        {
                            method = "CANCEL";
                        }
                        else
                        {
                            string[] sipArr = line.Split(':');
                            if (sipArr.Length < 2)
                            {
                                sipArr = line.Split('=');
                                if (sipArr.Length > 1)
                                {
                                    sw.WriteLine(sipArr[0] + " = " + sipArr[1]);
                                    if (sipArr[0].Equals("s")) sName = sipArr[1];
                                }
                            }
                            else
                            {
                                string key = sipArr[0];

                                switch (key)
                                {
                                    case "From":
                                        from = sipArr[2].Split('@')[0];
                                        sw.WriteLine("From = " + from);
                                        break;

                                    case "To":
                                        to = sipArr[2].Split('@')[0];
                                        sw.WriteLine("To = " + to);
                                        break;

                                    case "Call-ID":
                                        callid = sipArr[1].Split('@')[0];
                                        sw.WriteLine("Call-ID = " + callid);
                                        break;

                                    case "CSeq":
                                        cseq = sipArr[1].Split('@')[0];
                                        sw.WriteLine("CSeq = " + cseq);
                                        break;

                                    case "User-Agent":
                                        agent = sipArr[1].Split('@')[0];
                                        sw.WriteLine("User-Agent = " + cseq);
                                        break;

                                    default:

                                        string value = "";
                                        for (int i = 1; i < sipArr.Length; i++)
                                        {
                                            value += sipArr[i];
                                        }
                                        sw.WriteLine(key + " = " + value);

                                        break;
                                }
                            }
                        }
                    }
                }
                sw.WriteLine("\r\n");
                sw.WriteLine("###########");
                sw.Flush();
                sw.Close();
                if (!from.Equals(to) && !from.Equals("unknown") && !to.Equals("unknown"))
                {
                    logWrite(data);
                }
                SIPM.setSIPMessage(code, method, callid, cseq, from, to, agent, sName);

            }
            catch (Exception ex)
            {
                logWrite(ex.ToString());
                sw.Close();
            }

            return SIPM;
        }

        private void MnDBSetting_Click(object sender, EventArgs e)
        {
            dbinfo = new DBInfoForm();
            dbinfo.btn_confirm.MouseClick += new MouseEventHandler(btn_dbConfirm_MouseClick);
            dbinfo.btn_close.MouseClick += new MouseEventHandler(btn_close_MouseClick);
            dbinfo.tbx_host.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_HOST"];
            dbinfo.tbx_dbname.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_NAME"];
            dbinfo.tbx_id.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_USER"];
            dbinfo.tbx_pass.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_PASS"];
            dbinfo.Show();
        }

        private void btn_close_MouseClick(object sender, MouseEventArgs e)
        {
            dbinfo.tbx_host.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_HOST"];
            dbinfo.tbx_dbname.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_NAME"];
            dbinfo.tbx_id.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_USER"];
            dbinfo.tbx_pass.Text = System.Configuration.ConfigurationSettings.AppSettings["DB_PASS"];
        }

        private void btn_dbConfirm_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (dbinfo.cbx_modify.CheckState == CheckState.Checked)
                {
                    xmldoc.Load(AppConfigName);
                    XmlNode node = xmldoc.SelectSingleNode("//appSettings");
                    if (node.HasChildNodes)
                    {
                        XmlNodeList nodelist = node.ChildNodes;
                        foreach (XmlNode itemNode in nodelist)
                        {
                            if (itemNode.Attributes["key"].Value.Equals("DB_HOST"))
                            {
                                itemNode.Attributes["value"].Value = dbinfo.tbx_host.Text;
                            }

                            if (itemNode.Attributes["key"].Value.Equals("DB_NAME"))
                            {
                                itemNode.Attributes["value"].Value = dbinfo.tbx_dbname.Text;
                            }

                            if (itemNode.Attributes["key"].Value.Equals("DB_USER"))
                            {
                                itemNode.Attributes["value"].Value = dbinfo.tbx_id.Text;
                            }

                            if (itemNode.Attributes["key"].Value.Equals("DB_PASS"))
                            {
                                itemNode.Attributes["value"].Value = dbinfo.tbx_pass.Text;
                            }
                        }
                    }
                    xmldoc.Save(AppConfigName);
                    System.Configuration.ConfigurationSettings.AppSettings.Set("DB_HOST", dbinfo.tbx_host.Text);
                    System.Configuration.ConfigurationSettings.AppSettings.Set("DB_NAME", dbinfo.tbx_dbname.Text);
                    System.Configuration.ConfigurationSettings.AppSettings.Set("DB_USER", dbinfo.tbx_id.Text);
                    System.Configuration.ConfigurationSettings.AppSettings.Set("DB_PASS", dbinfo.tbx_pass.Text);

                    WDdbHost = System.Configuration.ConfigurationSettings.AppSettings["DB_HOST"];
                    WDdbName = System.Configuration.ConfigurationSettings.AppSettings["DB_NAME"];
                    WDdbUser = System.Configuration.ConfigurationSettings.AppSettings["DB_USER"];
                    WDdbPass = System.Configuration.ConfigurationSettings.AppSettings["DB_PASS"];
                }
                dbinfo.Close();
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void setSVR_typeXml(string filename, string svr_type, string device, string auto_start)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            XmlNode pnode = doc.SelectSingleNode("//appSettings");
            if (pnode.HasChildNodes)
            {
                XmlNodeList nodelist = pnode.ChildNodes;
                foreach (XmlNode node in nodelist)
                {
                    if (node.Attributes["key"].Value.Equals("SVR_TYPE"))
                    {
                        node.Attributes["value"].Value = svr_type;
                    }
                    else if (node.Attributes["key"].Value.Equals("DEVICE"))
                    {
                        node.Attributes["value"].Value = device;
                    }
                    else if (node.Attributes["key"].Value.Equals("AUTO_START"))
                    {
                        node.Attributes["value"].Value = auto_start;
                    }
                }

                doc.Save(filename);

                System.Configuration.ConfigurationSettings.AppSettings.Set("SVR_TYPE", svr_type);
                System.Configuration.ConfigurationSettings.AppSettings.Set("DEVICE", device);
                System.Configuration.ConfigurationSettings.AppSettings.Set("AUTO_START", auto_start);
            }
        }



        #region sip 캡쳐 테스트 부분

        public string Connect(string Device_Name)
        {
            int failCount = 0;
            string result = "";

            deviceList = CaptureDeviceList.Instance;
            foreach (ICaptureDevice item in deviceList)
            {
                if (item.Description.Equals(Device_Name))
                {
                    dev = item;
                    break;
                }
            }

            try
            {
                if (dev != null)
                {
                    dev.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
                    dev.Open(DeviceMode.Promiscuous, 500);
                    dev.Filter = "udp src port 5060";

                    try
                    {
                        dev.StartCapture();
                        //log("Packet capture Start!!");
                    }
                    catch (Exception ex1)
                    {
                        //log("capture fail");
                        failCount++;

                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                //Logwriter(ex.ToString());
            }
            if (failCount == 0)
            {
                result = "Success";
            }
            else
            {
                result = "Fail";
            }
            return result;
        }

        /// <summary>
        /// 설정된 NIC 디바이스의 패킷 수신 이벤트 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            //log("Packet 수신!");
            Packet p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            UdpPacket udpPacket = UdpPacket.GetEncapsulated(p);
            string data = Encoding.ASCII.GetString(udpPacket.PayloadData);
            SIPM = makeSIPConstructor(data);

            //log(data);
            if (!SIPM.from.Equals(SIPM.to) && !SIPM.from.Equals("unknown") && !SIPM.to.Equals("unknown"))
            {

                if (SIPM.method.Equals("INVITE"))
                {
                    if (SIPM.sName.Equals("session")) //Ringing
                    {
                        logWrite("Ringing : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                    else if (SIPM.sName.Equals("SIP Call")) //Dial
                    {
                        logWrite("Dialing : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                }
                else if (SIPM.code.Equals("200")) //Answer
                {
                    if (SIPM.sName.Equals("SIP Call"))
                    {
                        logWrite("Answer : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                    else if (SIPM.sName.Equals("session")) //발신 후 연결 
                    {
                        logWrite("CallConnect : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                    }
                }
                else if (SIPM.method.Equals("CANCEL")) //Abandon
                {
                    logWrite("Abandon : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                }
                else if (SIPM.method.Equals("BYE")) //Abandon
                {
                    logWrite("HangUp : " + SIPM.from + "|" + SIPM.to + "|" + SIPM.callid);

                }
            }
        }

        #endregion

        private void StripMenu_svrconfig_Click(object sender, EventArgs e)
        {
            setDevice();
        }

        private void 콜테스트ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeCallTestForm();
        }

        private void 보이기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            this.TopMost = true;
            this.Show();
        }

        private void notify_svr_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void MsgSvrForm_MinimumSizeChanged(object sender, EventArgs e)
        {
            NoParamDele dele = new NoParamDele(formHide);
            Invoke(dele);
        }

        private void formHide()
        {
            this.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        private void 서버종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopApplication();
        }

        private void MnServerStop_Click(object sender, EventArgs e)
        {
            stopApplication();
        }

        private void MnServerStart_Click(object sender, EventArgs e)
        {
            startServer();
        }

       

    }
}