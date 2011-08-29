@echo off
set JAVA_HOME="C:\Program Files\Java\jdk1.7.0"

%JAVA_HOME%\bin\javac -classpath jrexx-1.1.1.jar;automaton.jar;jregex1.2_01.jar;gnu-regexp-1.1.4.jar;patbinfree153.jar;jakarta-regexp-1.5.jar;jakarta-oro-2.0.8.jar;jint.jar;icu4j-4_8_1_1.jar;monq-1.1.1.jar regtest.java
%JAVA_HOME%\bin\java -classpath .;jrexx-1.1.1.jar;automaton.jar;jregex1.2_01.jar;gnu-regexp-1.1.4.jar;patbinfree153.jar;jakarta-regexp-1.5.jar;jakarta-oro-2.0.8.jar;jint.jar;icu4j-4_8_1_1.jar;monq-1.1.1.jar regtest > res.html
