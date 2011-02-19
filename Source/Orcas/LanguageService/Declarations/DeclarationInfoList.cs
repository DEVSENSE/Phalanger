/*

 Copyright (c) 2008 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    #region List of unique declaration info items.
    /// <summary>
    /// List of declarations without duplicities.
    /// </summary>
    public class DeclarationList:List<DeclarationInfo>
    {
        /// <summary>
        /// Hash table of existing declarations.
        /// </summary>
        private Dictionary<DeclarationInfo, bool> ExistingDecls = new Dictionary<DeclarationInfo, bool>();

        /// <summary>
        /// Add the item into the list if not yet.
        /// </summary>
        /// <param name="item">Item to be added.</param>
        public new void Add(DeclarationInfo item)
        {
            if ( !ExistingDecls.ContainsKey(item) )
            {
                ExistingDecls[item] = true;
                
                base.Add(item);
            }
        }

        /// <summary>
        /// remove given item from the list.
        /// </summary>
        /// <param name="item">Item to be removed.</param>
        public new void Remove(DeclarationInfo item)
        {
            ExistingDecls.Remove(item);

            base.Remove(item);
        }

        /// <summary>
        /// remove given item from the list by its index.
        /// </summary>
        /// <param name="index">Index of the item to be removed.</param>
        public new void RemoveAt(int index)
        {
            ExistingDecls.Remove(this[index]);

            base.RemoveAt(index);
        }

        /// <summary>
        /// Clear the list.
        /// </summary>
        public new void Clear()
        {
            ExistingDecls.Clear();

            base.Clear();
        }

        /// <summary>
        /// Checks if the list contains the given declaration.
        /// </summary>
        /// <param name="item">Item to be checked with.</param>
        /// <returns>True if the list contains given item, otherwise false.</returns>
        public new bool Contains(DeclarationInfo item)
        {
            return ExistingDecls.ContainsKey(item);
        }

        /// <summary>
        /// Inserts the declaration into the list.
        /// </summary>
        /// <param name="index">Insertion position</param>
        /// <param name="item">Declaration to insert.</param>
        public new void Insert(int index, DeclarationInfo item)
        {
            if (!ExistingDecls.ContainsKey(item))
            {
                ExistingDecls[item] = true;

                base.Insert(index, item);
            }
        }

        /// <summary>
        /// Remove all declaration with different type.
        /// </summary>
        /// <param name="typesmask">Types to be kept in the list.</param>
        public void FilterType(DeclarationInfo.DeclarationTypes typesmask)
        {
            if (typesmask == 0)
                return;

            int i = 0;
            while (i < Count)
            {
                if ((this[i].DeclarationType & typesmask) == 0)
                {   // remove [i]
                    RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }
    }

    #endregion
}