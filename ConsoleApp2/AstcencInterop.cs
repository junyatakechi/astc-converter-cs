using System;
using System.Runtime.InteropServices;

enum AstcencError
{
    Success = 0,
    OutOfMem,
    BadCpuFloat,
    BadParam,
    BadBlockSize,
    BadProfile,
    BadQuality,
    BadSwizzle,
    BadFlags,
    BadContext,
    NotImplemented,
    BadDecodeMode
}

enum AstcencProfile
{
    LdrSrgb = 0,
    Ldr,
    HdrRgbLdrA,
    Hdr
}

enum AstcencSwz
{
    R = 0,
    G,
    B,
    A,
    Zero = 4,
    One,
    Z
}

[StructLayout(LayoutKind.Sequential)]
struct AstcencSwizzle
{
    public uint R;
    public uint G;
    public uint B;
    public uint A;
}

enum AstcencType
{
    U8 = 0,
    F16,
    F32,
}

[StructLayout(LayoutKind.Sequential)]
struct AstcencImage
{
    public uint DimX;
    public uint DimY;
    public uint DimZ;
    public uint DataType;
    public IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
struct AstcencConfig
{
    public uint Profile;
    public uint Flags;
    public uint BlockX;
    public uint BlockY;
    public uint BlockZ;
    public float CwRWeight;
    public float CwGWeight;
    public float CwBWeight;
    public float CwAWeight;
    public uint AScaleRadius;
    public float RgbmMScale;
    public uint TunePartitionCountLimit;
    public uint Tune2PartitionIndexLimit;
    public uint Tune3PartitionIndexLimit;
    public uint Tune4PartitionIndexLimit;
    public uint TuneBlockModeLimit;
    public uint TuneRefinementLimit;
    public uint TuneCandidateLimit;
    public uint Tune2PartitioningCandidateLimit;
    public uint Tune3PartitioningCandidateLimit;
    public uint Tune4PartitioningCandidateLimit;
    public float TuneDbLimit;
    public float TuneMseOvershoot;
    public float Tune2PartitionEarlyOutLimitFactor;
    public float Tune3PartitionEarlyOutLimitFactor;
    public float Tune2PlaneEarlyOutLimitCorrelation;
    public float TuneSearchMode0Enable;
    public IntPtr ProgressCallback;
}

class AstcencInterop
{
    [DllImport("astcenc-sse2-shared.dll", EntryPoint = "astcenc_config_init", CallingConvention = CallingConvention.Cdecl)]
    public static extern AstcencError ConfigInit(
        AstcencProfile profile,
        uint blockX,
        uint blockY,
        uint blockZ,
        float quality,
        uint flags,
        out AstcencConfig config);

    [DllImport("astcenc-sse2-shared.dll", EntryPoint = "astcenc_context_alloc", CallingConvention = CallingConvention.Cdecl)]
    public static extern AstcencError ContextAlloc(
        ref AstcencConfig config,
        uint threadCount,
        out IntPtr context);

    [DllImport("astcenc-sse2-shared.dll", EntryPoint = "astcenc_compress_image", CallingConvention = CallingConvention.Cdecl)]
    public static extern AstcencError CompressImage(
        IntPtr context,
        ref AstcencImage image,
        ref AstcencSwizzle swizzle,
        IntPtr dataOut,
        UIntPtr dataLen,
        uint threadIndex
     );

    [DllImport("astcenc-sse2-shared.dll", EntryPoint = "astcenc_context_free", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextFree(IntPtr context);
}
