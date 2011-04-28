/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Collections;

namespace csammisrun.OscarLib.Utility
{
    internal class CapabilityProcessor
    {
        /// <summary>
        /// Creates a CLSID array from a Capabilities enumeration
        /// </summary>
        /// <param name="caps">A Capabilities enumeration</param>
        /// <returns>A byte array containing the CLSIDs of the capabilities</returns>
        public static byte[] GetCapabilityArray(Capabilities caps)
        {
            ArrayList list = new ArrayList();

            // These are sorted numerically
            if ((caps & Capabilities.OscarLib) != 0)
            {
                list.Add((uint)0x09191982);
                list.Add((uint)0xDEADBEEF);
                list.Add((uint)0xCAFE4445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.iChat) != 0)
            {
                list.Add((uint)0x09460000);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.VoiceChat) != 0)
            {
                list.Add((uint)0x09461341);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.DirectPlay) != 0)
            {
                list.Add((uint)0x09461342);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.SendFiles) != 0)
            {
                list.Add((uint)0x09461343);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.RouteFinder) != 0)
            {
                list.Add((uint)0x09461344);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.DirectIM) != 0)
            {
                list.Add((uint)0x09461345);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.BuddyIcon) != 0)
            {
                list.Add((uint)0x09461346);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.StocksAddIn) != 0)
            {
                list.Add((uint)0x09461347);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.GetFiles) != 0)
            {
                list.Add((uint)0x09461348);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.Channel2Ext) != 0)
            {
                list.Add((uint)0x09461349);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.Games) != 0)
            {
                list.Add((uint)0x0946134A);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.BuddyListTransfer) != 0)
            {
                list.Add((uint)0x0946134B);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.AIMtoICQ) != 0)
            {
                list.Add((uint)0x0946134D);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.UTF8) != 0)
            {
                list.Add((uint)0x0946134E);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.Unknown2) != 0)
            {
                list.Add((uint)0x10CF40D1);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.Unknown3) != 0)
            {
                list.Add((uint)0x2E7A6475);
                list.Add((uint)0xFADF4DC8);
                list.Add((uint)0x886FEA35);
                list.Add((uint)0x95FDB6DF);
            }
            if ((caps & Capabilities.Unknown4) != 0)
            {
                list.Add((uint)0x563FC809);
                list.Add((uint)0x0B6f41BD);
                list.Add((uint)0x9F794226);
                list.Add((uint)0x09DFA2F3);
            }
            if ((caps & Capabilities.Chat) != 0)
            {
                list.Add((uint)0x748F2420);
                list.Add((uint)0x628711D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.RTF) != 0)
            {
                list.Add((uint)0x97B12751);
                list.Add((uint)0x243C4334);
                list.Add((uint)0xAD22D6AB);
                list.Add((uint)0xF73F1492);
            }
            if ((caps & Capabilities.SIMKopete) != 0)
            {
                list.Add((uint)0x97B12751);
                list.Add((uint)0x243C4334);
                list.Add((uint)0xAD22D6AB);
                list.Add((uint)0xF73F1400);
            }
            if ((caps & Capabilities.Unknown1) != 0)
            {
                list.Add((uint)0xA0E93F37);
                list.Add((uint)0x4C7F11D1);
                list.Add((uint)0x82224445);
                list.Add((uint)0x53540000);
            }
            if ((caps & Capabilities.TrillianSecureIM) != 0)
            {
                list.Add((uint)0xF2E7C7F4);
                list.Add((uint)0xFEAD4DFB);
                list.Add((uint)0xB2353679);
                list.Add((uint)0x8BDF0000);
            }

            int index = 0;
            byte[] retval = new byte[4 * list.Count];
            foreach (uint i in list)
            {
                Marshal.InsertUint(retval, i, ref index);
            }
            return retval;
        }

        /// <summary>
        /// Creates a Capabilities enumeration from a CLSID array
        /// </summary>
        /// <param name="buffer">A byte array containing the CLSIDs of the capabilities</param>
        /// <returns>A Capabilities enumeration</returns>
        public static Capabilities ProcessCLSIDList(byte[] buffer)
        {
            Capabilities retval = Capabilities.None;
            if (buffer == null)
            {
                return retval;
            }

            using (ByteStream bstream = new ByteStream(buffer))
            {
                uint word1 = 0;
                uint word2 = 0;
                uint word3 = 0;
                uint word4 = 0;
                while (bstream.HasMoreData)
                {
                    word1 = bstream.ReadUint();
                    word2 = bstream.ReadUint();
                    word3 = bstream.ReadUint();
                    word4 = bstream.ReadUint();
                    if (word1 == 0x09461341 && word2 == 0x4C7F11D1
                        && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.VoiceChat;
                    }
                    else if (word1 == 0x09461342 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.DirectPlay;
                    }
                    else if (word1 == 0x09461343 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.SendFiles;
                    }
                    else if (word1 == 0x09461344 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.RouteFinder;
                    }
                    else if (word1 == 0x09461345 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.DirectIM;
                    }
                    else if (word1 == 0x09461346 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.BuddyIcon;
                    }
                    else if (word1 == 0x09461347 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.StocksAddIn;
                    }
                    else if (word1 == 0x09461348 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.GetFiles;
                    }
                    else if (word1 == 0x09461349 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.Channel2Ext;
                    }
                    else if (word1 == 0x0946134A && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.Games;
                    }
                    else if (word1 == 0x0946134B && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.BuddyListTransfer;
                    }
                    else if (word1 == 0x0946134D && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.AIMtoICQ;
                    }
                    else if (word1 == 0x0946134E && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.UTF8;
                    }
                    else if (word1 == 0x09460000 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.iChat;
                    }
                    else if (word1 == 0x97B12751 && word2 == 0x243C4334
                             && word3 == 0xAD22D6AB && word4 == 0xF73F1492)
                    {
                        retval |= Capabilities.RTF;
                    }
                    else if (word1 == 0xA0E93F37 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.Unknown1;
                    }
                    else if (word1 == 0x10CF40D1 && word2 == 0x4C7F11D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.Unknown2;
                    }
                    else if (word1 == 0x2E7A6475 && word2 == 0xFADF4DC8
                             && word3 == 0x886FEA35 && word4 == 0x95FDB6DF)
                    {
                        retval |= Capabilities.Unknown3;
                    }
                    else if (word1 == 0x563FC809 && word2 == 0x0B6f41BD
                             && word3 == 0x9F794226 && word4 == 0x09DFA2F3)
                    {
                        retval |= Capabilities.Unknown4;
                    }
                    else if (word1 == 0x748F2420 && word2 == 0x628711D1
                             && word3 == 0x82224445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.Chat;
                    }
                    else if (word1 == 0xF2E7C7F4 && word2 == 0xFEAD4DFB
                             && word3 == 0xB2353679 && word4 == 0x8BDF0000)
                    {
                        retval |= Capabilities.TrillianSecureIM;
                    }
                    else if (word1 == 0x97B12751 && word2 == 0x243C4334
                             && word3 == 0xAD22D6AB && word4 == 0xF73F1400)
                    {
                        retval |= Capabilities.SIMKopete;
                    }
                    else if (word1 == 0x09191982 && word2 == 0xDEADBEEF
                             && word2 == 0xCAFE4445 && word4 == 0x53540000)
                    {
                        retval |= Capabilities.OscarLib;
                    }
                }
            }
            return retval;
        }
    }
}