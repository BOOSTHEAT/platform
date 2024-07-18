using System;
using System.IO;
using System.Security.Cryptography;
using Chaos.NaCl;
using Renci.SshNet.Security;

namespace ImpliciX.DesktopServices.Helpers
{
  internal static class SshKeygenEd25519
  {
    public static string Generate(string passphrase)
    {
      var key = GenerateKey();
      return key.ToOpenSshFormat(passphrase);
    }

    private static Key GenerateKey()
    {
      using var rngCsp = new RNGCryptoServiceProvider();
      var seed = new byte[Ed25519.PrivateKeySeedSizeInBytes];
      rngCsp.GetBytes(seed);
      Ed25519.KeyPairFromSeed(out var edPubKey, out var edKey, seed);
      return new ED25519Key(edPubKey, edKey.Reverse());
    }

    public static string ToOpenSshFormat(this Key key, string passphrase)
    {
      var s = new StringWriter();
      s.Write("-----BEGIN OPENSSH PRIVATE KEY-----\n");
      s.Write(key.ToOpenSshPrivateKeyData(passphrase));
      s.Write("-----END OPENSSH PRIVATE KEY-----\n");
      return s.ToString();
    }

    private static string ToOpenSshPrivateKeyData(this Key key, string passphrase)
    {
      var encryption = new SshKeyEncryptionAes256();
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream);

      writer.EncodeNullTerminatedString("openssh-key-v1"); // Auth Magic
      writer.EncodeBinary(encryption.CipherName);
      writer.EncodeBinary(encryption.KdfName);
      writer.EncodeBinary(encryption.KdfOptions());
      writer.EncodeUInt(1); // Number of Keys

      // public key in ssh-format
      using var pubStream = new MemoryStream();
      using var pubWriter = new BinaryWriter(pubStream);
      key.PublicKeyData(pubWriter);
      writer.EncodeBinary(pubStream);

      // private key
      using var privStream = new MemoryStream();
      using var privWriter = new BinaryWriter(privStream);

      var rnd = new Random().Next(0, int.MaxValue);
      privWriter.EncodeInt(rnd); // check-int1
      privWriter.EncodeInt(rnd); // check-int2
      privWriter.EncodeBinary(key.ToString());
      switch (key.ToString())
      {
        case "ssh-ed25519":
          var ed25519 = (ED25519Key)key;
          privWriter.EncodeBinary(ed25519.PublicKey);
          privWriter.EncodeBinary(ed25519.PrivateKey);
          break;
        default:
          throw new NotSupportedException($"Unsupported KeyType: {key}");
      }

      // comment
      privWriter.EncodeBinary("");

      // private key padding (1, 2, 3, ...)
      var pad = 0;
      while (privStream.Length % encryption.BlockSize != 0)
      {
        privWriter.Write((byte)++pad);
      }

      writer.EncodeBinary(encryption.Encrypt(passphrase, privStream.ToArray()));

      // Content as Base64
      var base64 = Convert.ToBase64String(stream.ToArray()).ToCharArray();
      var pem = new StringWriter();
      for (var i = 0; i < base64.Length; i += 70)
      {
        pem.Write(base64, i, Math.Min(70, base64.Length - i));
        pem.Write("\n");
      }

      return pem.ToString();
    }
    
    public static void PublicKeyData(this Key key, BinaryWriter writer)
    {
      writer.EncodeBinary(key.ToString());
      switch (key.ToString())
      {
        case "ssh-ed25519":
          var ed25519 = (ED25519Key)key;
          writer.EncodeBinary(ed25519.PublicKey);
          break;
        default:
          throw new NotSupportedException($"Unsupported KeyType: {key}");
      }
    }


    public static T[] Reverse<T>(this T[] array)
    {
      Array.Reverse(array);
      return array;
    }
  }
}