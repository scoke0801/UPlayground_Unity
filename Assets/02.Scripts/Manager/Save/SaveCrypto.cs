using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 세이브 파일 암호화/복호화 유틸리티 (AES-256-CBC).
    ///
    /// 싱글플레이 게임의 세이브 변조 방지(난독화) 목적이다.
    /// 키는 빌드에 임베드되며, 파일마다 무작위 IV를 생성해 헤더에 붙인다.
    /// 키 관리 인프라를 과설계하지 않는다 — 로컬 세이브 보호 수준이면 충분하다.
    ///
    /// 파일 포맷: [16바이트 IV][AES-CBC 암호문]
    /// </summary>
    public static class SaveCrypto
    {
        // 빌드에 임베드되는 32바이트(256bit) 키. 변경하면 기존 세이브를 읽지 못한다.
        private static readonly byte[] Key =
        {
            0x55, 0x50, 0x6C, 0x61, 0x79, 0x47, 0x72, 0x6F,
            0x75, 0x6E, 0x64, 0x53, 0x61, 0x76, 0x65, 0x4B,
            0x65, 0x79, 0x32, 0x30, 0x32, 0x36, 0x41, 0x45,
            0x53, 0x32, 0x35, 0x36, 0x43, 0x42, 0x43, 0x21,
        };

        private const int IvSize = 16;

        /// <summary>평문 문자열(UTF-8)을 암호화한다. 결과는 [IV][암호문] 바이트 배열.</summary>
        public static byte[] Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText ?? string.Empty);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] result = new byte[IvSize + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, IvSize);
            Buffer.BlockCopy(cipher, 0, result, IvSize, cipher.Length);
            return result;
        }

        /// <summary>[IV][암호문] 바이트 배열을 복호화해 UTF-8 문자열로 반환한다.</summary>
        public static string Decrypt(byte[] data)
        {
            if (data == null || data.Length <= IvSize)
                throw new ArgumentException("암호화 데이터가 너무 짧습니다(IV 누락).", nameof(data));

            byte[] iv = new byte[IvSize];
            Buffer.BlockCopy(data, 0, iv, 0, IvSize);

            int cipherLength = data.Length - IvSize;

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(data, IvSize, cipherLength);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
