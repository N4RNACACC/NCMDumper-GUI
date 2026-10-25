using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NCMDumperGUI.Tools;

public partial class NCMDumper
{
    private const string DLL_PATH = "NativeLibs/libncmdump.dll";
    
    // 代码来源：https://github.com/taurusxin/ncmdump/blob/main/example/csharp/NeteaseCrypt.cs
    // 此部分代码引用声明详见THIRD_PARTY_LICENSES.txt
    
    // DLL Import
    #region DLL Import
    [LibraryImport(DLL_PATH)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial IntPtr CreateNeteaseCrypt(IntPtr path);
    [LibraryImport(DLL_PATH)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int Dump(IntPtr NeteaseCrypt, IntPtr outputPath);
    [LibraryImport(DLL_PATH)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void FixMetadata(IntPtr NeteaseCrypt);
    [LibraryImport(DLL_PATH)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void DestroyNeteaseCrypt(IntPtr NeteaseCrypt);
    #endregion
    
    
    private IntPtr NeteaseCryptClass = IntPtr.Zero;
    /// <summary>
    /// 创建 NeteaseCrypt 类的实例。
    /// </summary>
    /// <param name="FileName">网易云音乐 ncm 加密文件路径</param>
    public void NeteaseCrypt(string fileName)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(fileName);
        IntPtr inputPtr = Marshal.AllocHGlobal(bytes.Length + 1);
        try
        {
            Marshal.Copy(bytes, 0, inputPtr, bytes.Length);
            Marshal.WriteByte(inputPtr, bytes.Length, 0);
            NeteaseCryptClass = CreateNeteaseCrypt(inputPtr);
            if (NeteaseCryptClass == IntPtr.Zero)
                throw new InvalidOperationException("CreateNeteaseCrypt returned null.");
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
        }
    }
    /// <summary>
    /// 启动转换过程。
    /// </summary>
    /// <param name="outputPath">指定一个路径输出，如果为空，则输出到原路径</param>
    /// <returns>返回一个整数，指示转储过程的结果。如果成功，返回0；如果失败，返回1。</returns>
    public int Dump(string outputPath)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(outputPath);
        IntPtr outputPtr = Marshal.AllocHGlobal(bytes.Length + 1);
        try
        {
            Marshal.Copy(bytes, 0, outputPtr, bytes.Length);
            Marshal.WriteByte(outputPtr, bytes.Length, 0);
            return Dump(NeteaseCryptClass, outputPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(outputPtr);
        }
    }
    /// <summary>
    /// 修复音乐文件元数据。
    /// </summary>
    public void FixMetadata()
    {
        FixMetadata(NeteaseCryptClass);
    }
    /// <summary>
    /// 销毁 NeteaseCrypt 类的实例。
    /// </summary>
    public void Destroy()
    {
        DestroyNeteaseCrypt(NeteaseCryptClass);
    }
}