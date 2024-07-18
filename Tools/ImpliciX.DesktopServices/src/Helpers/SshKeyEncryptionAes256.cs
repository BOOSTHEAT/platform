using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Renci.SshNet.Security.Cryptography.Ciphers;
using Renci.SshNet.Security.Cryptography.Ciphers.Modes;
using Renci.SshNet.Security.Cryptography.Ciphers.Paddings;

namespace ImpliciX.DesktopServices.Helpers
{
  internal class SshKeyEncryptionAes256
  {
    public enum Aes256Mode
    {
      CBC,
      CTR
    }

    public string CipherName => $"aes256-{_mode.ToString().ToLower()}";
    public string KdfName => "bcrypt";
    public int BlockSize => 16;

    private const int SaltLen = 16;
    private const int Rounds = 16;
    private Aes256Mode _mode;
    private readonly byte[] _salt;

    public SshKeyEncryptionAes256()
    {
      _salt = new byte[SaltLen];
    }

    public SshKeyEncryptionAes256(Aes256Mode mode)
    {
      _mode = mode;
    }

    public byte[] KdfOptions()
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream);
      using var rng = new RNGCryptoServiceProvider();
      rng.GetBytes(_salt);
      writer.EncodeBinary(_salt);
      writer.EncodeUInt(Rounds);
      return stream.ToArray();
    }

    public byte[] Encrypt(string passphrase, byte[] data)
    {
      var passPhraseBytes = Encoding.ASCII.GetBytes(passphrase);
      var keyiv = new byte[48];
      new BCrypt().Pbkdf(passPhraseBytes, _salt, Rounds, keyiv);
      var key = new byte[32];
      var iv = new byte[16];
      Array.Copy(keyiv, 0, key, 0, 32);
      Array.Copy(keyiv, 32, iv, 0, 16);

      AesCipher cipher;
      switch (_mode)
      {
        case Aes256Mode.CBC:
          cipher = new AesCipher(key, new CbcCipherMode(iv), new PKCS7Padding());
          break;
        default:
          _mode = Aes256Mode.CTR;
          cipher = new AesCipher(key, new CtrCipherMode(iv), new PKCS7Padding());
          break;
      }

      return cipher.Encrypt(data);
    }
    
  }
}