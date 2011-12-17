//
// Oct-07 - Martin Fern�ndez translation for all the linux fans and detractors...
//  
/*
#########################################################
# md5crypt.py
#
# 0423.2000 by michal wallace http://www.sabren.com/
# based on perl's Crypt::PasswdMD5 by Luis Munoz (lem@cantv.net)
# based on /usr/src/libcrypt/crypt.c from FreeBSD 2.2.5-RELEASE
#
# MANY THANKS TO
#
#  Carey Evans - http://home.clear.net.nz/pages/c.evans/
#  Dennis Marti - http://users.starpower.net/marti1/
#
#  For the patches that got this thing working!
#
#########################################################
md5crypt.py - Provides interoperable MD5-based crypt() function

SYNOPSIS

    import md5crypt.py

    cryptedpassword = md5crypt.md5crypt(password, salt);

DESCRIPTION

unix_md5_crypt() provides a crypt()-compatible interface to the
rather new MD5-based crypt() function found in modern operating systems.
It's based on the implementation found on FreeBSD 2.2.[56]-RELEASE and
contains the following license in it:

 "THE BEER-WARE LICENSE" (Revision 42):
 <phk@login.dknet.dk> wrote this file.  As long as you retain this notice you
 can do whatever you want with this stuff. If we meet some day, and you think
 this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp

apache_md5_crypt() provides a function compatible with Apache's
.htpasswd files. This was contributed by Bryan Hart <bryan@eai.com>.
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PasswordEvolution
{

    public class MD5Crypt
    {
        /** Password hash magic */
        private String magic = "$1$";

        /** Characters for base64 encoding */
        private String itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// A function to concatenate bytes[]
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns>New adition array</returns>
        private byte[] Concat(byte[] array1, byte[] array2)
        {
            byte[] concat = new byte[array1.Length + array2.Length];
            System.Buffer.BlockCopy(array1, 0, concat, 0, array1.Length);
            System.Buffer.BlockCopy(array2, 0, concat, array1.Length, array2.Length);
            return concat;
        }
        /// <summary>
        /// Another function to concatenate bytes[]
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns>New adition array</returns>
        private byte[] PartialConcat(byte[] array1, byte[] array2, int max)
        {
            byte[] concat = new byte[array1.Length + max];
            System.Buffer.BlockCopy(array1, 0, concat, 0, array1.Length);
            System.Buffer.BlockCopy(array2, 0, concat, array1.Length, max);
            return concat;
        }

        /// <summary>
        /// Base64-Encode integer value
        /// </summary>
        /// <param name="value"> The value to encode</param>
        /// <param name="length"> Desired length of the result</param>
        /// <returns>@return Base64 encoded value</returns>
        private String to64(int value, int length)
        {
            StringBuilder result;

            result = new StringBuilder();
            while (--length >= 0)
            {
                result.Append(itoa64.Substring(value & 0x3f, 1));
                value >>= 6;
            }
            return (result.ToString());
        }
        /// <summary>
        /// Unix-like Crypt-MD5 function
        /// </summary>
        /// <param name="password">The user password</param>
        /// <param name="salt">The salt or the pepper of the password</param>
        /// <returns>a human readable string</returns>
        public String crypt(String password, String salt)
        {
            int saltEnd;
            int len;
            int value;
            int i;
            /*
            MessageDigest ctx;
            MessageDigest ctx1;
             */

            //        sResult = BitConverter.ToString(hashvalue1);
            byte[] final;
            byte[] passwordBytes;
            byte[] saltBytes;
            byte[] ctx;

            StringBuilder result;
            HashAlgorithm x_hash_alg = HashAlgorithm.Create("MD5");



            // Skip magic if it exists
            if (salt.StartsWith(magic))
            {
                salt = salt.Substring(magic.Length);
            }

            // Remove password hash if present
            if ((saltEnd = salt.LastIndexOf('$')) != -1)
            {
                salt = salt.Substring(0, saltEnd);
            }

            // Shorten salt to 8 characters if it is longer
            if (salt.Length > 8)
            {
                salt = salt.Substring(0, 8);
            }

            ctx = Encoding.ASCII.GetBytes((password + magic + salt));
            final = x_hash_alg.ComputeHash(Encoding.ASCII.GetBytes((password + salt + password)));


            // Add as many characters of ctx1 to ctx
            //   byte[] hashM;// = new byte[15];
            for (len = password.Length; len > 0; len -= 16)
            {
                if (len > 16)
                {
                    ctx = Concat(ctx, final);
                }
                else
                {
                    ctx = PartialConcat(ctx, final, len);

                }

                //System.Buffer.BlockCopy(final, 0, hash16, ctx.Length, len);
                //System.Buffer.BlockCopy(ctx, 0, hash16, 0, ctx.Length);

            }
            //ctx = hashM;

            // Then something really weird...
            passwordBytes = Encoding.ASCII.GetBytes(password);

            for (i = password.Length; i > 0; i >>= 1)
            {
                if ((i & 1) == 1)
                {
                    ctx = Concat(ctx, new byte[] { 0 });
                }
                else
                {
                    ctx = Concat(ctx, new byte[] { passwordBytes[0] });
                }
            }

            final = x_hash_alg.ComputeHash(ctx);

            byte[] ctx1;

            // Do additional mutations
            saltBytes = Encoding.ASCII.GetBytes(salt);//.getBytes();
            for (i = 0; i < 1000; i++)
            {
                ctx1 = new byte[] { };
                if ((i & 1) == 1)
                {
                    ctx1 = Concat(ctx1, passwordBytes);
                }
                else
                {
                    ctx1 = Concat(ctx1, final);
                }
                if (i % 3 != 0)
                {
                    ctx1 = Concat(ctx1, saltBytes);
                }
                if (i % 7 != 0)
                {
                    ctx1 = Concat(ctx1, passwordBytes);
                }
                if ((i & 1) != 0)
                {
                    ctx1 = Concat(ctx1, final);
                }
                else
                {
                    ctx1 = Concat(ctx1, passwordBytes);
                }
                final = x_hash_alg.ComputeHash(ctx1);

            }
            result = new StringBuilder();
            // Add the password hash to the result string
            value = ((final[0] & 0xff) << 16) | ((final[6] & 0xff) << 8)
                    | (final[12] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[1] & 0xff) << 16) | ((final[7] & 0xff) << 8)
                    | (final[13] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[2] & 0xff) << 16) | ((final[8] & 0xff) << 8)
                    | (final[14] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[3] & 0xff) << 16) | ((final[9] & 0xff) << 8)
                    | (final[15] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[4] & 0xff) << 16) | ((final[10] & 0xff) << 8)
                    | (final[5] & 0xff);
            result.Append(to64(value, 4));
            value = final[11] & 0xff;
            result.Append(to64(value, 2));

            // Return result string
            return magic + salt + "$" + result.ToString();
        }

    }
}