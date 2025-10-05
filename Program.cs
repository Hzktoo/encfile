using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace encfile
{
    public class FileEncryptor
    {
        private const int KeySize = 256;
        private const int SaltSize = 32;
        private const int IvSize = 16;
        private const int Iterations = 100000;

        public static void Encrypt(string inputFile, string password)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("File not found", inputFile);

            string outputFile = inputFile + ".enc";

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(iv);
            }

            byte[] key = DeriveKey(password, salt);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    fsOutput.Write(salt, 0, salt.Length);
                    fsOutput.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        fsInput.CopyTo(cs);
                    }
                }

                Array.Clear(key, 0, key.Length);
            }

            Console.WriteLine($"✓ Encrypted: {Path.GetFileName(outputFile)}");
            Console.WriteLine($"  Original size: {FormatSize(new FileInfo(inputFile).Length)}");
            Console.WriteLine($"  Encrypted size: {FormatSize(new FileInfo(outputFile).Length)}");
        }

        public static void Decrypt(string inputFile, string password)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("File not found", inputFile);

            if (!inputFile.EndsWith(".enc"))
                throw new ArgumentException("File must have .enc extension");

            string outputFile = inputFile.Substring(0, inputFile.Length - 4);

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];

            using (var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                if (fsInput.Read(salt, 0, salt.Length) != salt.Length)
                    throw new Exception("Invalid encrypted file format");
                if (fsInput.Read(iv, 0, iv.Length) != iv.Length)
                    throw new Exception("Invalid encrypted file format");

                byte[] key = DeriveKey(password, salt);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = key;
                    aes.IV = iv;

                    try
                    {
                        using (var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                        using (var cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            cs.CopyTo(fsOutput);
                        }

                        Array.Clear(key, 0, key.Length);

                        Console.WriteLine($"✓ Decrypted: {Path.GetFileName(outputFile)}");
                        Console.WriteLine($"  Size: {FormatSize(new FileInfo(outputFile).Length)}");
                    }
                    catch (CryptographicException)
                    {
                        Array.Clear(key, 0, key.Length);
                        if (File.Exists(outputFile))
                            File.Delete(outputFile);
                        throw new Exception("Decryption failed. Wrong password or corrupted file");
                    }
                }
            }
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(KeySize / 8);
            }
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int order = 0;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:F2} {sizes[order]}";
        }

        private static string ReadPassword(string prompt)
        {
            Console.Write(prompt);
            var password = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }

            return password.ToString();
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Encrypt: encfile <filename>");
                    Console.WriteLine("  Decrypt: encfile -d <filename.enc>");
                    return;
                }

                bool decrypt = args[0] == "-d" || args[0] == "--decrypt";
                string? filePath = decrypt ? (args.Length > 1 ? args[1] : null) : args[0];

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("✗ Error: No file specified");
                    return;
                }

                string password = ReadPassword("Password: ");

                if (string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("✗ Error: Password cannot be empty");
                    return;
                }

                if (decrypt)
                {
                    Decrypt(filePath, password);
                }
                else
                {
                    string confirmPassword = ReadPassword("Confirm password: ");

                    if (password != confirmPassword)
                    {
                        Console.WriteLine("✗ Error: Passwords do not match");
                        return;
                    }

                    Encrypt(filePath, password);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}