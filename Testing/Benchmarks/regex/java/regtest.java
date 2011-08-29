public class regtest
{
    /*
     * Copyright (c) 2005, Damien Mascord <tusker@tusker.org> All rights reserved.
     * 
     * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following
     * conditions are met:
     * 
     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following
     * disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of the <ORGANIZATION>
     * nor the names of its contributors may be used to endorse or promote products derived from this software without specific
     * prior written permission. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
     * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
     * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
     * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
     * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
     * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
     * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
     * 
     */

    private static final String[] _re = {"^(([^:]+)://)?([^:/]+)(:([0-9]+))?(/.*)", // URL match
        "(([^:]+)://)?([^:/]+)(:([0-9]+))?(/.*)", // URL match without starting ^
        "usd [+-]?[0-9]+.[0-9][0-9]", // Canonical US dollar amount
        "\\b(\\w+)(\\s+\\1)+\\b", // Duplicate words
        "\\{(\\d+):(([^}](?!-} ))*)", // this is meant to match against the "some more text and ..." but it causes ORO Matcher
    								  // to fail, so we won't include this by default... it is also WAY too slow to test
                                      // we will test large string 10 times
    };

    private static final String[] _str = {
        "http://www.linux.com/",
        "http://www.thelinuxshow.com/main.php3",
        "usd 1234.00",
        "he said she said he said no",
        "same same same",
        "{1:\n" + "this is some more text - and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more\n"
            + "this is some more text and some more and some more and even more at the end\n" + "-}\n", // very large bit of text...

    };

    private static boolean[][] expectedMatch = new boolean[_re.length][_str.length];

    static
    {
        expectedMatch[0][0] = true;
        expectedMatch[0][1] = true;
        expectedMatch[0][2] = false;
        expectedMatch[0][3] = false;
        expectedMatch[0][4] = false;
        expectedMatch[0][5] = false;
        expectedMatch[1][0] = true;
        expectedMatch[1][1] = true;
        expectedMatch[1][2] = false;
        expectedMatch[1][3] = false;
        expectedMatch[1][4] = false;
        expectedMatch[1][5] = false;
        expectedMatch[2][0] = false;
        expectedMatch[2][1] = false;
        expectedMatch[2][2] = true;
        expectedMatch[2][3] = false;
        expectedMatch[2][4] = false;
        expectedMatch[2][5] = false;
        expectedMatch[3][0] = false;
        expectedMatch[3][1] = false;
        expectedMatch[3][2] = false;
        expectedMatch[3][3] = false;
        expectedMatch[3][4] = true;
        expectedMatch[3][5] = false;
        expectedMatch[4][0] = false;
        expectedMatch[4][1] = false;
        expectedMatch[4][2] = false;
        expectedMatch[4][3] = false;
        expectedMatch[4][4] = false;
        expectedMatch[4][5] = false;
    }

    private static boolean debug = false;
    private static boolean html = true;

    private final static int ITERATIONS = 10000;

    public static final void main(String[] args)
    {
        try
        {
            // org.apache.regexp.* test
            if (debug) System.out.println("Testing org.apache.regexp.RE...");
            long[][][] timeTaken = new long[_re.length][ITERATIONS][_str.length];
            boolean[][] matches = new boolean[_re.length][_str.length];
            long startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
				if (debug)
				{
					System.out.print("New regnum " + regnum + "...\n");
				}
                try
                {
                    org.apache.regexp.RE regexpr = new org.apache.regexp.RE(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = regexpr.match(_str[strnum]);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            long endTime = System.currentTimeMillis();
            printResult("org.apache.regexp.RE", timeTaken, (endTime - startTime), matches, html);
            // ----------------------//

			System.out.println("Testing com.stevesoft.pat.Regex...");
			
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
				try
                {
					com.stevesoft.pat.Regex regexpr = new com.stevesoft.pat.Regex(_re[regnum]);
					int testedAgainstLargeString = 0;
					for (int itter = 0; itter < ITERATIONS; itter++)
					{
						for (int strnum = 0; strnum < _str.length; strnum++)
						{
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
							boolean b = regexpr.search(_str[strnum]);
							matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
							timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();
            printResult("com.stevesoft.pat.Regex", timeTaken, (endTime - startTime), matches, html);
            // ----------------------//
            
            // System.out.println("Testing com.ibm.regex.RegularExpression...");

            // startTime = System.currentTimeMillis();
            // for (int regnum = 0; regnum < _re.length; regnum++)
            // {
                // com.ibm.regex.RegularExpression regexpr = new com.ibm.regex.RegularExpression(_re[regnum]);
                // int testedAgainstLargeString = 0;
                // for (int itter = 0; itter < ITERATIONS; itter++)
                // {
                    // for (int strnum = 0; strnum < _str.length; strnum++)
                    // {
						// if (debug && (itter % 1000) == 0)
							// {
								// System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							// }
							
							// if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            // }
                            
                            // // only test the big one 10 iterations only per regex
                            // if (testedAgainstLargeString > 10 && strnum == 5)
                            // {
                                // break;
							// }
							
							// long iterStarTime = System.currentTimeMillis();
                        // boolean b = regexpr.matches(_str[strnum]);
                        // matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                        // timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // // count how many times we test against the large string on this regex
                            // if (strnum == 5)
                            // {
								// testedAgainstLargeString++;
							// }
                            
                            // if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(b);
                            // }
                            
                            // if (debug && (itter % 1000) == 0)
							// {
								// System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							// }
                        // }
                    // }
            // }
            // endTime = System.currentTimeMillis();
            // printResult("com.ibm.regex.RegularExpression", timeTaken, (endTime - startTime), matches, html);
            
            // ----------------------//

			System.out.println("Testing kmy.regex.util.Regex...");

            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    kmy.regex.util.Regex regexpr = kmy.regex.util.Regex.createRegex(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = regexpr.matches(_str[strnum]);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable th)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + " failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();
            printResult("kmy.regex.util.Regex", timeTaken, (endTime - startTime), matches, html);
            // ----------------------//
            // jdk 1.4 version
            
            System.out.println("Testing java.util.regex.Pattern...");

            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    java.util.regex.Pattern regexpr = java.util.regex.Pattern.compile(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            java.util.regex.Matcher m = regexpr.matcher(_str[strnum]);
                            boolean b = m.find();
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();
            printResult("java.util.regex.Pattern", timeTaken, (endTime - startTime), matches, html);
            // ----------------------//

			System.out.println("Testing jregex.Pattern...");

            // jregex.Pattern version
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    jregex.Pattern regexpr = new jregex.Pattern(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            jregex.Matcher m = regexpr.matcher(_str[strnum]);
                            boolean b = m.matches();
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();
            printResult("jregex.Pattern", timeTaken, (endTime - startTime), matches, html);

            // ----------------------//
			System.out.println("Testing org.apache.oro.text.regex.Perl5Matcher...");
            // org.apache.oro.text.regex.Perl5Matcher version
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    org.apache.oro.text.regex.Perl5Compiler perl5Compiler = new org.apache.oro.text.regex.Perl5Compiler();
                    org.apache.oro.text.regex.Perl5Matcher perl5Matcher = new org.apache.oro.text.regex.Perl5Matcher();
                    org.apache.oro.text.regex.Pattern regexpr = perl5Compiler.compile(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = perl5Matcher.matches(_str[strnum], regexpr);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();
            printResult("org.apache.oro.text.regex.Perl5Matcher", timeTaken, (endTime - startTime), matches, html);

            // ----------------------//
			// System.out.println("Testing RegularExpression.RE...");
            // // RegularExpression.RE version
            
            // startTime = System.currentTimeMillis();
            // for (int regnum = 0; regnum < _re.length; regnum++)
            // {
                // try
                // {
                    // RegularExpression.RE regexpr = new RegularExpression.RE(_re[regnum], true);
                    // int testedAgainstLargeString = 0;
                    // for (int itter = 0; itter < ITERATIONS; itter++)
                    // {
                        // for (int strnum = 0; strnum < _str.length; strnum++)
                        // {
							// if (debug && (itter % 1000) == 0)
							// {
								// System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							// }
							
							// if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            // }
                            
                            // // only test the big one 10 iterations only per regex
                            // if (testedAgainstLargeString > 10 && strnum == 5)
                            // {
                                // break;
							// }
							
							// long iterStarTime = System.currentTimeMillis();
                            // boolean b = regexpr.matches(_str[strnum]);
                            // matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            // timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // // count how many times we test against the large string on this regex
                            // if (strnum == 5)
                            // {
								// testedAgainstLargeString++;
							// }
                            
                            // if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(b);
                            // }
                            
                            // if (debug && (itter % 1000) == 0)
							// {
								// System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							// }
                        // }
                    // }
                // }
                // catch (Throwable e)
                // {
                    // if (debug)
                    // {
                        // System.out.println(_re[regnum] + "  failed badly");
                    // }
                // }
            // }
            // endTime = System.currentTimeMillis();
            // printResult("RegularExpression.RE", timeTaken, (endTime - startTime), matches, html);
            
           
            // ----------------------//
            // System.out.println("Testing gnu.rex.Rex...");
            // // gnu.rex.Rex version
            
            // startTime = System.currentTimeMillis();
            // for (int regnum = 0; regnum < _re.length; regnum++)
            // {
                // try
                // {
                    // gnu.rex.Rex.config_GroupBraces("(", ")"); 
                    // gnu.rex.Rex.config_Alternative("|"); 
                    // gnu.rex.Rex regexpr = gnu.rex.Rex.build(_re[regnum]);
                    // int testedAgainstLargeString = 0;
                    // for (int itter = 0; itter < ITERATIONS; itter++)
                    // {
                        // for (int strnum = 0; strnum < _str.length; strnum++)
                        // {
							// if (debug && (itter % 1000) == 0)
							// {
								// System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							// }
							
							// if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            // }
                            
                            // // only test the big one 10 iterations only per regex
                            // if (testedAgainstLargeString > 10 && strnum == 5)
                            // {
                                // break;
							// }
							
							// long iterStarTime = System.currentTimeMillis();
                            // boolean b = regexpr.match(_str[strnum].toCharArray(),0,0).length() > 0;
                            
                            // matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            // timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // // count how many times we test against the large string on this regex
                            // if (strnum == 5)
                            // {
								// testedAgainstLargeString++;
							// }
                            
                            // if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(b);
                            // }
                            
                            // if (debug && (itter % 1000) == 0)
							// {
								// System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							// }
                        // }
                    // }
                // }
                // catch (Throwable e)
                // {
                    // if (debug)
                    // {
                        // System.out.println(_re[regnum] + "  failed badly");
                    // }
                // }
            // }
            // endTime = System.currentTimeMillis();
            // printResult("gnu.rex.Rex", timeTaken, (endTime - startTime), matches, html);
            

            // ----------------------//
            // dk.brics.automaton.RegExp version [fails on URL tests]
            
            System.out.println("Testing dk.brics.automaton.RegExp...");
            
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    dk.brics.automaton.RegExp regexpr = new dk.brics.automaton.RegExp(_re[regnum]);
                    dk.brics.automaton.Automaton auto = regexpr.toAutomaton();
                    dk.brics.automaton.RunAutomaton runauto = new dk.brics.automaton.RunAutomaton(auto, true);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = runauto.run(_str[strnum]);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();
            printResult("dk.brics.automaton.RegExp", timeTaken, (endTime - startTime), matches, html);
            // ----------------------//
            // ----------------------//
            // com.karneim.util.collection.regex.Pattern version
            
            System.out.println("Testing com.karneim.util.collection.regex.Pattern...");
            
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    com.karneim.util.collection.regex.Pattern p = new com.karneim.util.collection.regex.Pattern(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = p.contains(_str[strnum]);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();

            printResult("com.karneim.util.collection.regex.Pattern", timeTaken, (endTime - startTime), matches, html);
            
            // ----------------------//
            // ----------------------//
            // org.apache.xerces.impl.xpath.regex.RegularExpression version
            
            // System.out.println("Testing org.apache.xerces.impl.xpath.regex.RegularExpression...");
            
            // startTime = System.currentTimeMillis();
            // for (int regnum = 0; regnum < _re.length; regnum++)
            // {
                // try
                // {
                    // org.apache.xerces.impl.xpath.regex.RegularExpression p = new org.apache.xerces.impl.xpath.regex.RegularExpression(_re[regnum]);
                    // int testedAgainstLargeString = 0;
                    // for (int itter = 0; itter < ITERATIONS; itter++)
                    // {
                        // for (int strnum = 0; strnum < _str.length; strnum++)
                        // {
							// if (debug && (itter % 1000) == 0)
							// {
								// System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							// }
							
							// if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            // }
                            
                            // // only test the big one 10 iterations only per regex
                            // if (testedAgainstLargeString > 10 && strnum == 5)
                            // {
                                // break;
							// }
							
							// long iterStarTime = System.currentTimeMillis();
                            // boolean b = p.matches(_str[strnum]);
                            // matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            // timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // // count how many times we test against the large string on this regex
                            // if (strnum == 5)
                            // {
								// testedAgainstLargeString++;
							// }
                            
                            // if (debug && (itter % 1000) == 0)
                            // {
                                // System.out.print(b);
                            // }
                            
                            // if (debug && (itter % 1000) == 0)
							// {
								// System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							// }
                        // }
                    // }
                // }
                // catch (Throwable e)
                // {
                    // if (debug)
                    // {
                        // System.out.println(_re[regnum] + "  failed badly");
                    // }
                // }
            // }
            // endTime = System.currentTimeMillis();

            // printResult("org.apache.xerces.impl.xpath.regex.RegularExpression", timeTaken, (endTime - startTime), matches, html);
            
            // ----------------------//
            // ----------------------//
            // monq.jfa.Regexp version
            System.out.println("Testing monq.jfa.Regexp...");
            
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    monq.jfa.Regexp p = new monq.jfa.Regexp(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = p.matches(_str[strnum]);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();

            printResult("monq.jfa.Regexp", timeTaken, (endTime - startTime), matches, html);
 
            
            
			// ----------------------//
            // ----------------------//
            // com.ibm.icu.text.UnicodeSet version
            
			System.out.println("Testing com.ibm.icu.text.UnicodeSet...");
            
            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                try
                {
                    com.ibm.icu.text.UnicodeSet p = new com.ibm.icu.text.UnicodeSet(_re[regnum]);
                    int testedAgainstLargeString = 0;
                    for (int itter = 0; itter < ITERATIONS; itter++)
                    {
                        for (int strnum = 0; strnum < _str.length; strnum++)
                        {
							if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                            boolean b = p.containsAll(_str[strnum]);
                            matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                            timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
                }
                catch (Throwable e)
                {
                    if (debug)
                    {
                        System.out.println(_re[regnum] + "  failed badly");
                    }
                }
            }
            endTime = System.currentTimeMillis();

            printResult("com.ibm.icu.text.UnicodeSet", timeTaken, (endTime - startTime), matches, html);            // -----gnu.regexp.RE-----//

			System.out.println("Testing gnu.regexp.RE...");

            startTime = System.currentTimeMillis();
            for (int regnum = 0; regnum < _re.length; regnum++)
            {
                gnu.regexp.RE regexpr = new gnu.regexp.RE(_re[regnum]);
                int testedAgainstLargeString = 0;
                for (int itter = 0; itter < ITERATIONS; itter++)
                {
                    
                    for (int strnum = 0; strnum < _str.length; strnum++)
                    {
						if (debug && (itter % 1000) == 0)
							{
								System.out.print("Iteration/regex number/string number " + itter + "/" + regnum + "/" + strnum + "... ");
							}
							
							if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(_re[regnum] + " against " + _str[strnum] + ":");
                            }
                            
                            // only test the big one 10 iterations only per regex
                            if (testedAgainstLargeString > 10 && strnum == 5)
                            {
                                break;
							}
							
							long iterStarTime = System.currentTimeMillis();
                        boolean b = regexpr.isMatch(_str[strnum]);
                        matches[regnum][strnum] = (b == expectedMatch[regnum][strnum]);
                        timeTaken[regnum][itter][strnum] = (System.currentTimeMillis() - iterStarTime);
                            
                            // count how many times we test against the large string on this regex
                            if (strnum == 5)
                            {
								testedAgainstLargeString++;
							}
                            
                            if (debug && (itter % 1000) == 0)
                            {
                                System.out.print(b);
                            }
                            
                            if (debug && (itter % 1000) == 0)
							{
								System.out.print(" took " + timeTaken[regnum][itter][strnum] + "ms" + "\n");
							}
                        }
                    }
            }
            endTime = System.currentTimeMillis();
            printResult("gnu.regexp.RE", timeTaken, (endTime - startTime), matches, html);
            
        }
        catch (Exception e)
        {
            e.printStackTrace();
        }
    }

    private static final void printResult(String regexName, long[][][] matrix, long totalTime, boolean[][] matches, boolean html)
    {
        // timeTaken[regnum][itter][strnum]
        if (html)
        {
            System.out.println("<table>");
            System.out.println("<tr><th colspan=\"3\"><h2>Regular expression library:</h2></th><td colspan=\"3\"><h2>" + regexName
                + "</h2></td></tr>");
        }
        else
        {
            System.out.println("------------------------------------------");
            System.out.println("Regular expression library: " + regexName + "\n");
        }
        for (int re = 0; re < _re.length; re++)
        {
            if (html)
            {
                System.out.println("<tr><th>RE:</th><td colspan=\"5\">" + _re[re] + "</td></tr>");
                System.out
                    .println("<tr><th>MS</th><th>MAX</th><th>AVG</th><th>MIN</th><th>DEV</th><th>INPUT</th><th>MATCH</th></tr>");
            }
            else
            {
                System.out.println("RE: " + _re[re]);
                System.out.println("  MS\tMAX\tAVG\tMIN\tDEV\tINPUT\tMATCH");
            }
            for (int str = 0; str < _str.length; str++)
            {
                long total = 0;
                long sumOfSq = 0;
                long min = Long.MAX_VALUE;
                long max = Long.MIN_VALUE;
                for (int i = 0; i < ITERATIONS; i++)
                {
                    long elapsed = matrix[re][i][str];
                    total += elapsed;
                    sumOfSq += elapsed * elapsed;
                    if (elapsed < min)
                    {
                        min = elapsed;
                    }
                    if (elapsed > max)
                    {
                        max = elapsed;
                    }
                }
                // calc std dev
                long stdDev = (long) java.lang.Math.sqrt((sumOfSq - ((total * total) / ITERATIONS)) / (ITERATIONS - 1));

                if (html)
                {
                    System.out.println("<tr><td>" + total + "</td><td>" + max + "</td><td>" + (double) total / ITERATIONS
                        + "</td><td>" + min + "</td><td>" + stdDev + "</td><td>" + _str[str] + "</td><td>" + matches[re][str]
                        + "</td></tr>");
                }
                else
                {
                    System.out.println("  " + total + "\t" + max + "\t" + (double) total / ITERATIONS + "\t" + min + "\t" + stdDev
                        + "\t'" + _str[str] + "\t'" + matches[re][str] + "'");
                }
            }
        }
        if (html)
        {
            System.out.println("<tr><th colspan=\"3\"><h2>Total time taken:</h2></th><td colspan=\"3\"><h2>" + totalTime
                + "</h2></td></tr>");
            System.out.println("</table>");
        }
        else
        {
            System.out.println("Total time taken: " + totalTime);
            System.out.println("------------------------------------------");
        }
    }

}
