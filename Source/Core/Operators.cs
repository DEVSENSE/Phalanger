/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

using PHP.Library;
using PHP.Core.Reflection;

#if SILVERLIGHT
using MathEx = PHP.CoreCLR.MathEx;
using ArrayEx = PHP.CoreCLR.ArrayEx;
#else
using MathEx = System.Math;
using ArrayEx = System.Array;
#endif

namespace PHP.Core
{
    #region Overview
    /// <summary>
    /// Operators used by PHP language.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler keeps track of whether or not a variable is a reference but doesn't do so in
    /// the case of a property or an array item. Moreover, a <see cref="PhpReference"/> variable cannot be <B>null</B>.
    /// Thus
    /// <list type="bullet">
    ///   <item>an operator returning an item of an array or a property of an object dereferences return value itself,</item>
    ///   <item>operands of type <see cref="object"/> should not be of type <see cref="PhpReference"/>,</item>
    ///   <item>an operator returning a <see cref="PhpReference"/> should never return a <B>null</B> reference.</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// In the following tables <c>p</c>, <c>q</c> are references while <c>x</c>, <c>y</c>, <c>z</c>, <c>u</c>, <c>v</c> 
    /// are not (if a corresponding variable is a <see cref="PhpReference"/> then <see cref="PhpReference.value"/> 
    /// is used instead). The <c>context</c> is the current <see cref="ScriptContext"/> and the 
    /// <c>type</c> is the current <see cref="DTypeDesc"/> as described in the following paragraph.
    /// </para>
    /// <para>
    /// Operators working on <see cref="DObject"/> have a <see cref="DTypeDesc"/> parameter named 
    /// <c>caller</c>. When such an operator is used in a PHP function, <B>null</B> is supplied.
    /// When the operator is used in a PHP method, the <see cref="DTypeDesc"/> of the class
    /// this method belongs to is supplied.
    /// Finally, when the operator is used in a script's Main() method,
    /// the <see cref="DTypeDesc"/> that comes as one of Main()'s parameters is supplied.
    /// </para>
    /// 
    /// <!-- simple operators ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <para>
    /// <list type="table">
    /// <listheader><term>Simple operators</term><term>Implementation</term></listheader>
    ///   <item><term><c>x &amp;&amp; y</c></term><term><c><see cref="Convert.ObjectToBoolean"/>(x) &amp;&amp; <see cref="Convert.ObjectToBoolean"/>(y)</c></term></item>
    ///   <item><term><c>x || y</c></term><term><c><see cref="Convert.ObjectToBoolean"/>(x) || <see cref="Convert.ObjectToBoolean"/>(y)</c></term></item>
    ///   <item><term><c>x xor y</c></term><term><c><see cref="Convert.ObjectToBoolean"/>(x) xor <see cref="Convert.ObjectToBoolean"/>(y)</c></term></item>
    ///   <item><term><c>!x</c></term><term><c>!<see cref="Convert.ObjectToBoolean"/>(x);</c></term></item>
    ///   <item><term><c>x &amp; y</c></term><term><c><see cref="BitOperation"/>(x,y,BitOp.<see cref="BitOp.And"/>)</c></term></item>
    ///   <item><term><c>x | y</c></term><term><c><see cref="BitOperation"/>(x,y,BitOp.<see cref="BitOp.Or"/>)</c></term></item>
    ///   <item><term><c>x ^ y</c></term><term><c><see cref="BitOperation"/>(x,y,BitOp.<see cref="BitOp.Xor"/>)</c></term></item>
    ///   <item><term><c>~x</c></term><term><c><see cref="BitNot"/>(x)</c></term></item>
    ///   <item><term><c>x++</c></term><term><c>x = <see cref="Increment"/>(x)</c></term></item>
    ///   <item><term><c>x--</c></term><term><c>x = <see cref="Decrement"/>(x)</c></term></item>
    ///   <item><term><c>x + y</c></term><term><c><see cref="Add"/>(x,y)</c></term></item>
    ///   <item><term><c>x - y</c></term><term><c><see cref="Subtract"/>(x,y)</c></term></item>
    ///   <item><term><c>x * y</c></term><term><c><see cref="Multiply"/>(x,y)</c></term></item>
    ///   <item><term><c>x / y</c></term><term><c><see cref="Divide"/>(x,y)</c></term></item>
    ///   <item><term><c>x % y</c></term><term><c><see cref="Remainder"/>(x,y)</c></term></item>
    ///   <item><term><c>x &lt;&lt; y</c></term><term><c><see cref="ShiftLeft"/>(x,y)</c></term></item>
    ///   <item><term><c>x &gt;&gt; y</c></term><term><c><see cref="ShiftRight"/>(x,y)</c></term></item>
    ///   <item><term><c>-x</c></term><term><c><see cref="Minus"/>(x)</c></term></item>
    ///   <item><term><c>+x</c></term><term><c><see cref="Plus"/>(x)</c></term></item>
    ///   <item><term><c>x . y</c></term><term><c><see cref="Concat"/>(x,y)</c></term></item>
    ///   <item><term><c>a . </c>...<c> . z</c></term><term><c><see cref="Concat"/>(<B>new</B> object[] {a,...,z})</c></term></item>
    ///   <item><term><c>x == y</c></term><term><c>PhpComparer.Default.<see cref="PhpComparer.CompareEq"/>(x,y)</c></term></item>
    ///   <item><term><c>x != y</c></term><term><c>!PhpComparer.Default.<see cref="PhpComparer.CompareEq"/>(x,y)</c></term></item>
    ///   <item><term><c>x === y</c></term><term><c><see cref="StrictEquality"/>(x,y)</c></term></item>
    ///   <item><term><c>x !== y</c></term><term><c>!<see cref="StrictEquality"/>(x,y)</c></term></item>
    ///   <item><term><c>x &lt;= y</c></term><term><c>PhpComparer.Default.<see cref="PhpComparer.Compare"/>(x,y) &lt;= 0</c></term></item>
    ///   <item><term><c>x &gt;= y</c></term><term><c>PhpComparer.Default.<see cref="PhpComparer.Compare"/>(x,y) &gt;= 0</c></term></item>
    ///   <item><term><c>x &lt; y</c></term><term><c>PhpComparer.Default.<see cref="PhpComparer.Compare"/>(x,y) &lt; 0</c></term></item>
    ///   <item><term><c>x &gt; y</c></term><term><c>PhpComparer.Default.<see cref="PhpComparer.Compare"/>(x,y) > 0</c></term></item>
    ///   <item><term><c>x = y</c></term><term><c>x = PhpVariable.<see cref="PhpVariable.Copy"/>(y,CopyReason.<see cref="CopyReason.Assigned"/>);</c></term></item>
    ///   <item><term><c>p =&amp; q</c></term><term><c>p = q</c></term></item>
    ///   <item><term><c>isset(x)</c></term><term><c>x != <B>null</B></c><SUP>1</SUP></term></item>
    ///   <item><term><c>unset(x)</c></term><term><c>x = <B>null</B></c></term></item>
    ///   <item><term><c>unset(p)</c></term><term><c>p.value = <B>null</B></c></term></item>
    ///   <item><term><c>({int|integer})x</c></term><term><c><see cref="Convert.ObjectToInteger"/>(x)</c></term></item>
    ///   <item><term><c>({bool|boolean})x</c></term><term><c><see cref="Convert.ObjectToBoolean"/>(x)</c></term></item>
    ///   <item><term><c>({float|real|double})x</c></term><term><c><see cref="Convert.ObjectToDouble"/>(x)</c></term></item>
    ///   <item><term><c>(string)x</c></term><term><c><see cref="Convert.ObjectToString"/>(x)</c></term></item>
    ///   <item><term><c>(array)x</c></term><term><c><see cref="Convert.ObjectToPhpArray"/>(x)</c></term></item>
    ///   <item><term><c>(object)x</c></term><term><c><see cref="Convert.ObjectToDObject"/>(x,context)</c></term></item>
    ///   <item><term><c>(unset)x</c></term><term><c><B>null</B></c></term></item>
    ///   <item><term><c>`x`</c></term><term><c>Execution.<see cref="Execution.ShellExec"/>(x)</c></term></item>
    ///   <item><term><c>@s</c></term><term><c>context.<see cref="ScriptContext.DisableErrorReporting"/>(); s; context.<see cref="ScriptContext.EnableErrorReporting"/>();</c></term></item>
    ///   <item><term><c>new A</c></term><term><c><see cref="New"/>("A",type_handle,context)</c></term></item>
    ///   <item><term><c>c?s:t</c></term><term><c>if (c) {s} else {t};</c></term></item>
    ///   <item><term><c>clone(x)</c></term><term><c><see cref="Clone"/>(x)</c></term></item>
    ///   <item><term><c>x instanceOf A</c></term><term><c><see cref="InstanceOf"/>(x,"A",type_handle,context)</c></term></item>
    /// </list>
    /// <SUP>1</SUP> <c>isset</c> doesn't distinguish between a <b>null</b> and uninitialized variable<BR/>
    /// </para>
    /// 
    /// <!-- item and property operators ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <para>
    /// <list type="table">
    /// <listheader><term>Item and property operators<SUP>2</SUP></term><term>Implementation</term></listheader>
    ///   <item><term><c>x[] = z</c></term><term><c><see cref="SetItem"/>(PhpVariable.<see cref="PhpVariable.Copy"/>(z,CopyReason.<see cref="CopyReason.Assigned"/>),<B>ref</B> x)</c></term></item>    
    ///   <item><term><c>x[] =&amp; p</c></term><term><c><see cref="SetItem"/>(p,<B>ref</B> x)</c></term></item>
    ///   <item><term><c>p =&amp; x[]</c></term><term><c><see cref="SetItem"/>(p = <B>new</B> PhpReference(),<B>ref</B> x)</c></term></item>
    ///   <item><term><c>x[y]</c></term><term><c><see cref="GetItem"/>(x,y,<B>false</B>)</c></term></item>
    ///   <item><term><c>x[y] = z</c></term><term><c><see cref="SetItem"/>(y,PhpVariable.<see cref="PhpVariable.Copy"/>(z,CopyReason.<see cref="CopyReason.Assigned"/>),<B>ref</B> x)</c></term></item>
    ///   <item><term><c>x[y] =&amp; p</c></term><term><c><see cref="SetItemRef"/>(y,p,<B>ref</B> x)</c></term></item>
    ///   <item><term><c>p =&amp; x[y]</c></term><term><c>p = <see cref="GetItemRef"/>(y,<B>ref</B> x)</c></term></item>
    ///   <item><term><c>isset(x[])</c></term><term><c>error - operator [] without key cannot be used for reading</c></term></item>
    ///   <item><term><c>isset(x[y])</c></term><term><c><see cref="GetItem"/>(x,y,<B>true</B>)!=<B>null</B></c></term></item>
    ///   <item><term><c>unset(x[y])</c></term><term><c><see cref="UnsetItem"/>(x,y)</c></term></item>
    ///   <item><term><c>x-&gt;y</c></term><term><c><see cref="GetProperty"/>(x,y,type_handle,<B>false</B>)</c></term></item>
    ///   <item><term><c>x-&gt;y = z</c></term><term><c><see cref="SetProperty"/>(z,<B>ref</B> x,y,PhpVariable.<see cref="PhpVariable.Copy"/>(y,CopyReason.<see cref="CopyReason.Assigned"/>),type_handle,context)</c></term></item>
    ///   <item><term><c>x-&gt;y =&amp; p</c></term><term><c><see cref="SetProperty"/>(p,<B>ref</B> x,y,type_handle,context)</c><SUP>3</SUP></term></item>
    ///   <item><term><c>p =&amp; x-&gt;y</c></term><term><c><see cref="GetPropertyRef"/>(<B>ref</B> x,y,type_handle,context)</c></term></item>
    ///   <item><term><c>isset(x-&gt;y)</c></term><term><c><see cref="GetProperty"/>(x,y,type_handle,<B>true</B>)</c></term></item>
    ///   <item><term><c>unset(x-&gt;y)</c></term><term><c><see cref="UnsetProperty"/>(x,y,type_handle)</c></term></item>
    ///   <item><term><c>A::$y</c></term><term><c><see cref="GetStaticProperty"/>("A",y,type_handle,context,<B>false</B>)</c></term></item>
    ///   <item><term><c>A::$y = z</c></term><term><c><see cref="SetStaticProperty"/>("A",PhpVariable.<see cref="PhpVariable.Copy"/>(y,CopyReason.<see cref="CopyReason.Assigned"/>),type_handle,context)</c></term></item>
    ///   <item><term><c>A::$y =&amp; p</c></term><term><c><see cref="SetStaticProperty"/>("A",y,p,type_handle,context)</c></term></item>
    ///   <item><term><c>p =&amp; A::$y</c></term><term><c><see cref="GetStaticPropertyRef"/>("A",y,type_handle,context)</c></term></item>
    ///   <item><term><c>isset(A::$y)</c></term><term><c><see cref="GetStaticProperty"/>("A",y,type_handle,context,<B>true</B>)</c></term></item>
    ///   <item><term><c>unset(A::$y)</c></term><term><c><see cref="UnsetStaticProperty"/>("A",y,type_handle,context)</c><SUP>4</SUP></term></item>
    ///   <item><term><c>A::x</c></term><term><c><see cref="GetClassConstant"/>("A",x,type_handle,context)</c></term></item>
    /// </list>
    /// <SUP>2</SUP> Note, operator <c>x{y}</c> is implemented in the same way as <c>x[y]</c>.<BR/>
    /// <SUP>3</SUP> Note, there is no -Ref suffix here.<BR/>
    /// <SUP>4</SUP> It is an error to unset static property.
    /// </para>
    /// 
    /// <!-- function and method calls ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <para>
    /// <list type="table">
    /// <listheader><term>Function and method calls</term><term>Implementation</term></listheader>
    ///   <item><term><c>x-&gt;f(args)</c></term><term><c><see cref="InvokeMethod"/>(x,"f",type_handle)</c></term></item>
    ///   <item><term><c>A::f(args)</c></term><term><c><see cref="InvokeStaticMethod"/>("A","f",type_handle)</c></term></item>
    ///   <item><term><c>$f(args)</c></term><term><c>PhpFunction.<see cref="ScriptContext.Call"/>("f",context)</c></term></item>
    /// </list>
    /// Before a function or a method is called arguments are pushed <c>context.<see cref="ScriptContext.Stack"/></c>
    /// by <see cref="PhpStack.AddFrame"/>. Operators stated in the table above are used only if the function/method
    /// being called is not known at the compile time. Otherwise, direct call to the function/method is emitted.
    /// </para>
    /// 
    /// <!-- chained operators ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <B>Chained operators</B>
    /// 
    /// <para>
    /// Several patterns are possible:
    /// <list type="number">
    ///   <item><c>x = chain</c> - chain is <I>read</I>,</item>
    ///   <item><c>chain = y</c> - chain is <I>written</I>,</item>
    ///   <item><c>x =&amp; chain</c> - chain is <I>written</I>,</item>
    ///   <item><c>chain =&amp; y</c> - chain is <I>written</I>,</item>
    /// </list>
    /// where <c>chain</c> is a sequence of item and/or member operators <c>[u]</c>, <c>[]</c>, <c>{u}</c>, <c>->v</c>
    /// at least 2 operators long. The first operator can also be a function call operator or a static property access 
    /// operator. The last operator can be method call operator.
    /// As of PHP 5 method call operator <c>()</c> can be also chained and a static method call can be the first
    /// operation in the chain.
    /// </para>
    /// 
    /// <para>
    /// It's suitable to decompose a chain into three parts for its compilation.
    /// </para>
    /// 
    /// Possible occurences of operators in the chain:
    /// <DIV class="tablediv" id="">
    /// <TABLE class="dtTABLE" cellspacing="0">
    ///   <TR>
    ///     <TH></TH>
    ///     <TH>Chain is <I>read</I></TH>
    ///     <TH>Chain is <I>written</I></TH>
    ///   </TR>
    ///   <TR>
    ///     <TD>first</TD>
    ///     <TD><c>x[y], x{y}, x->y, x->f(), f(), A::$x, A::f()</c></TD>
    ///     <TD><c>x[], x[y], x{y}, x->y, x->f(), f(), A::$x, A::f()</c></TD>
    ///   </TR>    
    ///   <TR>
    ///     <TD>middle</TD>
    ///     <TD><c>x[y], x{y}, x->y, x->f()</c></TD>
    ///     <TD><c>x[], x[y], x{y}, x->y, x->f()</c></TD>
    ///   </TR>    
    ///   <TR>
    ///     <TD>last</TD>
    ///     <TD><c>x[y], x{y}, x->y, x->f()</c></TD>
    ///     <TD><c>x[], x[y], x{y}, x->y</c></TD>
    ///   </TR>    
    /// </TABLE>
    /// </DIV>
    /// 
    /// <para>
    /// Example 1: <code>a[k1][k2]->k3->k4</code>
    /// Example 2: <code>a[k1][]->k2->k3[k4]</code>
    /// Example 3: <code>A::$x[k1]->f(arg)->x</code>
    /// Example 4: <code>g()->x->h()</code>
    /// </para>
    /// 
    /// <para>
    /// If a chain is <I>read</I> and if any item/property listed doesn't exist in the appropriate array/object the result 
    /// will be a <B>null</B> reference. In the case the chain is <I>written</I> and some item or property should 
    /// be an array (bacause it is followed by <c>[]</c> or <c>{}</c> operator in the chain) but it is 
    /// empty in the terms of <see cref="IsEmptyForEnsure"/> then such item/property is replaced by a new empty array.
    /// If some item or property should be a PHP object (bacause it is followed by <c>-&gt;</c> operator in the chain) but it is 
    /// empty in the terms of <see cref="IsEmptyForEnsure"/> then such item/property is replaced by a new instance of 
    /// <see cref="PHP.Library.stdClass"/>. However, a static property is not created if doesn't exist (an error is reported).
    /// </para>
    /// 
    /// <!-- chain reading ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// Chain reading:
    /// <para>
    /// It is a fatal error if there is a [] operator without a key in a chain which is read.
    /// If the chain doesn't contain any such operator it is compiled as a sequence of <see cref="GetItem"/>, 
    /// <see cref="GetProperty"/>, <see cref="GetStaticProperty"/> operators and function and method calls.
    /// </para>
    /// 
    /// The chain from the first example will be compiled as follows (<c>x = a[k1][k2]->k3->k4</c>):
    /// <code>
    /// x = PhpVariable.Copy(
    ///     GetProperty(
    ///     GetProperty(
    ///     GetItem(
    ///     GetItem(a,k1,false),k2,false),k3,type_handle,false),k4,type_handle,false),CopyReason.Assigned);
    /// </code>
    /// 
    /// The chain in the fourth example stated above is compiled as follows (<c>x = g()->x->h()</c>,
    /// assuming declarations <c>function g() {...}</c> and <c>function h() {...}</c> for example):
    /// <code>
    /// x = PhpVariable.Copy(
    ///     Operators.InvokeMethod(
    ///     Operators.GetProperty(
    ///     g(),"x",type_handle),"h",type_handle),CopyReason.Assigned);
    /// </code>
    /// 
    /// <!-- chain writting ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// Chain writing:
    /// <para>
    /// Chain which is written and contains function/method calls can be divided into subchains which 
    /// doesn't contain function calls in the middle. Subchaines are compiled separately as described below.
    /// Because subchains and chains not containing function/method calls are compiled in the same way,
    /// only the compilation of chains is described below.
    /// </para>
    /// 
    /// <para>
    /// Lets follow the chain decomposition:
    /// <list type="bullet">
    ///   <item>
    ///     The first operator in the chain ensures that a variable or a static property which it is applied on is 
    ///     an array or an object. It is implemented by <see cref="EnsureVariableIsArray"/>, 
    ///     <see cref="EnsureVariableIsObject"/>, <see cref="EnsureStaticPropertyIsArray"/> or 
    ///     <see cref="EnsureStaticPropertyIsObject"/>.
    ///   </item>
    ///   <item>
    ///     The next operators up to the last but one ensures that an item or a property on which it is used is an 
    ///     array or an object and is implemented by <see cref="PhpArray.EnsureItemIsArray"/>, <see cref="PhpArray.EnsureItemIsObject"/>,
    ///     <see cref="EnsurePropertyIsArray"/>, or <see cref="EnsurePropertyIsObject"/>. 
    ///   </item>
    ///   <item>
    ///     The last operator sets or gets the resulting value of the chain. It is implemented by 
    ///     <see cref="PhpArray.GetArrayItem"/>, <see cref="PhpArray.GetArrayItemRef"/>, <see cref="GetObjectProperty"/>, 
    ///     <see cref="GetObjectPropertyRef"/>, <see cref="PhpArray.SetArrayItem"/>, <see cref="PhpArray.SetArrayItemRef"/>, 
    ///     <see cref="SetObjectProperty"/>, by a function/method call.
    ///     These methods takes an argument of a particular type which is determined by the previous operator.
    ///   </item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// Each "Ensure" operator returns the requested item/property (possibly creates a new one if not exists or the existing 
    /// is empty in the terms of <see cref="IsEmptyForEnsure"/>). This returned value is passed to the next operator 
    /// in the chain. PhpArray.<see cref="PhpHashtable.Add(object)"/> operator always adds a new item to an array on 
    /// which is applied. The result passed to the next is the item added. The pattern is 
    /// <code>array.Add({result} = new PhpArray());</code>
    /// </para>
    /// 
    /// <para>
    /// The chain in the second example stated above is compiled as follows (<c>a[k1][]->k2->k3[k4] = x</c>):
    /// <code>
    /// PhpArray a1,a2; 
    /// DObject o1,o2;
    /// a1 = Operators.EnsureVariableIsArray(ref a);       
    /// if (a1 == null) goto end;
    /// a1 = Operators.EnsureItemIsArray(a1,k1);
    /// if (a1 == null) goto end;
    /// a1.Add(o2 = stdClass.CreateDefaultObject(context));  
    /// o1 = Operators.EnsurePropertyIsObject(o2,k2,context);
    /// if (o1 == null) goto end;
    /// a2 = Operators.EnsurePropertyIsArray(o1,k3);
    /// if (a2 == null) goto end;
    /// Operators.SetArrayItem(a2,k4,PhpVariable.Copy(x,CopyReason.Assignment));
    /// end:
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// The chain in the third example stated above is compiled as follows (<c>A::$x[k1]->f(arg)->x =&amp; x</c>,
    /// assuming declaration <c>function&amp; f($a) {...}</c> for example):
    /// <code>
    /// // the first subchain:
    /// PhpArray a1;
    /// DObject o1;
    /// a1 = Operators.EnsureStaticPropertyIsArray("A","x",type_handle,context);
    /// if (a1 == null) goto end;
    /// o1 = a1.EnsureItemIsObject(k1,context);
    /// if (o1 == null) goto end;
    /// 
    /// // an ordinary PHP method call:
    /// PhpReference r1;
    /// context.Stack.AddFrame(arg);
    /// r1 = Operators.InvokeMethod(o1,"f",type_handle);
    /// 
    /// // the second subchain:
    /// DObject o2;
    /// o2 = Operators.EnsureVariableIsObject(ref r1.value,context);
    /// if (o2 == null) goto end;
    /// Operators.SetObjectProperty(o2,"x",x,type_handle);
    /// end:
    /// </code>
    /// </para> 
    /// 
    /// <!-- array ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <B>Array construction</B>
    /// <para>
    /// The array keyword is used to create a new instance of PHP array (<see cref="PhpArray"/>).
    /// It takes a sequence of key-value pairs and/or single values. Both keys and values 
    /// can be expressions. Moreover, a value can be preceded by the reference modifier (&amp;) 
    /// allowing values to be added to the resulting array as references.
    /// </para>
    /// 
    /// <para>
    /// The <c>array</c> construct is implemented by construction a new <see cref="PhpArray"/> <c>a</c> in which
    /// entries are added as described in the following table.
    /// <list type="table">
    /// <listheader><term>Array construction element</term><description>Implementation</description></listheader>
    ///   <item><term><c>x => y</c></term><term><c><see cref="PhpArray.SetArrayItem"/>(a,x,PhpVariable.<see cref="PhpVariable.Copy"/>(y,CopyReason.<see cref="CopyReason.Assigned"/>))</c></term></item>
    ///   <item><term><c>x =>&amp; p</c></term><term><c><see cref="PhpArray.SetArrayItemRef"/>(a,x,p)</c></term></item>
    ///   <item><term><c>x</c></term><term><c>a.<see cref="PhpHashtable.Add(object)"/>(PhpVariable.<see cref="PhpVariable.Copy"/>(x,CopyReason.<see cref="CopyReason.Assigned"/>))</c></term></item>
    /// </list> 
    /// </para>
    /// 
    /// <!-- list ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <B>List language construct</B>
    /// <para>
    /// The <c>list</c> language construct is a shorthand for several assignments of array items.
    /// It can be used only on the left hand side of the = operator.
    /// Statement 
    /// <code>list(&lt;rw_expr_1&gt;,...,&lt;rw_expr_k&gt;) = &lt;expression&gt;</code>
    /// is implemented as a sequence of <see cref="PhpArray.GetArrayItem(object, bool)"/> and 
    /// PHP assignments if the rhs is an array (see operators = on variable, array item and object property in tables above).
    /// Otherwise, a <B>null</B> reference is assigned to each expression on the lhs.
    /// Sequence is in reverse order then it is stated in the list "arguments", i.e. the first
    /// item assigned is the last one in the list. Right hand side expression is evaluated
    /// once before assignments take place.
    /// </para>
    /// 
    /// <!-- $$x ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ -->
    /// 
    /// <para>
    /// <list type="table">
    /// <listheader><term>Indirect variable access</term><description>Implementation</description></listheader>
    ///   <item><term><c>$$x</c></term><term><c><see cref="Operators.GetVariable"/>(variables_table,x)</c></term></item>
    ///   <item><term><c>$$x = y</c></term><term><c><see cref="Operators.SetVariable"/>(variables_table,x,PhpVariable.<see cref="PhpVariable.Copy"/>(y,CopyReason.<see cref="CopyReason.Assigned"/>))</c></term></item>
    ///   <item><term><c>$$x =&amp; p</c></term><term><c><see cref="Operators.SetVariableRef"/>(variables_table,x,p)</c></term></item>
    ///   <item><term><c>p =&amp; $$x</c></term><term><c>p = <see cref="Operators.GetVariableRef"/>(variables_table,x)</c></term></item>
    ///   <item><term><c>isset($$x)</c></term><term><c><see cref="Operators.GetVariableUnchecked"/>(variables_table,x) != null</c></term></item>
    ///   <item><term><c>unset($$x)</c></term><term><c>variables_table.<see cref="IDictionary.Remove(object)"/>(x)</c></term></item>
    /// </list>
    /// </para>
    ///     
    /// </remarks>
    #endregion
    [DebuggerNonUserCode]
    public static class Operators
    {
        #region Arithmetic operators

        #region Addition

        /// <summary>
        /// Bit mask corresponding to the sign in <see cref="long"/> value.
        /// </summary>
        private const long LONG_SIGN_MASK = (1L << (8 * sizeof(long) - 1));

        /// <summary>
        /// Implements '+' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The result of type <see cref="PhpArray"/>, <see cref="int"/> or <see cref="double"/>.
        /// If both operands are of type <see cref="PhpArray"/> the result is their union made by 
        /// <see cref="PhpHashtable.Unite"/> on deep copies. 
        /// </returns>
        /// <exception cref="PhpException">Addition is not supported on the types of operands specified.</exception>
        [Emitted]
        public static object Add(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            double dx, dy;
            int ix, iy;
            long lx, ly;
            Convert.NumberInfo info, o1, o2;

            // converts x and y to numbers:
            info = (o1 = Convert.ObjectToNumber(x, out ix, out lx, out dx)) | (o2 = Convert.ObjectToNumber(y, out iy, out ly, out dy));

            if ((info & (Convert.NumberInfo.IsPhpArray | Convert.NumberInfo.Unconvertible)) != 0)
            {
                if (
                    // one of operands is unconvertible
                    ((info & Convert.NumberInfo.Unconvertible) != 0) ||
                    // one of operands is PhpArray
                    ((o1 & Convert.NumberInfo.IsPhpArray) != (o2 & Convert.NumberInfo.IsPhpArray))
                    )
                {
                    PhpException.UnsupportedOperandTypes();
                    return 0;
                }

                // both are PhpArray
                Debug.Assert(x is PhpArray && y is PhpArray);
                return ((PhpArray)((PhpArray)x).DeepCopy()).Unite((PhpArray)((PhpArray)y).DeepCopy());
            }

            // at least one operand is convertible to a double:
            if ((info & Convert.NumberInfo.Double) != 0)
                return dx + dy;

            // 
            long rl = unchecked(lx + ly);

            if ((lx & LONG_SIGN_MASK) != (rl & LONG_SIGN_MASK) &&   // result has different sign than x
                (lx & LONG_SIGN_MASK) == (ly & LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow:
                return dx + dy;
            }
            else
            {
                // int to long overflow check
                int il = unchecked((int)rl);
	            if ( il == rl )
                    return il;
                    
                // we need long
                return rl;
            }
        }

        /// <summary>
        /// Implements '+' operator optimized for addition with integer literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The result of type <see cref="int"/> or <see cref="double"/>.
        /// </returns>
        /// <exception cref="PhpException">Addition is not supported on the types of operands specified.</exception>
        [Emitted]
        public static object Add(object x, int y)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;
            Convert.NumberInfo info_x;

            // converts x to a number:
            info_x = Convert.ObjectToNumber(x, out ix, out lx, out dx);

            if ((info_x & (Convert.NumberInfo.Unconvertible|Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            if ((info_x & Convert.NumberInfo.Double) != 0)
                return dx + y;

            try
            {
                long rl = lx + y;

                // int to long overflow check
                int il = unchecked((int)rl);
                if (il == rl)
                    return il;

                // we need long
                return rl;
            }
            catch (OverflowException)
            {
                return dx + y;
            }
        }

        /// <summary>
        /// Implements '+' operator optimized for addition with double literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The result of addition.
        /// </returns>
        /// <exception cref="PhpException">Addition is not supported on the types of operands specified.</exception>
        [Emitted]
        public static double Add(object x, double y)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;

            // converts x to a number:
            if ((Convert.ObjectToNumber(x, out ix, out lx, out dx) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            return dx + y;
        }

        /// <summary>
        /// Implements '+' operator optimized for addition with double literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The result of addition.
        /// </returns>
        /// <exception cref="PhpException">Addition is not supported on the types of operands specified.</exception>
        [Emitted]
        public static double Add(double x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            double dy;
            int iy;
            long ly;

            // converts x to a number:
            if ((Convert.ObjectToNumber(y, out iy, out ly, out dy) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            return x + dy;
        }

        #endregion

        #region Subtraction

        /// <summary>
        /// Implements binary '-' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">Subtraction is not supported on the types of operands specified.</exception>
        [Emitted]
        public static object Subtract(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            double dx, dy;
            int ix, iy;
            long lx, ly;
            Convert.NumberInfo info;

            // converts x and y to numbers:
            info = Convert.ObjectToNumber(x, out ix, out lx, out dx) | Convert.ObjectToNumber(y, out iy, out ly, out dy);

            if ((info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            // at least one operand is convertible to a double:
            if ((info & Convert.NumberInfo.Double) != 0)
                return dx - dy;

            // 
            long rl = unchecked(lx - ly);

            if ((lx & LONG_SIGN_MASK) != (rl & LONG_SIGN_MASK) &&   // result has different sign than x
                (lx & LONG_SIGN_MASK) != (ly & LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow:
                return dx - dy;
            }
            else
            {
                // int to long overflow check
                int il = unchecked((int)rl);
                if (il == rl)
                    return il;

                // we need long
                return rl;
            }
        }

        /// <summary>
        /// Implements binary '-' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="iy">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">Subtraction is not supported on the types of operands specified.</exception>
        [Emitted]
        public static object Subtract(object x, int iy)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;
            Convert.NumberInfo info;

            // converts x and y to numbers:
            info = Convert.ObjectToNumber(x, out ix, out lx, out dx);

            if ((info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            // at least one operand is convertible to a double:
            if ((info & Convert.NumberInfo.Double) != 0)
                return dx - (double)iy;

            // 
            long rl = unchecked(lx - iy);

            if ((lx & LONG_SIGN_MASK) != (rl & LONG_SIGN_MASK) &&   // result has different sign than x
                (lx & LONG_SIGN_MASK) != ((long)iy & LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow:
                return dx - (double)iy;
            }
            else
            {
                // int to long overflow check
                int il = unchecked((int)rl);
                if (il == rl)
                    return il;

                // we need long
                return rl;
            }
        }

        /// <summary>
        /// Implements binary '-' operator optimized for subtraction from an integer literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">Subtraction is not supported on the types of operands specified.</exception>
        [Emitted]
        public static object Subtract(int x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            double dy;
            int iy;
            long ly;
            Convert.NumberInfo info_y;

            // converts x to a number:
            info_y = Convert.ObjectToNumber(y, out iy, out ly, out dy);

            if ((info_y & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            if ((info_y & Convert.NumberInfo.Double) != 0)
                return (double)x - dy;

            try
            {
                long rl = (long)x - ly;

                // int to long overflow check
                int il = unchecked((int)rl);
                if (il == rl)
                    return il;

                // we need long
                return rl;
            }
            catch (OverflowException)
            {
                return (double)x - dy;
            }
        }

        /// <summary>
        /// Implements binary '-' operator optimized for subtraction from a double literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <exception cref="PhpException">Subtraction is not supported on the types of operands specified.</exception>
        [Emitted]
        public static double Subtract(double x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            double dy;
            int iy;
            long ly;

            // converts x to a number:
            if ((Convert.ObjectToNumber(y, out iy, out ly, out dy) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            return x - dy;
        }

        #endregion

        #region Unary plus & minus

        /// <summary>
        /// Implements unary '-' operator.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static object Minus(object x)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;
            
            switch (Convert.ObjectToNumber(x, out ix, out lx, out dx) & (Convert.NumberInfo.TypeMask | Convert.NumberInfo.IsPhpArray))  // IsPhpArray causes unsupported_operand_types
            {
                case Convert.NumberInfo.Integer:
                    if (ix == Int32.MinValue)
                        return -(long)Int32.MinValue;
                    else
                        return -ix;

                case Convert.NumberInfo.LongInteger:
                    if (lx == Int64.MinValue)
                        return -(double)Int64.MinValue;
                    else
                    {
                        if (lx == (-(long)Int32.MinValue))
                            return (int)Int32.MinValue;

                        return -lx;
                    }

                case Convert.NumberInfo.Double:
                    return -dx;
            }

            PhpException.UnsupportedOperandTypes();
            return 0;
        }

        /// <summary>
        /// Implements unary '+' operator.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static object Plus(object x)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;
              // IsPhpArray causes unsupported_operand_types
            switch (Convert.ObjectToNumber(x, out ix, out lx, out dx) & (Convert.NumberInfo.TypeMask | Convert.NumberInfo.IsPhpArray))  // IsPhpArray causes unsupported_operand_types
            {
                case Convert.NumberInfo.Integer: return ix;
                case Convert.NumberInfo.LongInteger: return lx;
                case Convert.NumberInfo.Double: return dx;
            }

            PhpException.UnsupportedOperandTypes();
            return 0;
        }

        #endregion

        #region Division

        /// <summary>
        /// Implements the binary '/' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">Division is not supported on the types of operands specified.</exception>
        /// <exception cref="PhpException">Division by zero.</exception>
        [Emitted]
        public static object Divide(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            double dx, dy;
            int ix, iy;
            long lx, ly;
            Convert.NumberInfo info;

            info = Convert.ObjectToNumber(x, out ix, out lx, out dx) | Convert.ObjectToNumber(y, out iy, out ly, out dy);

            if ((info & Convert.NumberInfo.IsPhpArray) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }

            // at least one operand was converted to double:
            if ((info & Convert.NumberInfo.Double) != 0)
                return dx / dy;

            // division by zero:
            if (iy == 0)
            {
                Debug.Assert(ly == 0);
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("division_by_zero"));
                return false;
            }

            // overflow to double:
            if (iy == -1 && lx == Int64.MinValue)
                return -(double)Int64.MinValue;

            long reminder;
            long result = MathEx.DivRem(lx, ly, out reminder);

            if (reminder != 0)
                return dx / dy;

            // int to long overflow check
            int il = unchecked((int)result);
            if (il == result)
                return il;

            // we need long
            return result;
        }

        /// <summary>
        /// Implements the binary '/' operator optimized for division by an integer literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">Division is not supported on the types of operands specified.</exception>
        /// <exception cref="DivideByZeroException">Division by zero.</exception>
        [Emitted]
        public static object Divide(object x, int y)
        {
            Debug.Assert(y != 0, "Compiler should check this");
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;
            Convert.NumberInfo info_x;

            info_x = Convert.ObjectToNumber(x, out ix, out lx, out dx);

            // at least one operand was converted to double:
            if ((info_x & Convert.NumberInfo.Double) != 0)
                return dx / y;

            if ((info_x & Convert.NumberInfo.IsPhpArray) != 0)
            {   // test PhpArray, after Double (PhpArray is not converted to Double and we may spare this test)
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }
            
            // overflow to double:
            if (y == -1 && lx == Int64.MinValue)
                return -(double)Int64.MinValue;

            long reminder;
            long result = MathEx.DivRem(lx, y, out reminder);

            if (reminder != 0)
                return dx / y;

            // int to long overflow check
            int il = unchecked((int)result);
            if (il == result)
                return il;

            // we need long
            return result;
        }

        /// <summary>
        /// Implements the binary '/' operator optimized for division by a double literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of division.</returns>
        /// <exception cref="PhpException">Division is not supported on the types of operands specified.</exception>
        /// <exception cref="PhpException">Division by zero.</exception>
        [Emitted]
        public static double Divide(object x, double y)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;

            if ((Convert.ObjectToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }

            return dx / y;
        }

        /// <summary>
        /// Implements the binary '/' operator optimized for division of an integer literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">Division is not supported on the types of operands specified.</exception>
        /// <exception cref="PhpException">Division by zero.</exception>
        [Emitted]
        public static object Divide(int x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            double dy;
            int iy;
            long ly;
            Convert.NumberInfo info_y;

            info_y = Convert.ObjectToNumber(y, out iy, out ly, out dy);

            // at least one operand was converted to double:
            if ((info_y & Convert.NumberInfo.Double) != 0)
                return (double)x / dy;

            if ((info_y & Convert.NumberInfo.IsPhpArray) != 0)  // test PhpArray (after test for Double, PhpArray cannot be double, and it is rare case)
            {
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }

            long reminder;

            // division by zero:
            if (iy == 0)
            {
                Debug.Assert(ly == 0);
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("division_by_zero"));
                return false;
            }
            long result = MathEx.DivRem(x, ly, out reminder);

            if (reminder != 0)
                return (double)x / dy;

            // int to long overflow check
            int il = unchecked((int)result);
            if (il == result)
                return il;

            // we need long
            return result;
        }

        /// <summary>
        /// Implements the binary '/' operator optimized for division of a double literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <exception cref="PhpException">Division is not supported on the types of operands specified.</exception>
        /// <exception cref="PhpException">Division by zero.</exception>
        [Emitted]
        public static object Divide(double x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            double dy;
            int iy;
            long ly;

            if ((Convert.ObjectToNumber(y, out iy, out ly, out dy) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }

            return x / dy;
        }

        #endregion

        #region Multiplication

        /// <summary>
        /// Implements binary '*' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static object Multiply(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            double dx, dy;
            int ix, iy;
            long lx, ly;
            Convert.NumberInfo info;

            // converts x and y to numbers:
            info = Convert.ObjectToNumber(x, out ix, out lx, out dx) | Convert.ObjectToNumber(y, out iy, out ly, out dy);

            if ((info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            // at least one operand is convertible to a double:
            if ((info & Convert.NumberInfo.Double) != 0)
                return dx * dy;

            long rl;
            try
            {
                rl = lx * ly;
            }
            catch (OverflowException)
            {
                // we need double
                return dx * dy;
            }

            // int to long overflow check
            int il = unchecked((int)rl);
            if (il == rl)
                return il;

            // we need long
            return rl;
        }

        

        /// <summary>
        /// Implements binary '*' operator optimized for multiplication with an integer literal.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result of type <see cref="int"/> or <see cref="double"/>.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static object Multiply(object x, int y)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;
            Convert.NumberInfo info_x;

            // converts x to a number:
            info_x = Convert.ObjectToNumber(x, out ix, out lx, out dx);

            if ((info_x & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }

            if ((info_x & Convert.NumberInfo.Double) != 0)
                return dx * y;

            try
            {
                long rl = lx * y;

                // int to long overflow check
                int il = unchecked((int)rl);
                if (il == rl)
                    return il;

                // we need long
                return rl;
            }
            catch (OverflowException)
            {
                return dx * y;
            }
        }

        /// <summary>
        /// Implements binary '*' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static double Multiply(object x, double y)
        {
            Debug.Assert(!(x is PhpReference));

            double dx;
            int ix;
            long lx;

            // converts x to a number:
            if ((Convert.ObjectToNumber(x, out ix, out lx, out dx) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            return dx * y;
        }

        /// <summary>
        /// Implements binary '*' operator.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The result.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static double Multiply(double x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            double dy;
            int iy;
            long ly;

            // converts x to a number:
            if ((Convert.ObjectToNumber(y, out iy, out ly, out dy) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0.0;
            }

            return x * dy;
        }

        #endregion

        #region Remainder

        /// <summary>
        /// Implements binary % operator.
        /// </summary>
        /// <param name="x">The first operand of an arbitrary Phalanger type except for <see cref="PhpArray"/> and <see cref="DObject"/>.</param>
        /// <param name="y">The second operand of an arbitrary Phalanger type except for <see cref="PhpArray"/> and <see cref="DObject"/>.</param>
        /// <returns>The result.</returns>
        /// <remarks>
        /// Both operands are converted to integers by <see cref="Convert.ObjectToInteger"/> and then the remainder is computed.
        /// </remarks>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        [Emitted]
        public static object Remainder(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            int iy, ix;
            long ly, lx;
            double dy, dx;

            if ((Convert.ObjectToNumber(y, out iy, out ly, out dy) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return false;
            }

            if (iy == 0)
            {
                Debug.Assert(ly == 0);
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("division_by_zero"));
                return false;
            }

            // prevents OverflowException:
            if (iy == -1)
            {
                Debug.Assert(ly == -1);
                return 0;
            }

            if ((Convert.ObjectToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return false;
            }

            long result = lx % ly;
            // int to long overflow check
            int il = unchecked((int)result);
            if (il == result)
                return il;

            // we need long
            return result;
        }

        /// <summary>
        /// Implements binary '%' operator optimized for division by an integer literal.
        /// </summary>
        /// <param name="x">The first operand of an arbitrary Phalanger type except for <see cref="PhpArray"/> and <see cref="DObject"/>.</param>
        /// <param name="y">The second operand of an arbitrary Phalanger type except for <see cref="PhpArray"/> and <see cref="DObject"/>.</param>
        /// <returns>Both operands are converted to integers by <see cref="Convert.ObjectToInteger"/> and then the remainder is computed.</returns>
        /// <exception cref="PhpException">The operator is not supported on the type of operand specified.</exception>
        /// <exception cref="DivideByZeroException"><paramref name="y"/> is 0.</exception>
        [Emitted]
        public static object Remainder(object x, int y)
        {
            //Debug.Assert(y != 0, "Compiler should check this.");
            Debug.Assert(!(x is PhpReference));

            int ix;
            long lx;
            double dx;

            if (y == 0)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("division_by_zero"));
                return false;
            }

            // prevents OverflowException:
            if (y == -1) return 0;

            if ((Convert.ObjectToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                PhpException.UnsupportedOperandTypes();
                return 0;
            }

            long result = lx % y;
            // int to long overflow check
            int il = unchecked((int)result);
            if (il == result)
                return il;

            // we need long
            return result;
        }

        #endregion

        #endregion

        #region Incrementing/decrementing operators

        /// <summary>
        /// Implements '++' unary operator.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <returns>
        /// The incremented value of type <see cref="int"/>, <see cref="double"/> or <see cref="string"/>.
        /// </returns>
        /// <exception cref="PhpException">Incrementing is not supported on the type of operand specified. (Error)</exception>
        /// <remarks>
        /// Split to fast path (int32) and other types (<see cref="IncrementNonInt"/>) to be small enough
        /// to be inlined by JIT.
        /// </remarks>
        [Emitted]
        public static object Increment(object x)
        {
            Debug.Assert(!(x is PhpReference));

            if (x != null && x.GetType() == typeof(int))
            {
                int i = (int)x;
                return (i == int.MaxValue) ? (object)((long)int.MaxValue + 1) : (i+1);
            }
            else
            {
                return IncrementNonInt(x);
            }
        }

        /// <summary>
        /// Increments an operand which is surely not <see cref="int"/>.
        /// </summary>
        private static object IncrementNonInt(object x)
        {
            if (x == null)
                return 1;

            if (x.GetType() == typeof(long))
            {
                long i;
                return ((i = (long)x) == long.MaxValue) ? (object)((double)long.MaxValue + 1.0) : (object)unchecked(i + 1);
            }

            if (x.GetType() == typeof(double))
                return (double)x + 1;

            string s;
            if ((s = PhpVariable.AsString(x)) != null)
                return StringUtils.Increment(s);

            // PHP really doesn't do anything here:
            if (x.GetType() == typeof(bool))
                return x;

            // Other types are not supported (PHP returns x, but we want to prevent copying).
            // Although, it would be possible to return a deep copy of x it is quite strange to increment objects or arrays: 
            PhpException.Throw(PhpError.Error, CoreResources.GetString("unsupported_operand_type"));
            return 0;
        }

        /// <summary>
        /// Implements '--' unary operator.
        /// </summary>
        /// <param name="x">The operand.</param>
        /// <returns>
        /// The incremented value of type <see cref="int"/>, <see cref="double"/> or <see cref="string"/>.
        /// </returns>
        /// <exception cref="PhpException">Decrementing is not supported on the type of operand specified. (Error)</exception>
        [Emitted]
        public static object Decrement(object x)
        {
            string s;
            Debug.Assert(!(x is PhpReference));

            if (x == null)
                return null;

            if (x.GetType() == typeof(int))
            {
                int i = (int)x;
                if (i == int.MinValue)
                    return (long)i - 1.0;
                else
                    return i - 1;
            }

            if (x.GetType() == typeof(long))
            {
                long i = (long)x;
                if (i == long.MinValue)
                    return (double)i - 1.0;
                else
                    return i - 1;
            }

            if (x.GetType() == typeof(double))
                return (double)x - 1.0;

            if ((s = PhpVariable.AsString(x)) != null)
                return DecrementString(s);

            // PHP really doesn't do anything here:
            if (x.GetType() == typeof(bool))
                return x;

            // Other types are not supported (PHP returns x, but we want to prevent copying).
            // Although, it would be possible to return a deep copy of x it is quite strange to increment objects or arrays: 
            PhpException.Throw(PhpError.Error, CoreResources.GetString("unsupported_operand_type"));
            return 0;
        }

        /// <summary>
        /// Decrements a string.
        /// </summary>
        /// <param name="s">The string to decrement.</param>
        /// <returns>The result.</returns>
        private static object DecrementString(string/*!*/ s)
        {
            Debug.Assert(s != null);

            double dx;
            int ix;
            long lx;
            Convert.NumberInfo info;

            info = Convert.StringToNumber(s, out ix, out lx, out dx);

            if ((info & Convert.NumberInfo.IsNumber) != 0)
            {
                switch (info & Convert.NumberInfo.TypeMask)
                {
                    case Convert.NumberInfo.Double: return dx - 1.0;

                    case Convert.NumberInfo.Integer:
                        if (ix == int.MinValue)
                            return lx - 1.0;
                        else
                            return ix - 1;

                    case Convert.NumberInfo.LongInteger:
                        if (lx == long.MinValue)
                            return dx - 1.0;
                        else
                            return ix - 1;
                }
            }

            // does nothing with the "s":
            return s;
        }

        #endregion

        #region Bitwise operators

        /// <summary>
        /// Type of bitwise operation.
        /// </summary>
        public enum BitOp
        {
            /// <summary>Bitwise and binary operation.</summary>
            And,
            /// <summary>Bitwise or binary operation.</summary>
            Or,
            /// <summary>Bitwise xor binary operation.</summary>
            Xor
        };

        /// <summary>
        /// Performs bitwise binary operators.
        /// </summary>
        /// <param name="x">The first operand of an arbitrary PHP.NET type.</param>
        /// <param name="y">The sencond operand of an arbitrary PHP.NET type.</param>
        /// <param name="op">The type of the operation.</param>
        /// <returns>See the following table.</returns>
        /// <exception cref="ArgumentException">
        /// If the type of any operand is neither <see cref="String"/> nor <see cref="Byte"/>[] and it isn't convertible to an integer.
        /// </exception>
        [Emitted]
        public static object BitOperation(object x, object y, BitOp op)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            PhpBytes bx, by;

            if ((bx = PhpVariable.AsBytes(x)) == null || (by = PhpVariable.AsBytes(y)) == null)
            {
                // at least one of the operands is not string of characters nor string of bytes:
                long lx = Convert.ObjectToLongInteger(x);
                long ly = Convert.ObjectToLongInteger(y);
                long result;

                switch (op)
                {
                    case BitOp.And: result = lx & ly; break;
                    case BitOp.Or: result = lx | ly; break;
                    case BitOp.Xor: result = lx ^ ly; break;
                    default:
                        throw new ArgumentOutOfRangeException("op");
                }

                // int to long overflow check
                int il = unchecked((int)result);
                if (il == result)
                    return il;

                // we need long
                return result;
            }
            else
            {
                byte[] result;
                int length = (op == BitOp.Or) ? Math.Max(bx.Length, by.Length) : Math.Min(bx.Length, by.Length);

                // chooses the resulting array allocating a new one only if necessary;
                // if x or y has been converted from string to bytes and has the max. length it can be used for
                // storing a resulting array:
                if (!ReferenceEquals(bx, x) && bx.Length == length)
                    result = bx.Data;// bx is temporary PhpBytes instance, its internal data can be reused
                else if (!ReferenceEquals(by, y) && by.Data.Length == length)
                    result = by.Data;// by is temporary PhpBytes instance, its internal data can be reused
                else
                    result = new byte[length];

                return new PhpBytes(BitOperation(result, bx.ReadonlyData, by.ReadonlyData, op));
            }
        }

        /// <summary>
        /// Performs specified binary operation on arrays of bytes.
        /// </summary>
        /// <param name="result">An array where to store the result. Data previously stored here will be overwritten.</param>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand</param>
        /// <param name="op">The operation to perform.</param>
        /// <returns>The reference to the the <paramref name="result"/> array.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="op"/> has invalid value.</exception>
        private static byte[] BitOperation(byte[]/*!*/ result, byte[]/*!*/ x, byte[]/*!*/ y, BitOp op)
        {
            int min_length = Math.Min(x.Length, y.Length);

            Debug.Assert(result != null && x != null && y != null && result.Length >= min_length);

            switch (op)
            {
                case BitOp.And:

                    for (int i = 0; i < min_length; i++)
                        result[i] = unchecked((byte)(x[i] & y[i]));

                    // remaining bytes are ignored //          
                    break;

                case BitOp.Or:

                    for (int i = 0; i < min_length; i++)
                        result[i] = unchecked((byte)(x[i] | y[i]));

                    // copies remaining bytes from longer array:
                    if (x.Length > min_length)
                    {
                        if (x != result) Buffer.BlockCopy(x, min_length, result, min_length, result.Length - min_length);
                    }
                    else
                    {
                        if (y != result) Buffer.BlockCopy(y, min_length, result, min_length, result.Length - min_length);
                    }
                    break;

                case BitOp.Xor:

                    for (int i = 0; i < min_length; i++)
                        result[i] = unchecked((byte)(x[i] ^ y[i]));

                    // remaining bytes are ignored //
                    break;

                default:
                    throw new ArgumentOutOfRangeException("op");
            }
            return result;
        }

        /// <summary>
        /// Performs the '~' unary operator.
        /// </summary>
        /// <param name="x">The operand of type <see cref="Double"/>, <see cref="Int32"/>, <see cref="String"/> or <see cref="Byte"/>[].</param>
        /// <returns>See the following table.</returns>
        /// <include file='Doc/Operators.xml' path='docs/operator[@name="BitNot"]/*'/>
        /// <exception cref="PhpException">If <paramref name="x"/> has illegal type.</exception>
        [Emitted]
        public static object BitNot(object x)
        {
            Debug.Assert(!(x is PhpReference));

            if (x == null)
                return null;

            PhpBytes bx;

            if (x.GetType() == typeof(int))
                return ~(int)x;

            if (x.GetType() == typeof(long))
                return ~(long)x;

            if (x.GetType() == typeof(double))
                return ~unchecked((long)(double)x);

            if ((bx = PhpVariable.AsBytes(x)) != null)
            {
                // allocates an array for result if it is needed:
                PhpBytes result = (ReferenceEquals(x, bx)) ? new PhpBytes(new byte[bx.Length]) : bx;

                for (int i = 0; i < result.Length; i++)
                    result.Data[i] = unchecked((byte)~bx.ReadonlyData[i]);

                return result;
            }


            PhpException.UnsupportedOperandTypes();
            return null;
        }

        /// <summary>
        /// Performs shift left binary operation.
        /// </summary>
        /// <param name="x">The first argument of an arbitrary PHP.NET type.</param>
        /// <param name="y">The second argument of an arbitrary PHP.NET type.</param>
        /// <returns>The <paramref name="x"/> shifted by <paramref name="y"/> modulo 32 bits.</returns>
        [Emitted]
        public static object ShiftLeft(object x, object y)
        {
            int ix;
            long lx;
            double dx;

            int iy = Convert.ObjectToInteger(y);
            Convert.ObjectToNumber(x, out ix, out lx, out dx);

            long rl = unchecked(lx << iy);

            // int -> long overflow?
            int il = unchecked((int)rl);
            if (il == rl)
                return il;  // int is enought
            
            return rl;      // long result
        }

        /// <summary>
        /// Performs shift right binary operation.
        /// </summary>
        /// <param name="x">The first argument of an arbitrary PHP.NET type.</param>
        /// <param name="y">The second argument of an arbitrary PHP.NET type.</param>
        /// <returns>The <paramref name="x"/> shifted by <paramref name="y"/> modulo 32 bits.</returns>
        [Emitted]
        public static object ShiftRight(object x, object y)
        {
            int ix;
            long lx;
            double dx;

            int iy = Convert.ObjectToInteger(y);
            Convert.ObjectToNumber(x, out ix, out lx, out dx);

            long rl = unchecked(lx >> iy);

            // long -> int?
            int il = unchecked((int)rl);
            if (il == rl)
                return il;  // int is enought

            return rl;      // long result
        }

        #endregion

        #region String operators

        #region Concat

        /// <summary>
        /// Converts <paramref name="x"/> to most suitable PHP representation of string.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static object AsAnyString(object x)
        {
            if (x == null) return string.Empty;
            if (x.GetType() == typeof(string) ||
                x.GetType() == typeof(PhpBytes) ||
                x.GetType() == typeof(PhpString))
                return x;

            return Convert.ObjectToString(x);
        }

        /// <summary>
        /// Concatenates two strings or strings of bytes.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).</returns>
        [Emitted]
        public static object Concat(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            // catch null references:
            if (x == null) return AsAnyString(y);
            if (y == null) return AsAnyString(x);

            // concatenate surely not null values:
            if (x.GetType() == typeof(string) && y.GetType() == typeof(string))
                return string.Concat((string)x, (string)y);

            if (x.GetType() == typeof(PhpBytes))
            {
                if (y.GetType() == typeof(PhpBytes))
                    return PhpBytes.Concat((PhpBytes)x, (PhpBytes)y);

                // bytes.object
                return PhpBytes.Concat((PhpBytes)x, Convert.ObjectToPhpBytes(y));
            }
            else if (y.GetType() == typeof(PhpBytes))
            {
                // object.bytes
                return PhpBytes.Concat(Convert.ObjectToPhpBytes(x), (PhpBytes)y);
            }
            else
            {
                // object.object:
                return String.Concat(Convert.ObjectToString(x), Convert.ObjectToString(y));
            }
        }

        /// <summary>
        /// Concatenates strings or strings of bytes optimized for concatenation with a string.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// A concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static object Concat(object x, string y)
        {
            Debug.Assert(!(x is PhpReference));
            Debug.Assert(y != null);

            if (x == null)
                return y;

            if (x.GetType() == typeof(string))
                return String.Concat((string)x, y);

            if (x.GetType() == typeof(PhpBytes))
                return PhpBytes.Concat((PhpBytes)x, new PhpBytes(y));

            if (x.GetType() == typeof(PhpString))
            {
                var bld = ((PhpString)x).StringBuilder;
                if (bld.Length == 0) return y;
                return String.Concat(bld.ToString(), y);
            }
            
            return String.Concat(Convert.ObjectToString(x), y);
        }

        /// <summary>
        /// Concatenates two strings or strings of bytes optimized for concatenation with a string.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// A concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static object Concat(string x, object y)
        {
            Debug.Assert(!(y is PhpReference));
            Debug.Assert(x != null);

            if (y == null)
                return x;

            if (y.GetType() == typeof(string))
                return String.Concat(x, (string)y);
            
            if (y.GetType() == typeof(PhpBytes))
                return PhpBytes.Concat(new PhpBytes(x), (PhpBytes)y);
            
            return String.Concat(x, Convert.ObjectToString(y));
        }

        /// <summary>
        /// Concatenates objects contained in a specified array.
        /// </summary>
        /// <param name="args">An array of objects to concatenate.</param>
        /// <returns>
        /// If any of the arguments are of type <see cref="PhpBytes"/> the result is also of type <see cref="PhpBytes"/>, 
        /// otherwise each argument is converted to a string by <see cref="Convert.ObjectToString"/> and 
        /// the result is a string.
        /// </returns>
        [Emitted]
        public static object Concat(params object[]/*!*/args)
        {
            int count = args.Length;
            if (count == 0) return null;

            int startIndex = ArrayUtils.TakeWhileCount(args, obj => obj == null);// skip nulls
            if (startIndex >= count) return null;

            // if some element is PhpBytes:
            if (ArrayEx.Exists(args, (obj) => (obj != null && obj.GetType() == typeof(PhpBytes))))
            {
                // converts all items to PhpBytes (or nulls) and concatenate:
                return PhpBytes.Concat(ArrayEx.ConvertAll<object, PhpBytes>(args, x =>
                {
                    return (x != null) ? Convert.ObjectToPhpBytes(x) : null;
                }), startIndex, count - startIndex);
            }

            // none of the elements is PhpBytes,
            // convert all items to string and sum their total length:
            int length = 0;
            string[] args_string = ArrayEx.ConvertAll<object, string>(args, x =>
            {
                if (x != null)
                {
                    string str;
                    length += (str = Convert.ObjectToString(x)).Length;
                    return str;
                }
                else
                    return null;
            });

            if (length == 0)
                return string.Empty;

            // concatenate items via StringBuilder:
            StringBuilder sb = new StringBuilder(length, length);
            foreach (string str in args_string)
                sb.Append(str);

            return sb.ToString();
        }

        #endregion

        #region Append

        /// <summary>
        /// Concatenates two strings or strings of bytes.
        /// </summary>
        /// <param name="x">The first operand which will be appended to.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// A single-referenced concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static object Append(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            // catch null references:
            if (x == null) return y;
            if (y == null) return AsAnyString(x);

            //
            if (x.GetType() == typeof(PhpBytes))
            {
                if (y.GetType() == typeof(PhpBytes))
                {
                    // bytes.bytes:
                    return PhpBytes.Concat((PhpBytes)x, (PhpBytes)y);
                }

                // bytes.object:
                return PhpBytes.Concat((PhpBytes)x, Convert.ObjectToPhpBytes(y));
            }
            else if (y.GetType() == typeof(PhpBytes))
            {
                // object.bytes:
                return PhpBytes.Concat(Convert.ObjectToPhpBytes(x), (PhpBytes)y);
            }
            else if (x.GetType() == typeof(PhpString))
            {
                // builder.string:
                return ((PhpString)x).Append(Convert.ObjectToString(y));
            }
            else
            {
                // object.object:
                return new PhpString(Convert.ObjectToString(x), Convert.ObjectToString(y));
            }
        }

        /// <summary>
        /// Concatenates two strings or strings of bytes optimized for concatenation with a string.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The single-referenced concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static object Append(object x, string y)
        {
            Debug.Assert(!(x is PhpReference));

            if (object.ReferenceEquals(x, null)) return y;

            if (x.GetType() == typeof(PhpString))
            {
                // builder.string:
                return ((PhpString)x).Append(y);
            }
            else if (x.GetType() == typeof(PhpBytes))
            {
                return PhpBytes.Concat((PhpBytes)x, new PhpBytes(y));
            }
            else
            {
                // object.string:
                return new PhpString(Convert.ObjectToString(x), y);
            }
        }

        public static object Append(object x, params object[] args)
        {
            // todo
            return null;
        }

        #endregion

        #region Prepend

        /// <summary>
        /// Prepends one value with the other.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// A single-referenced concatenation of the <paramref name="y"/> and <paramref name="x"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static object Prepend(object x, object y)
        {
            Debug.Assert(!(x is PhpReference) && !(y is PhpReference));

            PhpString sx;
            PhpBytes bx, by;

            if ((bx = x as PhpBytes) != null)
            {
                if ((by = y as PhpBytes) != null)
                {
                    // bytes.bytes:
                    return PhpBytes.Concat(by, bx);
                }
                else
                {
                    // bytes.object:
                    return PhpBytes.Concat(Convert.ObjectToPhpBytes(y), bx);
                }
            }
            else if ((by = y as PhpBytes) != null)
            {
                // object.bytes:
                return PhpBytes.Concat(by, Convert.ObjectToPhpBytes(x));
            }
            if ((sx = x as PhpString) != null)
            {
                // builder.string:
                return sx.Prepend(Convert.ObjectToString(y));
            }
            else
            {
                // object.object:
                return new PhpString(Convert.ObjectToString(y), Convert.ObjectToString(x));
            }
        }

        /// <summary>
        /// Prepends one value with the other.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The single-referenced concatenation of the <paramref name="y"/> and <paramref name="x"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static object Prepend(object x, string y)
        {
            Debug.Assert(!(x is PhpReference));
            PhpString sx;
            PhpBytes bx;

            if ((sx = x as PhpString) != null)
            {
                // builder.string:
                return sx.Prepend(y);
            }
            else if ((bx = x as PhpBytes) != null)
            {
                return PhpBytes.Concat(new PhpBytes(y), bx);
            }
            {
                // object.string:
                return new PhpString(y, Convert.ObjectToString(x));
            }
        }

        public static object Prepend(object x, params object[] args)
        {
            // todo
            return null;
        }

        #endregion

        #endregion

        #region Variables Table Access Operators

        [Emitted]
        public static object GetVariableUnchecked(ScriptContext/*!*/ context, Dictionary<string, object> locals,
            string/*!*/ name)
        {
            Debug.Assert(name != null);
            object item;

            if (locals != null)
            {
                // global code included into a method => use the locals table only:
                if (locals.TryGetValue(name, out item))
                    return PhpVariable.Dereference(item);
                else
                    return null;
            }
            else
            {
                // true global code => work with globals:
                PhpArray globals = context.AutoGlobals.Globals.Value as PhpArray;

                if (globals != null && globals.TryGetValue(name, out item))
                    return PhpVariable.Dereference(item);
                else
                    return null;
            }
        }

        [Emitted]
        public static object GetVariable(ScriptContext/*!*/ context, Dictionary<string, object> locals,
            string/*!*/ name)
        {
            Debug.Assert(name != null);
            object item;

            if (locals != null)
            {
                // included in method //

                if (locals.TryGetValue(name, out item))
                    return PhpVariable.Dereference(item);
            }
            else
            {
                // true global code //

                PhpArray globals = context.AutoGlobals.Globals.Value as PhpArray;

                if (globals != null && globals.TryGetValue(name, out item))
                    return PhpVariable.Dereference(item);
            }

            // variable is undefined:
            PhpException.UndefinedVariable(name);
            return null;
        }

        [Emitted]
        public static PhpReference GetVariableRef(ScriptContext/*!*/ context, Dictionary<string, object> locals,
            string/*!*/ name)
        {
            Debug.Assert(name != null);

            if (locals != null)
            {
                // included in method //

                PhpReference result;

                object item;
                if (locals.TryGetValue(name, out item))
                {
                    result = item as PhpReference;

                    if (result != null)
                        return result;
                }
                else
                {
                    item = null;
                }

                // it is correct to box the item without making a deep copy since there was a single pointer on item
                // before this operation (by invariant) and there will be a single one after the operation as well:
                locals[name] = result = new PhpReference(item);

                return result;
            }
            else
            {
                // true global code //

                PhpArray globals = context.AutoGlobals.Globals.Value as PhpArray;
                PhpReference result;
                object item = null;

                if (globals == null)
                {
                    context.AutoGlobals.Globals.Value = globals = new PhpArray();
                }
                else if (globals.TryGetValue(name, out item))
                {
                    result = item as PhpReference;

                    if (result != null)
                        return result;
                }

                // it is correct to box the item without making a deep copy since there was a single pointer on item
                // before this operation (by invariant) and there will be a single one after the operation as well:
                globals[name] = result = new PhpReference(item);

                return result;
            }
        }

        [Emitted]
        public static void SetVariable(ScriptContext/*!*/ context, Dictionary<string, object> locals,
            string/*!*/ name, object value)
        {
            Debug.Assert(name != null && !(value is PhpReference));

            if (locals != null)
            {
                // included in method //

                object item;
                PhpReference ref_item;
                if (locals.TryGetValue(name, out item) && (ref_item = item as PhpReference) != null)
                    ref_item.Value = value;
                else
                    locals[name] = value;
            }
            else
            {
                // true global code //

                PhpArray globals = context.AutoGlobals.Globals.Value as PhpArray;

                if (globals == null)
                {
                    context.AutoGlobals.Globals.Value = globals = new PhpArray();
                    globals.Add(name, value);
                    return;
                }

                object item;
                PhpReference ref_item;
                if (globals.TryGetValue(name, out item) && (ref_item = item as PhpReference) != null)
                    ref_item.Value = value;
                else
                    globals[name] = value;
            }
        }

        [Emitted]
        public static void SetVariableRef(ScriptContext/*!*/ context, Dictionary<string, object> locals,
            string/*!*/ name, PhpReference reference)
        {
            Debug.Assert(name != null);

            if (locals != null)
            {
                locals[name] = reference;
            }
            else
            {
                PhpArray globals = context.AutoGlobals.Globals.Value as PhpArray;

                if (globals == null)
                    context.AutoGlobals.Globals.Value = globals = new PhpArray();

                globals[name] = reference;
            }
        }

        [Emitted]
        public static void UnsetVariable(ScriptContext/*!*/ context, Dictionary<string, object> locals,
            string/*!*/ name)
        {
            Debug.Assert(name != null);

            if (locals != null)
            {
                locals.Remove(name);
            }
            else
            {
                PhpArray globals = context.AutoGlobals.Globals.Value as PhpArray;

                if (globals != null)
                    globals.Remove(name);
            }
        }

        #endregion

        #region Helpers: IsEmptyForEnsure, CheckStringIndexRange, IsCallable

        /// <summary>
        /// Decides whether a variable is empty i.e. should be replaced by new array or object.
        /// </summary>
        /// <remarks>
        /// A variable is considered to be empty for ensure routines and item and property operators if 
        /// it is either <B>null</B> reference, an empty string, an empty string of bytes,
        /// <B>false</B>, 0 or 0.0 (PHP5 treats 0 and 0.0 as non-empty-for-ensure but it will probably change).
        /// </remarks>
        public static bool IsEmptyForEnsure(object var)
        {
            return
                var == null ||
                (var.GetType() == typeof(string) && (string)var == string.Empty) ||
                (var.GetType() == typeof(bool) && (bool)var == false) ||
                (var.GetType() == typeof(PhpString) && ((PhpString)var).Length == 0) ||
                (var.GetType() == typeof(PhpBytes) && ((PhpBytes)var).Length == 0) ||
                (var.GetType() == typeof(int) && (int)var == 0) ||
                (var.GetType() == typeof(double) && (double)var == 0.0) ||
                (var.GetType() == typeof(long) && (long)var == 0);
        }

        /// <summary>
        /// Verifies that the contents of a variable can be called as a function.
        /// </summary>
        /// <param name="caller">Current class context.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="syntaxOnly">If <B>true</B>, it is only checked that has <pararef name="variable"/>
        /// a valid structure to be used as a callback. if <B>false</B>, the existence of the function (or
        /// method) is also verified.</param>
        /// <returns><B>true</B> if <paramref name="variable"/> denotes a function, <B>false</B>
        /// otherwise.</returns>
        [Emitted]
        public static bool IsCallable(object variable, DTypeDesc caller, bool syntaxOnly)
        {
            PhpCallback callback = PHP.Core.Convert.ObjectToCallback(variable, true);
            if (callback == null || callback.IsInvalid) return false;

            return (syntaxOnly ? true : callback.Bind(true, caller, null));
        }

        public static bool CheckStringIndexRange(int index, int length, bool quiet)
        {
            // index is negative => notice:
            if (index < 0)
            {
                if (!quiet) PhpException.Throw(PhpError.Warning, CoreResources.GetString("illegal_string_offset", index));
                return false;
            }

            // index is greater than length: 
            if (index >= length)
            {
                if (!quiet) PhpException.Throw(PhpError.Notice, CoreResources.GetString("uninitialized_string_offset", index));
                return false;
            }

            return true;
        }

        #endregion

        #region Item Operators

        #region GetItem

        /// <summary>
        /// Kinds of <see cref="GetItem"/> operator.
        /// </summary>
        public enum GetItemKinds
        {
            /// <summary>Item getter with notice reporting.</summary>
            Get,
            /// <summary>Quite item getter.</summary>
            QuietGet,
            /// <summary>Item is loaded to be checked by "isset".</summary>
            Isset,
            /// <summary>Item is loaded to be checked by "empty".</summary>
            Empty
        }

        /// <summary>
        /// Gets an item of an array or a character of a string. Used in the read context chain.
        /// </summary>
        /// <param name="var">The variable which item to get.</param>
        /// <param name="key">The index of the item.</param>
        /// <param name="kind">The kind of operator.</param>
        /// <returns>The item.</returns>
        /// <remarks><para>Pattern: ... = var[index]</para></remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is a string and <paramref name="key"/> is negative integer (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is an array and <paramref name="key"/> is an illegal (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is an array and <paramref name="key"/> is not contained in it (Notice).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is a string and <paramref name="key"/> is greater or equal to its length (Notice).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is an <see cref="DObject"/> (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is a scalar (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is empty (Notice).</exception>
        /// <include file='Doc/Operators.xml' path='docs/operator[@name="GetItem"]/*'/>
        [Emitted]
        public static object GetItem(object var, object key, GetItemKinds kind)
        {
            Debug.Assert(!(var is PhpReference) && !(key is PhpReference));

            // an item of a PhpArray (fast check):
            if (var != null && var.GetType() == typeof(PhpArray))   // derived types checked in Epilogue
                return ((PhpArray)var).GetArrayItem(key, kind != GetItemKinds.Get);
            else
                return GetItemNonPhpArray(var, key, kind);
        }

        private static object GetItemNonPhpArray(object var, object key, GetItemKinds kind)
        {
            // handle null reference:
            if (var == null)
                return null;

            bool quiet = kind != GetItemKinds.Get;
            int index;

            // a character of a string:
            if (var.GetType() == typeof(string))
                return (CheckStringIndexRange(index = Convert.ObjectToInteger(key), ((string)var).Length, quiet)) ? ((string)var)[index].ToString() : null;

            // a character of a PhpString:
            if (var.GetType() == typeof(PhpString))
                return (CheckStringIndexRange(index = Convert.ObjectToInteger(key), ((PhpString)var).Length, quiet)) ? ((PhpString)var).GetCharUnchecked(index).ToString() : null;

            // a byte of a string of bytes:
            if (var.GetType() == typeof(PhpBytes))
                return (CheckStringIndexRange(index = Convert.ObjectToInteger(key), ((PhpBytes)var).Length, quiet)) ? new PhpBytes(new byte[] { ((PhpBytes)var)[index] }) : null;

            return GetItemEpilogue(var, key, kind);
        }

        [Emitted]
        public static object GetItem(object var, int key, GetItemKinds kind)
        {
            Debug.Assert(!(var is PhpReference));

            if (var != null && var.GetType() == typeof(PhpArray))   // derived types checked later in Epilogue
                // an item of a PhpArray:
                return ((PhpArray)var).GetArrayItem(key, kind != GetItemKinds.Get);
            else
                // the rest:
                return GetItemEpilogue(var, key, kind);
        }

        private static object GetItemEpilogue(object var, int key, GetItemKinds kind)
        {
            Debug.Assert(!(var is PhpReference));
            Debug.Assert(var == null || var.GetType() != typeof(PhpArray));

            // handle null reference:
            if (var == null)
                return null;

            //
            bool quiet = kind != GetItemKinds.Get;

            // a character of a string:
            if (var.GetType() == typeof(string))
                return (CheckStringIndexRange(key, ((string)var).Length, quiet)) ? ((string)var)[key].ToString() : null;

            // a character of a PhpString:
            if (var.GetType() == typeof(PhpString))
                return (CheckStringIndexRange(key, ((PhpString)var).Length, quiet)) ? ((PhpString)var).GetCharUnchecked(key).ToString() : null;

            // a byte of a string of bytes:
            if (var.GetType() == typeof(PhpBytes))
                return (CheckStringIndexRange(key, ((PhpBytes)var).Length, quiet)) ? new PhpBytes(new byte[] { ((PhpBytes)var)[key] }) : null;

            // general GetItem epilogue:
            return GetItemEpilogue(var, (object)key, kind);
        }

        [Emitted]
        public static object GetItem(object var, string/*!*/ key, GetItemKinds kind)
        {
            Debug.Assert(!(var is PhpReference) && key != null);

            if (var != null && var.GetType() == typeof(PhpArray))   // derived types checked in Epilogue
                return ((PhpArray)var).GetArrayItem(key, kind != GetItemKinds.Get);

            return GetStringItemEpilogue(var, key, kind);
        }

        [Emitted]
        public static object GetItemExact(object var, string/*!*/ key, GetItemKinds kind, int hashcode)
        {
            Debug.Assert(!(var is PhpReference) && key != null);

            if (var != null && var.GetType() == typeof(PhpArray))   // derived types checked in Epilogue
                return ((PhpArray)var).GetArrayItemExact(key, kind != GetItemKinds.Get, hashcode);

            return GetStringItemEpilogue(var, key, kind);
        }

        private static object GetStringItemEpilogue(object var, string key, GetItemKinds kind)
        {
            bool quiet = kind != GetItemKinds.Get;
            int index;

            if (var == null)
                return null;

            // a character of a string:
            if (var.GetType() == typeof(string))
                return (CheckStringIndexRange(index = Convert.StringToInteger(key), ((string)var).Length, quiet)) ? ((string)var)[index].ToString() : null;

            // a character of a PhpString:
            if (var.GetType() == typeof(PhpString))
                return (CheckStringIndexRange(index = Convert.StringToInteger(key), ((PhpString)var).Length, quiet)) ? ((PhpString)var).GetCharUnchecked(index).ToString() : null;

            // a byte of a string of bytes:
            if (var.GetType() == typeof(PhpBytes))
                return (CheckStringIndexRange(index = Convert.StringToInteger(key), ((PhpBytes)var).Length, quiet)) ? new PhpBytes(new byte[] { ((PhpBytes)var)[index] }) : null;

            return GetItemEpilogue(var, key, kind);
        }

        //Similar to ArrayAccess.GetUserArrayItem, but getting access to a C# IDictionary
        internal static object GetDictionaryItem(IDictionary arrayAccess, object key, Operators.GetItemKinds kind)
        {
            switch (kind)
            {
                case Operators.GetItemKinds.Isset:
                    // pass isset() ""/null to say true/false depending on the value returned from "offsetExists":
                    return arrayAccess.Contains(key) ? "" : null;

                case Operators.GetItemKinds.Empty:
                    // if "offsetExists" returns false, the empty()/isset() returns false (pass null to say true/false): 
                    // otherwise, "offsetGet" is called to retrieve the value, which is passed to isset():
                    if (!arrayAccess.Contains(key))
                        return null;
                    else
                        goto default;

                default:
                    // regular getter:
                    return ClrObject.WrapDynamic(PhpVariable.Dereference(arrayAccess[key]));
            }

        }

        //Similar to ArrayAccess.GetUserArrayItem, but getting access to a C# IDictionary
        internal static object GetListItem(IList arrayAccess, object key, Operators.GetItemKinds kind)
        {
            int index = Convert.ObjectToInteger(key);   // index used as key in IList

            switch (kind)
            {
                case Operators.GetItemKinds.Isset:
                    // pass isset() ""/null to say true/false depending on the value returned from "offsetExists":
                    return (index >= 0 && index < arrayAccess.Count) ? "" : null;

                case Operators.GetItemKinds.Empty:
                    // if "offsetExists" returns false, the empty()/isset() returns false (pass null to say true/false): 
                    // otherwise, "offsetGet" is called to retrieve the value, which is passed to isset():
                    if (index < 0 || index >= arrayAccess.Count)
                        return null;
                    else
                        goto default;

                default:
                    // regular getter:
                    return ClrObject.WrapDynamic(PhpVariable.Dereference(arrayAccess[index]));
            }

        }

        private static object GetItemEpilogue(object var, object key, GetItemKinds kind)
        {
            bool quiet = kind != GetItemKinds.Get;

            // empty:
            if (PhpVariable.IsEmpty(var))
            {
                /* silently returns null, see PHP specs, issue 22019 */
                //if (!quiet) PhpException.Throw(PhpError.Notice, CoreResources.GetString("empty_used_as_array"));

                return null;
            }

            // an item of a PhpArray (check inherited types):
            PhpArray array;
            if ((array = var as PhpArray) != null)
                return array.GetArrayItem(key, quiet);

            // object behaving as array:
            DObject dobj = var as DObject;
            if (dobj != null)
            {
                var realObject = dobj.RealObject;

                if (realObject is Library.SPL.ArrayAccess)
                    return Library.SPL.PhpArrayObject.GetUserArrayItem(dobj, key, kind);

                if (realObject is IList)
                    return GetListItem((IList)realObject, key, kind);

                if (realObject is IDictionary)
                    return GetDictionaryItem((IDictionary)realObject, key, kind);
            }

            // warnings (DObject, scalar type):
            /* silently returns null, see PHP specs, issue 22019 */
            //PhpException.VariableMisusedAsArray(var, false);

            return null;
        }

        #endregion

        #region GetItemRef

        /// <summary>
        /// Adds a new reference item to the array. 
        /// Implements key-less [] operator applied on a variable in read reference context.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <returns>The new reference item added to the array.</returns>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        [Emitted]
        public static PhpReference GetItemRef(ref object var)
        {
            Debug.Assert(!(var is PhpReference));

            // PhpArray:
            if (var != null && var.GetType() == typeof(PhpArray))   // fast check for PhpArray, not derived types
                return ((PhpArray)var).GetArrayItemRef();

            // creates a new reference and adds it to an a new array:
            if (IsEmptyForEnsure(var))
            {
                PhpArray array;
                var = array = new PhpArray(1, 0);
                PhpReference result = new PhpReference();
                array.Add(result);
                return result;
            }

            return GetItemRefEpilogue(null, ref var);
        }

        /// <summary>
        /// Retrieves a reference on keyed item of an array.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <param name="key">The index.</param>
        /// <returns>The reference.</returns>
        /// <remarks>
        /// <para>Retrieves an instance of <see cref="PhpReference"/> which is an item of the array.
        /// If respective item doesn't exist or is empty in a meaning of <see cref="IsEmptyForEnsure"/> 
        /// a new instance of <see cref="PhpReference"/> is created in its place.</para>
        /// <para>Pattern: ... =&amp; var[index]</para></remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        [Emitted]
        public static PhpReference/*!*/ GetItemRef(object key, ref object var)
        {
            Debug.Assert(!(var is PhpReference) && !(key is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            // PhpArray:
            if (var != null && var.GetType() == typeof(PhpArray))   // fast vcheck for PhpArray, not derived types
                return ((PhpArray)var).GetArrayItemRef(key);

            // creates a new reference and adds it to an a new array:
            if (IsEmptyForEnsure(var))
            {
                PhpArray array;
                var = array = new PhpArray(1, 0);
                PhpReference result = new PhpReference();
                array.SetArrayItemRef(key, result);
                return result;
            }

            return GetItemRefEpilogue(key, ref var);
        }

        [Emitted]
        public static PhpReference/*!*/ GetItemRef(int key, ref object var)
        {
            Debug.Assert(!(var is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            // PhpArray:
            if (var != null && var.GetType() == typeof(PhpArray))   // fast vcheck for PhpArray, not derived types
                return ((PhpArray)var).GetArrayItemRef(key);

            // creates a new reference and adds it to an a new array:
            if (IsEmptyForEnsure(var))
            {
                PhpArray array;
                var = array = new PhpArray(1, 0);
                PhpReference result = new PhpReference();
                array.SetArrayItemRef(key, result);
                return result;
            }

            return GetItemRefEpilogue(key, ref var);
        }

        [Emitted]
        public static PhpReference/*!*/ GetItemRef(string key, ref object var)
        {
            Debug.Assert(!(var is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            // PhpArray:
            if (var != null && var.GetType() == typeof(PhpArray))   // fast check for PhpArray, not derived types
                return ((PhpArray)var).GetArrayItemRef(key);

            // creates a new reference and adds it to an a new array:
            if (IsEmptyForEnsure(var))
            {
                PhpArray array;
                var = array = new PhpArray(0, 1);
                PhpReference result = new PhpReference();
                array.SetArrayItemRef(key, result);
                return result;
            }

            return GetItemRefEpilogue(key, ref var);
        }

        private static PhpReference/*!*/ GetItemRefEpilogue(object key, ref object/*!*/var)
        {
            Debug.Assert(var != null);

            // object behaving as array:
            DObject dobj = var as DObject;
            if (dobj != null && dobj.RealObject is Library.SPL.ArrayAccess)
                return Library.SPL.PhpArrayObject.GetUserArrayItemRef(dobj, key, ScriptContext.CurrentContext);

            // PhpArray (derived types):
            PhpArray array;
            if ((array = var as PhpArray) != null)
                return array.GetArrayItemRef(key);

            // errors:
            PhpException.VariableMisusedAsArray(var, true);
            return new PhpReference();
        }

        #endregion

        #region SetItem

        /// <summary>
        /// Adds a new item (value or reference) to an array or sets a character of a string.
        /// </summary>
        /// <param name="var">The array.</param>
        /// <param name="value">The value or reference of added item.</param>
        /// <remarks>
        /// <para>Patterns: var[] = value, var[] =&amp; value</para>
        /// <para>If <paramref name="var"/> is empty in a meaning of <see cref="IsEmptyForEnsure"/> 
        /// its value is replaced by a new instance of <see cref="PhpArray"/>.</para>
        /// </remarks>
        /// <exception cref="PhpException">A new key cannot be generated because it reached maximal value (<see cref="int.MaxValue"/>).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        [Emitted]
        public static void SetItem(object value, ref object var)
        {
            Debug.Assert(!(var is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            if (var != null && var.GetType() == typeof(PhpArray))
                ((PhpArray)var).Add(value); // Add never returns 0 now
            else
                SetItemEpilogue(value, ref var);            
        }

        private static void SetItemEpilogue(object value, ref object var)
        {
            Debug.Assert(var == null || var.GetType() != typeof(PhpArray));

            PhpArray array;
            
            // creates a new array and stores it into a new item which is added to the array:
            if (IsEmptyForEnsure(var))
            {
                array = new PhpArray(1, 0);
                array.Add(value);
                var = array;
                return;
            }
            
            // PhpArray derivates:
            if ((array = var as PhpArray) != null)
            {
                if (array.Add(value) == 0)
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("integer_key_reached_max_value"));

                return;
            }

            // object behaving as array:
            DObject dobj = var as DObject;
            if (dobj != null && dobj.RealObject is Library.SPL.ArrayAccess)
            {
                //PhpStack stack = ScriptContext.CurrentContext.Stack;
                //stack.AddFrame(null, value);
                //dobj.InvokeMethod(Library.SPL.PhpArrayObject.offsetSet, null, stack.Context);
                ((Library.SPL.ArrayAccess)dobj.RealObject).offsetSet(ScriptContext.CurrentContext, null, value);
                return;
            }

            // errors:
            PhpException.VariableMisusedAsArray(var, false);
        }

        /// <summary>
        /// Sets an item of an array or a character of a string.
        /// </summary>
        /// <param name="var">The variable whose item to set.</param>
        /// <param name="key">The index of the item.</param>
        /// <param name="value">The new value of item.</param>
        /// <remarks>
        /// <para>Pattern: var[index] = value, var{index} = value.</para>
        /// <para>If <paramref name="var"/> is empty in a meaning of <see cref="IsEmptyForEnsure"/> 
        /// its value is replaced by a new instance of <see cref="PhpArray"/>.</para>
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is <see cref="DObject"/> (Error).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is a scalar (Error).</exception>
        [Emitted]
        public static void SetItem(object value, object key, ref object var)
        {
            Debug.Assert(!(var is PhpReference) && !(key is PhpReference) && !(value is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            if (var != null)
            {
                int index;

                // PhpArray:
                if (var.GetType() == typeof(PhpArray))
                {
                    ((PhpArray)var).SetArrayItem(key, value);
                    return;
                }

                // string:
                if (var.GetType() == typeof(string) && (string)var != "")
                {
                    if (CheckStringIndexRange(index = Convert.ObjectToInteger(key), Int32.MaxValue, false))
                        var = SetStringItem(new PhpString((string)var), index, value);
                    return;
                }

                // string builder:
                if (var.GetType() == typeof(PhpString) && ((PhpString)var).Length != 0)
                {
                    if (CheckStringIndexRange(index = Convert.ObjectToInteger(key), Int32.MaxValue, false))
                        SetStringItem((PhpString)var, index, value);
                    return;
                }

                // PhpBytes:
                if (var.GetType() == typeof(PhpBytes) && ((PhpBytes)var).Length != 0)
                {
                    if (CheckStringIndexRange(index = Convert.ObjectToInteger(key), Int32.MaxValue, false))
                        SetBytesItem((PhpBytes)var, index, value);
                    return;
                }
            }

            SetItemEpilogue(value, key, ref var);
        }

        [Emitted]
        public static void SetItem(object value, int key, ref object var)
        {
            Debug.Assert(!(var is PhpReference) && !(value is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            if (var != null && var.GetType() == typeof(PhpArray))
                // PhpArray:
                ((PhpArray)var).SetArrayItem(key, value);
            else
                // the rest:
                SetItemEpilogue(value, key, ref var);
        }

        private static void SetItemEpilogue(object value, int key, ref object var)
        {
            Debug.Assert(var == null || var.GetType() != typeof(PhpArray));

            if (var != null)
            {
                // string:
                if (var.GetType() == typeof(string) && (string)var != "")
                {
                    if (CheckStringIndexRange(key, Int32.MaxValue, false))
                        var = SetStringItem(new PhpString((string)var), key, value);
                    return;
                }

                // string builder:
                if (var.GetType() == typeof(PhpString) && ((PhpString)var).Length != 0)
                {
                    if (CheckStringIndexRange(key, Int32.MaxValue, false))
                        SetStringItem((PhpString)var, key, value);
                    return;
                }

                // PhpBytes:
                if (var.GetType() == typeof(PhpBytes) && ((PhpBytes)var).Length != 0)
                {
                    if (CheckStringIndexRange(key, Int32.MaxValue, false))
                        SetBytesItem((PhpBytes)var, key, value);
                    return;
                }
            }

            SetItemEpilogue(value, (object)key, ref var);
        }

        [Emitted]
        public static void SetItem(object value, string key, ref object var)
        {
            Debug.Assert(!(var is PhpReference) && !(value is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            if (var != null && var.GetType() == typeof(PhpArray))
                ((PhpArray)var).SetArrayItem(key, value);
            else
                SetItemEpilogue(value, (object)key, ref var);
        }

        [Emitted]
        public static void SetItemExact(object value, string key, ref object var, int hashcode)
        {
            Debug.Assert(!(var is PhpReference) && !(value is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            if (var != null && var.GetType() == typeof(PhpArray))
                ((PhpArray)var).SetArrayItemExact(key, value, hashcode);
            else
                SetStringItemEpilogue(value, key, ref var);
        }

        private static void SetStringItemEpilogue(object value, string key, ref object var)
        {
            if (var != null)
            {
                int index;

                // string:
                if (var.GetType() == typeof(string) && (string)var != "")
                {
                    if (CheckStringIndexRange(index = Convert.StringToInteger(key), Int32.MaxValue, false))
                        var = SetStringItem(new PhpString((string)var), index, value);
                    return;
                }

                // string builder:
                if (var.GetType() == typeof(PhpString) && ((PhpString)var).Length != 0)
                {
                    if (CheckStringIndexRange(index = Convert.StringToInteger(key), Int32.MaxValue, false))
                        SetStringItem((PhpString)var, index, value);
                    return;
                }

                // PhpBytes:
                if (var.GetType() == typeof(PhpBytes) && ((PhpBytes)var).Length != 0)
                {
                    if (CheckStringIndexRange(index = Convert.StringToInteger(key), Int32.MaxValue, false))
                        SetBytesItem((PhpBytes)var, index, value);
                    return;
                }
            }

            SetItemEpilogue(value, (object)key, ref var);
        }

        private static void SetItemEpilogue(object value, object key, ref object var)
        {
            // empty:
            if (IsEmptyForEnsure(var))
            {
                PhpArray var_array = new PhpArray(0, 1);
                var_array.SetArrayItem(key, value);
                var = var_array;
                return;
            }

            // PhpArray (derived types):
            PhpArray array;
            if ((array = var as PhpArray) != null)
            {
                array.SetArrayItem(key, value);
                return;
            }

            // object behaving as array:
            DObject dobj = var as DObject;
            if (dobj != null)
            {
                var realObject = dobj.RealObject;

                if (realObject is Library.SPL.ArrayAccess)
                {
                    //PhpStack stack = ScriptContext.CurrentContext.Stack;
                    //stack.AddFrame(key, value);
                    //dobj.InvokeMethod(Library.SPL.PhpArrayObject.offsetSet, null, stack.Context);
                    ((Library.SPL.ArrayAccess)realObject).offsetSet(ScriptContext.CurrentContext, key, value);
                    return;
                }

                if (realObject is IList)
                {
                    ((IList)realObject)[Convert.ObjectToInteger(key)] = value;
                    return;
                }

                if (realObject is IDictionary)
                {
                    ((IDictionary)realObject)[key] = value;
                    return;
                }
            }
            
            // errors - DObject, scalars:
            PhpException.VariableMisusedAsArray(var, false);
        }

        #endregion

        #region SetStringItem, SetBytesItem

        /// <summary>
        /// Implements oprators [],{} on a string.
        /// </summary>
        /// <param name="str">The string builder which character to set.</param>
        /// <param name="index">The index of an item.</param>
        /// <param name="value">The new value of an item.</param>
        /// <exception cref="PhpException"><paramref name="index"/> converted to integer by <see cref="Convert.ObjectToInteger"/> is negative. (Warning)</exception>
        /// <include file='Doc/Operators.xml' path='docs/operator[@name="SetStringItem"]/*'/>
        internal static PhpString/*!*/ SetStringItem(PhpString/*!*/ str, int index, object value)
        {
            Debug.Assert(str != null);

            // the new character will be the first character of the value converted to string or the '\0'
            // if the length of the converted value is zero; dereferencing is also done:
            char c = Convert.ObjectToChar(value);

            if (index >= str.Length)
            {
                // if index is greater than the string length the string is padded by spaces:
                str.Append(' ', index - str.Length);
                str.Append(c);
            }
            else
            {
                // otherwise, the respective character of the string is replaced by the new one:
                str.SetCharUnchecked(index, c);
            }

            return str;
        }

        /// <summary>
        /// Implements oprators [],{} on a byte array.
        /// </summary>
        /// <param name="bytes">The variable which item to set.</param>
        /// <param name="index">The index of an item.</param>
        /// <param name="value">The new value of an item.</param>
        /// <exception cref="PhpException"><paramref name="index"/> converted to integer by <see cref="Convert.ObjectToInteger"/> is negative. (Warning)</exception>
        /// <include file='Doc/Operators.xml' path='docs/operator[@name="SetBytesItem"]/*'/>
        internal static void SetBytesItem(PhpBytes/*!*/ bytes, int index, object value)
        {
            Debug.Assert(bytes != null && bytes.Length > 0);

            // the new byte will be the first byte of the value converted to byte[] or zero byte
            // if the length of the converted value is zero; dereferencing is also done:
            byte[] bval = Convert.ObjectToPhpBytes(value).ReadonlyData;
            byte b = (bval.Length == 0) ? (byte)0 : bval[0];

            // if index is greater than the data length the array is padded by space bytes (0x20):
            if (index >= bytes.Length)
            {
                // TODO (J): optimize by using elastic array (some future implementation PhpString)
                byte[] new_bytes = new byte[index + 1];

                Buffer.BlockCopy(bytes.ReadonlyData, 0, new_bytes, 0, bytes.Length);
                ArrayUtils.Fill(new_bytes, 0x20, bytes.Length, index - bytes.Length);
                new_bytes[index] = b;

                bytes.Data = new_bytes;
            }
            else
            {
                // otherwise, the respective byte of the array is replaced by the new one: 
                bytes.Data[index] = b;
            }
        }

        #endregion

        #region SetItemRef

        /// <summary>
        /// Sets a reference keyed item of an array.
        /// </summary>
        /// <param name="var">The variable.</param>
        /// <param name="key">The index.</param>
        /// <param name="value">The reference to be assigned to the item.</param>
        /// <remarks>
        /// <para>Pattern: var[index] =&amp; value</para>
        /// <para>If <paramref name="var"/> is empty in a meaning of <see cref="IsEmptyForEnsure"/> 
        /// its value is replaced by a new instance of <see cref="PhpArray"/>.</para>
        /// <para>This method provides no more functionality than <see cref="SetItem"/> for arrays.
        /// However, if applied on strings its behavior is different.</para>
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        [Emitted]
        public static void SetItemRef(PhpReference value, object key, ref object var)
        {
            Debug.Assert(!(var is PhpReference) && !(key is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            PhpArray array;

            // PhpArray:
            if ((array = var as PhpArray) != null)
            {
                // a reference in "value" is directly assigned to the array's item:
                array.SetArrayItemRef(key, value);
                return;
            }

            // null, empty string or empty string of bytes
            if (IsEmptyForEnsure(var))
            {
                // a reference in "value" is directly assigned to the array's item:
                PhpArray var_array = new PhpArray();
                var_array.SetArrayItemRef(key, value);
                var = var_array;
                return;
            }

            SetItemRefEpilogue(value, key, ref var);
        }

        [Emitted]
        public static void SetItemRef(PhpReference value, int key, ref object var)
        {
            Debug.Assert(!(var is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            PhpArray array;

            // PhpArray:
            if ((array = var as PhpArray) != null)
            {
                // a reference in "value" is directly assigned to the array's item:
                array.SetArrayItemRef(key, value);
                return;
            }

            // null, empty string or empty string of bytes
            if (IsEmptyForEnsure(var))
            {
                // a reference in "value" is directly assigned to the array's item:
                PhpArray var_array = new PhpArray();
                var_array.SetArrayItemRef(key, value);
                var = var_array;
                return;
            }

            SetItemRefEpilogue(value, key, ref var);
        }

        [Emitted]
        public static void SetItemRef(PhpReference value, string key, ref object var)
        {
            Debug.Assert(!(var is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            PhpArray array;

            // PhpArray:
            if ((array = var as PhpArray) != null)
            {
                // a reference in "value" is directly assigned to the array's item:
                array.SetArrayItemRef(key, value);
                return;
            }

            // null, empty string or empty string of bytes
            if (IsEmptyForEnsure(var))
            {
                // a reference in "value" is directly assigned to the array's item:
                PhpArray var_array = new PhpArray();
                var_array.SetArrayItemRef(key, value);
                var = var_array;
                return;
            }

            SetItemRefEpilogue(value, key, ref var);
        }

        private static void SetItemRefEpilogue(PhpReference value, object key, ref object var)
        {
            // object behaving as array:
            DObject dobj = var as DObject;
            if (dobj != null && dobj.RealObject is Library.SPL.ArrayAccess)
            {
                PhpStack stack = ScriptContext.CurrentContext.Stack;
                stack.AddFrame(key, value);
                dobj.InvokeMethod(Library.SPL.PhpArrayObject.offsetSet, null, stack.Context);
                return;
            }

            // errors - non-empty string, DObject, scalar:
            PhpException.VariableMisusedAsArray(var, true);
        }

        #endregion

        #region UnsetItem

        /// <summary>
        /// Implements <c>unset</c> construct used along with [] operator.
        /// </summary>
        /// <param name="var">The object which item to unset.</param>
        /// <param name="index">The index of an item ot unset.</param>
        /// <remarks>
        /// <para>Pattern: unset(var[index])</para>
        /// <para>
        /// If <paramref name="var"/> is of type <see cref="PhpArray"/> then the <paramref name="index"/>
        /// is converted to array key by <see cref="Convert.ObjectToArrayKey"/> and an entry with such key is 
        /// removed from the <paramref name="var"/> array.
        /// </para>
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is <see cref="PhpArray"/> and <paramref name="index"/> is an illegal array key (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is <see cref="string"/> or <see cref="DObject"/> (Error).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="string"/> nor <see cref="DObject"/> nor <see cref="PhpArray"/> (Notice).</exception>
        [Emitted]
        public static void UnsetItem(object var, object index)
        {
            Debug.Assert(!(var is PhpReference) && !(index is PhpReference));
            Debug.Assert(!(var is PhpArrayString), "ensures and end-of-chain operators only");

            // removes an entry from the array:
            PhpArray array = var as PhpArray;
            if (array != null)
            {
                IntStringKey array_key;
                if (!Convert.ObjectToArrayKey(index, out array_key))
                    PhpException.IllegalOffsetType();
                else
                    array.Remove(array_key);

                return;
            }

            // object behaving as array:
            DObject dobj = var as DObject;
            if (dobj != null && dobj.RealObject is Library.SPL.ArrayAccess)
            {
                PhpStack stack = ScriptContext.CurrentContext.Stack;
                stack.AddFrame(index);
                dobj.InvokeMethod(Library.SPL.PhpArrayObject.offsetUnset, null, stack.Context);
                return;
            }

            // if variable is not set:
            if (PhpVariable.IsEmpty(var))
                return;

            // string item cannot be unset:
            if (PhpVariable.IsString(var))
            {
                PhpException.Throw(PhpError.Error, CoreResources.GetString("cannot_unset_string_offsets"));
                return;
            }

            // warnings:
            PhpException.VariableMisusedAsArray(var, false);
        }

        #endregion

        #endregion

        #region Chaining Operators

        #region EnsureVariableIsArray, EnsureVariableIsObject

        /// <summary>
        /// Ensures specified variable is an instance of <see cref="PhpArray"/>. 
        /// </summary>
        /// <param name="var">The variable which should be an array.</param>
        /// <returns>The <paramref name="var"/>, its new value or <b>null</b> on error.</returns>
        /// <remarks>A new instance of <see cref="PhpArray"/> is assigned to the item if it is empty in a meaning of <see cref="IsEmptyForEnsure"/>.</remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        [Emitted]
        public static PhpArray EnsureVariableIsArray(ref object var)
        {
            Debug.Assert(!(var is PhpReference));
            
            if (var != null && var.GetType() == typeof(PhpArray))
                return (PhpArray)var;

            object new_var;
            var wrappedarray = EnsureObjectIsArray(var, out new_var);
            if (wrappedarray != null)
            {
                if (new_var != null) var = new_var;
                return wrappedarray;
            }
            
            // warnings - variable is a DObject, a scalar:
            PhpException.VariableMisusedAsArray(var, false);
            return null;
        }

        /// <summary>
        /// Ensures that a variable is an instance of <see cref="DObject"/>.
        /// </summary>
        /// <param name="var">Address of the variable to check.</param>
        /// <param name="context">The <see cref="ScriptContext"/> in which potential new object will be created.</param>
        /// <returns>The <paramref name="var"/>, its new value or <B>null</B> on error.</returns>
        /// <remarks>A new instance of <see cref="stdClass"/> is assigned to the item if it is empty in a meaning of <see cref="IsEmptyForEnsure"/>.</remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="DObject"/> nor empty (Error).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is empty and new <see cref="stdClass"/> instance is created (Strict).</exception>
        [Emitted]
        public static DObject EnsureVariableIsObject(ref object var, ScriptContext/*!*/ context)
        {
            Debug.Assert(!(var is PhpReference) && context != null);

            // if var is DObject, nothing has to be done:
            DObject obj = var as DObject;
            if (obj != null) return obj;

            // if x is empty creates a new stdClass:
            if (IsEmptyForEnsure(var))
            {
                var = obj = stdClass.CreateDefaultObject(context);
                return obj;
            }

            // errors - variable is a scalar, a non-empty string or a PhpArray:
            PhpException.VariableMisusedAsObject(var, false);
            return null;
        }

        #endregion

        #region EnsureItemIsArraySimple

        /// <summary>
        /// Ensures a specified array item is an instance of <see cref="PhpArray"/>. 
        /// </summary>
        /// <param name="array">The <see cref="PhpArray"/> which item should be an array.</param>
        /// <param name="key">The key identifying which item should be an array.</param>
        /// <remarks>
        /// A new instance of <see cref="PhpArray"/> is assigned to the item if it is not an array yet.
        /// Array is expected to contain no <see cref="PhpReference"/>.
        /// Treats empty key as a missing key.
        /// </remarks>
        internal static PhpArray EnsureItemIsArraySimple(PhpArray/*!*/ array, string key)
        {
            Debug.Assert(array != null);
            Debug.Assert(!(array is PhpArrayString) && !(array is Library.SPL.PhpArrayObject));

            // treats empty key as a missing key:
            if (key == String.Empty)
            {
                PhpArray array_item = new PhpArray();
                array.Add(array_item);
                return array_item;
            }

            IntStringKey array_key = Core.Convert.StringToArrayKey(key);

            return array.table._ensure_item_array(ref array_key, array);
            //element = array.GetElement(array_key);

            //// creates a new array if an item is not one:
            //array_item = (element != null) ? element.Value as PhpArray : null;
            //if (array_item == null)
            //{
            //    array_item = new PhpArray();
            //    if (element != null)
            //    {
            //        if (array.table.IsShared)
            //        {
            //            // we are going to change the internal array, it must be writable
            //            array.EnsureWritable();
            //            element = array.table.dict[array_key]; // get the item again
            //        }

            //        element.Value = array_item;
            //    }
            //    else
            //        array.Add(array_key, array_item);
            //}

            //return array_item;
        }

        #endregion

        #region EnsurePropertyIsArray, EnsurePropertyIsObject

        /// <summary>
        /// Ensures that a property value is of <see cref="PhpArray"/> type.
        /// </summary>
        /// <param name="obj">The object whose property is to be checked.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="Type"/> of the object that request the operation.</param>
        /// <param name="propValue">The property value (might get updated).</param>
        /// <returns>The new property value (dereferenced) or <B>null</B> if evaluation of this compound
        /// statement should not proceed.</returns>
        internal static PhpArray EnsurePropertyIsArrayInternal(DObject obj, string name, DTypeDesc caller, ref object propValue)
        {
            PhpArray result;
            PhpReference reference = propValue as PhpReference;

            object value;
            if (reference != null && !reference.IsSet)
            {
                // this CT property has been unset
                if (obj.TypeDesc.GetMethod(DObject.SpecialMethodNames.Set) != null &&
                    obj.TypeDesc.RealType.Namespace != null &&
                    obj.TypeDesc.RealType.Namespace.StartsWith(Namespaces.Library))
                {
                    ScriptContext context = ScriptContext.CurrentContext;

                    // create a chain of arguments to be passed to the setter
                    context.BeginSetterChain(obj);
                    context.ExtendSetterChain(new RuntimeChainProperty(name));

                    return ScriptContext.SetterChainSingletonArray;
                }

                // try to invoke __get
                bool getter_exists;
                reference = obj.InvokeGetterRef(name, caller, out getter_exists);
                if (!getter_exists)
                {
                    result = new PhpArray();
                    propValue = new PhpReference(result);
                    return result;
                }
                else if (reference == null) return null; // error

                value = reference.Value;
            }
            else value = PhpVariable.Dereference(propValue);

            // try to wrap into PhpArray:
            object new_value;
            var wrappedarray = EnsureObjectIsArray(value, out new_value);
            if (wrappedarray != null)
            {
                if (new_value != null)
                {
                    if (reference != null) reference.Value = new_value;
                    else propValue = new_value;
                }
                return wrappedarray;
            }

            // error - the property is a scalar or a PhpObject:
            PhpException.VariableMisusedAsArray(value, false);
            return null;
        }

        /// <summary>
        /// Ensures that an instance property is of <see cref="PhpArray"/> type.
        /// </summary>
        /// <param name="obj">The object whose property is to be checked.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the operation.</param>
        /// <returns>The new value of the <paramref name="name"/> property or <B>null</B> if evaluation of this compound
        /// statement should not proceed.</returns>
        /// <remarks>PHP also allows <B>false</B> to be converted to an empty <see cref="PhpArray"/> but we consider this behavior
        /// to be inconsistent.</remarks>
        /// <exception cref="PhpException">The property is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        /// <exception cref="PhpException">The property is undefined and <c>__get</c> or <c>__set</c> exists in <paramref name="obj"/>
        /// (Error).</exception>
        [Emitted]
        public static PhpArray EnsurePropertyIsArray(DObject obj, string name, DTypeDesc caller)
        {
            Debug.Assert(name != null);

            if (ReferenceEquals(obj, ScriptContext.SetterChainSingletonObject))
            {
                ScriptContext context = ScriptContext.CurrentContext;

                // extend the setter chain if one already exists
                context.ExtendSetterChain(new RuntimeChainProperty(name));

                return ScriptContext.SetterChainSingletonArray;
            }

            // search in CT properties
            DPropertyDesc property;
            GetMemberResult get_res =
                obj.TypeDesc.GetProperty(new VariableName(name), caller, out property);

            if (get_res == GetMemberResult.BadVisibility)
            {
                DObject.ThrowPropertyVisibilityError(name, property, caller);
                return null;
            }

            PhpArray ret_val;
            object old_val, value;

            // was a CT property found?
            if (get_res == GetMemberResult.OK)
            {
                old_val = property.Get(obj);
                value = old_val;
                ret_val = EnsurePropertyIsArrayInternal(obj, name, caller, ref value);

                if (!Object.ReferenceEquals(value, old_val)) property.Set(obj, value);
            }
            else
            {
                // search in RT fields
                var namekey = new IntStringKey(name);
                if (obj.RuntimeFields != null && obj.RuntimeFields.TryGetValue(namekey, out old_val))
                {
                    // old_val
                }
                else
                {
                    PhpReference reference = new PhpSmartReference();
                    reference.IsSet = false;
                    old_val = reference;
                }

                value = old_val;
                ret_val = EnsurePropertyIsArrayInternal(obj, name, caller, ref value);

                if (!Object.ReferenceEquals(value, old_val))
                {
                    if (obj.RuntimeFields == null) obj.RuntimeFields = new PhpArray();
                    
                    obj.RuntimeFields[namekey] = value;
                }
            }

            return ret_val;
        }

        /// <summary>
        /// Ensures that a property value is of <see cref="DObject"/> type.
        /// </summary>
        /// <param name="obj">The object whose property is to be checked.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="Type"/> of the object that request the operation.</param>
        /// <param name="propValue">The property value (might get updated).</param>
        /// <param name="context">The current <see cref="ScriptContext"/>.</param>
        /// <returns>The new property value (dereferenced) or <B>null</B> if evaluation of this compound
        /// statement should not proceed.</returns>
        internal static DObject EnsurePropertyIsObjectInternal(DObject obj, string name, DTypeDesc caller, ref object propValue,
            ScriptContext context)
        {
            DObject result;
            PhpReference reference = propValue as PhpReference;

            object value;
            if (reference != null && !reference.IsSet)
            {
                // this CT property has been unset
                if (obj.TypeDesc.GetMethod(DObject.SpecialMethodNames.Set) != null &&
                    obj.TypeDesc.RealType.Namespace != null &&
                    obj.TypeDesc.RealType.Namespace.StartsWith(Namespaces.Library))
                {
                    // create a chain of arguments to be passed to the setter
                    context.BeginSetterChain(obj);
                    context.ExtendSetterChain(new RuntimeChainProperty(name));

                    return ScriptContext.SetterChainSingletonObject;
                }

                // try to invoke __get
                bool getter_exists;
                reference = obj.InvokeGetterRef(name, caller, out getter_exists);
                if (!getter_exists)
                {
                    result = stdClass.CreateDefaultObject(context);
                    propValue = new PhpReference(result);
                    return result;
                }
                else if (reference == null) return null; // error

                value = reference.Value;
            }
            else value = PhpVariable.Dereference(propValue);

            // if property value is a DObject, nothing has to be done
            result = value as DObject;
            if (result != null) return result;

            // if the property is "empty"?
            if (IsEmptyForEnsure(value))
            {
                // create a new stdClass and update the reference
                result = stdClass.CreateDefaultObject(context);
                if (reference != null)
                {
                    reference.Value = result;
                    reference.IsSet = true;
                }
                else propValue = result;
                return result;
            }

            // error - the property is a scalar or a PhpArray or a non-empty string
            PhpException.VariableMisusedAsObject(value, false);
            return null;
        }

        /// <summary>
        /// Ensures that an instance property is of <see cref="DObject"/> type.
        /// </summary>
        /// <param name="obj">The object whose property is to be checked.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <param name="context">The context in which a new instance of <see cref="stdClass"/> is possibly created.</param>
        /// <returns>The new value of the <paramref name="name"/> property or <B>null</B> if evaluation of this
        /// compound statement should not proceed.</returns>
        /// <remarks>PHP also allows <B>false</B> to be converted to an empty <see cref="stdClass"/> but we consider
        /// this behavior to be inconsistent.</remarks>
        /// <exception cref="PhpException">The property is neither <see cref="DObject"/> nor empty (Error).
        /// </exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        /// <exception cref="PhpException">The property is undefined and <c>__get</c> or <c>__set</c> exists in <paramref name="obj"/>
        /// (Error).</exception>
        /// <exception cref="PhpException">The property is empty and new <see cref="stdClass"/> instance is created (Strict).
        /// </exception>
        [Emitted]
        public static DObject EnsurePropertyIsObject(DObject obj, string name, DTypeDesc caller,
            ScriptContext context)
        {
            Debug.Assert(name != null);

            if (ReferenceEquals(obj, ScriptContext.SetterChainSingletonObject))
            {
                // extend the setter chain if one already exists
                context.ExtendSetterChain(new RuntimeChainProperty(name));

                return ScriptContext.SetterChainSingletonObject;
            }

            // search in CT properties
            DPropertyDesc property;
            GetMemberResult get_res =
                obj.TypeDesc.GetProperty(new VariableName(name), caller, out property);

            if (get_res == GetMemberResult.BadVisibility)
            {
                DObject.ThrowPropertyVisibilityError(name, property, caller);
                return null;
            }

            DObject ret_val;
            object old_val, value;

            // was a CT property found?
            if (get_res == GetMemberResult.OK)
            {
                old_val = property.Get(obj);
                value = old_val;
                ret_val = EnsurePropertyIsObjectInternal(obj, name, caller, ref value, context);

                if (!Object.ReferenceEquals(value, old_val)) property.Set(obj, value);
            }
            else
            {
                // search in RT fields

                var namekey = new IntStringKey(name);
                if (obj.RuntimeFields != null && obj.RuntimeFields.TryGetValue(namekey, out old_val))
                {
                    //old_val = element.Value;
                }
                else
                {
                    PhpReference reference = new PhpSmartReference();
                    reference.IsSet = false;
                    old_val = reference;
                }

                value = old_val;
                ret_val = EnsurePropertyIsObjectInternal(obj, name, caller, ref value, context);

                if (!Object.ReferenceEquals(value, old_val))
                {
                    if (obj.RuntimeFields == null) obj.RuntimeFields = new PhpArray();
                    
                    obj.RuntimeFields[name] = value;
                }
            }

            return ret_val;
        }

        #endregion

        #region EnsureStaticPropertyIsArray, EnsureStaticPropertyIsObject

        /// <summary>
        /// Ensures that a static property is of <see cref="PhpArray"/> type.
        /// </summary>
        /// <param name="type">Represents the type whose property is to be checked.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the operation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The new value of the <paramref name="propertyName"/> property or <B>null</B> if evaluation of this compound
        /// statement should not proceed.</returns>
        /// <remarks>PHP also allows <B>false</B> to be converted to an empty <see cref="PhpArray"/> but we consider this behavior
        /// to be inconsistent.</remarks>
        /// <exception cref="PhpException">The property denoted by <paramref name="propertyName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The property is neither <see cref="PhpArray"/> nor empty (Error).</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static PhpArray EnsureStaticPropertyIsArray(DTypeDesc type, object propertyName, DTypeDesc caller,
            ScriptContext context)
        {
            DPropertyDesc property = GetStaticPropertyDesc(type, propertyName, caller, context, false);
            if (property == null) return null;

            object property_value = property.Get(null);
            PhpReference property_value_ref = PhpVariable.Dereference(ref property_value);

            // convert obj to array or wrap obj into new array if possible:
            object new_value;
            var wrappedarray = EnsureObjectIsArray(property_value, out new_value);
            if (wrappedarray != null)
            {
                if (new_value != null)
                {
                    if (property_value_ref != null) property_value_ref.Value = new_value;
                    else property.Set(null, new_value);
                }

                return wrappedarray;
            }

            // error - the property is a scalar or a DObject:
            PhpException.VariableMisusedAsArray(property_value, false);
            return null;
        }

        /// <summary>
        /// Ensures that a static property is of <see cref="DObject"/> type.
        /// </summary>
        /// <param name="type">Represents the type whose property is to be checked.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the operation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The new value of the <paramref name="propertyName"/> property or <B>null</B> if evaluation of this compound
        /// statement should not proceed.</returns>
        /// <remarks>PHP also allows <B>false</B> to be converted to an empty <see cref="stdClass"/> but we consider this
        /// behavior to be inconsistent.</remarks>
        /// <exception cref="PhpException">The property denoted by <paramref name="propertyName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The property is neither <see cref="DObject"/> nor empty (Error).</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static DObject EnsureStaticPropertyIsObject(DTypeDesc type, object propertyName, DTypeDesc caller,
            ScriptContext context)
        {
            DPropertyDesc property = GetStaticPropertyDesc(type, propertyName, caller, context, false);
            if (property == null) return null;

            object property_value = property.Get(null);
            PhpReference property_value_ref = PhpVariable.Dereference(ref property_value);

            // if the property is DObject, nothing has to be done
            DObject result = property_value as DObject;
            if (result != null) return result;

            // if the property is "empty"?
            if (IsEmptyForEnsure(property_value))
            {
                // create a new stdClass and update the PhpReference
                result = stdClass.CreateDefaultObject(context);
                if (property_value_ref != null) property_value_ref.Value = result;
                else property.Set(null, result);

                return result;
            }

            // error - the property is a scalar or a PhpArray or a non-empty string
            PhpException.VariableMisusedAsObject(property_value, false);
            return null;
        }

        #endregion

        /// <summary>
        /// Wraps <c>null</c>, <see cref="string"/>, <see cref="PhpString"/>, <see cref="PhpBytes"/>, <c>EmptyForEnsure</c> and others into an instance assignable to <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="obj">An object which has to be accessed as <see cref="PhpArray"/>.</param>
        /// <param name="convertedobj">In case <paramref name="obj"/> was converted (upgraded, e.g. from read-only to read/write), contains an instance of new object.
        /// Can be <c>null</c> reference if <paramref name="obj"/> was not changed.</param>
        /// <remarks>Note <c>null</c> reference is converted to new instance of <see cref="PhpArray"/>.</remarks>
        public static PhpArray EnsureObjectIsArray(object obj, out object convertedobj)
        {
            convertedobj = null;

            // PhpArray instance already:
            PhpArray arrayobj;
            if ((arrayobj = obj as PhpArray) != null)
                return arrayobj;

            // empty variable:
            if (IsEmptyForEnsure(obj))
            {
                PhpArray tmparray = new PhpArray();
                convertedobj = tmparray;
                return tmparray;
            }

            // ensure for optimizations below:
            Debug.Assert(typeof(PhpString).IsSealed);
            Debug.Assert(typeof(PhpBytes).IsSealed);

            // non-empty immutable string:
            if (obj.GetType() == typeof(string)) return new PhpArrayString(convertedobj = new PhpString((string)obj));

            // non-empty mutable string:
            if (obj.GetType() == typeof(PhpString)) return new PhpArrayString((PhpString)obj);
            if (obj.GetType() == typeof(PhpBytes)) return new PhpArrayString((PhpBytes)obj);

            // checks an object behaving like an array:
            DObject dobj;
            if ((dobj = obj as DObject) != null)
            {
                var realObject = dobj.RealObject;
                if (realObject is Library.SPL.ArrayAccess)
                    return new Library.SPL.PhpArrayObject(dobj);

                // TODO: IList, IDictionary
                if (realObject is IList)
                    throw new NotImplementedException();

                if (realObject is IDictionary)
                    throw new NotImplementedException();
            }

            // obj cannot be accessed as an array:
            return null;
        }

        #endregion

        #region Object Operators

        #region GetProperty, GetObjectProperty, GetPropertyRef, GetObjectPropertyRef

        /// <summary>
        /// Gets the value of an instance property of an object.
        /// </summary>
        /// <param name="var">The variable to get the property of.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the retrieval.</param>
        /// <param name="quiet">Disables notices reporting. Used for implementation of <c>isset</c> operator.</param>
        /// <returns>The value of the instance property.</returns>
        /// <exception cref="PhpReference">If <paramref name="var"/> is not an instance of <see cref="DObject"/> (Notice).</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is empty (Notice).</exception>
        [Emitted]
        public static object GetProperty(object var, string name, DTypeDesc caller, bool quiet)
        {
            Debug.Assert(!(var is PhpReference) && name != null);

            DObject obj;

            // a property of a DObject:
            if ((obj = var as DObject) != null)
                return GetObjectProperty(obj, name, caller, quiet);

            // warnings:
            if (!quiet) // not in isset() operator only
            {
                if (PhpVariable.IsEmpty(var))
                    // empty:
                    PhpException.Throw(PhpError.Notice, CoreResources.GetString("empty_used_as_object"));
                else
                    // PhpArray, string, scalar type:
                    PhpException.VariableMisusedAsObject(var, false);
            }

            // property does not exist
            return null;
        }

        /// <summary>
        /// Retrieves a reference on a property of an object.
        /// </summary>
        /// <param name="var">The variable to get the property of.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <param name="context">The context in which a new instance of <see cref="stdClass"/> is possibly created.</param>
        /// <returns>The reference.</returns>
        /// <remarks>Retrieves the instance of <see cref="PhpReference"/> which is the property of a <see cref="DObject"/>
        /// if already exists and is of type <see cref="PhpReference"/>, otherwise replaces the property by a new
        /// instance of <see cref="PhpReference"/> referencing the original property.</remarks>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="DObject"/> nor empty (Error).</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        /// <exception cref="PhpException">The property is undefined and <c>__get</c> or <c>__set</c> exists in <paramref name="var"/>
        /// (Error).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is empty and a new <see cref="stdClass"/> instance is
        /// created (Strict).</exception>
        [Emitted]
        public static PhpReference GetPropertyRef(ref object var, string name, DTypeDesc caller, ScriptContext context)
        {
            Debug.Assert(!(var is PhpReference) && name != null);

            DObject obj;
            PhpReference result;

            // DObject
            if ((obj = var as DObject) != null)
                return GetObjectPropertyRef(obj, name, caller);

            // creates a new stdClass and adds a new property referencing null value
            if (IsEmptyForEnsure(var))
            {
                result = new PhpReference();
                stdClass var_object = stdClass.CreateDefaultObject(context);
                SetObjectProperty(var_object, name, result, caller);
                var = var_object;
                return result;
            }

            // errors - PhpArray, a scalar, string
            PhpException.VariableMisusedAsObject(var, true);
            return new PhpReference();
        }

        /// <summary>
        /// Gets the value of an instance property of an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the retrieval.</param>
        /// <param name="quiet">Disables reporting errors. Used for implementation of <c>isset</c> operator.</param>
        /// <returns>The value of the instance property (eventual <see cref="PhpReference"/> is dereferenced).</returns>
        /// <remarks>Assumes that <paramref name="obj"/> is not null.</remarks>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static object GetObjectProperty(DObject/*!*/ obj, string name, DTypeDesc caller, bool quiet)
        {
            Debug.Assert(obj != null && name != null);

            object property = obj.GetProperty(name, caller, quiet);

            return PhpVariable.Dereference(property);
        }

        /// <summary>
        /// Retrieves a reference on a property of an object.
        /// </summary>
        /// <param name="obj">The object to get the property of.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <returns>The reference.</returns>
        /// <remarks>Assumes that <paramref name="obj"/> is not null.</remarks>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static PhpReference GetObjectPropertyRef(DObject/*!*/ obj, string name, DTypeDesc caller)
        {
            Debug.Assert(obj != null && name != null);

            if (ReferenceEquals(obj, ScriptContext.SetterChainSingletonObject))
            {
                ScriptContext.CurrentContext.AbortSetterChain(false);
                return new PhpReference();
            }

            return obj.GetPropertyRef(name, caller);
        }

        [Emitted]
        public static object GetObjectFieldDirect(DObject/*!*/ obj, PhpReference/*!*/ field,
            string/*!*/ name, DTypeDesc caller, bool quiet)
        {
            Debug.Assert(obj != null && field != null && name != null);

            if (field.IsSet)
            {
                return field.Value;
            }
            else
            {
                return GetObjectProperty(obj, name, caller, quiet);
            }
        }

        [Emitted]
        public static PhpReference GetObjectFieldDirectRef(DObject/*!*/ obj, PhpReference/*!*/ field,
            string/*!*/ name, DTypeDesc caller)
        {
            Debug.Assert(obj != null && field != null && name != null);

            if (field.IsSet)
            {
                field.IsAliased = true;
                return field;
            }
            else
            {
                return GetObjectPropertyRef(obj, name, caller);
            }
        }

        #endregion

        #region SetProperty, SetObjectProperty

        /// <summary>
        /// Sets the value of an instance property of an object.
        /// </summary>
        /// <param name="var">The variable to set the property of.</param>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new property value (can be a <see cref="PhpReference"/>).</param>
        /// <param name="context">The context in which a new instance of <see cref="stdClass"/> is possibly created.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <exception cref="PhpException"><paramref name="var"/> is neither <see cref="DObject"/> nor empty (Error).</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        /// <exception cref="PhpException"><paramref name="var"/> is empty and new <see cref="stdClass"/> instance is created
        /// (Strict).</exception>
        [Emitted]
        public static void SetProperty(object value, ref object var, string name, DTypeDesc caller,
            ScriptContext context)
        {
            Debug.Assert(!(var is PhpReference) && name != null);

            DObject obj;

            // DObject:
            if ((obj = var as DObject) != null)
            {
                SetObjectProperty(obj, name, value, caller);
                return;
            }

            // empty variable:
            if (IsEmptyForEnsure(var))
            {
                stdClass var_object = stdClass.CreateDefaultObject(context);
                SetObjectProperty(var_object, name, value, caller);
                var = var_object;
                return;
            }

            // errors - variable is a scalar, a PhpArray or a non-empty string:
            PhpException.VariableMisusedAsObject(var, false);
        }

        /// <summary>
        /// Sets the value of an instance property of an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new property value (can be a <see cref="PhpReference"/>).</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <remarks>Assumes that <paramref name="obj"/> is not null.</remarks>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static void SetObjectProperty(DObject/*!*/ obj, string name, object value, DTypeDesc caller)
        {
            Debug.Assert(obj != null && name != null);

            if (ReferenceEquals(obj, ScriptContext.SetterChainSingletonObject))
            {
                ScriptContext context = ScriptContext.CurrentContext;

                if (value is PhpReference)
                {
                    context.AbortSetterChain(false);
                    return;
                }

                // extend and finish the setter chain
                context.ExtendSetterChain(new RuntimeChainProperty(name));
                context.FinishSetterChain(value);
                return;
            }

            obj.SetProperty(name, value, caller);
        }

        [Emitted]
        public static void SetObjectFieldDirect(object value, DObject/*!*/ obj, PhpReference/*!*/ field,
            string/*!*/ name, DTypeDesc caller)
        {
            Debug.Assert(obj != null && field != null && name != null && !(value is PhpReference));

            if (field.IsSet)
            {
                field.Value = value;
            }
            else
            {
                SetObjectProperty(obj, name, value, caller);
            }
        }

        [Emitted]
        public static void SetObjectFieldDirectRef(PhpReference value, DObject/*!*/ obj, ref PhpReference/*!*/ field,
            string/*!*/ name, DTypeDesc caller)
        {
            Debug.Assert(obj != null && field != null && name != null);

            if (field.IsSet)
            {
                field = value;
            }
            else
            {
                SetObjectProperty(obj, name, value, caller);
            }
        }

        #endregion

        #region UnsetProperty

        /// <summary>
        /// Unsets an instance property.
        /// </summary>
        /// <param name="x">The variable to unset the property of.</param>
        /// <param name="name">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <remarks><para>Pattern: unset(x->name)</para></remarks>
        /// <exception cref="PhpException"><paramref name="x"/> is non-null and is not <see cref="DObject"/> (Error).
        /// </exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static void UnsetProperty(object x, string name, DTypeDesc caller)
        {
            Debug.Assert(!(x is PhpReference) && name != null);

            DObject obj = x as DObject;
            if (obj == null)
            {
                // PHP doesn't report any error but we do:
                if (x != null) PhpException.VariableMisusedAsObject(x, false);
                return;
            }

            obj.UnsetProperty(name, caller);
        }

        #endregion

        #region InvokeMethod

        /// <summary>
        /// Performs the &quot;instance style&quot; invocation (<c>$x->f()</c>) of a method.
        /// </summary>
        /// <param name="x">The object to invoke the method on.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the invocation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The method's return value (always a <see cref="PhpReference"/>).</returns>
        /// <remarks>
        /// Invokes both <c>instance</c> and <c>static</c> methods on a given object.
        /// </remarks>
        /// <exception cref="PhpException">The <paramref name="methodName"/> is not a string. (Error)</exception>
        /// <exception cref="PhpException"><paramref name="x"/> is not an instance of <see cref="DObject"/>. (Error)</exception>
        /// <exception cref="PhpException">The method is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static PhpReference InvokeMethod(object x, object methodName, DTypeDesc caller, ScriptContext context)
        {
            Debug.Assert(!(x is PhpReference));

            // verify that methodName is a string
            string name = PhpVariable.AsString(methodName);
            if (name == null)
            {
                context.Stack.RemoveFrame();
                PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_method_name"));
                return new PhpReference();
            }

            return InvokeMethod(x, name, caller, context);
        }

        /// <summary>
        /// Performs the &quot;instance style&quot; invocation (<c>$x->f()</c>) of a method (optimized version to be used
        /// when the name is surely a string).
        /// </summary>
        /// <param name="x">The object to invoke the method on.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the invocation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The method's return value (always a <see cref="PhpReference"/>).</returns>
        /// <remarks>
        /// Invokes both <c>instance</c> and <c>static</c> methods on a given object.
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="x"/> is not an instance of <see cref="DObject"/>. (Error)</exception>
        /// <exception cref="PhpException">The method is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static PhpReference InvokeMethod(object x, string methodName, DTypeDesc caller, ScriptContext context)
        {
            Debug.Assert(!(x is PhpReference));

            DObject obj = x as DObject;
            if (obj == null)
            {
                if (x != null && Configuration.Application.Compiler.ClrSemantics)
                {
                    // TODO: some normalizing conversions (PhpString, PhpBytes -> string):
                    obj = ClrObject.WrapRealObject(x);
                }
                else
                {
                    context.Stack.RemoveFrame();
                    PhpException.Throw(PhpError.Error, CoreResources.GetString("method_called_on_non_object", methodName));
                    return new PhpReference();
                }
            }

            object result = obj.InvokeMethod(methodName, caller, context);

            // boxes a copy of the result:
            return PhpVariable.MakeReference(PhpVariable.Copy(result, CopyReason.ReturnedByCopy));
        }

        #endregion

        #region Clone

        /// <summary>
        /// Implementation of the <c>clone</c> operator.
        /// </summary>
        /// <param name="x">The object to clone.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The clone or <B>null</B> on an error.</returns>
        /// <exception cref="PhpException">If <paramref name="x"/> is not an instance of <see cref="DObject"/>. (Warning)</exception>
        [Emitted]
        public static object Clone(object x, DTypeDesc caller, ScriptContext context)
        {
            Debug.Assert(!(x is PhpReference));

            DObject obj = x as DObject;
            if (obj == null)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("clone_called_on_non_object"));
                return null;
            }

            return obj.CloneObject(caller, context, false);
        }

        #endregion

        #endregion

        #region Class Operators

        #region GetClassConstant

        /// <summary>
        /// Gets the value of a constant of a class or interface.
        /// </summary>
        /// <param name="type">Represents the type to get the constant of.</param>
        /// <param name="constantName">The constant name</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the access.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The value of the constant.</returns>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the constant at
        /// compile time. Otherwise, the appropriate direct access IL instructions are directly emitted.
        /// </remarks>
        /// <exception cref="PhpException">The constant denoted by <paramref name="constantName"/> was not found. (Error)</exception>
        [Emitted]
        public static object GetClassConstant(DTypeDesc type, string constantName, DTypeDesc caller, ScriptContext context)
        {
            if (type == null) return null;

            // lookup the constant desc
            DConstantDesc constant;
            switch (type.GetConstant(new VariableName(constantName), caller, out constant))
            {
                case GetMemberResult.NotFound:
                    {
                        PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_class_constant",
                            type.MakeFullName(), constantName));
                        return null;
                    }

                case GetMemberResult.BadVisibility:
                    {
                        PhpException.ConstantNotAccessible(
                            constant.DeclaringType.MakeFullName(),
                            constantName,
                            (caller == null ? String.Empty : caller.MakeFullName()),
                            constant.IsProtected);
                        return null;
                    }
            }

            // make sure that the constant has been initialized for this request
            return PhpVariable.Dereference(constant.GetValue(context));
        }

        #endregion

        #region GetStaticPropertyDesc

        /// <summary>
        /// Gets the <see cref="DPropertyDesc"/> of a static property of a class.
        /// </summary>
        /// <param name="type">The class to get the property of.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the retrieval.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="quiet">If <B>true</B>, the &quot;property not found&quot; exception should not be thrown.</param>
        /// <returns>The <see cref="DPropertyDesc"/> representing the static property or <B>null</B> if an error occurs.</returns>
        /// <exception cref="PhpException">The property denoted by <paramref name="propertyName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        internal static DPropertyDesc GetStaticPropertyDesc(DTypeDesc type, object propertyName, DTypeDesc caller,
            ScriptContext context, bool quiet)
        {
            if (type == null) return null;

            // convert propertyName to string
            string name = (propertyName == null ? String.Empty : Convert.ObjectToString(propertyName));

            // find the property
            DPropertyDesc property;
            switch (type.GetProperty(new VariableName(name), caller, out property))
            {
                case GetMemberResult.NotFound:
                    {
                        if (!quiet) PhpException.UndeclaredStaticProperty(type.MakeFullName(), name);
                        return null;
                    }

                case GetMemberResult.BadVisibility:
                    {
                        if (!quiet)
                        {
                            PhpException.PropertyNotAccessible(
                                property.DeclaringType.MakeFullName(),
                                name,
                                (caller == null ? String.Empty : caller.MakeFullName()),
                                property.IsProtected);
                        }
                        return null;
                    }

                case GetMemberResult.OK:
                    {
                        if (!property.IsStatic) goto case GetMemberResult.NotFound;
                        break;
                    }
            }

            // make sure that the property has been initialized for this request
            property.EnsureInitialized(context);

            return property;
        }

        #endregion

        #region GetStaticProperty, GetStaticPropertyRef

        /// <summary>
        /// Gets the value of a static property of a class.
        /// </summary>
        /// <param name="type">Represents the type to get the property of.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the retrieval.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="quiet">If <B>true</B>, the &quot;property not found&quot; exception should not be thrown.</param>
        /// <returns>The value of the static property.</returns>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the property or the calling type desc at
        /// compile time. Otherwise, appropriate IL instructions that access the property directly are emitted.
        /// </remarks>
        /// <exception cref="PhpException">The property denoted by <paramref name="propertyName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static object GetStaticProperty(DTypeDesc type, object propertyName, DTypeDesc caller,
            ScriptContext context, bool quiet)
        {
            DPropertyDesc property = GetStaticPropertyDesc(type, propertyName, caller, context, quiet);
            if (property == null) return null;

            return PhpVariable.Dereference(property.Get(null));
        }

        /// <summary>
        /// Retrieves a reference to a static property of a class.
        /// </summary>
        /// <param name="type">Represents the type to get the property of.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The reference.</returns>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the property or the calling type desc at
        /// compile time. Otherwise, appropriate IL instructions that access the property directly are emitted.
        /// </remarks>
        /// <exception cref="PhpException">The property denoted by <paramref name="propertyName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static PhpReference GetStaticPropertyRef(DTypeDesc type, object propertyName, DTypeDesc caller,
            ScriptContext context)
        {
            DPropertyDesc property = GetStaticPropertyDesc(type, propertyName, caller, context, false);
            if (property == null) return new PhpReference();

            object property_value = property.Get(null);
            PhpReference property_value_ref = PhpVariable.Dereference(ref property_value);

            if (property_value_ref == null) return new PhpReference(property_value);
            else
            {
                property_value_ref.IsAliased = true;
                return property_value_ref;
            }
        }

        #endregion

        #region SetStaticProperty

        /// <summary>
        /// Sets the value of a static property of a class.
        /// </summary>
        /// <param name="type">Represents the type to set the property of.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The new property value (can be a <see cref="PhpReference"/>).</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the property or the calling type desc at
        /// compile time. Otherwise, appropriate IL instructions that access the property directly are emitted.
        /// </remarks>
        /// <exception cref="PhpException">The property denoted by <paramref name="propertyName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The property is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static void SetStaticProperty(DTypeDesc type, object propertyName, object value, DTypeDesc caller,
            ScriptContext context)
        {
            DPropertyDesc property = GetStaticPropertyDesc(type, propertyName, caller, context, false);
            if (property == null) return;

            property.Set(null, value);
        }

        #endregion

        #region UnsetStaticProperty

        /// <summary>
        /// Throws the &quot;Attempt to unset static property&quot; error.
        /// </summary>
        /// <param name="type">Represents the type to &quot;unset&quot; the property of.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the operation.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the property or the calling type desc at
        /// compile time. Otherwise, the error throwing code is directly emitted.
        /// </remarks>
        /// <exception cref="PhpException">Static properties cannot be unset (Error).</exception>
        [Emitted]
        public static void UnsetStaticProperty(DTypeDesc type, object propertyName, DTypeDesc caller, ScriptContext context)
        {
            // convert propertyName to string
            string name = (propertyName == null ? String.Empty : Convert.ObjectToString(propertyName));

            // throw the error
            PhpException.StaticPropertyUnset(type.MakeFullName(), name);
        }

        #endregion

        #region GetStaticMethodDesc, InvokeStaticMethod

        /// <summary>
        /// Attemps to find a method desc according to a given class name and method name. Used when
        /// a non-virtual dispatch is about to be performed and when a <c>array(class, method)</c>
        /// callback is being bound.
        /// </summary>
        /// <param name="requestedType">The type whose method should be returned.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="self">Current <c>$this</c>. Will be set to an instance, on which the resulting
        /// CLR method should be invoked (<B>null</B> if the CLR method is static).</param>
        /// <param name="caller"><see cref="Type"/> of the object that request the lookup.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="quiet">If <B>true</B>, no exceptions will be thrown if an error occurs.</param>
        /// <param name="removeFrame">If <B>true</B>, <see cref="PhpStack.RemoveFrame"/> will be called
        /// before throwing an exception.</param>
        /// <param name="isCallerMethod">Will be set to true, if required method was not found but __callStatic was.</param>
        /// <returns>The <see cref="DRoutineDesc"/> or <B>null</B> on error.</returns>
        internal static DRoutineDesc GetStaticMethodDesc(DTypeDesc requestedType, string methodName, ref DObject self,
            DTypeDesc caller, ScriptContext context, bool quiet, bool removeFrame, out bool isCallerMethod)
        {
            Debug.Assert(requestedType != null);

            isCallerMethod = false;

            DRoutineDesc method;
            GetMemberResult result = requestedType.GetMethod(new Name(methodName), caller, out method);

            if (result == GetMemberResult.NotFound)
            {
                // if not found, perform __callStatic or __call 'magic' method lookup
                Name callMethod = (self != null && requestedType.IsAssignableFrom(self.TypeDesc)) ?
                    DObject.SpecialMethodNames.Call : DObject.SpecialMethodNames.CallStatic;

                if ((result = requestedType.GetMethod(callMethod, caller, out method)) != GetMemberResult.NotFound)
                {
                    isCallerMethod = true;
                }
                else
                {
                    // there is no such method in the class
                    if (removeFrame) context.Stack.RemoveFrame();
                    if (!quiet) PhpException.UndefinedMethodCalled(requestedType.MakeFullName(), methodName);

                    return null;
                }
            }

            if (result == GetMemberResult.BadVisibility)
            {
                if (removeFrame) context.Stack.RemoveFrame();
                if (!quiet)
                {
                    PhpException.MethodNotAccessible(
                        method.DeclaringType.MakeFullName(),
                        method.MakeFullName(),
                        (caller == null ? String.Empty : caller.MakeFullName()),
                        method.IsProtected);
                }
                return null;
            }

            // check whether the method is abstract
            if (method.IsAbstract)
            {
                if (removeFrame) context.Stack.RemoveFrame();
                if (!quiet) PhpException.AbstractMethodCalled(method.DeclaringType.MakeFullName(), method.MakeFullName());

                return null;
            }

            if (method.IsStatic)
            {
                self = null;
            }
            else
            {
                // check whether self is of acceptable type
                if (self != null && !method.DeclaringType.RealType.IsInstanceOfType(self.RealObject)) self = null;


                /*
                // PHP allows for static invocations of instance method
				if (self == null &&
					(requestedType.IsAbstract || !(requestedType is PhpTypeDesc)) &&
					(method.DeclaringType.IsAbstract || !(method.DeclaringType is PhpTypeDesc)))
				{
					// calling instance methods declared in abstract classes statically through abstract classes
					// is unsupported -  passing null as 'this' to such instance method could result in
					// NullReferenceException even if the method does not touch $this
					if (removeFrame) context.Stack.RemoveFrame();
					if (!quiet)
					{
						PhpException.Throw(PhpError.Error, CoreResources.GetString("nonstatic_method_called_statically",
							method.DeclaringType.MakeFullName(), method.MakeFullName()));
					}
					return null;
				}

				if (self == null)
				{
                    if (!quiet && !context.Config.Variables.ZendEngineV1Compatible)
                    {
                        PhpException.Throw(PhpError.Strict, CoreResources.GetString("nonstatic_method_called_statically",
                            method.DeclaringType.MakeFullName(), method.MakeFullName()));
                    }

					// create a dummy instance to be passed as 'this' to the instance method
					DTypeDesc dummy_type =
						(!requestedType.IsAbstract && requestedType is PhpTypeDesc) ? requestedType : method.DeclaringType;

					self = PhpFunctionUtils.InvokeConstructor(
                        dummy_type,
                        //Emit.Types.ScriptContext_Bool,
                        context, false);
				}*/


                //
                // The code above was commented and replaced with following.
                //
                // We can call instance method, and pass null as 'this', and expect
                // it can fail with NullReferenceException (even if the method does not touch $this).
                // 
                // Note this solution has no side effect as above - invoking constructor of dummy instance.
                //

                // !! self can be null !!

                if (self == null)
                {
                    if (!quiet /*&& !context.Config.Variables.ZendEngineV1Compatible*/)
                    {
                        PhpException.Throw(PhpError.Strict, CoreResources.GetString("nonstatic_method_called_statically",
                            method.DeclaringType.MakeFullName(), method.MakeFullName()));
                    }
                }

            }

            return method;
        }

        /// <summary>
        /// Performs the &quot;static style&quot; invocation (<c>A::f()</c>) of a method.
        /// </summary>
        /// <param name="type"><see cref="DTypeDesc"/> representing the type to invoke the method on.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="self">Current object context. If an instance method is invoked in another instance method statically,
        /// it is passed current <c>$this</c> and no notice is thrown.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that request the invocation. Should not be unknown.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The method's return value (always a <see cref="PhpReference"/>).</returns>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the method or the calling type desc at
        /// compile time. Otherwise, the <c>OpCodes.Call</c> IL instruction is directly emitted.
        /// </remarks>
        /// <exception cref="PhpException">The <paramref name="methodName"/> is not a string. (Error)</exception>
        /// <exception cref="PhpException">The method denoted by <paramref name="methodName"/> was not found. (Error)</exception>
        /// <exception cref="PhpException">The method is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        /// <exception cref="PhpException">The method is abstract (Error).</exception>
        /// <exception cref="PhpException">The method is not static (Error or Strict).</exception>
        [Emitted]
        public static PhpReference InvokeStaticMethod(DTypeDesc type, object methodName, DObject self,
            DTypeDesc caller, ScriptContext context)
        {
            if (type == null)
            {
                // error thrown earlier
                return new PhpReference();
            }

            // verify that methodName is a string
            string name = PhpVariable.AsString(methodName);
            if (String.IsNullOrEmpty(name))
            {
                context.Stack.RemoveFrame();
                PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_method_name"));
                return new PhpReference();
            }

            // find the method desc
            bool isCallStaticMethod;
            DRoutineDesc method = GetStaticMethodDesc(type, name, ref self, caller, context, false, true, out isCallStaticMethod);

            if (method == null) return new PhpReference();

            // invoke the method
            object result;
            var stack = context.Stack;
            stack.LateStaticBindType = type;
               
            if (isCallStaticMethod)
            {
                // __callStatic was found instead, not {methodName}
                PhpArray args = stack.CollectFrame();   // get array with args, remove the previous stack

                // original parameters are passed to __callStatic in an array as the second parameter
                stack.AddFrame(methodName, args);
                result = method.Invoke(self, stack, caller);
            }
            else
            {
//                try
//                {
                    result = method.Invoke(self, stack, caller);
//                }
//                catch (NullReferenceException)
//                {
//                    if (self == null && !method.IsStatic)
//                    {   // $this was null, it is probably caused by accessing $this
//#if DEBUG
//                        throw;
//#else
//                    PhpException.ThisUsedOutOfObjectContext();
//                    result = null;
//#endif
//                    }
//                    else
//                    {
//                        throw;  // $this was not null, this should not be handled here
//                    }
//                }
            }

            return PhpVariable.MakeReference(PhpVariable.Copy(result, CopyReason.ReturnedByCopy));
        }

        #endregion

        #region New, InstanceOf, MakeGenericTypeInstantiation

        /// <summary>
        /// Creates a new instance of a given type.
        /// </summary>
        /// <param name="type"><see cref="DTypeDesc"/> representing the type to instantiate.</param>
        /// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the instantiation.
        /// </param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="nameContext">Current <see cref="NamingContext"/>.</param>
        /// <returns>The new instance or <B>null</B> if an error occurs.</returns>
        /// <remarks>
        /// <para>
        /// This operator expects that constructor parameters have been pushed onto <see cref="ScriptContext.Stack"/>.
        /// </para>
        /// <para>
        /// Note that this operator is used only when it is impossible to resolve the class at compile time. Otherwise,
        /// the <c>OpCodes.Newobj</c> IL instruction is directly emitted.
        /// </para>
        /// </remarks>
        /// <exception cref="PhpException"><paramref name="type"/> denotes an interface. (Error)</exception>
        /// <exception cref="PhpException"><paramref name="type"/> denotes an abstract class. (Error)</exception>
        /// <exception cref="PhpException">A constructor is inaccessible due to its protected or private visibility level (Error).
        /// </exception>
        [Emitted]
        public static object New(DTypeDesc type, DTypeDesc caller, ScriptContext context, NamingContext nameContext)
        {
            // error has been thrown by Convert.ObjectToTypeDesc or MakeGenericTypeInstantiation
            if (type == null)
            {
                context.Stack.RemoveFrame();
                return null;
            }

            // interfaces and abstract classes cannot be instantiated
            if (type.IsAbstract)
            {
                context.Stack.RemoveFrame();
                PhpException.CannotInstantiateType(type.MakeFullName(), type.IsInterface);
                return null;
            }

            return type.New(context.Stack, caller, nameContext);
        }

        /// <summary>
        /// Creates a new instance of a given CLR type.
        /// </summary>
        /// <param name="clrType"><see cref="ClrTypeDesc"/> representing the type to instantiate.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>The new instance or <B>null</B> if an error occurs.</returns>
        [Emitted]
        public static object NewClr(DTypeDesc clrType, ScriptContext context)
        {
            PhpStack stack = context.Stack;
            if (clrType == null)
            {
                stack.RemoveFrame();
                return null;
            }

            // invoke constructor argless stub, which will instantiate the type
            stack.AllowProtectedCall = true;
            return /*(DObject)*/((ClrTypeDesc)clrType).Constructor.Invoke(null, stack);
        }

        /// <summary>
        /// Determines whether a variable is an instance of a given type.
        /// </summary>
        /// <param name="var">The variable to check.</param>
        /// <param name="type"><see cref="DTypeDesc"/> representing the given type.</param>
        /// <returns><B>true</B> if <paramref name="var"/> is an instance of a class or interface given by
        /// <paramref name="type"/>, <B>false</B> otherwise.</returns>
        /// <remarks>
        /// Note that this operator is used only when it is impossible to resolve the class at compile time. Otherwise,
        /// the <c>OpCodes.Isinst</c> IL instruction is directly emitted.
        /// </remarks>
        [Emitted]
        public static bool InstanceOf(object var, DTypeDesc type)
        {
            return (type != null) ? type.RealType.IsInstanceOfType(PhpVariable.Unwrap(var)) : false;
        }

        [Emitted]
        public static DObject TypeOf(DTypeDesc type)
        {
            return (type != null) ? ClrObject.WrapRealObject(type.RealType) : null;
        }

        [Emitted]
        public static DTypeDesc MakeGenericTypeInstantiation(DTypeDesc genericType, DTypeDesc arg1)
        {
            return MakeGenericTypeInstantiation(genericType, new DTypeDesc[] { arg1 });
        }

        [Emitted]
        public static DTypeDesc MakeGenericTypeInstantiation(DTypeDesc genericType, DTypeDesc arg1, DTypeDesc arg2)
        {
            return MakeGenericTypeInstantiation(genericType, new DTypeDesc[] { arg1, arg2 });
        }

        [Emitted]
        public static DTypeDesc MakeGenericTypeInstantiation(DTypeDesc genericType, DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3)
        {
            return MakeGenericTypeInstantiation(genericType, new DTypeDesc[] { arg1, arg2, arg3 });
        }

        [Emitted]
        public static DTypeDesc MakeGenericTypeInstantiation(DTypeDesc genericType, DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3, DTypeDesc arg4)
        {
            return MakeGenericTypeInstantiation(genericType, new DTypeDesc[] { arg1, arg2, arg3, arg4 });
        }

        [Emitted]
        public static DTypeDesc MakeGenericTypeInstantiation(DTypeDesc genericType, DTypeDesc[]/*!*/ args)
        {
            return MakeGenericTypeInstantiation(genericType, args, args.Length);
        }

        [Emitted]
        public static DTypeDesc MakeGenericTypeInstantiation(DTypeDesc genericType, DTypeDesc[]/*!*/ args, int argCount)
        {
            // error already reported:
            if (genericType == null) return null;

            // checks the arguments and substitutes the default types to the missing ones if applicable:
            if (!genericType.MakeGenericArguments(ref args, ref argCount, _ReportErrorMakingInstantiation))
            {
                // some mandatory arguments are missing:
                return null;
            }

            Type[] real_args = new Type[argCount];
            for (int i = 0; i < argCount; i++)
            {
                // error already reported:
                if (args[i] == null) return null;
                real_args[i] = args[i].RealType;
            }

            Type instantiation = genericType.RealType.MakeGenericType(real_args);

            return DTypeDesc.Create(instantiation);
        }

        private static void ReportErrorMakingInstantiation(DTypeDesc.MakeGenericArgumentsResult/*!*/ error,
            DTypeDesc/*!*/ genericType, DTypeDesc argument, GenericParameterDesc/*!*/ parameter)
        {
            switch (error)
            {
                case DTypeDesc.MakeGenericArgumentsResult.IncompatibleConstraint:
                    PhpException.Throw(PhpError.Error, CoreResources.GetString("incompatible_type_parameter_constraints_type",
                        argument.MakeFullName(), parameter.RealType.GenericParameterPosition, parameter.RealType.Name));
                    break;

                case DTypeDesc.MakeGenericArgumentsResult.MissingArgument:
                    PhpException.Throw(PhpError.Error, CoreResources.GetString("missing_type_argument_in_type_use",
                        genericType.MakeFullName(), parameter.RealType.GenericParameterPosition, parameter.RealType.Name));
                    break;

                case DTypeDesc.MakeGenericArgumentsResult.TooManyArguments:
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("too_many_type_arguments_in_type_use",
                        genericType.MakeFullName(), genericType.GenericParameters.Length));
                    break;
            }
        }

        private static readonly Action<DTypeDesc.MakeGenericArgumentsResult, DTypeDesc, DTypeDesc, GenericParameterDesc>/*!*/ _ReportErrorMakingInstantiation =
            new Action<DTypeDesc.MakeGenericArgumentsResult, DTypeDesc, DTypeDesc, GenericParameterDesc>(ReportErrorMakingInstantiation);

        #endregion

        #endregion

        #region Strict Equality Operator

        /// <summary>
        /// Compares two objects for strict equality in a manner of the PHP.
        /// </summary>
        /// <param name="x">The first object.</param>
        /// <param name="y">The second object.</param>
        /// <returns>Whether the values and types of operands are the same.</returns>
        [Emitted]
        public static bool StrictEquality(object x, object y)
        {
            if (x == null || y == null) return x == y;

            // objects are strictly equal in a terms of ZE2 iff references are the same:
            DObject objx = x as DObject;
            if (objx != null)
            {
                //if (!ScriptContext.CurrentContext.Config.Variables.ZendEngineV1Compatible)
                //{
                return Object.ReferenceEquals(x, y);
                //}

                //DObject objy = y as DObject;
                //return (objy != null && objx.TypeDesc == objy.TypeDesc && PhpComparer./*Default.*/CompareEq(x, y));
            }

            // compares arrays strictly:
            PhpArray ax, ay;
            if ((ax = x as PhpArray) != null)
            {
                return ((ay = y as PhpArray) != null) ? ax.StrictCompareEq(ay) : false;
            }

            Type xtype;
            Type ytype;

            if (x.GetType() == typeof(PhpBytes) || x.GetType() == typeof(PhpString)) xtype = typeof(string); else xtype = x.GetType();
            if (y.GetType() == typeof(PhpBytes) || y.GetType() == typeof(PhpString)) ytype = typeof(string); else ytype = y.GetType();

            return xtype == ytype && PhpComparer./*Default.*/CompareEq(x, y);
        }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Throws an exception.
        /// </summary>
        /// <param name="context">A script context.</param>
        /// <param name="variable">An object representing the exception to throw.</param>
        /// <exception cref="PhpException"><paramref name="variable"/> is not valid exception object (Error).</exception>
        /// <exception cref="PhpUserException">The required exception thrown.</exception>
        [Emitted]
        public static void Throw(ScriptContext context, object variable)
        {
            Library.SPL.Exception e = variable as Library.SPL.Exception;
            if (e == null)
            {
                PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_exception_object"));
            }
            else
            {
                throw new PhpUserException(e);
            }
        }

        #endregion

        #region Paths

        /// <summary>
        /// Converts relative path to absolute using source root. For internal use only.
        /// </summary>
        public static string ToAbsoluteSourcePath(sbyte level, string/*!*/ path)
        {
            Debug.Assert(path != null);
            return new RelativePath(level, path).ToFullPath(Configuration.Application.Compiler.SourceRoot).ToString();
        }

        #endregion

        #region LINQ Operators
        /*
    public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
		}

    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		{
		}
   
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
		}

    public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
		}

		public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
		}

    public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
		}
   
    public static IEnumerable<TResult> SelectMany<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
		{
		}
*/
        public static object Where(object source, object predicate)
        {
            // TODO: convert from LinqSource
            source = ClrObject.WrapRealObject(source);

            bool isMagic;
            DRoutineDesc desc = ((DObject)source).GetMethodDesc("Where", null, true, out isMagic);
            if (desc != null)
            {
                ScriptContext.CurrentContext.Stack.AddFrame(source, predicate);
                return Operators.InvokeMethod(source, "Where", null, ScriptContext.CurrentContext).Value;
            }
            else
            {
                ScriptContext.CurrentContext.Stack.AddFrame(source, predicate);
                DTypeDesc dty = DTypeDesc.Create(Emit.LinqExterns.Sequence);
                return Operators.InvokeStaticMethod(dty, "Where", null, null, ScriptContext.CurrentContext).Value;
            }
        }

        public static object Select(object source, object selector)
        {
            return source;
        }

        #endregion
    }

    #region Unit Tests
#if DEBUG

    sealed class TestOperators
    {
        [Test]
        static void TestAddition()
        {
            object[,] cases = 
			{
			{ 1, 2, 3 },
			{ Int32.MaxValue-10, "10dfghgfh", Int32.MaxValue },
			{ "-100", "+100", 0 },
			{ "100", "100.0000000001", 200.0000000001 },
			};

            for (int i = 0; i < cases.GetLength(1); i++)
            {
                object result = Operators.Add(cases[i, 0], cases[i, 1]);
                Debug.Assert(Object.Equals(result, cases[i, 2]));
            }

            PhpArray a = Operators.Add(
                PhpArray.New(new object[] { "a", 5, 7 }),
                PhpArray.New(new object[] { "8q", 1 })
            ) as PhpArray;

            Debug.Assert(a != null && a.Count == 3 && (string)a[0] == "a" && (int)a[1] == 5 && (int)a[2] == 7);
        }

        [Test]
        static void TestShiftLeft()
        {
            object[,] cases = 
			{
			{ "1.5xxx", -35, 536870912 },
			{ "1.5xxx",   0, 1 },
			{ "1.5xxx",  34, 17179869184L } // 64bit behaviour
			};

            for (int i = 0; i < cases.GetLength(1); i++)
            {
                object result = Operators.ShiftLeft(cases[i, 0], cases[i, 1]);
                Debug.Assert(Object.Equals(result, cases[i, 2]));
            }
        }

        [Test]
        static void Concat()
        {
            PhpBytes a = new PhpBytes(new byte[] { 61, 62, 63 });
            string b = "-hello-";
            PhpBytes c = new PhpBytes(new byte[] { 61, 61, 61 });
            string d = "-bye-";

            object result = Operators.Concat(a, b, c, d);
            Debug.Assert(Operators.StrictEquality(result, "=>?-hello-===-bye-"));
        }
    }

#endif
    #endregion
}
