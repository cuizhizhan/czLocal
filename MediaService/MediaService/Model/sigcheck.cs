using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class dllpath
    {
        public const string DllPath = @"C:\MediaService\sigcheck.dll";     // 32 位
    }

    public class sigcheck
    {
        [DllImport(dllpath.DllPath, EntryPoint = "tls_gen_sig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int tls_gen_sig(
                UInt32 expire,
                string appid3rd,
                UInt32 sdkappid,
                string identifier,
                UInt32 acctype,
                StringBuilder sig,
                UInt32 sig_buff_len,
                string pri_key,
                UInt32 pri_key_len,
                StringBuilder err_msg,
                UInt32 err_msg_buff_len
        );

        [DllImport(dllpath.DllPath, EntryPoint = "tls_vri_sig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int tls_vri_sig(
            string sig,
            string pub_key,
            UInt32 pub_key_len,
            UInt32 acctype,
            string appid3rd,
            UInt32 sdkappid,
            string identifier,
            StringBuilder err_msg,
            UInt32 err_msg_buff_len
        );

        [DllImport(dllpath.DllPath, EntryPoint = "tls_gen_sig_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int tls_gen_sig_ex(
            UInt32 sdkappid,
            string identifier,
            StringBuilder sig,
            UInt32 sig_buff_len,
            string pri_key,
            UInt32 pri_key_len,
            StringBuilder err_msg,
            UInt32 err_msg_buff_len
        );

        [DllImport(dllpath.DllPath, EntryPoint = "tls_vri_sig_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public extern static int tls_vri_sig_ex(
            string sig,
            string pub_key,
            UInt32 pub_key_len,
            UInt32 sdkappid,
            string identifier,
            ref UInt32 expire_time,
            ref UInt32 init_time,
            StringBuilder err_msg,
            UInt32 err_msg_buff_len
        );
    }
}
