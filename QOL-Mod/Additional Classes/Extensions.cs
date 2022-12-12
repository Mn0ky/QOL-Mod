namespace QOL;

public static class Extensions
{
    public static string ReplaceCharWithStr(this string origStr, char replaceableChar, string replacedWith) 
        => origStr.Replace(replaceableChar.ToString(), replacedWith);

    public static bool StartsWithChar(this string str, char charToFind)
        => str.StartsWith(charToFind.ToString());
}