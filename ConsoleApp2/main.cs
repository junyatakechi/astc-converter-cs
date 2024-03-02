using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    // Bitmapオブジェクトのピクセルデータを上下反転させる関数
    static Bitmap FlipBitmapVertically(Bitmap original)
    {
        Bitmap flipped = new Bitmap(original.Width, original.Height, original.PixelFormat);

        BitmapData originalData = original.LockBits(new Rectangle(0, 0, original.Width, original.Height), ImageLockMode.ReadOnly, original.PixelFormat);
        BitmapData flippedData = flipped.LockBits(new Rectangle(0, 0, flipped.Width, flipped.Height), ImageLockMode.WriteOnly, flipped.PixelFormat);

        int bytesPerPixel = Image.GetPixelFormatSize(original.PixelFormat) / 8;
        int heightInPixels = originalData.Height;
        int widthInBytes = originalData.Width * bytesPerPixel;
        byte[] pixelBuffer = new byte[widthInBytes];
        IntPtr currentLine;

        for (int y = 0; y < heightInPixels; y++)
        {
            currentLine = originalData.Scan0 + (y * originalData.Stride);
            Marshal.Copy(currentLine, pixelBuffer, 0, widthInBytes);
            currentLine = flippedData.Scan0 + ((heightInPixels - y - 1) * flippedData.Stride);
            Marshal.Copy(pixelBuffer, 0, currentLine, widthInBytes);
        }

        original.UnlockBits(originalData);
        flipped.UnlockBits(flippedData);

        return flipped;
    }

    static void Main(string[] args)
    {
        string imagePath = "texture_00.png"; // 実際のファイル名に置き換えてください

        using (Bitmap bmp = new Bitmap(imagePath))
        {
            using (Bitmap flippedBmp = FlipBitmapVertically(bmp)){

                Console.WriteLine($"{bmp.Width}, {bmp.Height}"); // => 2048, 2048
                
                Rectangle rect = new Rectangle(0, 0, flippedBmp.Width, flippedBmp.Height);
                BitmapData bmpData = flippedBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                

                try
                {

                    int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
                    byte[] argbValues = new byte[bytes];
                    Marshal.Copy(bmpData.Scan0, argbValues, 0, bytes);

                    // ARGBからRGBAに変換
                    byte[] rgbaValues = new byte[bytes];
                    for (int i = 0; i < bytes; i += 4)
                    {
                        rgbaValues[i] = argbValues[i + 2]; // R: ARGBのGをRGBAのRへ
                        rgbaValues[i + 1] = argbValues[i + 1]; // G: ARGBのGをそのまま使用
                        rgbaValues[i + 2] = argbValues[i]; // B: ARGBのBをRGBAのBへ
                        rgbaValues[i + 3] = argbValues[i + 3]; // A: ARGBのAをそのまま使用
                    }

                    IntPtr imageDataPtr = Marshal.AllocHGlobal(bytes);
                    Marshal.Copy(rgbaValues, 0, imageDataPtr, bytes);

                    IntPtr[] imageDataArray = { imageDataPtr };

                    AstcencImage image = new AstcencImage
                    {
                        DimX = (uint)bmp.Width,
                        DimY = (uint)bmp.Height,
                        DimZ = 1,
                        DataType = (uint)AstcencType.U8,
                        Data = Marshal.UnsafeAddrOfPinnedArrayElement(imageDataArray, 0)
                    };

                    AstcencSwizzle swizzle = new AstcencSwizzle
                    {
                        R = (uint)AstcencSwz.R,
                        G = (uint)AstcencSwz.G,
                        B = (uint)AstcencSwz.B,
                        A = (uint)AstcencSwz.A
                    };

                    AstcencConfig config;
                    AstcencInterop.ConfigInit(AstcencProfile.LdrSrgb, 4, 4, 1, 1.0f, 0, out config);
                    IntPtr context;
                    AstcencInterop.ContextAlloc(ref config, 1, out context);

                    int outputBufferSize = CalculateAstcCompressedSize(bmp.Width, bmp.Height, 4, 4);
                    IntPtr outputBufferPtr = Marshal.AllocHGlobal(outputBufferSize);

                    try
                    {
                        AstcencError result = AstcencInterop.CompressImage(context, ref image, ref swizzle, outputBufferPtr, (UIntPtr)outputBufferSize, 0);
                        if (result == AstcencError.Success)
                        {
                            Console.WriteLine("圧縮成功");

                            // ASTCヘッダー情報を設定
                            byte[] astcHeader = new byte[16] {
                                0x13, 0xAB, 0xA1, 0x5C, // ASTCマジックナンバー (修正: リトルエンディアン表記)
                                4, 4, 1, // ブロック寸法: 幅、高さ、深さ
                                (byte)(bmp.Width & 0xFF), (byte)((bmp.Width >> 8) & 0xFF), (byte)((bmp.Width >> 16) & 0xFF), // 画像の幅 (リトルエンディアン)
                                (byte)(bmp.Height & 0xFF), (byte)((bmp.Height >> 8) & 0xFF), (byte)((bmp.Height >> 16) & 0xFF), // 画像の高さ (リトルエンディアン)
                                1, 0, 0 // 画像の深さ (2D画像の場合は1)
                            };

                            // 圧縮された画像データを読み込む
                            byte[] outputBuffer = new byte[outputBufferSize];
                            Marshal.Copy(outputBufferPtr, outputBuffer, 0, outputBufferSize);

                            // ヘッダーと圧縮データを結合
                            byte[] astcFileData = new byte[astcHeader.Length + outputBuffer.Length];
                            Buffer.BlockCopy(astcHeader, 0, astcFileData, 0, astcHeader.Length);
                            Buffer.BlockCopy(outputBuffer, 0, astcFileData, astcHeader.Length, outputBuffer.Length);

                            // 結合したデータをファイルに書き出す
                            string outputPath = "compressed_texture1.astc"; // 出力ファイル名
                            File.WriteAllBytes(outputPath, astcFileData);
                            Console.WriteLine($"{outputPath}に圧縮された画像を保存しました。");
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(outputBufferPtr);
                        AstcencInterop.ContextFree(context);
                    }
                }
                finally
                {
                    flippedBmp.UnlockBits(bmpData);
                }                
            }
        }
    }

    static int CalculateAstcCompressedSize(int width, int height, int blockWidth, int blockHeight)
    {
        // この関数は、ASTC圧縮後の画像のサイズを計算するためのものです。
        // 実際の実装は、ASTCの圧縮パラメータに依存します。
        int blocksWide = (width + blockWidth - 1) / blockWidth;
        int blocksHigh = (height + blockHeight - 1) / blockHeight;
        return blocksWide * blocksHigh * 16; // ASTCはブロックあたり16バイト
    }
}
