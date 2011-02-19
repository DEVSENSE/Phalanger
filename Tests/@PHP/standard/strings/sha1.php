[expect php]
[file]
<?
echo sha1("abc")."\n";
echo sha1("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq")."\n";
echo sha1("a")."\n";
echo sha1("0123456701234567012345670123456701234567012345670123456701234567")."\n";
?>