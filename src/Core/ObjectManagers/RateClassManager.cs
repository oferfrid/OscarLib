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
    /// <summary>
    /// Manages the five SNAC rate classes
    /// </summary>
    public class RateClassManager
    {
        private RateClass[] _classes;
        private Hashtable _lookups;

        /// <summary>
        /// Creates a new RateClassManager
        /// </summary>
        public RateClassManager()
        {
            // Initialize RateClass tables
            _lookups = new Hashtable();
            _classes = new RateClass[5];
            for (int i = 0; i < 5; i++)
            {
                _classes[i] = new RateClass();
            }
        }

        /// <summary>
        /// Gets or sets a rate class by SNAC family and subtype
        /// </summary>
        public RateClass this[int key]
        {
            get
            {
                if (_lookups.ContainsKey(key))
                {
                    int index = (int) _lookups[key];
                    return _classes[index - 1];
                }
                return null;
            }
            set
            {
                if (_lookups.ContainsKey(key))
                {
                    int index = (int) _lookups[key];
                    _classes[index - 1] = value;
                }
            }
        }

        /// <summary>
        /// Associates a rate class with a SNAC family and subtype
        /// </summary>
        /// <param name="family">A SNAC family ID</param>
        /// <param name="subfamily">A SNAC subtype ID</param>
        /// <param name="rateclass">The index of the rate class to associate</param>
        public void SetRateClassKey(ushort family, ushort subfamily, int rateclass)
        {
            int key = (int) ((family << 16) | subfamily);
            _lookups[key] = rateclass;
        }

        /// <summary>
        /// Gets a <see cref="RateClass"/> object by its ID number
        /// </summary>
        /// <param name="Id">A rate class ID number, 1 - 5</param>
        /// <returns>A <see cref="RateClass"/> object</returns>
        public RateClass GetByID(int Id)
        {
            return _classes[Id - 1];
        }

        /// <summary>
        /// Sets a <see cref="RateClass"/> object by its ID number
        /// </summary>
        /// <param name="Id">A rate class ID number, 1 - 5</param>
        /// <param name="rc">A <see cref="RateClass"/> object</param>
        public void SetByID(int Id, RateClass rc)
        {
            _classes[Id - 1] = rc;
        }
    }
}