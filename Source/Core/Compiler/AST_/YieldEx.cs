/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(YieldEx))]
        sealed class YieldExCompiler : ExpressionCompiler<YieldEx>
        {
            public override Evaluation Analyze(YieldEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                Statistics.AST.AddNode("YieldEx");

                throw new NotImplementedException();
            }

            public override PhpTypeCode Emit(YieldEx node, CodeGenerator codeGenerator)
            {
                throw new NotImplementedException();
            }
        }
    }
}