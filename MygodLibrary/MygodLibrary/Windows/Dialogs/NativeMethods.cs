using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

namespace Mygod.Windows.Dialogs
{
	internal class NativeMethods
	{
		internal const int GwlStyle = -16;
		internal const int GwlExstyle = -20;
		internal const int SwpNosize = 0x0001;
		internal const int SwpNomove = 0x0002;
		internal const int SwpNozorder = 0x0004;
		internal const int SwpFramechanged = 0x0020;
		internal const uint WmSeticon = 0x0080;
		internal const int WsSysmenu = 0x00080000;
		internal const int WsExDlgmodalframe = 0x0001;

		[DllImport("user32.dll")]
		internal extern static int SetWindowLong(IntPtr hwnd, int index, int value);
		[DllImport("user32.dll")]
		internal extern static int GetWindowLong(IntPtr hwnd, int index);
		[DllImport("user32.dll")]
		internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);
		[DllImport("user32.dll")]
		internal static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll")]
        internal static extern IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        #region LoadLibrary

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeModuleHandle LoadLibraryEx(
            string lpFileName,
            IntPtr hFile,
            LoadLibraryExFlags dwFlags
            );

        [DllImport("kernel32", SetLastError = true),
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [Flags]
        public enum LoadLibraryExFlags : uint
        {
            DontResolveDllReferences = 0x00000001,
            LoadLibraryAsDatafile = 0x00000002,
            LoadWithAlteredSearchPath = 0x00000008,
            LoadIgnoreCodeAuthzLevel = 0x00000010
        }

        #endregion

        #region Credentials

        internal const int CREDUI_MAX_USERNAME_LENGTH = 256 + 1 + 256;
        internal const int CREDUI_MAX_PASSWORD_LENGTH = 256;

        [Flags]
        public enum CREDUI_FLAGS
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000
        }

        [Flags]
        public enum CredUIWinFlags
        {
            Generic = 0x1,
            Checkbox = 0x2,
            AutoPackageOnly = 0x10,
            InCredOnly = 0x20,
            EnumerateAdmins = 0x100,
            EnumerateCurrentUser = 0x200,
            SecurePrompt = 0x1000,
            Pack32Wow = 0x10000000
        }

        internal enum CredUIReturnCodes
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004
        }

        internal enum CredTypes
        {
            CRED_TYPE_GENERIC = 1,
            CRED_TYPE_DOMAIN_PASSWORD = 2,
            CRED_TYPE_DOMAIN_CERTIFICATE = 3,
            CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4
        }

        internal enum CredPersist
        {
            Session = 1,
            LocalMachine = 2,
            Enterprise = 3
        }

        [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
        internal struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hbmBanner;
            public IntPtr hwndParent;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCaptionText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMessageText;
        }

        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        public static extern CredUIReturnCodes CredUIPromptForWindowsCredentials(
            ref CREDUI_INFO pUiInfo,
            uint dwAuthError,
            ref uint pulAuthPackage,
            IntPtr pvInAuthBuffer,
            uint ulInAuthBufferSize,
            out IntPtr ppvOutAuthBuffer,
            out uint pulOutAuthBufferSize,
            [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
            CredUIWinFlags dwFlags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CredReadW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredRead(string TargetName, CredTypes Type, int Flags, out IntPtr Credential);

        [DllImport("advapi32.dll"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void CredFree(IntPtr Buffer);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CredDeleteW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredDelete(string TargetName, CredTypes Type, int Flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CredWriteW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredWrite(ref CREDENTIAL Credential, int Flags);

        [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CredPackAuthenticationBuffer(uint dwFlags, string pszUserName, string pszPassword,
                                                               IntPtr pPackedCredentials, ref uint pcbPackedCredentials);

        [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CredUnPackAuthenticationBuffer(uint dwFlags, IntPtr pAuthBuffer, uint cbAuthBuffer,
                                                                 StringBuilder pszUserName, ref uint pcchMaxUserName,
                                                                 StringBuilder pszDomainName, ref uint pcchMaxDomainName,
                                                                 StringBuilder pszPassword, ref uint pcchMaxPassword);

        // Disable the "Internal field is never assigned to" warning.
#pragma warning disable 649
        // This type does not own the IntPtr native resource; when CredRead is used, CredFree must be called on the
        // IntPtr that the struct was marshalled from to release all resources including the CredentialBlob IntPtr,
        // When allocating the struct manually for CredWrite you should also manually deallocate the CredentialBlob.
        [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
        public struct CREDENTIAL
        {
            public int AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            // Since the resource pointed to must be either released manually or by CredFree, SafeHandle is not appropriate here
            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            public IntPtr CredentialBlob;
            public uint CredentialBlobSize;
            public int Flags;
            public long LastWritten;
            [MarshalAs(UnmanagedType.U4)]
            public CredPersist Persist;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;
            public CredTypes Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserName;
        }
#pragma warning restore 649

        #endregion
	}
}
