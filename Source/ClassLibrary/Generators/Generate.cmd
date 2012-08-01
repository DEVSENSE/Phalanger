"..\..\..\Tools\cslex" "StrToTime.lex" "..\Generated\StrToTimeScanner.cs" /v:2

"..\..\..\Tools\cslex" "json.lex" "..\Generated\jsonLexer.cs" /v:2
"..\..\..\Tools\gppg" /l /r "json.y" "..\Generated\jsonParser.cs" "..\Generated\json.log" 

pause