/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

.namespace PHP.Core
{
  /// <summary>
  /// Low level unsafe utilities not programmable in unsafe C#.
  /// </summary>
  .class public Utils
  {
    /// <summary>
    /// Copies a block of memory.
    /// </summary>
    /// <param name="dst">The destination address.</param>
    /// <param name="src">The source block address.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <remarks>Performs no checks.</remarks>
    /// <exception cref="NullReferenceException">An invalid address is specified.</exception>
    .method public static void MemCpy(void* dst, void* src,int32 count)
    {
      .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
      
      ldarg dst
      ldarg src
      ldarg count
      cpblk
      ret
    }

    /// <summary>
    /// Fills a block of memory by specified byte.
    /// </summary>
    /// <param name="address">The address of the first byte of the block to be filled.</param>
    /// <param name="value">The value with to do filling with.</param>
    /// <param name="count">The number of bytes to fill.</param>
    /// <remarks>Performs no checks.</remarks>
    /// <exception cref="NullReferenceException">An invalid address is specified.</exception>
    .method public static void MemFill(unsigned int8 *address,unsigned int8 'value',int32 count)
    {
      .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
      
      ldarg address
      ldarg 'value'
      ldarg count
      initblk
      ret
    }
  }  
}
