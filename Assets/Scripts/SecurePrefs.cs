using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;

public static class SecurePrefs
{
    // This is your secret encryption key. Hackers won't know this!
    // It MUST be exactly 32 characters long.
    private static readonly string SECRET_KEY = "30DaysUndergroundSecretKey200422";

    public static void SetInt(string keyName, int value)
    {
        SetString(keyName, value.ToString());
    }

    public static int GetInt(string keyName, int defaultValue = 0)
    {
        string val = GetString(keyName, "");
        if (int.TryParse(val, out int result)) return result;
        return defaultValue;
    }

    public static void SetString(string keyName, string value)
    {
        try
        {
            byte[] iv = new byte[16];
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(SECRET_KEY);
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] inputBytes = Encoding.UTF8.GetBytes(value);
                byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

                // Save the scrambled data!
                PlayerPrefs.SetString(keyName, Convert.ToBase64String(encrypted));
            }
        }
        catch { PlayerPrefs.SetString(keyName, value); }
    }

    public static string GetString(string keyName, string defaultValue = "")
    {
        string encryptedValue = PlayerPrefs.GetString(keyName, "");
        if (string.IsNullOrEmpty(encryptedValue)) return defaultValue;

        try
        {
            byte[] iv = new byte[16];
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(SECRET_KEY);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedValue);
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                // Return the unscrambled data!
                return Encoding.UTF8.GetString(decrypted);
            }
        }
        catch { return defaultValue; } // If it fails to decrypt (e.g. old unencrypted save), return default
    }

    public static void Save() => PlayerPrefs.Save();
    public static void DeleteKey(string keyName) => PlayerPrefs.DeleteKey(keyName);
    public static void DeleteAll() => PlayerPrefs.DeleteAll();
}