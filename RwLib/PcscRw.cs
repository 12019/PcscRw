using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwLib
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;


    namespace Rwlib
    {

        public class PcscRw
        {



            #region ApiImport

            const int SCARD_SCOPE_USER = 0;
            const int SCARD_SCOPE_TERMINAL = 1;
            const int SCARD_SCOPE_SYSTEM = 2;

            const string SCARD_ALL_READERS = "SCard$AllReaders\000";
            const string SCARD_DEFAULT_READERS = "SCard$DefaultReaders\000";
            const string CARD_LOCAL_READERS = "SCard$LocalReaders\000";
            const string SCARD_SYSTEM_READERS = "SCard$SystemReaders\000";

            const int SCARD_SHARE_SHARED = 0x00000002; // - This application will allow others to share the reader
            const int SCARD_SHARE_EXCLUSIVE = 0x00000001; // - This application will NOT allow others to share the reader
            const int SCARD_SHARE_DIRECT = 0x00000003; // - Direct control of the reader, even without a card

            const int SCARD_PROTOCOL_T0 = 0x00000001;
            const int SCARD_PROTOCOL_T1 = 0x00000002;
            const int SCARD_PROTOCOL_RAW = 0x00000004;

            const int SCARD_LEAVE_CARD = 0;
            const int SCARD_RESET_CARD = 1;
            const int SCARD_UNPOWER_CARD = 2;
            const int SCARD_EJECT_CARD = 3;

            [DllImport("winscard.dll")]
            static extern uint SCardEstablishContext(int dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out int phContext);

            [DllImport("winscard.dll", EntryPoint = "SCardListReadersA", CharSet = CharSet.Ansi)]
            public static extern uint SCardListReaders(int hContext, string mszGroups, byte[] mszReaders, ref int pcchReaders);

            [DllImport("winscard.dll", EntryPoint = "SCardConnect", CharSet = CharSet.Auto)]
            static extern uint SCardConnect(
                 int hContext,
                 [MarshalAs(UnmanagedType.LPTStr)] string szReader, //I was getting SCARD_E_UNKNOWN_READER until i removed [MarshalAs(UnmanagedType.LPTStr)]
                 UInt32 dwShareMode,
                 UInt32 dwPreferredProtocols,
                 out int phCard,
                 out UInt32 pdwActiveProtocol);

            [DllImport("winscard.dll")]
            static extern uint SCardStatus(
                int hCard,
                byte[] szReaderName,
                ref int pcchReaderLen,
                ref int pdwState,
                ref int pdwProtocol,
                byte[] pbAtr,
                ref int pcbAtrLen);

            [DllImport("winscard.dll")]
            static extern uint SCardTransmit(int hCard, SCARD_IO_REQUEST pioSendPci, byte[] pbSendBuffer, int cbSendLength, SCARD_IO_REQUEST pioRecvPci,
                    byte[] pbRecvBuffer, ref int pcbRecvLength);


            [DllImport("winscard.dll")]
            static extern uint SCardDisconnect(int hCard, int dwDisposition);

            [DllImport("winscard.dll")]
            static extern uint SCardFreeMemory(int hContext, IntPtr pvMem);

            [DllImport("winscard.dll")]
            static extern uint SCardReleaseContext(int hContext);

            [StructLayout(LayoutKind.Sequential)]
            internal class SCARD_IO_REQUEST
            {
                internal uint dwProtocol;
                internal uint cbPciLength;
                public SCARD_IO_REQUEST()
                {
                    dwProtocol = 2;
                }
            }



            [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
            static extern int LoadLibrary(
                [MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

            [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
            static extern IntPtr GetProcAddress(int hModule,
                [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

            [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
            static extern bool FreeLibrary(int hModule);


            #endregion


            //定数：エラーコード

            /// <summary>
            /// 成功
            /// </summary>
            public const uint SUCCESS = 0;

            /// <summary>
            /// カードが取り出されている
            /// </summary>
            public const uint E_REMOVED_CARD = 0x80100069;

            /// <summary>
            /// カードが反応しない
            /// </summary>
            public const uint E_UNRESPONSIVE_CARD = 0x80100066;

            /// <summary>
            /// カードの応答がタイムアウトした
            /// </summary>
            public const uint E_TIMEOUT = 0x8010000A;

            /// <summary>
            /// カードまたはﾘｰﾀﾞﾗｲﾀの準備ができていない
            /// </summary>
            public const uint E_NOT_READY = 0x80100010;


            private int hContext = 0;
            private System.Collections.ArrayList readerList = new System.Collections.ArrayList();

            private byte[] atr;

            int hCard = 0;
            int activeProtocol;

            SCARD_IO_REQUEST pciT0;
            SCARD_IO_REQUEST pciT1;

            public PcscRw()
            {
                //initializetion for member variables
                readerList = new System.Collections.ArrayList();

                this.pciT0 = GetPci(SCARD_PROTOCOL_T0);
                this.pciT1 = GetPci(SCARD_PROTOCOL_T1);


                //enumurate readers
                uint res = 0;
                res = SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out hContext);

                System.Text.StringBuilder readers = new StringBuilder(1024);

                byte[] reader = new byte[1024];

                int lenReaderName = reader.Length;

                res = SCardListReaders(hContext, SCARD_ALL_READERS, reader, ref lenReaderName);

                int lastIndex = 0;

                for (int i = 0; i < lenReaderName - 1; i++)
                {
                    if (reader[i] == 0)
                    {
                        int textCount = i - lastIndex;
                        readerList.Add(Encoding.ASCII.GetString(reader, lastIndex, textCount));
                        lastIndex = i + 1;
                    }
                }
            }



            public string[] GetRwNames()
            {
                return (string[])(this.readerList.ToArray(typeof(string)));
            }

            private SCARD_IO_REQUEST GetPci(int T)
            {
                int handle = LoadLibrary("Winscard.dll");

                IntPtr pci = IntPtr.Zero;

                SCARD_IO_REQUEST ret = new SCARD_IO_REQUEST();

                switch (T)
                {
                    case SCARD_PROTOCOL_T0:
                        pci = GetProcAddress(handle, "g_rgSCardT0Pci");
                        Marshal.PtrToStructure(pci, ret);
                        break;
                    case SCARD_PROTOCOL_T1:
                        pci = GetProcAddress(handle, "g_rgSCardT1Pci");
                        Marshal.PtrToStructure(pci, ret);
                        break;

                    default:
                        ret = null;
                        break;
                }

                FreeLibrary(handle);

                return ret;
            }



            public byte[] getAtr()
            {
                return (byte[])this.atr.Clone();
            }

            public byte[] Connect(string readerName)
            {
                // connect to the Card !!!"

                uint res;

                uint activveProtocol;
                res = SCardConnect(hContext, readerName,
                    SCARD_SHARE_SHARED, SCARD_PROTOCOL_T1 | SCARD_PROTOCOL_T0, out hCard, out activveProtocol);
                if (res != SUCCESS)
                {
                    throw new Exception("Failed to connect");
                }

                byte[] atr = new byte[64];
                int lenAtr = atr.Length;

                int dwState = 0;

                byte[] reader = new byte[1024];
                int lenReaderName = reader.Length;

                res = SCardStatus(hCard, reader, ref lenReaderName, ref dwState, ref activeProtocol, atr, ref lenAtr);
                if(res != SUCCESS ){
                    throw new Exception("Failed to connect");
                }



                this.atr = new byte[lenAtr];
                Array.Copy(atr, this.atr, lenAtr);

                return (byte[])this.atr.Clone();
            }

            
            public byte[] Transmit(byte[] cmd, int lenCmd)
            {
                byte[] tmpRsp = new byte[1024];
                int lenTmpRsp = tmpRsp.Length;

                SCARD_IO_REQUEST pci = null;
                if (this.activeProtocol == SCARD_PROTOCOL_T0)
                {
                    pci = pciT0;
                }
                else if (this.activeProtocol == SCARD_PROTOCOL_T1)
                {
                    pci = pciT1;
                }
                else
                {
                    throw new Exception("unsupported protocol");
                }

                uint res = SCardTransmit(hCard, pci, cmd, lenCmd, null, tmpRsp, ref lenTmpRsp);
                if (res != SUCCESS) {
                    throw new Exception("failed to transmit");
                }

                byte[] rsp = new byte[lenTmpRsp];
                Array.Copy(tmpRsp, rsp, lenTmpRsp);

                return rsp;
            }
            

            public void Disconnect()
            {
                SCardDisconnect(hCard, SCARD_UNPOWER_CARD);
                hCard = 0;

                return ;
            }


            ~PcscRw()
            {
                if (hCard != 0) {
                    this.Disconnect();
                }
                SCardReleaseContext(hContext);
            }
            

        }


    }
}
