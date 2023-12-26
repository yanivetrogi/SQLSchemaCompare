//ttjjjj::    ;;jjjjii  iijjjjii      ;;GGGGGGii      iijjjjjjjjjjjjjj::          
//GG####ff    jj####LL  GG####LL    ::GG######DD,,    LL############WWtt          
//DD######..  ff####LL  GG####GG  ..WW##########WW::  LL##############tt          
//DD######jj  ff####LL  GG####GG  ff####WWGGKK####LL  LL####WWDDDDDDGGii          
//DD######WW  ff####LL  GG####GG  WW####..    WW##WW  LL####GG                    
//DD########ttjj####GG;;DD####GGiiKK##DD      iiffffiiGGKK##KKtttttt..            
//DD####WW##DDLL##KKEEEEKK####KKEEEEKKGG            EEEEEE##########::            
//DD####GG####GG##KKEEEEKK####KKEEEEWWLL            EEKKKK##########::            
//DD####ttDD######KKEELLKK####KKGGKKWWGG      ..::::LLEEKK##WWDDDDDD::            
//DD####ttii########LL  GG####LL..####WW      DD####  LL####GG                    
//DD####tt  WW######LL  GG####GG  GG####jj,,tt####DD  LL####DDiiiiiiii..          
//DD####tt  tt######LL  GG####GG  ii##############tt  LL##############tt          
//DD####tt    WW####LL  GG####GG    ff##########GG    LL##############tt          
//iitttt::    ,,tttt,,  ;;tttt;;      ::ttttttii      ;;ttttttttttttii::          
/// <creator>lirons</creator>
/// <creationdate>25/02/2007 20:29:12</creationdate>
/// <summary>
/// 
/// </summary>  

using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Collections.Generic;
using ToolBox;


internal sealed class CryptoString
{
    private CryptoString() { }

    public const string COMPUTERID = "CompUnID";
    public const string EDITION = "Edition";

    private static byte[] savedKey = null;
    private static byte[] savedIV = null;

    public static byte[] Key
    {
        get { return savedKey; }
        set { savedKey = value; }
    }

    public static byte[] IV
    {
        get { return savedIV; }
        set { savedIV = value; }
    }

    private static void RdGenerateSecretKey(RijndaelManaged rdProvider)
    {
        if (savedKey == null)
        {
            byte[] MyKey = { 249, 225, 127, 107, 39, 13, 101, 85, 138, 21, 209, 141, 220, 169, 150, 229, 61, 147, 83, 54, 36, 241, 36, 175, 210, 19, 189, 96, 48, 93, 153, 207 };
            savedKey = MyKey;
            //rdProvider.KeySize = 256;
            //rdProvider.GenerateKey();
            //savedKey = rdProvider.Key;
        }
    }

    private static void RdGenerateSecretInitVector(RijndaelManaged rdProvider)
    {
        if (savedIV == null)
        {
            byte[] MyIv = { 114, 197, 138, 252, 141, 3, 188, 5, 58, 144, 135, 109, 143, 166, 45, 90 };
            savedIV = MyIv;
            //rdProvider.GenerateIV();
            //savedIV = rdProvider.IV;
        }
    }

    public static string Encrypt(string originalStr)
    {
        // Encode data string to be stored in memory.
        byte[] originalStrAsBytes = Encoding.ASCII.GetBytes(originalStr);
        byte[] originalBytes = { };

        // Create MemoryStream to contain output.
        using (MemoryStream memStream = new
        MemoryStream(originalStrAsBytes.Length))
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                // Generate and save secret key and init vector.
                RdGenerateSecretKey(rijndael);
                RdGenerateSecretInitVector(rijndael);

                if (savedKey == null || savedIV == null)
                {
                    throw (new NullReferenceException(
                    "savedKey and savedIV must be non-null."));
                }

                // Create encryptor and stream objects.
                using (ICryptoTransform rdTransform =
                rijndael.CreateEncryptor((byte[])savedKey.
                Clone(), (byte[])savedIV.Clone()))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memStream,
                    rdTransform, CryptoStreamMode.Write))
                    {
                        // Write encrypted data to the MemoryStream.
                        cryptoStream.Write(originalStrAsBytes, 0,
                        originalStrAsBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        originalBytes = memStream.ToArray();
                    }
                }
            }
        }
        // Convert encrypted string.
        string encryptedStr = Convert.ToBase64String(originalBytes);
        return (encryptedStr);
    }

    public static string Decrypt(string encryptedStr)
    {
        // Unconvert encrypted string.
        byte[] encryptedStrAsBytes = Convert.FromBase64String(encryptedStr);
        byte[] initialText = new Byte[encryptedStrAsBytes.Length];

        using (RijndaelManaged rijndael = new RijndaelManaged())
        {
            using (MemoryStream memStream = new MemoryStream(encryptedStrAsBytes))
            {
                RdGenerateSecretKey(rijndael);
                RdGenerateSecretInitVector(rijndael);
                if (savedKey == null || savedIV == null)
                {
                    throw (new NullReferenceException(
                    "savedKey and savedIV must be non-null."));
                }

                // Create decryptor, and stream objects.
                using 
                    (
                        ICryptoTransform rdTransform = 
                            rijndael.CreateDecryptor((byte[])savedKey.Clone(), (byte[])savedIV.Clone())
                    )
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memStream,
                    rdTransform, CryptoStreamMode.Read))
                    {
                        // Read in decrypted string as a byte[].
                        cryptoStream.Read(initialText, 0, initialText.Length);
                    }
                }
            }
        }

        // Convert byte[] to string.
        string decryptedStr = Encoding.ASCII.GetString(initialText);
        return (decryptedStr);
    }


    /// <summary>
    /// Create Enc Activation key, get Dictionary<string, string> and return long string enc 
    /// </summary>
    /// <param name="data">key and value</param>
    /// <returns></returns>
    public static string GenerateActivationKey(BUS data)
    {
        StringBuilder sb = new StringBuilder();
        //run on the keys 
        foreach (KeyValuePair<string, object> kvp in data)
        {
            sb.Append(kvp.Key);
            sb.Append("=");
            sb.Append(kvp.Value.ToString());
            sb.Append(";");
        }

        string enc = CryptoString.Encrypt(sb.ToString());

        return enc;
    }


    /// <summary>
    /// Dec Activation key, get long string enc return  Dictionary<string, string>
    /// </summary>
    /// <param name="data">key and value</param>
    /// <returns></returns>
    public static BUS DecryptActivationKey(string enc)
    {
        //decrpty th string 
        string dec = Decrypt(enc);
        //split it by ;
        string[] kvps = dec.Split(';');
        //create new bus

        BUS data = new BUS();

        foreach (string s in kvps)
        {
            string[] spl = s.Split('=');
            if (spl.Length==2)
            {
                data.Insert(spl[0], spl[1]);
            }
            
        }               

        return data;
    }
}
